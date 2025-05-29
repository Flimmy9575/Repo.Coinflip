using BepInEx;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using REPOLib.Modules;
using UnityEngine;

namespace Coinflip;

[BepInPlugin("NotDrunkJustHigh.Coinflip", "Coinflip", "1.0.0")]
public class Coinflip : BaseUnityPlugin
{
    internal static Coinflip Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }
    public static NetworkedEvent AddOrRemoveMoneyEvent;

    private void Awake()
    {
        Instance = this;

        // Prevent the plugin from being deleted
        gameObject.transform.parent = null;
        gameObject.hideFlags = HideFlags.HideAndDontSave;

        AddOrRemoveMoneyEvent = new NetworkedEvent("AddOrRemoveMoney", AddOrRemoveMoney);
        
        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }

    private void Update()
    {
        // Code that runs every frame goes here
    }
    
    // This cannot be static
    private void AddOrRemoveMoney(EventData eventData)
    {
        // Check if we are running this on the host
        var sender = eventData.Sender;
        
        if (!PhotonNetwork.IsMasterClient)
        {
            
            Logger.LogInfo("[CoinFlip] Received RPC but we are not the host. Ignoring.");
            return;
        }

        var totalCurrency = SemiFunc.StatGetRunCurrency();
        
        var data = ((string)eventData.CustomData).Split(' ');
        var amount = int.Parse(data[0]);
        var action = data[1];
        var endingAmount = totalCurrency + (action == "won" ? amount : -amount);

        Logger.LogInfo($"[CoinFlip] Received request to modify currency by {amount} from {sender} with action {action}.");
        
        SemiFunc.StatSetRunCurrency(endingAmount);
        var totalCurrencyAfter = SemiFunc.StatGetRunCurrency();

        if (endingAmount != totalCurrencyAfter)
        {
            Logger.LogError($"[CoinFlip] Currency was not updated correctly. Expected: {endingAmount} | Actual: {totalCurrencyAfter}");
        }
    }
}