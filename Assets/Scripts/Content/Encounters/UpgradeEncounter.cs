using System.Collections.Generic;

namespace Tossup.Encounters
{
    public sealed class UpgradeEncounter : RunEncounterDef
    {
        public UpgradeEncounter()
        {
            Id = "upgrade";
            Name = "Upgrade";
            Description = "Start with a random Common coin.";
            RunStart = game =>
            {
                var pool = new List<CoinDef>();
                foreach (var coin in Game.UsablePool(game))
                    if (coin.Rarity == Rarity.Common) pool.Add(coin);
                if (pool.Count == 0) return;
                Game.AddToDeck(game, pool[Rng.Int(game, 1, pool.Count) - 1]);
            };
        }
    }
}
