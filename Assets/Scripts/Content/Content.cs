using System;
using System.Collections.Generic;

namespace Tossup
{
    // Every coin, chip, prize and character. Add a coin here and to CoinOrder (the collection order).
    // Hook reference: Core/Hooks.cs. Randomness inside a hook must use Rng.Random(game) / Rng.Int(game, a, b).
    public static class Content
    {
        static Effect E(string type, double amount = 0, int? coins = null) => new Effect(type, amount, coins);
        static List<Effect> L(params Effect[] effects) => new List<Effect>(effects);

        // Display order of all coins (collection screen).
        public static readonly List<string> CoinOrder = new List<string>
        {
            "normal", "copper", "sword", "lucky", "cursed", "loaded", "dagger", "hammer", "blood", "spark", "focus",
            "snowball", "gambler", "momentum", "echo", "vampire", "miser", "fuse", "phoenix", "contrarian", "chain", "bank",
            "lucky_seven", "hourglass", "capacitor", "martyr", "bounty", "jester", "flock", "megaphone", "cheerleader",
            "mirror", "twin", "pot", "domino", "hot_hand", "anchor", "bettor", "cash_out", "cold_streak", "amplifier",
            "true_echo", "doubler",
        };

        public static readonly List<string> ItemOrder = new List<string>
            { "force_heads", "force_tails", "weighted", "double_down", "swap", "peek", "extra_draw" };

        public static readonly List<string> RelicOrder = new List<string> { "magnet", "penny", "clock", "metronome", "baton" };

        public static readonly List<string> CharacterOrder = new List<string> { "blade", "seer", "trader" };

        public static readonly Dictionary<string, CoinDef> Coins = Index(BuildCoins(), c => c.Id);
        public static readonly Dictionary<string, ItemDef> Items = Index(BuildItems(), i => i.Id);
        public static readonly Dictionary<string, RelicDef> Relics = Index(BuildRelics(), r => r.Id);
        public static readonly Dictionary<string, CharacterDef> Characters = Index(BuildCharacters(), c => c.Id);

        static Dictionary<string, T> Index<T>(List<T> list, Func<T, string> key)
        {
            var map = new Dictionary<string, T>();
            foreach (var entry in list) map.Add(key(entry), entry);
            return map;
        }

        static List<CoinDef> BuildCoins()
        {
            // Jester ignores its sides: a seeded random effect replaces whatever Heads/Tails would give.
            var jesterResults = L(E("score", 6), E("gold", 4), E("energy", 2), E("score", 2));
            var coins = new List<CoinDef>
            {
                // Effect "amplify": all active "next coins" buffs last one coin longer and get stronger.
                new CoinDef
                {
                    Id = "amplifier", Name = "Amplifier", Rarity = "SR", EnergyCost = 1, Probability = .5,
                    Description = "Heads: 1 point, and all active buffs last 1 coin longer and get stronger. Tails: 1 point.",
                    Heads = L(E("score", 1), E("amplify", 1)), Tails = L(E("score", 1)),
                },
                // Effect "combo_shield": the next result that would break the combo is ignored instead (once per shield).
                new CoinDef
                {
                    Id = "anchor", Name = "Anchor", Rarity = "R", Probability = .5,
                    Description = "Heads: 2 points, and the next time the combo would break it holds instead.",
                    Heads = L(E("score", 2), E("combo_shield", 1)), Tails = L(E("score", 1)),
                },
                // Interest on held gold.
                new CoinDef
                {
                    Id = "bank", Name = "Bank", Rarity = "R", Probability = .5,
                    Description = "Heads: +3 gold, plus 1 per 10 gold held (max +3).",
                    Heads = L(E("gold", 3)), Tails = L(),
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result != Side.Heads) return;
                        double interest = Math.Min(3, Math.Floor(game.Player.Gold / 10));
                        if (interest > 0) res.Effects.Add(E("gold", interest));
                    },
                },
                // Reads the combo length (already updated for this flip); the combo multiplier applies on top.
                new CoinDef
                {
                    Id = "bettor", Name = "Bettor", Rarity = "SR", EnergyCost = 1, Probability = .45,
                    Description = "Heads: 3 points per flip in the current combo (max 30). Tails: quota +2.",
                    Heads = L(), Tails = L(E("penalty", 2)),
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result != Side.Heads) return;
                        res.Effects.Add(E("score", Math.Min(30, 3 * game.Encounter.ComboLen)));
                    },
                },
                new CoinDef
                {
                    Id = "blood", Name = "Blood", Rarity = "R", Cost = 22, EnergyCost = 1, Probability = .6,
                    Description = "Big points, but Tails raises the quota.",
                    Heads = L(E("score", 11)), Tails = L(E("penalty", 3)),
                },
                // Register: subscribes to a bus event itself. Reacts to effects the game actually applied.
                new CoinDef
                {
                    Id = "bounty", Name = "Bounty", Rarity = "SR", EnergyCost = 1, Probability = .5,
                    Description = "Pays 1 gold for every 2 points it scores.",
                    Heads = L(E("score", 4)), Tails = L(E("score", 2)),
                    Register = ctx => ctx.On("effect_applied", e =>
                    {
                        if (e.Effect.Type == "score") e.Game.Player.Gold += Math.Floor(e.Effect.Amount / 2);
                    }),
                },
                // Turns the energy stat into points (makes energy worth saving instead of discarding).
                new CoinDef
                {
                    Id = "capacitor", Name = "Capacitor", Rarity = "R", EnergyCost = 1, Probability = .5,
                    Description = "Heads: 2 points per energy you hold. Tails: +1 energy.",
                    Heads = L(), Tails = L(E("energy", 1)),
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result != Side.Heads) return;
                        res.Effects.Add(E("score", 2 * game.Player.Energy));
                    },
                },
                // Sets res.CashOut: the game squares the combo multiplier for this flip and then resets the combo.
                new CoinDef
                {
                    Id = "cash_out", Name = "Cash Out", Rarity = "SR", Probability = .5,
                    Description = "Heads: 3 points, the combo multiplier counts twice, then the combo resets.",
                    Heads = L(E("score", 3)), Tails = L(),
                    OnResolve = (game, inst, res) => { if (res.Result == Side.Heads) res.CashOut = true; },
                },
                // Reads the Heads streak tracked by the game (already includes this flip).
                new CoinDef
                {
                    Id = "chain", Name = "Chain", Rarity = "R", Probability = .5,
                    Description = "Heads: 2 points per Heads in a row, including this one.",
                    Heads = L(), Tails = L(),
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result != Side.Heads) return;
                        res.Effects.Add(E("score", 2 * game.Encounter.Streak));
                    },
                },
                // Buffs the odds of the coins that follow: effect "next_odds".
                new CoinDef
                {
                    Id = "cheerleader", Name = "Cheerleader", Rarity = "R", Probability = .5,
                    Description = "Heads: 2 points, next 2 coins +20% Heads. Tails: next coin +20%.",
                    Heads = L(E("score", 2), E("next_odds", .2, 2)), Tails = L(E("next_odds", .2, 1)),
                },
                // The Tails twin of Chain: Tails pays per Tails in a row (the combo counts both sides).
                new CoinDef
                {
                    Id = "cold_streak", Name = "Cold Streak", Rarity = "R", Probability = .5,
                    Description = "Tails: 2 points per Tails in a row (max 20). Heads: 1 point.",
                    Heads = L(E("score", 1)), Tails = L(),
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result != Side.Tails) return;
                        res.Effects.Add(E("score", Math.Min(20, 2 * game.Encounter.ComboLen)));
                    },
                },
                // Always lands opposite of the previous flip.
                new CoinDef
                {
                    Id = "contrarian", Name = "Contrarian", Rarity = "SR", Probability = .5,
                    Description = "Always lands opposite of the previous flip.",
                    Heads = L(E("score", 4)), Tails = L(E("score", 1)),
                    OnFlip = (game, inst, flip) =>
                    {
                        var previous = game.LastResult;
                        if (previous != null) flip.Result = previous.Final == Side.Heads ? Side.Tails : Side.Heads;
                    },
                },
                new CoinDef
                {
                    Id = "copper", Name = "Copper", Rarity = "N", Cost = 10, Probability = .5,
                    Description = "Funds your next move.",
                    Heads = L(E("gold", 2)), Tails = L(E("energy", 1)),
                },
                new CoinDef
                {
                    Id = "cursed", Name = "Cursed", Rarity = "SR", Probability = .25,
                    Description = "A powerful, dangerous wager.",
                    Heads = L(E("score", 15)), Tails = L(E("penalty", 2)),
                },
                new CoinDef
                {
                    Id = "dagger", Name = "Dagger", Rarity = "N", Cost = 12, Probability = .75,
                    Description = "Scores either way.",
                    Heads = L(E("score", 4)), Tails = L(E("score", 1)),
                },
                // Effect "next_heads": the next coin is guaranteed to land Heads (relics and the boss can still change it).
                new CoinDef
                {
                    Id = "domino", Name = "Domino", Rarity = "UR", Probability = .5,
                    Description = "Heads: 2 points, and the next coin lands Heads. Tails: quota +1.",
                    Heads = L(E("score", 2), E("next_heads", 0, 1)), Tails = L(E("penalty", 1)),
                },
                // Every Doubler flip this level doubles the next one's Heads value (the counter is shared by all
                // copies and resets each level).
                new CoinDef
                {
                    Id = "doubler", Name = "Doubler", Rarity = "SR", Probability = .5,
                    Description = "Heads: 3 points, doubled for every Doubler flip so far this level (3, 6, 12, 24...).",
                    Heads = L(), Tails = L(),
                    OnResolve = (game, inst, res) =>
                    {
                        var e = game.Encounter;
                        int flips = e.Doubler;
                        e.Doubler = flips + 1;
                        if (res.Result == Side.Heads) res.Effects.Add(E("score", 3 * Math.Pow(2, Math.Min(flips, 7))));
                    },
                },
                // Replays the previous coin's printed effects for this side.
                new CoinDef
                {
                    Id = "echo", Name = "Echo", Rarity = "UR", Probability = .5,
                    Description = "Repeats the effects the previous coin had for this side.",
                    Heads = L(), Tails = L(),
                    OnResolve = (game, inst, res) =>
                    {
                        var previous = game.LastResult;
                        if (previous == null) return;
                        var def = Coins[Game.GetCoin(game, previous.Uid).Id];
                        foreach (var effect in res.Result == Side.Heads ? def.Heads : def.Tails)
                            res.Effects.Add(E(effect.Type, effect.Amount));
                    },
                },
                // Synergy with duplicates in the deck. OnOdds is pure.
                new CoinDef
                {
                    Id = "flock", Name = "Flock", Rarity = "R", Probability = .4,
                    Description = "+10% Heads for every other Flock in your bank.",
                    Heads = L(E("score", 4)), Tails = L(),
                    OnOdds = (game, inst, odds) =>
                    {
                        foreach (var other in game.Coins)
                            if (other != inst && other.Id == inst.Id) odds.P += .1;
                    },
                },
                new CoinDef
                {
                    Id = "focus", Name = "Focus", Rarity = "R", Cost = 10, Probability = .5,
                    Description = "Builds its own Heads chance.",
                    Heads = L(E("probability", 0.15)), Tails = L(E("score", 4)),
                },
                // Discarding charges it (stored on the coin instance, so it survives between levels).
                new CoinDef
                {
                    Id = "fuse", Name = "Fuse", Rarity = "SR", Cost = 10, Probability = .5,
                    Description = "Discard it to charge +6. Heads spends all charge as points.",
                    Heads = L(E("score", 1)), Tails = L(),
                    OnDiscard = (game, inst) => inst.Charge += 6,
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result != Side.Heads || inst.Charge == 0) return;
                        res.Effects.Add(E("score", inst.Charge));
                        inst.Charge = 0;
                    },
                },
                // Seeded RNG rewrites or cancels the effects.
                new CoinDef
                {
                    Id = "gambler", Name = "Gambler", Rarity = "SR", Cost = 22, EnergyCost = 1, Probability = .5,
                    Description = "Heads is a bet: 50% triple points, otherwise nothing.",
                    Heads = L(E("score", 7)), Tails = L(),
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result != Side.Heads) return;
                        if (Rng.Random(game) < .5)
                        {
                            foreach (var effect in res.Effects) effect.Amount *= 3;
                            Game.Log(game, "Gambler wins the bet.");
                        }
                        else
                        {
                            res.Effects = new List<Effect>();
                            Game.Log(game, "Gambler loses the bet.");
                        }
                    },
                },
                new CoinDef
                {
                    Id = "hammer", Name = "Hammer", Rarity = "R", Cost = 22, EnergyCost = 2, Probability = .35,
                    Description = "A rare but crushing hit.",
                    Heads = L(E("score", 16)), Tails = L(E("score", 1)),
                },
                // Effect "combo_bonus": the combo (same result in a row) counts extra steps.
                new CoinDef
                {
                    Id = "hot_hand", Name = "Hot Hand", Rarity = "R", Probability = .5,
                    Description = "Heads: 2 points, and the combo grows by 1 extra step.",
                    Heads = L(E("score", 2), E("combo_bonus", 1)), Tails = L(),
                },
                // OnOdds is pure: it is evaluated every time the odds are displayed.
                new CoinDef
                {
                    Id = "hourglass", Name = "Hourglass", Rarity = "R", Cost = 10, Probability = .4,
                    Description = "+30% Heads when 3 or fewer coins are left. Tails: goes back into the pile.",
                    Heads = L(E("score", 4)), Tails = L(E("extra_draw", 1)),
                    OnOdds = (game, inst, odds) =>
                    {
                        var level = game.Encounter;
                        if (level != null && level.Queue.Count + level.Pile.Count <= 3) odds.P += .3;
                    },
                },
                new CoinDef
                {
                    Id = "jester", Name = "Jester", Rarity = "UR", EnergyCost = 1, Probability = .5,
                    Description = "Heads or Tails, it does something random.",
                    Heads = L(), Tails = L(),
                    OnResolve = (game, inst, res) =>
                    {
                        var pick = jesterResults[Rng.Int(game, 1, jesterResults.Count) - 1];
                        res.Effects = L(E(pick.Type, pick.Amount));
                    },
                },
                new CoinDef
                {
                    Id = "loaded", Name = "Loaded", Rarity = "N", Cost = 12, Probability = .75,
                    Description = "Reliable income on Heads.",
                    Heads = L(E("gold", 4)), Tails = L(),
                },
                new CoinDef
                {
                    Id = "lucky", Name = "Lucky", Rarity = "N", Cost = 10, Probability = .5,
                    Description = "Heads: 2 points, and it goes back into the pile to play again.",
                    Heads = L(E("score", 2), E("extra_draw", 1)), Tails = L(),
                },
                // Seeded 1-in-7 jackpot across two hooks (OnFlip decides, OnResolve pays). State on the instance.
                new CoinDef
                {
                    Id = "lucky_seven", Name = "Lucky Seven", Rarity = "SR", Probability = .4,
                    Description = "1 in 7: lands Heads and pays triple points.",
                    Heads = L(E("score", 3)), Tails = L(),
                    OnFlip = (game, inst, flip) =>
                    {
                        inst.Jackpot = Rng.Int(game, 1, 7) == 7;
                        if (inst.Jackpot) flip.Result = Side.Heads;
                    },
                    OnResolve = (game, inst, res) =>
                    {
                        if (!inst.Jackpot) return;
                        inst.Jackpot = false;
                        foreach (var effect in res.Effects) effect.Amount *= 3;
                    },
                },
                // Tails raises the quota; every penalty taken this level is paid back as bonus points on Heads.
                new CoinDef
                {
                    Id = "martyr", Name = "Martyr", Rarity = "SR", EnergyCost = 1, Probability = .5,
                    Description = "Tails: quota +3. Heads: 4 points, +1 per Tails so far this level.",
                    Heads = L(E("score", 4)), Tails = L(E("penalty", 3)),
                    Grow = (inst, evt) => { if (evt == "level") inst.Debt = 0; },
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result == Side.Tails) inst.Debt += 1;
                        else if (inst.Debt > 0) res.Effects.Add(E("score", inst.Debt));
                    },
                },
                // Buffs the coins that follow: effect "next_mult" (amount = factor, coins = how many coins it lasts).
                new CoinDef
                {
                    Id = "megaphone", Name = "Megaphone", Rarity = "R", Cost = 20, EnergyCost = 1, Probability = .45,
                    Description = "Heads: 2 points, and the next 2 coins pay double.",
                    Heads = L(E("score", 2), E("next_mult", 2, 2)), Tails = L(),
                },
                // Effect "next_swap": the next coin resolves with the effects of its other side.
                new CoinDef
                {
                    Id = "mirror", Name = "Mirror", Rarity = "SR", Probability = .5,
                    Description = "Heads: the next coin uses the effects of its other side. Tails: 2 points.",
                    Heads = L(E("score", 1), E("next_swap", 0, 1)), Tails = L(E("score", 2)),
                },
                // Reads the gold stat.
                new CoinDef
                {
                    Id = "miser", Name = "Miser", Rarity = "R", Probability = .55,
                    Description = "Heads: 1 point per 10 gold you hold.",
                    Heads = L(), Tails = L(E("gold", 2)),
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result != Side.Heads) return;
                        double amount = Math.Floor(game.Player.Gold / 10);
                        if (amount > 0) res.Effects.Add(E("score", amount));
                    },
                },
                // OnOdds (pure) reads the level's Heads streak.
                new CoinDef
                {
                    Id = "momentum", Name = "Momentum", Rarity = "SR", Probability = .4,
                    Description = "+5% Heads for every Heads in a row this level.",
                    Heads = L(E("score", 6)), Tails = L(),
                    OnOdds = (game, inst, odds) =>
                    {
                        var e = game.Encounter;
                        if (e != null) odds.P += .05 * e.Streak;
                    },
                },
                // The baseline coin: nothing special. Cheap, so the shop can top the bank up.
                new CoinDef
                {
                    Id = "normal", Name = "Normal", Rarity = "N", Cost = 5, Probability = .5,
                    Description = "A plain coin. Barely a scratch.",
                    Heads = L(E("score", 1)), Tails = L(),
                },
                // Anger builds on Tails and is spent on the next Heads. State lives on the instance.
                new CoinDef
                {
                    Id = "phoenix", Name = "Phoenix", Rarity = "UR", Probability = .5,
                    Description = "Each Tails stores anger (max 5). Heads: 3 points +2 per anger.",
                    Heads = L(E("score", 3)), Tails = L(E("penalty", 2)),
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result == Side.Tails) inst.Anger = Math.Min(5, inst.Anger + 1);
                        else
                        {
                            foreach (var effect in res.Effects)
                                if (effect.Type == "score") effect.Amount += 2 * inst.Anger;
                            inst.Anger = 0;
                        }
                    },
                },
                // The pot grows with every flip of the level. e.Flips already counts the flip being resolved.
                new CoinDef
                {
                    Id = "pot", Name = "Pot", Rarity = "R", Probability = .5,
                    Description = "Heads: points equal to the flips made so far this level (max 12).",
                    Heads = L(), Tails = L(E("score", 1)),
                    OnResolve = (game, inst, res) =>
                    {
                        if (res.Result != Side.Heads) return;
                        res.Effects.Add(E("score", Math.Min(12, game.Encounter.Flips)));
                    },
                },
                // Grow("flip") for run-long growth, OnResolve to edit the effect list.
                new CoinDef
                {
                    Id = "snowball", Name = "Snowball", Rarity = "SR", Cost = 24, EnergyCost = 1, Probability = .5,
                    Description = "Grows +1 point every flip for the whole run (max +10).",
                    Heads = L(E("score", 3)), Tails = L(E("score", 1)),
                    Grow = (inst, evt) => { if (evt == "flip") inst.Stack = Math.Min(10, inst.Stack + 1); },
                    OnResolve = (game, inst, res) =>
                    {
                        foreach (var effect in res.Effects)
                            if (effect.Type == "score") effect.Amount += inst.Stack;
                    },
                },
                new CoinDef
                {
                    Id = "spark", Name = "Spark", Rarity = "R", Probability = .5,
                    Description = "Energy or a small strike.",
                    Heads = L(E("energy", 2)), Tails = L(E("score", 3)),
                },
                new CoinDef
                {
                    Id = "sword", Name = "Sword", Rarity = "N", Cost = 12, Probability = .5,
                    Description = "Steady points on Heads.",
                    Heads = L(E("score", 5)), Tails = L(),
                },
                // Repeats the previous coin's real effects (after its own hooks, before multipliers), whichever
                // side it landed on. Echo copies the printed values; True Echo copies what actually happened.
                new CoinDef
                {
                    Id = "true_echo", Name = "True Echo", Rarity = "UR", Probability = .5,
                    Description = "Repeats what the previous coin really did, including its buffs and growth, on either side.",
                    Heads = L(), Tails = L(),
                    OnResolve = (game, inst, res) =>
                    {
                        var previous = game.LastResult;
                        if (previous == null || previous.BaseEffects == null) return;
                        foreach (var effect in previous.BaseEffects) res.Effects.Add(effect.Copy());
                    },
                },
                // Always lands like the previous flip (the mirror image of Contrarian).
                new CoinDef
                {
                    Id = "twin", Name = "Twin", Rarity = "SR", Probability = .5,
                    Description = "Always lands the same as the previous flip.",
                    Heads = L(E("score", 4)), Tails = L(E("score", 1)),
                    OnFlip = (game, inst, flip) =>
                    {
                        var previous = game.LastResult;
                        if (previous != null) flip.Result = previous.Final;
                    },
                },
                // Turns a side effect into a cross-stat trade (points for gold).
                new CoinDef
                {
                    Id = "vampire", Name = "Vampire", Rarity = "R", Probability = .5,
                    Description = "Heads: 2 points and it drains 2 gold from the house.",
                    Heads = L(E("score", 2)), Tails = L(),
                    OnResolve = (game, inst, res) => { if (res.Result == Side.Heads) res.Effects.Add(E("gold", 2)); },
                },
            };
            return coins;
        }

        static List<ItemDef> BuildItems() => new List<ItemDef>
        {
            new ItemDef
            {
                Id = "double_down", Name = "Double Down", Short = "DOUBLE", Cost = 14, Description = "Next coin effects x2",
                Use = game =>
                {
                    global::Tossup.Items.Arm("coin_resolve", e =>
                    {
                        foreach (var effect in e.Res.Effects) effect.Amount *= 2;
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
                    Game.ApplyEffect(game, null, E("extra_draw", 1));
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
                    game.Dealt.Probability = Math.Min(1, game.Dealt.Probability + .25);
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
                Register = ctx => ctx.On("coin_outcome", e => { if (e.Flips % 10 == 0) e.Result = Side.Heads; }),
            },
            new RelicDef
            {
                Id = "metronome", Name = "Metronome", Description = "Every 4th flip pays double",
                Register = ctx => ctx.On("coin_resolve", e =>
                {
                    if (e.Game.Encounter.Flips % 4 != 0) return;
                    foreach (var effect in e.Res.Effects)
                        if (effect.Type == "score" || effect.Type == "gold") effect.Amount *= 2;
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

        static List<string> S(params string[] ids) => new List<string>(ids);
        static LockedCoin K(string id, int cost) => new LockedCoin(id, cost);

        static List<CharacterDef> BuildCharacters() => new List<CharacterDef>
        {
            new CharacterDef
            {
                Id = "blade", Name = "The Blade", Description = "Reliable points", Starter = "normal",
                Deck = S("normal", "normal", "normal", "normal", "sword"),
                Pool = S("normal", "sword", "dagger"),
                Locked = new List<LockedCoin>
                {
                    K("hammer", 3), K("blood", 4), K("vampire", 4), K("chain", 5), K("cursed", 5), K("fuse", 5),
                    K("focus", 6), K("martyr", 6), K("snowball", 8), K("megaphone", 5), K("pot", 4),
                    K("hot_hand", 4), K("cash_out", 5), K("doubler", 6), K("amplifier", 6),
                },
            },
            new CharacterDef
            {
                Id = "seer", Name = "The Seer", Description = "Risk and changing odds", Starter = "normal",
                Deck = S("normal", "normal", "normal", "focus", "spark"),
                Pool = S("normal", "cursed", "gambler", "spark", "focus", "lucky"),
                Locked = new List<LockedCoin>
                {
                    K("dagger", 3), K("contrarian", 4), K("lucky_seven", 4), K("blood", 4), K("hourglass", 5),
                    K("jester", 5), K("echo", 6), K("phoenix", 6), K("mirror", 5), K("domino", 6), K("twin", 5),
                    K("cold_streak", 4), K("anchor", 4), K("true_echo", 6), K("amplifier", 6),
                },
            },
            new CharacterDef
            {
                Id = "trader", Name = "The Trader", Description = "Gold and energy", Starter = "normal",
                Deck = S("normal", "normal", "normal", "loaded", "dagger"),
                Pool = S("normal", "copper", "loaded", "dagger", "sword"),
                Locked = new List<LockedCoin>
                {
                    K("spark", 3), K("bank", 4), K("miser", 4), K("hammer", 4), K("bounty", 5), K("flock", 5),
                    K("momentum", 5), K("capacitor", 6), K("cheerleader", 4), K("megaphone", 5), K("bettor", 5),
                    K("anchor", 4), K("doubler", 6), K("true_echo", 6),
                },
            },
        };
    }
}
