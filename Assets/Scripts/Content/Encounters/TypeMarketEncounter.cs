namespace Tossup.Encounters
{
    public sealed class TypeMarketEncounter : RunEncounterDef
    {
        public TypeMarketEncounter()
        {
            Id = "type_market";
            Name = "The Type Market";
            Description = "Coins sharing a type with your deck cost 3 less gold in shops.";
            CoinDiscount = (game, offer) =>
            {
                foreach (var owned in game.Coins)
                    foreach (var type in offer.Types)
                        if (Game.HasType(owned, type)) return 3;
                return 0;
            };
        }
    }
}
