using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using REPOLib.Modules;

namespace CoinFlip.Handlers;

public static class CurrencyManager
{
    public static int ProcessGambleResult(bool userWon, int amount, double superFlipMultiplier = 1.0)
    {
        var totalCurrency = SemiFunc.StatGetRunCurrency();
        amount = (int)(amount * superFlipMultiplier);

        // Apply tax if enabled and user won
        if (Coinflip.TaxEnabled && userWon)
        {
            var taxPercent = Coinflip.TaxAmount;
            Coinflip.Logger.LogDebug($"[CoinFlip] Tax enabled with a tax percent of {taxPercent}");
            var taxAmount = (int)(amount * taxPercent);

            amount -= taxAmount;
            Coinflip.Logger.LogDebug($"[CoinFlip] Total Tax: {taxAmount} | new amount: {amount}");
        }

        if (SemiFunc.IsMultiplayer())
        {
            // Handle multiplayer currency update
            if (PhotonNetwork.IsMasterClient)
            {
                Coinflip.Logger.LogDebug("[CoinFlip] User is host. modifying directly");
                SemiFunc.StatSetRunCurrency(totalCurrency + (userWon ? amount : -amount));

                return amount;
            }

            // Send event to the master client
            Coinflip.Logger.LogInfo("[CoinFlip] Sending handle CoinFlip result event to master client");
            var actionToPerform = userWon ? "won" : "lost";
            var eventData = $"{amount} {actionToPerform}";
            Coinflip.HandleCoinFlipResultEvent.RaiseEvent(eventData, NetworkingEvents.RaiseMasterClient, SendOptions.SendReliable);


            return amount;
        }

        // Handle single player currency update
        Coinflip.Logger.LogInfo("[CoinFlip] Single player detected. Updating currency directly.");
        SemiFunc.StatSetRunCurrency(totalCurrency + (userWon ? amount : -amount));


        return amount;
    }

    public static void ProcessUpgradeFlipResult(bool userWon, string type, int amount)
    {
        throw new NotImplementedException();
    }
}