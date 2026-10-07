using System.Collections.Generic;

namespace Tossup.Encounters
{
    public sealed class GuildHallEncounter : RunEncounterDef
    {
        public GuildHallEncounter()
        {
            Id = "guild_hall";
            Name = "The Guild Hall";
            Description = "At run start, gain 2 gold for each coin type shared by at least two coins in your deck.";
            RunStart = game =>
            {
                var counts = new Dictionary<CoinType, int>();
                foreach (var coin in game.Coins)
                    foreach (var type in coin.Definition.Types)
                    {
                        counts.TryGetValue(type, out int count);
                        counts[type] = count + 1;
                    }
                int bonus = 0;
                foreach (var count in counts.Values) if (count > 1) bonus += 2;
                if (bonus <= 0) return;
                game.Player.Gold += bonus;
                Game.Log(game, "Guild Hall: +" + bonus + " gold from shared coin types.");
            };
        }
    }
}
