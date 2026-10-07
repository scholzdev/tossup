namespace Tossup.Encounters
{
    public sealed class ThinMarketEncounter : RunEncounterDef
    {
        public ThinMarketEncounter()
        {
            Id = "thin_market";
            Name = "Thin Market";
            Description = "Shops show one fewer coin, but every coin costs 2 fewer gold.";
            RunStart = game =>
            {
                game.Shop.CoinOfferCount = 3;
                game.Shop.CoinPriceDiscount = 2;
            };
        }
    }
}
