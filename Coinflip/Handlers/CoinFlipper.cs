using System;
using System.Security.Cryptography;

namespace CoinFlip.Handlers;

public static class CoinFlipper
{
    public static bool FlipCoin()
        => GenerateRandomBoolean();

    public static (bool success, string message) SuperFlip(string totalSuperFlipsString, double initialMultiplier, out bool won, out double finalMultiplier)
    {
        if (!Coinflip.SuperFlipEnabled)
        {
            Coinflip.Logger.LogDebug("[CoinFlip] Super Flips are not enabled");
            won = false;
            finalMultiplier = 0;
            return (false, "Super Flips are not enabled");
        }

        var superFlipsResult = BetValidator.TryParseSuperFlipAndValidateAmount(totalSuperFlipsString, out var superFlipsAmount);

        if (!superFlipsResult.success)
        {
            Coinflip.Logger.LogDebug($"[CoinFlip] Super Flips amount was not valid. {totalSuperFlipsString}");
            won = false;
            finalMultiplier = 0;
            return superFlipsResult;
        }
        
        var multiplier = initialMultiplier;
        var winRate = 1.0;
        // NOTE: This is 2 for multiple reasons. One is so the win rate is calculated correctly. The other is so we can skip the first flip.
        for (var i = 2; i < superFlipsAmount; i++)
        {
            var result = FlipCoin();

            if (!result)
            {
                Coinflip.Logger.LogDebug("[CoinFlip] User lost the CF.");
                won = false;
                finalMultiplier = 1.0;

                return (true, string.Empty);
            }

            winRate /= 2;
            multiplier += initialMultiplier - (initialMultiplier * (1.0 - winRate));
        }

        Coinflip.Logger.LogDebug("[CoinFlip] Coin was heads but player chose to flip tails.");
        won = true;
        finalMultiplier = multiplier;
        return (true, string.Empty);
    }

    private static bool GenerateRandomBoolean()
        => (int)(GenerateRandomPercentage(2)) == 0;

    private static float GenerateRandomPercentage(int maxNumber = 100)
    {
        using var rng = RandomNumberGenerator.Create();
        var randomByte = new byte[1];
        rng.GetBytes(randomByte);
        return randomByte[0] % maxNumber;
    }
}