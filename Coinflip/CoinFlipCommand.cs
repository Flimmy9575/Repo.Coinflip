﻿using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using REPOLib.Commands;
using UnityEngine;

namespace Coinflip;

public class CoinFlipCommand
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
        Coinflip.Logger.LogDebug($"[CoinFlip] IsMasterClient: {isMasterClient} | IsMultiplayer: {isMultiplayer} | PlayerName: {avatar.playerName}");

        // Checking to see if the user is in the shop or not
        var shopOnlyGambling = Coinflip.Instance.ShopOnly.Value; 
        
        if (shopOnlyGambling && !SemiFunc.RunIsShop())
        {
            Coinflip.Logger.LogInfo("[CoinFlip] Player is not in the shop.");

            const string coinFlipShopMessage = "I can only coin flip in the shop";
            if (isMultiplayer)
            {
                SendPublicChatMessage(coinFlipShopMessage, ChatManager.PossessChatID.SelfDestruct, false, Color.yellow);
                return;
            }
            
            SendPrivateChatMessage(coinFlipShopMessage);
            return;
        }


        // Parse the arguments
        var amountAsText = args.Split(' ')[0];
        var selection = args.Split(' ')[1];
        
        
        var totalCurrency = SemiFunc.StatGetRunCurrency();
        var amount = ConvertBetAmountToInteger(amountAsText);
        var minBetAmount = Coinflip.Instance.MinBetAmount.Value;
        var maxBetAmount = Coinflip.Instance.MaxBetAmount.Value;
        Coinflip.Logger.LogDebug($"[CoinFlip] TotalCurrency: {totalCurrency} | Amount: {amount} | Selection: {selection}");
        Coinflip.Logger.LogDebug($"[CoinFlip] MinBetAmount: {minBetAmount} | MaxBetAmount: {maxBetAmount}");
        
        
        if (amount < minBetAmount || amount > maxBetAmount)
        {
            if (isMultiplayer)
            {
                SendPublicChatMessage($"The minimum I can bet is {minBetAmount}", ChatManager.PossessChatID.SelfDestruct, false, Color.red);;
                return;
            }
            
            SendPrivateChatMessage($"The minimum I can bet is {minBetAmount}");
            
            return;
        }
        if (amount > maxBetAmount)
        {
            if (isMultiplayer)
            {
                SendPublicChatMessage($"The maximum I can bet is {maxBetAmount}", ChatManager.PossessChatID.SelfDestruct, false, Color.red);;
                return;
            }
            
            SendPrivateChatMessage($"The maximum I can bet is {maxBetAmount}");
            return;
        }
        
        // Check if the amount is valid
        if (amount < 1)
        {
            Coinflip.Logger.LogInfo("[CoinFlip] Invalid amount");

            if (isMultiplayer)
            {
                SendPublicChatMessage($"I can only flip {minBetAmount}k or more", ChatManager.PossessChatID.SelfDestruct, false, Color.red);
                return;
            }
            
            SendPrivateChatMessage(message: $"I can only flip {minBetAmount}k or more");
            return;
        }

        if (amount > totalCurrency)
        {
            Coinflip.Logger.LogInfo("[CoinFlip] Not enough money");

            if (isMultiplayer)
            {
                SendPublicChatMessage("We don't have enough money", ChatManager.PossessChatID.SelfDestruct, false, Color.red);
                return;
            }
            
            SendPrivateChatMessage(message: "I don't have enough money");
            return;
        }

        // Generate a secure random number for the coin flip
        var isHeads = GenerateRandomBoolean();

        // Check if user won
        var userWon = (isHeads && selection.ToLower() == "heads") || (isHeads && selection.ToLower() == "h") ||
                      (!isHeads && selection.ToLower() == "tails") || (!isHeads && selection.ToLower() == "t");

        
        
        var messageToSend = userWon ? $"I won {amount}k from coin flipping" : $"I lost {amount}k from coin flipping";
        
        
        if (Coinflip.Instance.TaxEnabled.Value && userWon)
        {
            var taxPercent = Coinflip.Instance.TaxAmount.Value;
            Coinflip.Logger.LogDebug("[CoinFlip] Tax enabled with a tax percent of ${taxPercent}");
            var taxAmount = (int)(amount * taxPercent);

            amount -= taxAmount;
            Coinflip.Logger.LogDebug($"[CoinFlip] Total Tax: {taxAmount} | new amount: {amount}");
        }
        

        if (SemiFunc.IsMultiplayer())
        {
            Coinflip.Logger.LogDebug("[CoinFlip] Multiplayer detected.");
            
            
            var actionToPerform = userWon ? "won" : "lost";
            var eventData = $"{amount} {actionToPerform}";

            // Handling if multiplayer and player is host
            if (PhotonNetwork.IsMasterClient)
            {
                Coinflip.Logger.LogDebug("[CoinFlip] User is host. modifying directly");
                SemiFunc.StatSetRunCurrency(totalCurrency + (userWon ? amount : -amount));
                
                await Task.Delay(timeToDelay);
                SendPublicChatMessage(messageToSend, userWon ? ChatManager.PossessChatID.SelfDestructCancel : ChatManager.PossessChatID.SelfDestruct, true, Color.gray);
                
                return;
            }
            // Handling if multiplayer and not host

            Coinflip.Logger.LogInfo("[CoinFlip] Sending event to master client");
            Coinflip.AddOrRemoveMoneyEvent.RaiseEvent(eventData, REPOLib.Modules.NetworkingEvents.RaiseMasterClient, SendOptions.SendReliable);
            
            await Task.Delay(timeToDelay);
            SendPublicChatMessage(messageToSend, userWon ? ChatManager.PossessChatID.SelfDestructCancel : ChatManager.PossessChatID.SelfDestruct, true, Color.gray);
            
            return;
        }

        // Handling if single player
        Coinflip.Logger.LogInfo("[CoinFlip] Single player detected. Updating currency directly.");
        SemiFunc.StatSetRunCurrency(totalCurrency + (userWon ? amount : -amount));
        await Task.Delay(timeToDelay);
        SendPrivateChatMessage(messageToSend);
    }

    /// <summary>
    /// Makes the player speak to themselves(Others can't hear what they say)
    /// </summary>
    /// <param name="message"></param>
    private static void SendPrivateChatMessage(string message = "I forgot what I was going to say")
    {
        Coinflip.Logger.LogDebug("[CoinFlip] Sending private chat message");
        
        var isCrouching = PlayerAvatar.instance.isCrouching;
        PlayerAvatar.instance.ChatMessageSpeak(message, isCrouching);
    }


    /// <summary>
    /// Sends a public chat message in the game, visible and audible to all players near the player
    /// </summary>
    /// <param name="message">The content of the message to be sent. Defaults to "I forgot what I was going to say".</param>
    /// <param name="possessChat">The color the semi-bots eyes should glow.</param>
    /// <param name="sendInTaxmanChat">Specifies whether the message should be sent in the taxman chat.</param>
    /// <param name="color">The color of the message text. Defaults to the Unity default color.</param>
    private static void SendPublicChatMessage(string message = "I forgot what I was going to say", ChatManager.PossessChatID possessChat = ChatManager.PossessChatID.SelfDestruct,
        bool sendInTaxmanChat = false, Color color = default)
    {
        Coinflip.Logger.LogDebug("[CoinFlip] Sending public chat message");
        
        ChatManager.instance.PossessChatScheduleStart(-1);
        ChatManager.instance.PossessChat(possessChat, message, 5f, color, 0f, sendInTaxmanChat, 1);
        ChatManager.instance.PossessChatScheduleEnd();
    }

    private static int ConvertBetAmountToInteger(string betAmount)
    {
        var amount = int.Parse(betAmount[..^1]);
        if (betAmount.EndsWith('k'))
        {
            return amount;
        }

        if (betAmount.EndsWith('m'))
        {
            return amount * 1_000;
        }

        // Returns -1 if the amount is invalid or not specified
        var parseResult = int.TryParse(betAmount, out var parsedAmount);

        return parseResult ? parsedAmount : -1;
    }

// Generate a cryptographically secure random boolean
    private static bool GenerateRandomBoolean()
    {
        using var rng = RandomNumberGenerator.Create();
        var randomByte = new byte[1];
        rng.GetBytes(randomByte);
        return (randomByte[0] % 2) == 0;
    }
}