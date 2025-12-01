using BepInEx;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using Newtonsoft.Json;
using Photon.Pun;
using REPOLib.Modules;
using UnityEngine;

namespace CoinFlip;

[BepInPlugin("NotDrunkJustHigh.Coinflip", "Coinflip", "2.0.0")]
public class Coinflip : BaseUnityPlugin
{
    internal static Coinflip Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }
    public static NetworkedEvent HandleCoinFlipResultEvent;

    /// <summary>
    /// Represents a networked event used to synchronize configuration data related to the Coinflip plugin.
    /// This event is utilized to ensure consistent configuration settings are shared across connected clients.
    /// </summary>
    private static  NetworkedEvent SyncConfigEvent;

    public static NetworkedEvent UpgradeFlipEvent;

    // Config related
    public static bool ShopOnly { get; set; }


    public static int MaxBetAmount { get; set; }
    public static int MinBetAmount { get; set; }

    public static bool TaxEnabled { get; set; }
    public static float TaxAmount { get; set; }

    // Super Flips
    public static bool SuperFlipEnabled { get; set; }
    public static int SuperFlipMax { get; set; }
    public static float SuperFlipMultiplier { get; set; }

    private float _configSyncTimer = 0f;
    private const float CONFIG_SYNC_INTERVAL = 30f; // 10 seconds


    private void Awake()
    {
        Instance = this;
        SetupConfig();

        // Prevent the plugin from being deleted
        gameObject.transform.parent = null;
        gameObject.hideFlags = HideFlags.HideAndDontSave;


        HandleCoinFlipResultEvent = new NetworkedEvent("Handle CoinFlip Result", RPC_AddOrRemoveMoney);
        SyncConfigEvent = new NetworkedEvent("SyncConfig", RPC_SyncConfig);

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    // ReSharper disable once UnusedMember.Global
    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }

    private void Update()
    {
        // Code that runs every frame goes here


        if (PhotonNetwork.IsMasterClient && SemiFunc.RunIsShop())
        {
            // Sending config sync every 10 seconds
            _configSyncTimer += Time.deltaTime;

            if (_configSyncTimer >= CONFIG_SYNC_INTERVAL)
            {
                _configSyncTimer = 0f; // Reset timer

                // Create config data object
                var configData = new RPC_ConfigSyncData
                {
                    ShopOnly = ShopOnly,
                    MaxBetAmount = MaxBetAmount,
                    MinBetAmount = MinBetAmount,
                    TaxEnabled = TaxEnabled,
                    TaxAmount = TaxAmount,
                    SuperFlipEnabled = SuperFlipEnabled,
                    SuperFlipMax = SuperFlipMax,
                    SuperFlipMultiplier = SuperFlipMultiplier
                };
                var jsonSerializedConfigData = JsonConvert.SerializeObject(configData);
                

                // Send config data to all clients
                SyncConfigEvent.RaiseEvent(jsonSerializedConfigData, NetworkingEvents.RaiseOthers, SendOptions.SendUnreliable);
                Logger.LogDebug("[CoinFlip] Sent config sync to all clients");
            }
        }
    }

    public void SetupConfig()
    {
        const string generalCategory = "General";
        const string betLimitingCategory = "Bet Limitations";
        const string taxCategory = "Taxes";

        var shopOnly = Config.Bind(generalCategory, "Shop Only", true, "Whether you can flip coins only within the shop");
        shopOnly.SettingChanged += (_, _) => ShopOnly = shopOnly.Value;

        ShopOnly = shopOnly.Value;

        // These represent thousands
        const int defaultMinimumBet = 1; // 1,000
        const int defaultMaximumBet = 1_000; // 1,000,000
        const float defaultTaxPercentage = 0.1f; // 10%

        var maxBetAmount = Config.Bind(betLimitingCategory, "Max Bet Amount", defaultMaximumBet, "The maximum amount of money that can be bet. 1 is equal 1,000(100 would set a max of 100,000) ");
        var minBetAmount = Config.Bind(betLimitingCategory, "Min Bet Amount", defaultMinimumBet, "The minimum amount of money that can be bet. 1 is equal 1,000");
        maxBetAmount.SettingChanged += (_, _) => MaxBetAmount = maxBetAmount.Value;
        minBetAmount.SettingChanged += (_, _) => MinBetAmount = minBetAmount.Value;

        MaxBetAmount = maxBetAmount.Value;
        MinBetAmount = minBetAmount.Value;


        var taxEnabled = Config.Bind(taxCategory, "Tax Enabled", false, "Whether or not tax is enabled");
        var taxAmount = Config.Bind(taxCategory, "Tax Amount", defaultTaxPercentage, "The amount of money that is taxed on every win");
        taxEnabled.SettingChanged += (_, _) => TaxEnabled = taxEnabled.Value;
        taxAmount.SettingChanged += (_, _) => TaxAmount = taxAmount.Value;

        TaxEnabled = taxEnabled.Value;
        TaxAmount = taxAmount.Value;

        // Super Flips
        const string superFlipCategory = "Super Flips";

        var superFlipEnabled = Config.Bind(superFlipCategory, "Super Flips Enabled", true, "Whether or not super flips are enabled");
        var superFlipMax = Config.Bind(superFlipCategory, "Super Flips Max", 5, "The maximum amount of super flips that can be played");
        var superFlipMultiplier = Config.Bind(superFlipCategory, "Super Flips Multiplier", 2.75f, "The multiplier per flip that is applied to the amount of money won on a super flip");
        superFlipEnabled.SettingChanged += (_, _) => SuperFlipEnabled = superFlipEnabled.Value;
        superFlipMax.SettingChanged += (_, _) => SuperFlipMax = superFlipMax.Value;
        superFlipMultiplier.SettingChanged += (_, _) => SuperFlipMultiplier = superFlipMultiplier.Value;

        SuperFlipEnabled = superFlipEnabled.Value;
        SuperFlipMax = superFlipMax.Value;
        SuperFlipMultiplier = superFlipMultiplier.Value;
    }

    #region RPC_Events

    // This cannot be static
    public void RPC_AddOrRemoveMoney(EventData eventData)
    {
        Logger.LogDebug("[CoinFlip] Received RPC to add or remove money.");
        
        var sender = eventData.Sender;

        if (!PhotonNetwork.IsMasterClient)
        {
            Logger.LogDebug("[CoinFlip] Received RPC but we are not the host. Ignoring.");
            return;
        }

        var totalCurrency = SemiFunc.StatGetRunCurrency();

        var data = ((string)eventData.CustomData).Split(' ');
        var amount = int.Parse(data[0]);
        var action = data[1];
        var endingAmount = totalCurrency + (action == "won" ? amount : -amount);

        Logger.LogDebug($"[CoinFlip] Received request to modify currency by {amount} from {sender} with action {action}.");

        SemiFunc.StatSetRunCurrency(endingAmount);
        var totalCurrencyAfter = SemiFunc.StatGetRunCurrency();

        if (endingAmount != totalCurrencyAfter)
        {
            Logger.LogError($"[CoinFlip] Currency was not updated correctly. Expected: {endingAmount} | Actual: {totalCurrencyAfter}");
        }
    }

    public void RPC_SyncConfig(EventData eventData)
    {
        Logger.LogDebug("[CoinFlip] Received RPC to sync config.");
        
        // Check if we are running this on the host
        if (PhotonNetwork.IsMasterClient)
        {
            Logger.LogDebug("[CoinFlip] Received RPC to sync config, but current client is host. Ignoring.");
            return;
        }

        Logger.LogDebug("[CoinFlip] Received RPC to sync config. Attempting to sync config...");
        var data = eventData.CustomData as string;

        if (data is null)
        {
            Logger.LogError("[CoinFlip] Received Config was null");
            return;
        }
        
        var configData = JsonConvert.DeserializeObject<RPC_ConfigSyncData>(data);
            
            
        if (configData is not null)
        {
            ShopOnly = configData.ShopOnly;
            MaxBetAmount = configData.MaxBetAmount;
            MinBetAmount = configData.MinBetAmount;
            TaxEnabled = configData.TaxEnabled;
            TaxAmount = configData.TaxAmount;
            SuperFlipEnabled = configData.SuperFlipEnabled;
            SuperFlipMax = configData.SuperFlipMax;
            SuperFlipMultiplier = configData.SuperFlipMultiplier;

            Logger.LogDebug("[CoinFlip] Received and set Config successfully");

            return;
        }


        Logger.LogError("[CoinFlip] Received Config could not be serialized");
    }
}

public class RPC_ConfigSyncData
{
    public bool ShopOnly { get; set; }
    public int MaxBetAmount { get; set; }
    public int MinBetAmount { get; set; }
    public bool TaxEnabled { get; set; }
    public float TaxAmount { get; set; }
    public bool SuperFlipEnabled { get; set; }
    public int SuperFlipMax { get; set; }
    public float SuperFlipMultiplier { get; set; }
}

#endregion