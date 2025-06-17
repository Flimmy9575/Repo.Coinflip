using System.Threading.Tasks;
using CoinFlip.Handlers;
using CoinFlip.Validators;
using Photon.Pun;
using REPOLib.Commands;

namespace CoinFlip.Commands;

public static class CoinFlipCommand
{
    [CommandExecution("coinflip", "Gamble your money by flipping a coin. Try /cf 1k h", true, false)]
    [CommandAlias("cf")]
    [CommandAlias("coinflip")]
    public static async Task Execute(string args)
    {
        const int timeToDelay = 3_200;
        var avatar = PlayerAvatar.instance;
        var isMasterClient = PhotonNetwork.IsMasterClient;
        var isMultiplayer = SemiFunc.IsMultiplayer();
        CoinFlip.Coinflip.Logger.LogDebug($"[CoinFlip] IsMasterClient: {isMasterClient} | IsMultiplayer: {isMultiplayer} | PlayerName: {avatar.playerName}");

        if (!LocationValidator.CanGambleInCurrentLocation())
        {
            CoinFlip.Coinflip.Logger.LogDebug("[CoinFlip] Player is not in the shop.");
            CoinFlipMessenger.SendIncorrectLocationMessage();
            return;
        }

        // NOTE: In the future we'll need to check to see if it's a upgrade flip or not.

        var coinFlipArgs = args.Split(' ');
        CoinFlip.Coinflip.Logger.LogDebug($"[CoinFlip] Args are {args} arg length {coinFlipArgs.Length}");
        var amountBetString = coinFlipArgs[0];
        var betValidatorResult = BetValidator.TryParseBetAmount(amountBetString, out var amount);


        if (!betValidatorResult.success)
        {
            CoinFlip.Coinflip.Logger.LogDebug($"[CoinFlip] Amount was not valid. {amountBetString}");
            await Task.Delay(timeToDelay);
            CoinFlipMessenger.SendMessageWithRedEyes(betValidatorResult.message, false);
            return;
        }

        var betChoiceString = coinFlipArgs[1];
        var coinSideResult = BetValidator.TryParseCoinSide(betChoiceString, out var isHeads);

        if (!coinSideResult.success)
        {
            CoinFlip.Coinflip.Logger.LogDebug($"[CoinFlip] Coin side was not valid. {betChoiceString}");
            await Task.Delay(timeToDelay);
            CoinFlipMessenger.SendMessageWithRedEyes(coinSideResult.message, false);
            return;
        }


        int amountWon;
        int amountLost;

        if (coinFlipArgs.Length == 2)
        {
            var headsWon = CoinFlipper.FlipCoin();

            if (!headsWon && isHeads)
            {
                CoinFlip.Coinflip.Logger.LogDebug("[CoinFlip] Coin was heads but player chose to flip tails.");
                amountLost = CurrencyManager.ProcessGambleResult(false, amount);

                await Task.Delay(timeToDelay);
                CoinFlipMessenger.SendLostMessage(amountLost);

                return;
            }

            CoinFlip.Coinflip.Logger.LogDebug("[CoinFlip] Coin was tails but player chose to flip heads.");

            amountWon = CurrencyManager.ProcessGambleResult(true, amount);
            await Task.Delay(timeToDelay);
            CoinFlipMessenger.SendWonMessage(amountWon);
            return;
        }


        var totalSuperFlipsString = coinFlipArgs[2];
        var superFlipResult = CoinFlipper.SuperFlip(totalSuperFlipsString, CoinFlip.Coinflip.SuperFlipMultiplier, out var won, out var multiplier);

        if (!superFlipResult.success)
        {
            CoinFlip.Coinflip.Logger.LogDebug($"[CoinFlip] Super flip was not valid. {totalSuperFlipsString}");
            CoinFlipMessenger.SendMessageWithRedEyes(superFlipResult.message, false);
            return;
        }

        if (!won)
        {
            CoinFlip.Coinflip.Logger.LogDebug("[CoinFlip] Super flip was not won.");

            amountLost = CurrencyManager.ProcessGambleResult(false, amount, multiplier);
            await Task.Delay(timeToDelay);
            CoinFlipMessenger.SendLostMessage(amountLost);
            return;
        }

        CoinFlip.Coinflip.Logger.LogDebug("[CoinFlip] Super flip was won.");

        amountWon = CurrencyManager.ProcessGambleResult(true, amount, multiplier);
        await Task.Delay(timeToDelay);
        CoinFlipMessenger.SendWonMessage(amountWon);
    }
}