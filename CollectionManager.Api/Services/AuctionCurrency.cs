namespace CollectionManager.Api.Services;

public static class AuctionCurrency
{
    // Existing auction records are stored in BGN. The public API and UI use EUR.
    public const decimal LevPerEuro = 1.95583m;
    public const decimal MinimumBidInLeva = 1m;

    public static decimal ToEuro(decimal leva) => Math.Round(leva / LevPerEuro, 2, MidpointRounding.AwayFromZero);

    public static decimal ToLeva(decimal euro) => Math.Round(euro * LevPerEuro, 2, MidpointRounding.AwayFromZero);

    public static decimal MinimumBidInLevaAfter(decimal? highestBidInLeva)
    {
        var next = highestBidInLeva is null
            ? MinimumBidInLeva
            : Math.Max(MinimumBidInLeva, Math.Ceiling(highestBidInLeva.Value * 1.05m * 100m) / 100m);
        return next;
    }

    public static decimal MinimumBidInEuroAfter(decimal? highestBidInLeva)
    {
        var minimumInLeva = MinimumBidInLevaAfter(highestBidInLeva);
        return Math.Ceiling(minimumInLeva / LevPerEuro * 100m) / 100m;
    }
}

