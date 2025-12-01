using UnityEngine;

namespace CoinFlip.Handlers;

public static class CoinFlipMessenger
{
    public static void SendIncorrectLocationMessage()
    {
        SendMessageWithRedEyes("I can only gamble in the shop!", false);
    }

    public static void SendWonMessage(int amount)
    {
        var message = FormatMessage(amount, true);
        
        SendMessageWithGreenEyes(message, true);
    }

    public static void SendLostMessage(int amount)
    {
        var message = FormatMessage(amount, false);
        
        SendMessageWithRedEyes(message, true);
    }

    private static string FormatMessage(int amount, bool won)
    {
        string message;
        var wonStringThing = won ? "won" : "lost";
        

        if (amount >= 1_000)
        {
            message = $"I {wonStringThing} {amount} million from coin flipping";
        }
        else
        {
            message = $"I {wonStringThing} {amount} thousand from coin flipping";
        }

        return message;
    }
    
    public static void SendMessageWithGreenEyes(string message, bool sendInTaxManChat)
    {
        SendChatMessage(message, ChatManager.PossessChatID.SelfDestructCancel, sendInTaxManChat, Color.green);
    }

    public static void SendMessageWithRedEyes(string message, bool sendInTaxManChat)
    {
        SendChatMessage(message, ChatManager.PossessChatID.SelfDestruct, sendInTaxManChat, Color.red);
    }

    /// <summary>
    /// Sends a public chat message in the game, visible and audible to all players near the player
    /// </summary>
    /// <param name="message">The content of the message to be sent. Defaults to "I forgot what I was going to say".</param>
    /// <param name="possessChat">The color the semi-bots eyes should glow.</param>
    /// <param name="sendInTaxmanChat">Specifies whether the message should be sent in the taxman chat.</param>
    /// <param name="color">The color of the message text. Defaults to the Unity default color.</param>
    public static void SendChatMessage(string message = "I forgot what I was going to say", ChatManager.PossessChatID possessChat = ChatManager.PossessChatID.SelfDestruct,
        bool sendInTaxmanChat = false, Color color = default)
    {
        Coinflip.Logger.LogDebug("[CoinFlip] Sending public chat message");

        
        ChatManager.instance.PossessChatScheduleStart(-1);
        ChatManager.instance.PossessChat(possessChat, message, 4f, color, 0f, sendInTaxmanChat);
        ChatManager.instance.PossessChatScheduleEnd();
    }
}