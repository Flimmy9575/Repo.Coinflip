using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using REPOLib.Modules;
using UnityEngine;

namespace Coinflip;

[BepInPlugin("NotDrunkJustHigh.Coinflip", "Coinflip", "1.1.0")]
public class Coinflip : BaseUnityPlugin
{
    internal static Coinflip Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }
    public static NetworkedEvent AddOrRemoveMoneyEvent;
    
    // Config related
    public ConfigEntry<bool> ShopOnly;
    
    public ConfigEntry<int> MaxBetAmount;
    public ConfigEntry<int> MinBetAmount;
    
    public ConfigEntry<bool> TaxEnabled;
    public ConfigEntry<float> TaxAmount;

    private void Awake()
    {
        const string generalCategory = "General";
        const string betLimitingCategory = "Bet Limitations";
        const string taxCategory = "Taxes";

        ShopOnly = Config.Bind(generalCategory, "Shop Only", true, "Whether you can flip coins only within the shop");

        // These represent thousands
        const int defaultMinimumBet = 1; // 1,000
        const int defaultMaximumBet = 1_000; // 1,000,000
        const float defaultTaxPercentage = 0.1f; // 10%
        
        MaxBetAmount = Config.Bind(betLimitingCategory, "Max Bet Amount", defaultMaximumBet, "The maximum amount of money that can be bet. 1 is equal 1,000(100 would set a max of 100,000) ");
        MinBetAmount = Config.Bind(betLimitingCategory, "Min Bet Amount", defaultMinimumBet, "The minimum amount of money that can be bet. 1 is equal 1,000");
        
        TaxEnabled = Config.Bind(taxCategory, "Tax Enabled", false, "Whether or not tax is enabled");
        TaxAmount = Config.Bind(taxCategory, "Tax Amount", defaultTaxPercentage, "The amount of money that is taxed on every win");


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