using System;
using System.Collections.Generic;

namespace Tossup
{
    // A fight's enemy: a fixed pouch of coins it flips from every round, and at most one simple trait
    // (Draw above 5, RoundPoints or HeadsBonus). Its coins use only their printed Heads chance and Score effects.
    public sealed class EnemyDef
    {
        public string Id, Name, Description;
        public List<string> Pouch = new List<string>(); // coin ids
        public int Draw = 5; // coins flipped per round
        public double RoundPoints; // trait: points gained every round
        public double HeadsBonus; // trait: added to every coin's Heads chance
        public int MinLevel = 1, MaxLevel = int.MaxValue;
        public bool Elite, Boss;
    }

    public static class EnemyCatalog
    {
        static List<string> P(params (string id, int n)[] coins)
        {
            var list = new List<string>();
            foreach (var (id, n) in coins) for (int i = 0; i < n; i++) list.Add(id);
            return list;
        }

        public static readonly List<EnemyDef> Ordered = new List<EnemyDef>
        {
            new EnemyDef { Id = "rookie", Name = "The Rookie", Description = "Plain Normals, nothing else.",
                Pouch = P(("normal", 8)), MaxLevel = 2 },
            new EnemyDef { Id = "pickpocket", Name = "The Pickpocket", Description = "Gains 1 point every round.",
                Pouch = P(("normal", 6), ("slug", 2)), Draw = 4, RoundPoints = 1, MaxLevel = 3 },
            new EnemyDef { Id = "dealer", Name = "The Dealer", Description = "Lunges for steady points.",
                Pouch = P(("normal", 4), ("lunge", 4)), MinLevel = 2, MaxLevel = 4 },
            new EnemyDef { Id = "brawler", Name = "The Brawler", Description = "Daggers score either way.",
                Pouch = P(("dagger", 4), ("lunge", 3), ("normal", 1)), MinLevel = 3, MaxLevel = 6 },
            new EnemyDef { Id = "hustler", Name = "The Hustler", Description = "Heads chance +10% on all its coins.",
                Pouch = P(("sword", 3), ("dagger", 4), ("normal", 1)), HeadsBonus = .1, MinLevel = 4, MaxLevel = 7 },
            new EnemyDef { Id = "big_spender", Name = "The Big Spender", Description = "Hammers: rarely, but hard.",
                Pouch = P(("normal", 6), ("hammer", 2), ("dagger", 1)), MinLevel = 5, MaxLevel = 7 },
            new EnemyDef { Id = "enforcer", Name = "The Enforcer", Description = "Elite. Flips 6 coins a round.",
                Pouch = P(("dagger", 5), ("sword", 3)), Draw = 6, Elite = true, MaxLevel = 7 },
            new EnemyDef { Id = "high_roller", Name = "The High Roller", Description = "Elite. Cursed coins, huge or nothing.",
                Pouch = P(("normal", 5), ("cursed", 3), ("lunge", 2)), Elite = true, MinLevel = 4, MaxLevel = 7 },
            new EnemyDef { Id = "the_house", Name = "The House", Description = "The boss. Flips 6 coins a round.",
                Pouch = P(("hammer", 6), ("dagger", 6), ("lunge", 2), ("sword", 2)), Draw = 6, Boss = true },
        };

        public static readonly Dictionary<string, EnemyDef> ById = Build();

        static Dictionary<string, EnemyDef> Build()
        {
            var map = new Dictionary<string, EnemyDef>();
            foreach (var d in Ordered) map.Add(d.Id, d);
            return map;
        }
    }
}
