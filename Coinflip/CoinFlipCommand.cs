using System.Security.Cryptography;
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
    [CommandInitializer]
    public static void Initialize()
    {
       
    }

    [CommandExecution("coinflip", "Gamble your money by flipping a coin. Try /cf 1k tails", true, false)]
    [CommandAlias("cf")]
    [CommandAlias("coinflip")]
    public static async Task Execute(string args)
    {
        var timeToDelay = 3_200;
        var avatar = PlayerAvatar.instance;
        var isMasterClient = PhotonNetwork.IsMasterClient;
        var isMultiplayer = SemiFunc.IsMultiplayer();
        Coinflip.Logger.LogDebug($"[CoinFlip] IsMasterClient: {isMasterClient} | IsMultiplayer: {isMultiplayer} | PlayerName: {avatar.playerName}");

        // Checking to see if the user is in the shop or not
        if (!SemiFunc.RunIsShop())
        {
            Coinflip.Logger.LogInfo("[CoinFlip] Player is not in the shop.");
            SendPrivateChatMessage(message: "I can only coin flip in the shop");
            return;
        }

        // Parse the arguments
        var amountAsText = args.Split(' ')[0];
        var selection = args.Split(' ')[1];


        var totalCurrency = SemiFunc.StatGetRunCurrency();
        var amount = ConvertBetAmountToInteger(amountAsText);
        Coinflip.Logger.LogDebug($"[CoinFlip] TotalCurrency: {totalCurrency} | Amount: {amount} | Selection: {selection}");

        // Check if the amount is valid
        if (amount < 1)
        {
            Coinflip.Logger.LogInfo("[CoinFlip] Invalid amount");
            SendPrivateChatMessage(message: "I can only flip coins with 1k or more");
            return;
        }

        if (amount > totalCurrency)
        {
            Coinflip.Logger.LogInfo("[CoinFlip] Not enough money");
            SendPrivateChatMessage(message: "I don't have enough money");
            return;
        }

        // Generate a secure random number for the coin flip
        var isHeads = GenerateRandomBoolean();

        // Check if user won
        var userWon = (isHeads && selection.ToLower() == "heads") || (isHeads && selection.ToLower() == "h") ||
                      (!isHeads && selection.ToLower() == "tails") || (!isHeads && selection.ToLower() == "t");

        
        var messageToSend = userWon ? $"I won {amount}k from coin flipping" : $"I lost {amount}k from coin flipping";


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
    /// Makes the player speak to everyone(Others can hear what they say)
    /// </summary>
    /// <param name="message"></param>
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