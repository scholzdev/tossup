using System;
using System.Collections.Generic;

namespace Tossup
{
    // Coin objects live in Coins/; this catalog also holds chips, prizes and characters.
    // Hook reference: Core/Hooks.cs. Randomness inside a hook must use Rng.Random(game) / Rng.Int(game, a, b).
    public static partial class Content
    {
        static Effect E(EffectType type, double amount = 0, int? coins = null) => new Effect(type, amount, coins);
        static List<Effect> L(params Effect[] effects) => new List<Effect>(effects);

        // Definitions in display/RNG order; IDs are only used for lookup and persistence.
        public static readonly List<CoinDef> CoinOrder = CoinCatalog.Ordered;

        public static readonly List<string> ItemOrder = new List<string>
            { "force_heads", "force_tails", "weighted", "double_down", "swap", "peek", "extra_draw", "energy_drink", "shortcut", "safety_net", "lucky_charm" };

        public static readonly List<string> RelicOrder = new List<string> { "magnet", "penny", "clock", "metronome", "baton" };

        public static readonly List<string> CharacterOrder = new List<string> { "blade", "seer", "trader" };

        public static readonly Dictionary<string, CoinDef> Coins = Index(CoinOrder, c => c.Id);
        public static readonly Dictionary<string, ItemDef> Items = BuildItemMap();
        public static readonly Dictionary<string, RelicDef> Relics = Index(BuildRelics(), r => r.Id);
        public static readonly Dictionary<string, CharacterDef> Characters = BuildCharacterMap();

        static Dictionary<string, ItemDef> BuildItemMap()
        {
            var map = Index(BuildItems(), i => i.Id);
            ApplyLatestItems(map);
            return map;
        }

        static Dictionary<string, CharacterDef> BuildCharacterMap()
        {
            var map = Index(BuildCharacters(), c => c.Id);
            ApplyLatestCharacters(map);
            return map;
        }

        static Dictionary<string, T> Index<T>(List<T> list, Func<T, string> key)
        {
            var map = new Dictionary<string, T>();
            foreach (var entry in list) map.Add(key(entry), entry);
            return map;
        }

        static List<ItemDef> BuildItems() => new List<ItemDef>
        {
            new ItemDef
            {
                Id = "double_down", Name = "Double Down", Short = "DOUBLE", Cost = 14, Description = "Next coin: points, gold, energy and penalties x2",
                Use = game =>
                {
                    global::Tossup.Items.Arm("coin_resolve", e =>
                    {
                        foreach (var effect in e.Res.Effects)
                            if (effect.Type == EffectType.Score || effect.Type == EffectType.Gold || effect.Type == EffectType.Energy || effect.Type == EffectType.Penalty) effect.Amount *= 2;
                    });
                    return true;
                },
            },
            new ItemDef
            {
                Id = "extra_draw", Name = "Extra Draw", Short = "+DRAW", Cost = 10, Description = "A played coin returns to the pile",
                Use = game =>
                {
                    var e = game.Encounter;
                    if (e.Returned >= Game.ReturnCap) return false;
                    bool any = false;
                    foreach (int uid in e.Played) if (!e.Discarded.Contains(uid)) any = true;
                    if (!any) return false;
                    Game.ApplyEffect(game, null, Effect.ExtraDraw( 1));
                    return true;
                },
            },
            new ItemDef
            {
                Id = "force_heads", Name = "Force Heads", Short = "FORCE H", Cost = 12, Description = "Dealt coin lands Heads",
                Use = game =>
                {
                    global::Tossup.Items.Arm("coin_flip", e => { e.Flip.Result = Side.Heads; e.Flip.Forced = true; });
                    return true;
                },
            },
            new ItemDef
            {
                Id = "force_tails", Name = "Force Tails", Short = "FORCE T", Cost = 12, Description = "Dealt coin lands Tails",
                Use = game =>
                {
                    global::Tossup.Items.Arm("coin_flip", e => { e.Flip.Result = Side.Tails; e.Flip.Forced = true; });
                    return true;
                },
            },
            new ItemDef
            {
                Id = "peek", Name = "Peek", Short = "PEEK", Cost = 6, Description = "See the next two coins",
                Use = game =>
                {
                    var peek = new List<int>();
                    for (int i = 0; i < 2 && i < game.Encounter.Pile.Count; i++) peek.Add(game.Encounter.Pile[i]);
                    if (peek.Count == 0) return false;
                    game.Peek = peek;
                    return true;
                },
            },
            new ItemDef
            {
                Id = "swap", Name = "Swap", Short = "SWAP", Cost = 8, Description = "Free discard, new coin",
                Use = game => Game.Discard(game) > 0,
            },
            new ItemDef
            {
                Id = "weighted", Name = "Weighted", Short = "WEIGHT", Cost = 8, Description = "+25% Heads, one flip",
                Use = game =>
                {
                    game.Dealt.Probability = Math.Min(1 - game.Dealt.TieProbability, game.Dealt.Probability + .25);
                    return true;
                },
            },
        };

        static List<RelicDef> BuildRelics() => new List<RelicDef>
        {
            // Every 3rd Heads in a row adds +5% Heads to every coin for the rest of the level.
            new RelicDef
            {
                Id = "magnet", Name = "Magnet", Description = "Every 3 Heads in a row: +5% Heads this level",
                Register = ctx => ctx.On("coin_resolved", e =>
                {
                    var level = e.Game.Encounter;
                    if (level.Streak > 0 && level.Streak % 3 == 0) level.Magnet += .05;
                }),
            },
            new RelicDef
            {
                Id = "penny", Name = "Lucky Penny", Description = "First Tails each level becomes Heads",
                Register = ctx =>
                {
                    bool used = false;
                    ctx.On("encounter_start", e => used = false);
                    ctx.On("coin_outcome", e =>
                    {
                        if (e.Result == Side.Tails && !used)
                        {
                            used = true;
                            e.Result = Side.Heads;
                        }
                    });
                },
            },
            new RelicDef
            {
                Id = "clock", Name = "Broken Clock", Description = "Every 10th flip is Heads",
                Register = ctx => ctx.On("coin_outcome", e => { if (e.Flips % 10 == 0) { e.Result = Side.Heads; e.Final = true; } }),
            },
            new RelicDef
            {
                Id = "metronome", Name = "Metronome", Description = "Every 4th flip pays double",
                Register = ctx => ctx.On("coin_resolve", e =>
                {
                    if (e.Game.Encounter.Flips % 4 != 0) return;
                    foreach (var effect in e.Res.Effects)
                        if (effect.Type == EffectType.Score || effect.Type == EffectType.Gold) effect.Amount *= 2;
                }),
            },
            new RelicDef
            {
                Id = "baton", Name = "Baton", Description = "Combo: +0.4 per step instead of 0.25, up to x4",
                Register = ctx => ctx.On("encounter_start", e =>
                {
                    e.Encounter.ComboStep = 0.4;
                    e.Encounter.ComboCap = 4;
                }),
            },
        };

        static List<CoinDef> S(params CoinDef[] coins) => new List<CoinDef>(coins);
        static LockedCoin K(CoinDef coin, int cost) => new LockedCoin(coin, cost);

        static List<CharacterDef> BuildCharacters() => new List<CharacterDef>
        {
            new CharacterDef
            {
                Id = "blade", Name = "The Blade", Description = "Reliable points", Starter = CoinCatalog.Normal,
                Deck = S(CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Sword),
                Pool = S(CoinCatalog.Normal, CoinCatalog.Sword, CoinCatalog.Dagger),
                Locked = new List<LockedCoin>
                {
                    K(CoinCatalog.Hammer, 3), K(CoinCatalog.Blood, 4), K(CoinCatalog.Vampire, 4), K(CoinCatalog.Chain, 5), K(CoinCatalog.Cursed, 5), K(CoinCatalog.Fuse, 5),
                    K(CoinCatalog.Focus, 6), K(CoinCatalog.Martyr, 6), K(CoinCatalog.Snowball, 8), K(CoinCatalog.Megaphone, 5), K(CoinCatalog.Pot, 4),
                    K(CoinCatalog.HotHand, 4), K(CoinCatalog.CashOut, 5), K(CoinCatalog.Doubler, 6), K(CoinCatalog.Amplifier, 6),
                },
            },
            new CharacterDef
            {
                Id = "seer", Name = "The Seer", Description = "Risk and changing odds", Starter = CoinCatalog.Normal,
                Deck = S(CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Focus, CoinCatalog.Spark),
                Pool = S(CoinCatalog.Normal, CoinCatalog.Cursed, CoinCatalog.Gambler, CoinCatalog.Spark, CoinCatalog.Focus, CoinCatalog.Lucky),
                Locked = new List<LockedCoin>
                {
                    K(CoinCatalog.Dagger, 3), K(CoinCatalog.Contrarian, 4), K(CoinCatalog.LuckySeven, 4), K(CoinCatalog.Blood, 4), K(CoinCatalog.Hourglass, 5),
                    K(CoinCatalog.Jester, 5), K(CoinCatalog.Echo, 6), K(CoinCatalog.Phoenix, 6), K(CoinCatalog.Mirror, 5), K(CoinCatalog.Domino, 6), K(CoinCatalog.Twin, 5),
                    K(CoinCatalog.ColdStreak, 4), K(CoinCatalog.Anchor, 4), K(CoinCatalog.TrueEcho, 6), K(CoinCatalog.Amplifier, 6),
                },
            },
            new CharacterDef
            {
                Id = "trader", Name = "The Trader", Description = "Gold and energy", Starter = CoinCatalog.Normal,
                Deck = S(CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Normal, CoinCatalog.Loaded, CoinCatalog.Dagger),
                Pool = S(CoinCatalog.Normal, CoinCatalog.Copper, CoinCatalog.Loaded, CoinCatalog.Dagger, CoinCatalog.Sword),
                Locked = new List<LockedCoin>
                {
                    K(CoinCatalog.Spark, 3), K(CoinCatalog.Bank, 4), K(CoinCatalog.Miser, 4), K(CoinCatalog.Hammer, 4), K(CoinCatalog.Bounty, 5), K(CoinCatalog.Flock, 5),
                    K(CoinCatalog.Momentum, 5), K(CoinCatalog.Capacitor, 6), K(CoinCatalog.Cheerleader, 4), K(CoinCatalog.Megaphone, 5), K(CoinCatalog.Bettor, 5),
                    K(CoinCatalog.Anchor, 4), K(CoinCatalog.Doubler, 6), K(CoinCatalog.TrueEcho, 6),
                },
            },
        };
    }
}
