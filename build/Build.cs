using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Octokit;
using Octokit.Internal;
using Serilog;

[GitHubActions("continuous", GitHubActionsImage.UbuntuLatest, On = [GitHubActionsTrigger.Push, GitHubActionsTrigger.PullRequest], InvokedTargets = ["Compile"],AutoGenerate = true)]
[GitHubActions("bumpVersion", GitHubActionsImage.UbuntuLatest, On = [GitHubActionsTrigger.PullRequest], EnableGitHubToken = true, InvokedTargets = ["BumpVersion"],AutoGenerate = true)]
[GitHubActions("publish", GitHubActionsImage.UbuntuLatest, On = [GitHubActionsTrigger.PullRequest], EnableGitHubToken = true, InvokedTargets = ["CreateRelease", "PublishToThunderStore"],AutoGenerate = true)]
class Build : NukeBuild
{
    [GitRepository] readonly GitRepository Repository;

    [Nuke.Common.Parameter("GitHub token for authentication")] string GitHubToken => GitHubActions.Instance?.Token;

    [Nuke.Common.Parameter("Committer name for version bump commits")] readonly string GitCommitterName = "NDJH GitHub Actions";

    [Nuke.Common.Parameter("Committer email for version bump commits")] readonly string GitCommitterEmail = "cicd@ndjh.dev";


    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    [Nuke.Common.Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;



    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetTasks.DotNetRestore();
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTasks.DotNetBuild(s =>
                s.SetConfiguration(Configuration)
                    .EnableNoRestore()
                    .EnableNoIncremental());
        });

    Target RunTests => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            // This will be setup later
            DotNetTasks.DotNetTest(t => t.EnableNoBuild().EnableNoRestore());
        });

    Target BumpVersion => _ => _
        .Requires(() => IsServerBuild)
        .Requires(() => GitHubToken != null)
        .Executes(() =>
        {
            // Get current branch name
            var branchName = Repository.Branch;
            Log.Information("Current branch name: {BranchName}", branchName);

            // Determine version increment type based on branch name
            string incrementType;

            if (Regex.IsMatch(branchName, @"(fix(me)?|bugfix|patch|hotfix)\/", RegexOptions.IgnoreCase))
            {
                incrementType = "patch";
                Log.Information("Branch indicates a bugfix/patch - incrementing PATCH version");
            }
            else if (Regex.IsMatch(branchName, @"(feat(ure)?|minor)\/", RegexOptions.IgnoreCase))
            {
                incrementType = "minor";
                Log.Information("Branch indicates a feature - incrementing MINOR version");
            }
            else
            {
                // Default to minor for all other branches
                incrementType = "minor";
                Log.Information("Branch type not recognized - defaulting to MINOR version increment");
            }

            // Configure Git for committing
            GitTasks.Git($"config user.name \"{GitCommitterName}\"");
            GitTasks.Git($"config user.email \"{GitCommitterEmail}\"");

            // Read the current GitVersion.yml file
            var gitVersionYmlPath = RootDirectory / "GitVersion.yml";
            var gitVersionContent = File.ReadAllText(gitVersionYmlPath);

            // Get the current version before change
            var currentVersion = GitVersionTasks.GitVersion().Result;
            Log.Information("Current version: {Version}", currentVersion.FullSemVer);

            // Update GitVersion.yml to increment the appropriate component
            // Note: This is a simple example that assumes GitVersion.yml has a next-version field
            // For more complex setups, you might need to adjust this part
            var updatedContent = gitVersionContent;

            if (incrementType == "patch")
            {
                // Update next-version with patch increment
                var newVersion = $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Patch + 1}";
                updatedContent = Regex.Replace(gitVersionContent,
                    @"next-version:\s*\d+\.\d+\.\d+",
                    $"next-version: {newVersion}");
            }
            else if (incrementType == "minor")
            {
                // Update next-version with minor increment
                var newVersion = $"{currentVersion.Major}.{currentVersion.Minor + 1}.0";
                updatedContent = Regex.Replace(gitVersionContent,
                    @"next-version:\s*\d+\.\d+\.\d+",
                    $"next-version: {newVersion}");
            }

            // Write the updated content back to GitVersion.yml
            File.WriteAllText(gitVersionYmlPath, updatedContent);

            // Commit the change
            GitTasks.Git($"add {gitVersionYmlPath}");
            GitTasks.Git($"commit -m \"Bump version [{incrementType}]\"");

            // Push the change
            GitTasks.Git("push");

            // Get the new version after the change
            var newVersionInfo = GitVersionTasks.GitVersion().Result;
            Log.Information("Version bumped to: {Version}", newVersionInfo.FullSemVer);

            // Create a comment on the PR using the GitHub API

            var prNumber = int.Parse(Repository.Branch!.Split('/').Last());
            var repoOwner = Repository.Identifier.Split('/')[0];
            var repoName = Repository.Identifier.Split('/')[1];

            var credentials = new Credentials(GitHubToken);
            var github = new GitHubClient(new ProductHeaderValue(nameof(NukeBuild)),
                new InMemoryCredentialStore(credentials));

            var comment = $"Version bumped from `{currentVersion.FullSemVer}` to `{newVersionInfo.FullSemVer}` based on branch type (`{incrementType}`)";

            github.Issue.Comment.Create(repoOwner, repoName, prNumber, comment).Wait();

            Log.Information("Added version bump comment to PR #{PRNumber}", prNumber);
        });


    Target CreateRelease => _ => _
        .Requires(() => IsServerBuild)
        .DependsOn(Compile)
        .Executes(() =>
        {
            Log.Information("Creating release from branch: {Branch} on commit: {Commit} with the following tags: {Tags}", Repository.Branch, Repository.Commit, Repository.Tags);

            // Create output directory if it doesn't exist
            var outputPath = RootDirectory / "output";
            Directory.CreateDirectory(outputPath);

            // Create the release zip file
            var zipFilePath = outputPath / "Coinflip.zip";
            if (File.Exists(zipFilePath))
                File.Delete(zipFilePath);

            // Create a zip file containing required files
            using (var zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                // Add required files to the zip
                zipArchive.CreateEntryFromFile(RootDirectory / "README.md", "README.md");
                zipArchive.CreateEntryFromFile(RootDirectory / "CHANGELOG.md", "CHANGELOG.md");
                zipArchive.CreateEntryFromFile(RootDirectory / "manifest.json", "manifest.json");

                // Add the compiled DLL
                var dllPath = RootDirectory / "Coinflip" / "bin" / Configuration / "Coinflip.dll";
                zipArchive.CreateEntryFromFile(dllPath, "Coinflip.dll");
            }

            var credentials = new Credentials(GitHubActions.Instance.Token);
            GitHubTasks.GitHubClient = new GitHubClient(new ProductHeaderValue(nameof(NukeBuild)), new InMemoryCredentialStore(credentials));

            // Creating tag
            var releaseVersion = GitVersionTasks.GitVersion();
            var semVersion = releaseVersion.Result.FullSemVer;

            Log.Information("Creating release for version: {SemVersion}", semVersion);
            var release = new NewRelease($"v{semVersion}");

            // Set release properties
            release.Name = $"Version v{semVersion}";
            release.Body = GenerateChangelogForRelease();
            release.GenerateReleaseNotes = true;
            release.Prerelease = !string.IsNullOrEmpty(releaseVersion.Result.PreReleaseTag);
            release.Draft = false;

            // Create the release
            var repoOwner = Repository.Identifier.Split('/')[0];
            var repoName = Repository.Identifier.Split('/')[1];

            Log.Information("Creating release in repository {Owner}/{Name}", repoOwner, repoName);

            var createdRelease = GitHubTasks.GitHubClient.Repository.Release.Create(
                repoOwner,
                repoName,
                release).Result;

            Log.Information("Release created successfully: {ReleaseUrl}", createdRelease.HtmlUrl);

            // Upload the zip file as a release asset
            Log.Information("Uploading release asset: {ZipFile}", zipFilePath);

            using (var zipStream = File.OpenRead(zipFilePath))
            {
                var assetUpload = new ReleaseAssetUpload
                {
                    FileName = Path.GetFileName(zipFilePath),
                    ContentType = "application/zip",
                    RawData = zipStream
                };

                var uploadedAsset = GitHubTasks.GitHubClient.Repository.Release.UploadAsset(
                    createdRelease,
                    assetUpload).Result;

                Log.Information("Release asset uploaded successfully: {AssetUrl}", uploadedAsset.BrowserDownloadUrl);
            }
        })
        .ProceedAfterFailure();
    
    private string GenerateChangelogForRelease()
    {
        var changelogPath = RootDirectory / "CHANGELOG.md";
    
        if (!File.Exists(changelogPath))
        {
            Log.Warning("CHANGELOG.md not found at {Path}", changelogPath);
            return "No changelog provided.";
        }
    
        try
        {
            // Read the full changelog file
            var fullChangelog = File.ReadAllText(changelogPath);
        
            // Get current version
            var currentVersion = GitVersionTasks.GitVersion().Result;
            var versionHeader = $"## [{currentVersion.MajorMinorPatch}]";
        
            // Find the section for the current version
            var lines = fullChangelog.Split('\n');
            var currentVersionIndex = -1;
            var nextVersionIndex = -1;
        
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(versionHeader))
                {
                    currentVersionIndex = i;
                }
                else if (currentVersionIndex >= 0 && lines[i].StartsWith("## ["))
                {
                    nextVersionIndex = i;
                    break;
                }
            }
        
            // If we found the current version section
            if (currentVersionIndex >= 0)
            {
                var endIndex = nextVersionIndex > 0 ? nextVersionIndex : lines.Length;
                var sectionLines = lines[currentVersionIndex..endIndex];
            
                // Return the relevant changelog section
                return string.Join('\n', sectionLines);
            }
        
            Log.Warning("Could not find current version {Version} in changelog", currentVersion.MajorMinorPatch);
            return "See CHANGELOG.md for version details.";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error generating changelog for release");
            return "Error generating changelog. Please refer to CHANGELOG.md.";
        }
    }


    Target PublishToThunderStore => _ => _
        .Requires(() => IsServerBuild)
        .DependsOn(CreateRelease)
        .Executes(() =>
        {
            // Get version information
            var releaseVersion = GitVersionTasks.GitVersion();
            var semVersion = releaseVersion.Result.FullSemVer;

            Log.Information("Publishing version {Version} to ThunderStore", semVersion);

            // Create package for ThunderStore
            var outputPath = RootDirectory / "output";
            var thunderstorePackagePath = outputPath / $"Coinflip-{semVersion}.zip";

            // Ensure output directory exists
            Directory.CreateDirectory(outputPath);

            // If package already exists, delete it
            if (File.Exists(thunderstorePackagePath))
                File.Delete(thunderstorePackagePath);

            // Create the package zip file
            using (var zipArchive = ZipFile.Open(thunderstorePackagePath, ZipArchiveMode.Create))
            {
                // Add required files to the package
                zipArchive.CreateEntryFromFile(RootDirectory / "README.md", "README.md");
                zipArchive.CreateEntryFromFile(RootDirectory / "manifest.json", "manifest.json");
                zipArchive.CreateEntryFromFile(RootDirectory / "CHANGELOG.md", "CHANGELOG.md");

                // Add the compiled DLL
                var dllPath = RootDirectory / "Coinflip" / "bin" / Configuration / "Coinflip.dll";
                zipArchive.CreateEntryFromFile(dllPath, "Coinflip.dll");
            }

            // To publish we'll need to review this: https://thunderstore.io/api/docs/
            // We also need an APIKey/something similar 

            Log.Information("Package prepared for ThunderStore at: {PackagePath}", thunderstorePackagePath);
            Log.Information("Manual upload to ThunderStore is required since direct API publishing is not implemented");

            // End product will look something to this:
            /*
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {thunderstoreApiKey}");

            using var formData = new MultipartFormDataContent();
            using var fileContent = new StreamContent(File.OpenRead(thunderstorePackagePath));
            formData.Add(fileContent, "file", Path.GetFileName(thunderstorePackagePath));

            var response = httpClient.PostAsync("https://thunderstore.io/api/v1/package/", formData).Result;
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Successfully published to ThunderStore");
            }
            else
            {
                Log.Error("Failed to publish to ThunderStore: {ErrorMessage}",
                          response.Content.ReadAsStringAsync().Result);
            }
            */
        });
}