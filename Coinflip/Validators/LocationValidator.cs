namespace CoinFlip.Validators;

public static class LocationValidator
{
    public static bool CanGambleInCurrentLocation()
    {
        var shopOnlyGambling = Coinflip.ShopOnly;
        
        if (shopOnlyGambling && !SemiFunc.RunIsShop())
        {
            Coinflip.Logger.LogInfo("[CoinFlip] Player is not in the shop and shop only gaming is enabled. Not continuing.");
            return false;
        }
        
        Coinflip.Logger.LogInfo("[CoinFlip] Player is in the shop. Continuing...");
        
        return true;
    }

}