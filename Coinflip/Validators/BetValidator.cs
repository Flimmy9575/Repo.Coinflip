using System;
using System.Threading.Tasks;

namespace CoinFlip.Handlers;

public static class BetValidator
{
    /// <summary>
    /// Validates if the specified bet amount falls within the allowed range defined by the minimum and maximum bet limits and if users have enough money to make the bet.
    /// </summary>
    /// <param name="amount">The bet amount to validate.</param>
    /// <returns>True if the amount is within the valid range; otherwise, false.</returns>
    private static bool AmountIsValid(int amount)
        => amount <= Coinflip.MaxBetAmount && amount >= Coinflip.MinBetAmount;

    private static bool UsersHaveEnoughMoney(int required)
        => required <= SemiFunc.StatGetRunCurrency();


    /// <summary>
    /// Attempts to parse a bet amount from a string representation, supporting "k" (thousands)
    /// and "m" (millions) suffixes, and validates the parsed value against the allowable bet range.
    /// </summary>
    /// <param name="betAmount">The string representation of the bet amount to parse. It may include
    /// suffixes such as "k" for thousands or "m" for millions, or raw numeric values.</param>
    /// <param name="amount">The parsed and validated bet amount as an integer, returned via out parameter.</param>
    /// <returns>A tuple containing a success flag and an associated message. The `success` flag is
    /// true if the parsing and validation were successful; otherwise, false. The `message` provides
    /// details that can be played in-game.</returns>
    public static (bool success, string message) TryParseBetAmount(string betAmount, out int amount)
    {
        bool result;
        amount = 0;
        if (betAmount.EndsWith('k'))
        {
            var amountString = betAmount[..^1];
            result = int.TryParse(amountString, out var thousandParsedAmount);
            if (result)
            {
                amount = thousandParsedAmount;
            }
        }

        else if (betAmount.EndsWith('m'))
        {
            var amountString = betAmount[..^1];
            result = int.TryParse(amountString, out var millionParsedAmount);
            if (result)
            {
                amount = millionParsedAmount * 1_000;
            }
        }
        else if (betAmount.Length >= 4) // Raw Numbers
        {
            result = int.TryParse(betAmount, out var parsedAmount);
            if (result)
            {
                amount = parsedAmount / 1_000;
            }
        }
        else
        {
            return (false, "I have to specify a valid amount. Like 1k or 1000");
        }

        if (!UsersHaveEnoughMoney(amount))
        {
            Coinflip.Logger.LogDebug($"[CoinFlip] Amount: {amount} ActualAmount: {SemiFunc.StatGetRunCurrency()} | UsersHaveEnoughMoney: {UsersHaveEnoughMoney(amount)}");
            return (false, "We don't have enough money to make that bet.");
        }


        if (!AmountIsValid(amount))
        {
            return (false, $"My bet must be between {Coinflip.MinBetAmount} and {Coinflip.MaxBetAmount}");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Attempts to parse a coin side from a string representation and determines if it is "heads" or "tails".
    /// </summary>
    /// <param name="side">The string representation of the coin side to parse. Valid values are "heads", "h", "tails", or "t" (case-insensitive).</param>
    /// <param name="isHeads">The parsed result indicating whether the coin side is heads (true) or tails (false), returned via out parameter.</param>
    /// <returns>A tuple containing a success flag and an associated message. The `success` flag is true if the parsing was successful; otherwise, false. The `message` provides feedback about the result.</returns>
    public static (bool success, string message) TryParseCoinSide(string side, out bool isHeads)
    {
        if (side.ToLower() is "heads" or "h")
        {
            isHeads = true;
            return (true, string.Empty);
        }

        if (side.ToLower() is "tails" or "t")
        {
            isHeads = false;
            return (true, string.Empty);
        }

        isHeads = false;
        return (false, "Invalid coin side. Must be heads or tails");
    }

    /// <summary>
    /// Attempts to parse the provided string as a valid Super Flip amount, applying relevant restrictions and settings.
    /// </summary>
    /// <param name="superFlipAmountString">The input string representing the Super Flip amount to parse.</param>
    /// <param name="superFlipAmount">The parsed Super Flip amount as an integer, or null if parsing fails.</param>
    /// <returns>A tuple containing a boolean indicating success or failure, and a message providing the outcome details.</returns>
    public static (bool success, string message) TryParseSuperFlipAndValidateAmount(string superFlipAmountString, out int? superFlipAmount)
    {
        var superFlipEnabled = Coinflip.SuperFlipEnabled;
        var superFlipMax = Coinflip.SuperFlipMax;
        Coinflip.Logger.LogDebug($"[CoinFlip] SuperFlipEnabled: {superFlipEnabled} | SuperFlipMax: {superFlipMax}");

        if (!superFlipEnabled)
        {
            Coinflip.Logger.LogDebug("[CoinFlip] Super Flips are not enabled. Stopping execution");

            superFlipAmount = null;
            return (false, "Super Flips are not enabled");
        }

        var result = int.TryParse(superFlipAmountString, out var parsedAmount);

        if (!result)
        {
            superFlipAmount = null;
            return (false, "Super Flip Amount must be a number");
        }

        if (parsedAmount < 2)
        {
            Coinflip.Logger.LogDebug("[CoinFlip] Super Flips amount was less than 2. This is not allowed.");

            superFlipAmount = null;
            return (false, "I need to super flip at least 2 times");
        }

        if (parsedAmount > superFlipMax)
        {
            Coinflip.Logger.LogDebug($"[CoinFlip] Super Flip Amount: {parsedAmount} | SuperFlipMax: {superFlipMax}");

            superFlipAmount = null;
            return (false, $"My super flip can't be more than {superFlipMax}");
        }

        Coinflip.Logger.LogDebug("[CoinFlip] Super Flip validation passed");


        superFlipAmount = parsedAmount;
        return (true, "Success");
    }

    public static bool TryParseUpgradeType(string amountString, string upgradeType, out int amount)
    {
        throw new NotImplementedException();
    }
}