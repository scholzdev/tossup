using System;
using System.Collections.Generic;

namespace Tossup
{
    public sealed class StageDef
    {
        public string Name;
        public double? Payout;
        public bool Boss, Inverts;
        public int? Endless;
    }

    // The rules. Pure C# (no UnityEngine), so the whole game can be simulated and tested outside Unity.
    public static partial class Game
    {
        public const int Rounds = 5; // rounds in a fight
        public const int HandSize = 5; // coins in your hand; it refills to this at the end of every round
        public const int PouchSize = 30; // coins a run starts with: your set (x2 each) and Normals to fill
        public const int StartMax = 6; // coins in a starting set (each goes into the pouch twice)
        public const int DeckMax = 50; // the pouch cannot grow past this: buying is refused when it is full
        public const int SetCopies = 2; // copies of every set coin that go into the pouch
        public const double ComboStep = 0.25, ComboCap = 3; // combo: x1 + 0.25 per extra same result in a row, up to x3
        public const int ReturnCap = 3; // "extra draw" effects (a coin returning to the pouch) per fight
        public const double PityStep = 0.05, PityCap = 0.15; // visible pity: +5% Heads per Tails in a row, up to +15%
        public const int StartGold = 7;
        public const double SurplusRate = .5; // gold per point of winning margin (rounded down in total)
        public const int MaxCopies = 3; // copies of one coin in a set; the plain Normal coin is exempt (up to the set size)

        public static readonly List<StageDef> Route = new List<StageDef>
        {
            new StageDef { Name = "Opening", Payout = 25 },
            new StageDef { Name = "Second Chance", Payout = 30 },
            new StageDef { Name = "High Stakes", Payout = 20 },
            new StageDef { Name = "Rising Tide", Payout = 35 },
            new StageDef { Name = "Double Down", Payout = 40 },
            new StageDef { Name = "Last Call", Payout = 45 },
            new StageDef { Name = "Final Table", Payout = 50 },
            new StageDef { Name = "The House", Boss = true },
        };

        public static void Log(GameState game, string message) => game.Log.Add(message);

        static string N(double v) => GameText.Num(v);
        static string CoinName(CoinInst inst) => inst.Definition.Name;

        static CoinInst NewCoin(GameState game, CoinDef definition)
        {
            game.NextUid++;
            return new CoinInst { Uid = game.NextUid, Definition = definition };
        }

        internal static CoinInst NewCoin(GameState game, string id) => NewCoin(game, Content.Coins[id]);

        public static CoinInst GetCoin(GameState game, int uid)
        {
            foreach (var item in game.Coins)
                if (item.Uid == uid) return item;
            return null;
        }

        public static int ActiveCount(GameState game) => game.Coins.Count;

        internal static CoinInst AddToDeck(GameState game, CoinDef definition)
        {
            var item = NewCoin(game, definition);
            game.Coins.Add(item);
            game.SelectedUid = item.Uid;
            return item;
        }

        static CoinInst AddToDeck(GameState game, string id) => AddToDeck(game, Content.Coins[id]);

        // A coin reward joins the pouch for future fights, without changing the current fight.
        public static bool TryGrantCoin(GameState game, CoinDef definition)
        {
            if (game.Coins.Count >= DeckMax) return false;
            game.Coins.Add(NewCoin(game, definition));
            Log(game, "Granted " + definition.Name + " coin.");
            return true;
        }

        // "Stamps" an owned Normal coin into a colorless coin (e.g. Gilded). Anything else is refused.
        public static bool StampNormal(GameState game, int uid, string coinId)
        {
            var coin = GetCoin(game, uid);
            if (coin == null || coin.Id != "normal" || Array.IndexOf(Tossup.Coins.ColorlessCoin.Ids, coinId) < 0) return false;
            coin.Definition = Content.Coins[coinId];
            Log(game, "Stamped Normal into " + coin.Definition.Name + ".");
            return true;
        }

        static List<T> Offers<T>(GameState game, List<T> pool, int count)
        {
            var choices = new List<T>();
            var remaining = new List<T>(pool);
            int n = Math.Min(count, remaining.Count);
            for (int k = 0; k < n; k++)
            {
                int index = Rng.Int(game, 1, remaining.Count);
                choices.Add(remaining[index - 1]);
                remaining.RemoveAt(index - 1);
            }
            return choices;
        }

        // Coins a coin set may contain: the character's starting pool plus coins already unlocked.
        internal static List<CoinDef> UsablePool(GameState game)
        {
            var pool = new List<CoinDef>();
            var seen = new HashSet<CoinDef>();
            foreach (var coin in Content.Characters[game.CharacterId].Pool) { pool.Add(coin); seen.Add(coin); }
            foreach (var id in game.Unlocked)
                if (seen.Add(Content.Coins[id])) pool.Add(Content.Coins[id]);
            return pool;
        }

        // Every coin the shop may offer this character, including coins that are still locked: buying a
        // locked coin in the shop is what unlocks it (see game.Purchased).
        static List<CoinDef> ShopPool(GameState game)
        {
            if (game.Sandbox != null)
            {
                var restricted = new List<CoinDef>();
                foreach (var id in game.Sandbox.Coins) if (!restricted.Contains(Content.Coins[id])) restricted.Add(Content.Coins[id]);
                return restricted;
            }
            var pool = UsablePool(game);
            var seen = new HashSet<CoinDef>(pool);
            foreach (var entry in Content.Characters[game.CharacterId].Locked)
                if (seen.Add(entry.Coin)) pool.Add(entry.Coin);
            return pool;
        }

        // Four distinct coin offers from the whole coin list.
        static List<CoinDef> ShopStock(GameState game) => Offers(game, ShopPool(game), 4);

        // The pouch of a fight: every owned coin, shuffled.
        static List<int> ShuffleDeck(GameState game)
        {
            var uids = new List<int>();
            foreach (var item in game.Coins) uids.Add(item.Uid);
            Shuffle(game, uids);
            return uids;
        }

        static void Shuffle<T>(GameState game, List<T> list)
        {
            for (int i = list.Count; i >= 2; i--)
            {
                int j = Rng.Int(game, 1, i);
                (list[i - 1], list[j - 1]) = (list[j - 1], list[i - 1]);
            }
        }

        public static Odds GetOdds(GameState game, CoinInst item)
        {
            SandboxOdds sandboxOdds = null;
            game.Sandbox?.Odds.TryGetValue(item.Id, out sandboxOdds);
            double bonus = 0, magnet = 0, boost = 0, pity = 0;
            var e = game.Encounter;
            if (game.Phase == Phase.Encounter && e != null)
            {
                if (e.Bonus.TryGetValue(item.Uid, out double b)) bonus = b;
                magnet = e.Magnet;
                pity = Pity(e);
                foreach (var buff in e.Buffs)
                    if (buff.Kind == "odds" && !buff.Fresh && BuffTargets(buff, item)) boost += buff.Amount;
            }
            var def = item.Definition;
            double fortune = HasType(item, "fortune") ? game.FortuneBonus : 0;
            double p = (sandboxOdds?.Heads ?? def.Probability) + bonus + magnet + boost + fortune + pity;
            double edge = Math.Max(0, Math.Min(1, sandboxOdds?.Tie ?? def.TieProbability));
            double heads = Math.Max(0, Math.Min(1 - edge, p));
            var odds = new Odds(heads, edge, 1 - heads - edge);
            if (game.Phase != Phase.Encounter) return odds;

            Hooks.Odds(game, item, odds);
            odds.Normalize();
            return odds;
        }

        public static double Pity(Encounter e) => Math.Min(PityCap, PityStep * e.TailsRun);

        public static double Probability(GameState game, CoinInst item) => GetOdds(game, item).Heads;

        public static double TieProbability(GameState game, CoinInst item)
            => GetOdds(game, item).Edge;

        // Fight time advances only while the run is actively being played (hooks may tick on it).
        public static void AdvanceClock(GameState game, double dt)
        {
            if (game == null || game.Paused || game.Phase != Phase.Encounter || game.Encounter == null ||
                dt <= 0 || double.IsNaN(dt) || double.IsInfinity(dt)) return;
            game.Encounter.ElapsedSeconds += dt;
            Hooks.Tick(game, dt);
        }

        // Top the hand up to HandSize from the pouch. When the pouch runs dry the discard pile (the coins you
        // flipped this fight) is shuffled back in; with nothing left anywhere the hand stays short.
        static void Refill(GameState game)
        {
            var e = game.Encounter;
            while (e.Hand.Count < e.HandSize)
            {
                if (e.Pouch.Count == 0)
                {
                    if (e.Discard.Count == 0) return;
                    e.Pouch = new List<int>(e.Discard);
                    e.Discard.Clear();
                    Shuffle(game, e.Pouch);
                    Log(game, "The pouch is empty: the flipped coins are shuffled back in.");
                }
                e.Hand.Add(e.Pouch[0]);
                e.Pouch.RemoveAt(0);
            }
        }

        // Draw extra coins into the hand right now (chips), past the hand size. Returns how many came.
        public static int DrawCoins(GameState game, int count)
        {
            var e = game.Encounter;
            int drawn = 0;
            for (; drawn < count; drawn++)
            {
                if (e.Pouch.Count == 0)
                {
                    if (e.Discard.Count == 0) break;
                    e.Pouch = new List<int>(e.Discard);
                    e.Discard.Clear();
                    Shuffle(game, e.Pouch);
                }
                e.Hand.Add(e.Pouch[0]);
                e.Pouch.RemoveAt(0);
            }
            return drawn;
        }

        // Coins you could still flip this fight (hand and pouch; the discard pile comes back when the pouch runs dry).
        public static int CoinsLeft(GameState game)
        {
            var e = game.Encounter;
            return e.Hand.Count + e.Pouch.Count + e.Discard.Count;
        }

        // A fight of the run. Beyond the route (after the boss) the fights are endless: the boss's pouch again,
        // +2 points for the enemy per round and level beyond it, it pays more, and it inverts every 5th flip.
        public static StageDef Stage(int level)
        {
            if (level >= 1 && level <= Route.Count) return Route[level - 1];
            int k = level - Route.Count;
            return new StageDef { Name = "Endless", Endless = k, Payout = 40 + 5 * k, Inverts = true };
        }

        static void StartRound(GameState game)
        {
            var e = game.Encounter;
            double energy = Math.Max(0, game.Player.MaxEnergy + e.EnergyBonus);
            game.Player.Energy = e.Round == 1 ? energy : Math.Max(game.Player.Energy, energy);
            e.RoundFlips = 0;
            e.RoundLog.Clear();
            if (e.Round > 1) { e.RoundStartScored = e.Scored; e.RoundStartEnemy = e.EnemyScore; } // round 1 also counts what the fight started with
            Refill(game);
        }

        static void StartEncounter(GameState game)
        {
            var stage = Stage(game.EncounterIndex);
            double edge = game.NextFightEdge;
            game.NextFightEdge = 0;
            game.Encounter = null;
            game.Encounter = new Encounter
            {
                Name = stage.Name, Boss = stage.Boss, Inverts = stage.Inverts, Endless = stage.Endless, Payout = stage.Payout,
                ComboStep = ComboStep, ComboCap = ComboCap, Pouch = ShuffleDeck(game),
                HandSize = (int)Rule(game, "hand_size", HandSize),
                EnemyScore = Math.Max(0, edge), Scored = Math.Max(0, -edge),
            };
            StartEnemy(game, stage);
            game.Player.Energy = game.Player.MaxEnergy;
            ApplyModifier(game);
            ApplyCharacterEncounterPerks(game, game.Encounter);
            TriggerRunHook(game, RunHookEvent.EncounterStart);
            game.Pending = null;
            game.LastResult = null;
            game.Phase = Phase.Encounter;
            var enemy = EnemyOf(game);
            Log(game, "Fight " + game.EncounterIndex + ": " + stage.Name + (enemy != null ? " against " + enemy.Name : "") + ".");
            foreach (var owned in game.Coins) Hooks.Grow(game, owned, CoinGrowthEvent.Level);
            Signal.Emit(GameSignal.EncounterStart, new GameEvent { Game = game, Encounter = game.Encounter });
            StartRound(game);
            DrawCoins(game, (int)CharacterPerkValue(game, CharacterPerkType.OpeningDraw)); // Second Wind: a bigger first hand
        }

        // The pouch a run starts with: every coin of the set (SetCopies of each, a coin at most MaxCopies times,
        // Normals exempt), then plain Normal coins up to PouchSize.
        static List<CoinDef> BuildPouch(IEnumerable<CoinDef> set)
        {
            var result = new List<CoinDef>();
            var counts = new Dictionary<string, int>();
            foreach (var coin in set)
                for (int k = 0; k < SetCopies; k++)
                {
                    counts.TryGetValue(coin.Id, out int n);
                    if (coin.Id != "normal" && n >= MaxCopies) break;
                    counts[coin.Id] = n + 1;
                    result.Add(coin);
                }
            while (result.Count < PouchSize) result.Add(CoinCatalog.Normal);
            return result;
        }

        // unlocked: extra coin ids the character may use. loadout: the starting set (at most StartMax coins, from
        // the character's pool plus unlocked coins); defaults to the character's signature coins. The run starts
        // with a pouch built from it (BuildPouch).
        public static GameState New(double seed, string characterId = null, List<string> unlocked = null,
            List<CoinDef> loadout = null, int stake = 1, bool applyRunEncounter = true, SandboxConfig sandbox = null, bool map = false)
        {
            characterId = characterId ?? "blade";
            if (!Content.Characters.ContainsKey(characterId)) throw new GameRuleException("unknown character: " + characterId);
            long normalized = Rng.Seed(seed);
            var game = new GameState
            {
                Seed = normalized, RngState = normalized, CharacterId = characterId, Phase = Phase.Encounter,
                Player = new Player { Gold = StartGold, Energy = 3, MaxEnergy = 3 },
                Unlocked = unlocked ?? new List<string>(), EncounterIndex = 1, Stake = Math.Max(1, Math.Min(8, stake)), Sandbox=sandbox,
            };
            var def = Content.Characters[characterId];
            game.Player.Gold = RuntimeMode.Dev ? 5000 : Rule(game, "start_gold", StartGold);
            if (loadout != null && sandbox == null)
            {
                if (loadout.Count < 1 || loadout.Count > StartMax)
                    throw new GameRuleException("loadout must have 1-" + StartMax + " coins");
                var allowed = new HashSet<CoinDef>(UsablePool(game));
                var copies = new Dictionary<string, int>();
                foreach (var coin in loadout)
                {
                    if (coin == null || !allowed.Contains(coin)) throw new GameRuleException("coin not available to this character: " + coin?.Id);
                    copies.TryGetValue(coin.Id, out int n);
                    copies[coin.Id] = n + 1;
                    if (copies[coin.Id] > Profile.RarityLimit(coin.Id)) throw new GameRuleException("too many " + coin.Id + " coins");
                }
            }
            var ids = sandbox?.Coins;
            if (ids != null) foreach (var id in ids) game.Coins.Add(NewCoin(game, id));
            else foreach (var coin in BuildPouch(loadout ?? def.Deck ?? new List<CoinDef> { def.Starter })) game.Coins.Add(NewCoin(game, coin));
            if (sandbox != null)
            {
                if (sandbox.Gold.HasValue) game.Player.Gold = sandbox.Gold.Value;
                if (sandbox.Energy.HasValue) game.Player.Energy = game.Player.MaxEnergy = sandbox.Energy.Value;
            }
            game.SelectedUid = game.Coins[0].Uid;
            Log(game, "Seed: " + normalized);
            Hooks.Unbind();
            Relics.Bind(game);
            Items.Clear();
            if (applyRunEncounter && !RuntimeMode.Sandbox)
            {
                game.SetRunEncounter(Encounters[Rng.Int(game, 1, Encounters.Count) - 1]);
                TriggerRunHook(game, RunHookEvent.RunStart);
            }
            if (map && sandbox == null) { game.Map = BuildMap(game); game.Phase = Phase.Map; } // the run starts on the map
            else StartEncounter(game);
            if(game.RunEncounter!=null) Log(game, "Run Encounter: " + game.RunEncounter.Name + ".");
            return game;
        }

        public static GameState NewSandbox(SandboxConfig config)
        {
            if (config == null) throw new GameRuleException("sandbox scene is missing");
            RuntimeMode.Configure(RuntimeMode.Dev, true);
            var game = New(config.Seed ?? DateTime.UtcNow.Ticks, config.Character, null, null, config.Stake, false, config);
            if (config.Gold.HasValue) game.Player.Gold = config.Gold.Value;
            if (config.Energy.HasValue) game.Player.Energy = game.Player.MaxEnergy = config.Energy.Value;
            return game;
        }

        public static void OpenSandboxShop(GameState game) => EnterShop(game);

        public static bool Select(GameState game, int uid)
        {
            if (GetCoin(game, uid) == null) return false;
            game.SelectedUid = uid;
            return true;
        }

        // The side that will count is decided here, before the UI animates it: relics may change the
        // outcome, then the boss inverts every 5th flip (an encounter rule, so it comes last).
        static void Finalize(GameState game, CoinInst item, FlipState flip)
        {
            var e = game.Encounter;
            int nth = e.Flips + 1;
            string before = flip.Result;
            var outcome = new GameEvent { Game = game, Inst = item, Flips = nth, Result = before };
            Signal.Emit(GameSignal.CoinOutcome, outcome);
            string final = outcome.Result;
            flip.Altered = final != before ? "RELIC" : null; // shown in the UI so a changed side is never a mystery
            int interval = (int)Rule(game, "boss_every", 5);
            bool stageInvert = !outcome.Final && final != Side.Tie && (e.Inverts || e.Boss && Rule(game, "boss_every", 0) > 0) && nth % interval == 0;
            bool clockInvert = final != Side.Tie && game.RunEncounter?.InvertsFlip?.Invoke(nth) == true && !stageInvert;
            if (stageInvert || clockInvert)
            {
                final = Side.Other(final);
                flip.Altered = (flip.Altered != null ? flip.Altered + " + " : "") + (clockInvert ? "THE HOUSE'S CLOCK" : "THE HOUSE");
            }
            flip.Result = final;
            game.RunFlips++; // luck report counts every toss, so a re-flip adds one (its odds are the same, so it stays unbiased)
            if (final == Side.Heads) game.RunHeads++;
            game.RunExpectedHeads += flip.Probability;
        }

        // Buffs: "the next N coins ..." effects. kind: "mult" (x amount on score and gold), "odds" (+amount
        // Heads), "swap" (use the other side's effects), "heads" (guaranteed Heads). A buff made while a coin
        // resolves is "fresh" and starts with the following coin; every resolved coin uses up one coin.
        public static void AddBuff(GameState game, string kind, double amount, int? coins, bool immediate = false, CoinInst source = null)
        {
            var buff = new Buff { Kind = kind, Amount = amount, Left = coins ?? 1,
                Fresh = !immediate, SourceUid = source?.Uid ?? 0 };
            if (source != null) source.Definition.On.Coins.RaiseBuffCreated(new HookContext(game, source), buff);
            game.Encounter.Buffs.Add(buff);
        }

        public static void AddBuff(GameState game, BuffSpec spec, bool immediate = false, CoinInst source = null)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            var buff = new Buff
            {
                Kind = "effect", SpecId = spec.Id, TargetType = spec.Target.Type,
                AppliesOn = spec.AppliesOn, AppliedEffect = spec.Effect.Copy(),
                Left = spec.Target.Count, Fresh = !immediate,
                SourceUid = source?.Uid ?? 0,
            };
            switch (spec.Effect.Type)
            {
                case EffectType.NextOdds: buff.Kind = "odds"; buff.Amount = spec.Effect.Amount; break;
                case EffectType.NextMult: buff.Kind = "mult"; buff.Amount = spec.Effect.Amount; break;
                case EffectType.NextSwap: buff.Kind = "swap"; break;
                case EffectType.NextHeads: buff.Kind = "heads"; break;
                case EffectType.TypeBuff:
                    if (!spec.Effect.Kind.HasValue) throw new ArgumentException("Type buffs require a coin type.", nameof(spec));
                    buff.Kind = DefinitionKeys.Key(spec.Effect.Kind.Value);
                    buff.TargetType = spec.Effect.Kind;
                    if (spec.Effect.Kind.Value == CoinType.Steel)
                    {
                        buff.Amount = 3;
                        buff.PenaltyAmount = 2;
                    }
                    break;
            }
            if (source != null) source.Definition.On.Coins.RaiseBuffCreated(new HookContext(game, source), buff);
            game.Encounter.Buffs.Add(buff);
        }

        static bool BuffTargets(Buff buff, CoinInst item) => !buff.TargetType.HasValue || HasType(item, buff.TargetType.Value);

        static Buff BuffActive(GameState game, string kind, CoinInst item)
        {
            foreach (var buff in game.Encounter.Buffs)
                if (buff.Kind == kind && !buff.Fresh && BuffTargets(buff, item)) return buff;
            return null;
        }

        static void TickBuffs(GameState game, CoinInst item)
        {
            var kept = new List<Buff>();
            foreach (var buff in game.Encounter.Buffs)
            {
                if (buff.Fresh) buff.Fresh = false;
                else if (buff.TargetType.HasValue ? HasType(item, buff.TargetType.Value) :
                    !IsTypeBuff(buff.Kind) || HasType(item, buff.Kind))
                {
                    buff.Left--;
                    if (buff.Kind != "effect" && !IsTypeBuff(buff.Kind))
                        Signal.Emit(GameSignal.BuffApplied, new GameEvent { Game = game, Inst = item, Buff = buff });
                }
                if (buff.Left > 0) kept.Add(buff);
            }
            game.Encounter.Buffs = kept;
        }

        static string RawSide(GameState game, FlipState flip)
        {
            double value = Rng.Random(game);
            return value < flip.Probability ? Side.Heads : value < flip.Probability + flip.TieProbability ? Side.Tie : Side.Tails;
        }

        // Roll the dice for a pending flip, then let on_flip hooks change the outcome.
        static void Roll(GameState game, CoinInst item, FlipState flip)
        {
            flip.Raw = flip.Result = RawSide(game, flip);
            flip.Forced = false;
            flip.Altered = null;
            if (game.Tutorial && game.TutorialHeads > 0)
            {
                game.TutorialHeads--;
                flip.Raw = flip.Result = Side.Heads;
                flip.Forced = true;
                flip.Altered = "TUTORIAL";
            }
            if (BuffActive(game, "heads", item) != null) { flip.Result = Side.Heads; flip.Altered = "BUFF"; }
            Signal.Emit(GameSignal.CoinFlip, new GameEvent { Game = game, Inst = item, Flip = flip });
            Finalize(game, item, flip);
        }

        // Flip one coin of your hand: choose any, in any order, as often as energy allows.
        public static bool Flip(GameState game, int uid)
        {
            if (!CanFlip(game, uid)) return false;
            var e = game.Encounter;
            var item = GetCoin(game, uid);
            double cost = FlipCost(game, uid);
            game.Player.Energy -= cost;
            e.Hand.Remove(uid); // the coin leaves the hand at once; flipped coins wait in the discard pile
            e.Discard.Add(uid);
            e.RoundFlips++;
            game.SelectedUid = uid;
            Hooks.Bind(game, item);
            Signal.Emit(GameSignal.CoinDeal, new GameEvent { Game = game, Inst = item });
            var odds = GetOdds(game, item); // on_deal may have changed the odds
            game.Pending = new FlipState { Uid = uid, CoinId = item.Id, Probability = odds.Heads,
                TieProbability = odds.Edge, OddsSampleTime = e.ElapsedSeconds };
            Roll(game, item, game.Pending);
            Log(game, CoinName(item) + " #" + uid + " rolled " + game.Pending.Raw +
                (game.Pending.Result != game.Pending.Raw ? " → " + game.Pending.Result : "") + ".");
            return true;
        }

        // Energy a coin costs to flip (0 for most coins).
        public static int FlipCost(GameState game, int uid) => Content.Coins[GetCoin(game, uid).Id].EnergyCost;

        public static bool CanFlip(GameState game, int uid)
        {
            if (game.Phase != Phase.Encounter || game.Pending != null || game.Encounter == null || game.Encounter.Cleared) return false;
            return game.Encounter.Hand.Contains(uid) && FlipCost(game, uid) <= game.Player.Energy;
        }

        // Discard coins from the hand (free). They wait in the discard pile until the pouch runs dry.
        // Returns how many were discarded.
        public static int Discard(GameState game, IList<int> uids)
        {
            var e = game.Encounter;
            if (game.Phase != Phase.Encounter || game.Pending != null || e == null || e.Cleared) return 0;
            int count = 0;
            foreach (int uid in uids)
            {
                if (!e.Hand.Remove(uid)) continue;
                e.Discard.Add(uid);
                e.Discards++;
                var inst = GetCoin(game, uid);
                Log(game, CoinName(inst) + " #" + uid + " discarded.");
                Hooks.Bind(game, inst); // so the coin's own on_discard hook runs
                Signal.Emit(GameSignal.CoinDiscard, new GameEvent { Game = game, Inst = inst });
                Hooks.Unbind();
                Hooks.Grow(game, inst, CoinGrowthEvent.Discard);
                count++;
            }
            return count;
        }

        // One re-flip per coin, 1 energy, while the result is pending (before Resolve). It re-rolls the raw
        // side from the same odds and re-runs Finalize (relics, boss inversion), nothing else: coin on_flip
        // hooks and armed chips already fired and are not repeated; Broken Clock and the boss every-Nth
        // inversion key off Encounter.Flips (bumped in Resolve), so only Lucky Penny's use stays spent.
        public static bool CanReroll(GameState game) =>
            game.Phase == Phase.Encounter && game.Pending != null && !game.Pending.Rerolled && !game.Tutorial && game.Player.Energy >= 1;

        public static bool Reroll(GameState game)
        {
            if (!CanReroll(game)) return false;
            var flip = game.Pending;
            var item = GetCoin(game, flip.Uid);
            game.Player.Energy -= 1;
            flip.Rerolled = true;
            flip.Raw = flip.Result = RawSide(game, flip);
            flip.Forced = false;
            if (BuffActive(game, "heads", item) != null) flip.Result = Side.Heads;
            Finalize(game, item, flip);
            Log(game, CoinName(item) + " #" + flip.Uid + " re-flipped: " + flip.Raw + (flip.Result != flip.Raw ? " → " + flip.Result : "") + ".");
            return true;
        }

        public static bool Force(GameState game, string side)
        {
            var result = game.Phase == Phase.Encounter ? game.Pending : null;
            if (result == null || game.Player.Energy < 2 || (side != Side.Heads && side != Side.Tails)) return false;
            game.Player.Energy -= 2;
            result.Result = side;
            result.Forced = true;
            Finalize(game, GetCoin(game, result.Uid), result);
            Log(game, CoinName(GetCoin(game, result.Uid)) + " forced to " + side);
            return true;
        }

        // Apply one effect to the running game; returns the log text. item may be null (an item's effect).
        public static string ApplyEffect(GameState game, CoinInst item, Effect effect)
        {
            var p = game.Player;
            var e = game.Encounter;
            switch (effect.Type)
            {
                case EffectType.Gold:
                    double gold = effect.Amount * e.GoldMult;
                    p.Gold += gold;
                    return "+" + N(gold) + " gold";
                case EffectType.GoldLoss:
                    double lost = Math.Min(p.Gold, effect.Amount); p.Gold -= lost; return "-" + N(lost) + " gold";
                case EffectType.Score:
                    e.Scored += effect.Amount;
                    return "+" + N(effect.Amount) + " points";
                case EffectType.Energy:
                    p.Energy += effect.Amount;
                    return "+" + N(effect.Amount) + " energy";
                case EffectType.AllOdds:
                    e.Magnet += effect.Amount; return "all coins +" + N(Math.Floor(effect.Amount * 100 + .5)) + "% Heads";
                case EffectType.FortuneOdds:
                    if (item.CompostLevel == game.EncounterIndex) return "Fortune already fertilized this level";
                    item.CompostLevel = game.EncounterIndex; game.FortuneBonus = Math.Min(.55, game.FortuneBonus + effect.Amount);
                    return "Fortune +" + N(Math.Floor(game.FortuneBonus * 100 + .5)) + "% Heads for the run";
                case EffectType.TypeBuff:
                    string type = DefinitionKeys.Key(effect.Kind.Value);
                    AddBuff(game, type, 0, effect.Coins, source: item); return "next " + (effect.Coins ?? 1) + " " + type + " coins";
                case EffectType.BankDiscard:
                    e.BankDiscards += (int)effect.Amount; return "discard one";
                case EffectType.DrawCoin:
                    int drawn = DrawCoins(game, (int)effect.Amount);
                    return drawn > 0 ? "drew " + drawn + (drawn == 1 ? " coin" : " coins") : "the pouch is empty";
                case EffectType.Amplify:
                    // every active buff lasts one coin longer and gets stronger (x2 -> x3, +20% -> +40% Heads)
                    foreach (var buff in e.Buffs)
                    {
                        buff.Left++;
                        if (buff.Kind == "mult") buff.Amount += 1;
                        else if (buff.Kind == "odds") buff.Amount = Math.Min(.6, buff.Amount * 2);
                    }
                    return "buffs amplified";
                case EffectType.ComboBonus:
                    e.ComboLen += effect.Amount;
                    return "combo +" + N(effect.Amount);
                case EffectType.ComboShield:
                    e.Shield += effect.Amount;
                    return "combo shield";
                case EffectType.NextMult:
                    AddBuff(game, "mult", effect.Amount, effect.Coins, source: item);
                    return "next " + (effect.Coins ?? 1) + " coins x" + N(effect.Amount);
                case EffectType.NextOdds:
                    AddBuff(game, "odds", effect.Amount, effect.Coins, source: item);
                    return "next " + (effect.Coins ?? 1) + " coins +" + N(Math.Floor(effect.Amount * 100 + .5)) + "% Heads";
                case EffectType.NextSwap:
                    AddBuff(game, "swap", 0, effect.Coins, source: item);
                    return "next coin uses its other side";
                case EffectType.NextHeads:
                    AddBuff(game, "heads", 0, effect.Coins, source: item);
                    return "next coin lands Heads";
                case EffectType.Penalty:
                    e.EnemyScore += effect.Amount; // a penalty is points for the enemy
                    return "enemy +" + N(effect.Amount);
                case EffectType.ExtraDraw:
                    {
                        // a flipped coin goes back into the pouch (the flipping coin itself, or a random flipped
                        // one when no coin is flipping), so it can be drawn again. Capped per fight.
                        int back = 0;
                        for (int k = 0; k < (int)effect.Amount; k++)
                        {
                            if (e.Returned >= ReturnCap) break;
                            int uid;
                            if (item != null) uid = item.Uid;
                            else
                            {
                                if (e.Discard.Count == 0) break;
                                uid = e.Discard[Rng.Int(game, 1, e.Discard.Count) - 1];
                            }
                            if (e.Hand.Contains(uid) || e.Pouch.Contains(uid) || !e.Discard.Remove(uid)) break;
                            e.Pouch.Insert(Rng.Int(game, 1, e.Pouch.Count + 1) - 1, uid);
                            e.Returned++;
                            back++;
                        }
                        return back > 0 ? back + " coin back in the pouch" : "no coin could return";
                    }
                case EffectType.FetchBest:
                    if (item.FetchedLevel == game.EncounterIndex) return "already fetched this fight";
                    int bestUid = 0; double bestScore = 0;
                    foreach (int uid in e.Discard) if (uid != item.Uid && e.BestScores.TryGetValue(uid, out var score) && score > bestScore) { bestUid = uid; bestScore = score; }
                    if (bestUid == 0 || e.Returned >= ReturnCap || !e.Discard.Remove(bestUid)) return "no scored coin to fetch";
                    e.Pouch.Insert(Rng.Int(game, 1, e.Pouch.Count + 1) - 1, bestUid); e.Returned++; item.FetchedLevel = game.EncounterIndex;
                    return CoinName(GetCoin(game, bestUid)) + " fetched back into the pouch";
                case EffectType.Probability:
                    e.Bonus.TryGetValue(item.Uid, out double bonus);
                    e.Bonus[item.Uid] = bonus + effect.Amount;
                    return "+" + N(Math.Floor(effect.Amount * 100 + .5)) + "% Heads this encounter";
            }
            throw new GameRuleException("unknown effect: " + effect.Type);
        }

        static string PickUnownedRelic(GameState game)
        {
            var owned = new HashSet<string>(game.Relics);
            var unowned = new List<string>();
            foreach (var id in Content.Relics.Keys)
                if (!owned.Contains(id)) unowned.Add(id);
            unowned.Sort(string.CompareOrdinal); // stable order so the seeded pick is deterministic
            return unowned.Count > 0 ? unowned[Rng.Int(game, 1, unowned.Count) - 1] : null;
        }

        // Stock the shop after a cleared level: four coins and one relic, all from the seeded RNG.
        static void EnterShop(GameState game)
        {
            game.Phase = Phase.Shop;
            game.RerollCost = game.Shop.RefreshCost;
            game.RerollStep = 2;
            SetShopStock(game);
            game.ShopRelic = PickUnownedRelic(game);
            var itemIds = new List<string>(Content.Items.Keys);
            itemIds.Sort(string.CompareOrdinal);
            game.ShopItems = Offers(game, itemIds, 2);
            Hooks.ShopOpened(game);
        }

        static ShopPurchase PrepareShopPurchase(GameState game, ShopPurchaseKind kind, string id, int cost)
        {
            var purchase = new ShopPurchase(game, kind, id, cost);
            if (!Hooks.BeforePurchase(game, purchase) || game.Player.Gold < purchase.Cost) return null;
            return purchase;
        }

        // The fight is over for good: you won it and move on (to the map or the shop; the boss ends the run).
        public static bool EndLevel(GameState game)
        {
            var e = game.Encounter;
            if (game.Phase != Phase.Encounter || !e.Cleared || game.Pending != null) return false;
            if (e.ComboPot > 0) BankComboPot(game, false);
            Hooks.Unbind();
            Items.Clear();
            Signal.Emit(GameSignal.EncounterEnd, new GameEvent { Game = game, Won = true });
            if (e.Boss) game.Phase = Phase.Victory;
            else if (game.Map != null && !game.Endless) ReturnToMap(game);
            else EnterShop(game);
            return true;
        }

        public static bool Resolve(GameState game)
        {
            if (game.Phase != Phase.Encounter || game.Pending == null) return false;
            var e = game.Encounter;
            var result = game.Pending;
            var item = GetCoin(game, result.Uid);
            var def = item.Definition;
            e.Flips++;
            string final = result.Result; // already decided (relics, boss inversion) when the coin was flipped
            result.Final = final;
            e.Results.Add(final);
            if (final == Side.Heads) { e.Streak++; e.TailsRun = 0; }
            else if (final == Side.Tails) { e.Streak = 0; e.TailsRun++; } // a Tie holds both
            // combo: consecutive identical results. A shield (Anchor) lets one different result pass without breaking it.
            string previousComboSide = e.ComboSide;
            bool tieBreaks = game.RunEncounter?.BreaksComboOnTie ?? false;
            if (final == Side.Tie && e.ComboSide != null && tieBreaks) { e.ComboSide = null; e.ComboLen = 0; }
            else if (final != Side.Tie)
            {
                if (e.ComboSide == final) e.ComboLen += 1;
                else if (e.ComboSide != null && e.Shield > 0) e.Shield -= 1;
                else { e.ComboSide = final; e.ComboLen = 1; }
            }
            e.BestComboLen = Math.Max(e.BestComboLen, e.ComboLen);
            bool comboBroke = previousComboSide != null && e.ComboSide != previousComboSide;
            if (comboBroke) e.ComboPot = 0;
            // hooks get a private copy of the effect list so they can edit it without touching the def
            var res = new Res { Result = final, Raw = result.Raw };
            bool swap = BuffActive(game, "swap", item) != null;
            bool headsEffects = final != Side.Tie && (swap ? final != Side.Heads : final == Side.Heads);
            var outcome = final == Side.Tie ? OutcomeSide.Edge : headsEffects ? OutcomeSide.Heads : OutcomeSide.Tails;
            foreach (var effect in def.EffectsFor(outcome)) res.Effects.Add(effect.Copy());
            double tailsGold = final == Side.Tails ? CharacterPerkValue(game, CharacterPerkType.TailsGold) : 0;
            if (tailsGold > 0) res.Effects.Add(Effect.Gold(tailsGold));
            Signal.Emit(GameSignal.CoinResolve, new GameEvent { Game = game, Inst = item, Res = res });
            foreach (var buff in def.BuffsFor())
                if (buff.Trigger == outcome) res.Buffs.Add(buff.Copy());
            foreach (var buff in res.Buffs) AddBuff(game, buff, source: item);
            foreach (var buff in e.Buffs)
                if (!buff.Fresh && buff.Kind == "effect" &&
                    (!buff.TargetType.HasValue || HasType(item, buff.TargetType.Value)) &&
                    (!buff.AppliesOn.HasValue || buff.AppliesOn == outcome) && buff.AppliedEffect != null)
                {
                    res.Effects.Add(buff.AppliedEffect.Copy());
                    Signal.Emit(GameSignal.BuffApplied, new GameEvent { Game = game, Inst = item, Buff = buff });
                }
            foreach (var augment in ActiveAugments(game)) augment.OnResolve(game, item, final, res);
            ApplyTypeBuffs(game, item, final, comboBroke, res.Effects);
            if(final==Side.Tails)e.Tails++;
            result.BaseEffects = new List<Effect>(); // what this coin did before multipliers (True Echo repeats it)
            foreach (var effect in res.Effects) result.BaseEffects.Add(effect.Copy());
            result.BaseBuffs = new List<BuffSpec>();
            foreach (var buff in res.Buffs) result.BaseBuffs.Add(buff.Copy());
            double multiplier = 1;
            foreach (var buff in e.Buffs)
                if (buff.Kind == "mult" && !buff.Fresh && BuffTargets(buff, item)) multiplier *= buff.Amount;
            double combo = Math.Min(e.ComboCap, 1 + e.ComboStep * (Math.Max(e.ComboLen, 1) - 1));
            if (res.CashOut) combo *= combo; // Cash Out spends the combo twice (then resets it)
            result.Combo = new Combo { Len = e.ComboLen, Mult = combo, Side = e.ComboSide };
            multiplier *= combo;
            if (multiplier != 1) // buffs ("next coins pay double") and the combo multiply points and gold
            {
                foreach (var effect in res.Effects)
                    if (effect.Type == EffectType.Score || effect.Type == EffectType.Gold) effect.Amount = Math.Floor(effect.Amount * multiplier + .5);
            }
            var messages = new List<string>();
            double scoredBefore = e.Scored, enemyBefore = e.EnemyScore;
            int greed = HasType(item, "greed") ? ActiveTypeBuffs(e, "greed") : 0;
            for (int i = 0; i < res.Effects.Count; i++)
            {
                var effect = res.Effects[i];
                double goldBefore = game.Player.Gold;
                double energyBefore = game.Player.Energy;
                double scoreEffectBefore = e.Scored;
                double enemyEffectBefore = e.EnemyScore;
                int returnedBefore = e.Returned;
                int buffsAffected = effect.Type == EffectType.Amplify ? e.Buffs.Count : 0;
                string text = ApplyEffect(game, item, effect);
                var applied = Signal.Emit(GameSignal.EffectApplied, new GameEvent
                {
                    Game = game, Inst = item, Effect = effect, Text = text,
                    ScoreDelta = e.Scored - scoreEffectBefore,
                    GoldDelta = game.Player.Gold - goldBefore,
                    EnergyDelta = game.Player.Energy - energyBefore,
                    PenaltyDelta = e.EnemyScore - enemyEffectBefore,
                    ReturnedDelta = e.Returned - returnedBefore,
                    BuffsAffected = buffsAffected,
                });
                messages.Add(applied.Text);
                double gainedGold = Math.Max(0, game.Player.Gold - goldBefore);
                if (greed > 0 && gainedGold > 0) { double extra = gainedGold * (Math.Pow(2, greed) - 1); game.Player.Gold += extra; messages.Add("+" + N(extra) + " greed gold"); }
            }
            if (greed > 0 && final == Side.Tails) { double loss = Math.Min(game.Player.Gold, 3 * greed); game.Player.Gold -= loss; messages.Add("-" + N(loss) + " greed gold"); }
            e.ComboPot = ComboPot(e.ComboLen);
            if (res.CashOut)
            {
                int banked = BankComboPot(game);
                if (banked > 0) messages.Add("banked " + banked + " combo gold");
                e.ComboSide = null; e.ComboLen = e.ComboPot = 0;
            }
            TickBuffs(game, item);
            result.Gained = e.Scored - scoredBefore; // for the UI: what this flip was worth
            result.Penalty = e.EnemyScore - enemyBefore;
            e.RoundLog.Add(new RoundFlip { Uid = item.Uid, CoinId = item.Id, Result = final, Points = result.Gained.Value, Penalty = result.Penalty.Value });
            e.BestScores.TryGetValue(item.Uid, out var previousBest); e.BestScores[item.Uid] = Math.Max(previousBest, result.Gained.Value);
            Signal.Emit(GameSignal.CoinResolved, new GameEvent { Game = game, Inst = item, Res = res, Flip = result });
            Hooks.Unbind();
            Hooks.Grow(game, item, CoinGrowthEvent.Flip);
            Log(game, def.Name + " #" + item.Uid + ": " + final + (final != result.Raw ? " (raw " + result.Raw + ")" : "") +
                " → " + (messages.Count > 0 ? string.Join(", ", messages) : "nothing"));
            game.LastResult = result;
            game.Pending = null;
            return true;
        }

        // The round is over: the enemy flips its coins, the hand refills, and after round 5 the fight is decided.
        public static bool EndRound(GameState game)
        {
            var e = game.Encounter;
            if (game.Phase != Phase.Encounter || e == null || e.Cleared || game.Pending != null) return false;
            EnemyTurn(game);
            e.RoundScores.Add(e.Scored - e.RoundStartScored);
            e.EnemyRoundScores.Add(e.EnemyScore - e.RoundStartEnemy);
            Log(game, "Round " + e.Round + " over: you " + N(e.Scored) + ", enemy " + N(e.EnemyScore) + ".");
            if (e.Round >= Rounds) { FinishFight(game); return true; }
            e.Round++;
            StartRound(game);
            return true;
        }

        // After round 5: more points than the enemy wins the fight; a tie goes to the House.
        static void FinishFight(GameState game)
        {
            var e = game.Encounter;
            var enemy = EnemyOf(game);
            if (e.Scored <= e.EnemyScore)
            {
                LoseLevel(game, (enemy != null ? enemy.Name + " won " : "lost ") + N(e.EnemyScore) + " to " + N(e.Scored) + ".");
                return;
            }
            e.Cleared = true;
            game.Cleared++;
            Log(game, "Fight won, " + N(e.Scored) + " to " + N(e.EnemyScore) + ".");
            if (!e.Boss)
            {
                game.Player.Gold += e.Payout ?? 0;
                Log(game, "+" + N(e.Payout ?? 0) + " gold payout.");
            }
            double owed = Math.Floor((e.Scored - e.EnemyScore) * SurplusRate); // the winning margin pays gold
            if (owed > 0)
            {
                game.Player.Gold += owed;
                e.SurplusPaid = owed;
                Log(game, "+" + N(owed) + " gold for the winning margin.");
            }
            if (e.Boss) EndLevel(game); // beating the boss ends the run
        }

        static void LoseLevel(GameState game, string why)
        {
            game.Phase = Phase.GameOver;
            game.LostWhy = why;
            Log(game, "Defeat: " + why);
            Hooks.Unbind();
            Items.Clear();
            Signal.Emit(GameSignal.EncounterEnd, new GameEvent { Game = game, Won = false });
        }

        // Give the player a relic and (re)bind every owned relic to the event bus.
        public static void AddRelic(GameState game, string id)
        {
            game.Relics.Add(id);
            Relics.Bind(game);
        }

        public static bool BuyItem(GameState game, int index)
        {
            string id = game.Phase == Phase.Shop && index >= 0 && index < game.ShopItems.Count ? game.ShopItems[index] : null;
            if (id == null || game.Items.Count >= Items.Max) return false;
            var purchase = PrepareShopPurchase(game, ShopPurchaseKind.Item, id, Price(game, Content.Items[id].Cost));
            if (purchase == null) return false;
            game.ShopItems[index] = null;
            game.Player.Gold -= purchase.Cost;
            game.Items.Add(id);
            Log(game, "Bought " + Content.Items[id].Name + " for " + purchase.Cost + " gold.");
            Hooks.AfterPurchase(game, purchase);
            return true;
        }

        public static bool UseItem(GameState game, int slot) => Items.Use(game, slot);

        public const int RelicCost = 25;

        public static bool BuyRelic(GameState game)
        {
            if (game.Phase != Phase.Shop || game.ShopRelic == null) return false;
            var purchase = PrepareShopPurchase(game, ShopPurchaseKind.Relic, game.ShopRelic, Price(game, RelicCost));
            if (purchase == null) return false;
            game.Player.Gold -= purchase.Cost;
            AddRelic(game, game.ShopRelic);
            Log(game, "Bought relic " + Content.Relics[game.ShopRelic].Name + " for " + purchase.Cost + " gold.");
            game.ShopRelic = null;
            Hooks.AfterPurchase(game, purchase);
            return true;
        }

        public static int CoinCost(string id) => Content.Coins[id].Cost;

        public static bool Buy(GameState game, int index)
        {
            var coin = game.Phase == Phase.Shop && index >= 0 && index < game.ShopOffers.Count ? game.ShopOffers[index] : null;
            if (coin == null || game.Coins.Count >= DeckMax) return false;
            var purchase = PrepareShopPurchase(game, ShopPurchaseKind.Coin, coin.Id, CoinOfferCost(game, index));
            if (purchase == null) return false;
            game.ShopOffers[index] = null;
            game.Purchased.Add(coin.Id); // the UI turns purchases of locked coins into permanent unlocks
            game.Player.Gold -= purchase.Cost;
            AddToDeck(game, coin);
            Log(game, "Bought " + coin.Name + " for " + purchase.Cost + " gold.");
            Hooks.AfterPurchase(game, purchase);
            return true;
        }

        // Rerolling the coin offers costs 4 gold, 2 more each time within one shop visit.
        public static bool RerollShop(GameState game)
        {
            if (game.Phase != Phase.Shop) return false;
            int cost = game.RerollCost;
            int nextCost = cost + game.RerollStep;
            var purchase = PrepareShopPurchase(game, ShopPurchaseKind.Reroll, "reroll", cost);
            if (purchase == null) return false;
            game.Player.Gold -= purchase.Cost;
            game.RerollCost = nextCost;
            SetShopStock(game);
            Log(game, "Shop rerolled for " + purchase.Cost + " gold.");
            Hooks.AfterPurchase(game, purchase);
            return true;
        }

        public static bool BuyEnergy(GameState game)
        {
            if (game.Phase != Phase.Shop || game.Player.MaxEnergy >= 5) return false;
            var purchase = PrepareShopPurchase(game, ShopPurchaseKind.Energy, "energy", 20);
            if (purchase == null) return false;
            game.Player.Gold -= purchase.Cost;
            game.Player.MaxEnergy += 1;
            Log(game, "Bought +1 maximum energy for " + purchase.Cost + " gold.");
            Hooks.AfterPurchase(game, purchase);
            return true;
        }

        public static bool Remove(GameState game, int uid)
        {
            if (game.Phase != Phase.Shop || game.Coins.Count <= 1) return false;
            for (int index = 0; index < game.Coins.Count; index++)
            {
                var item = game.Coins[index];
                if (item.Uid != uid) continue;
                var purchase = PrepareShopPurchase(game, ShopPurchaseKind.CoinRemoval, item.Id, 8);
                if (purchase == null) return false;
                game.Coins.RemoveAt(index);
                game.Player.Gold -= purchase.Cost;
                game.SelectedUid = game.Coins[0].Uid;
                Log(game, "Removed " + CoinName(item) + " for " + purchase.Cost + " gold.");
                Hooks.AfterPurchase(game, purchase);
                return true;
            }
            return false;
        }

        public static bool LeaveShop(GameState game)
        {
            if (game.Phase != Phase.Shop) return false;
            return NextEncounter(game);
        }

        // After the boss: keep going through endless levels (the run is over only when you lose one).
        public static bool ContinueEndless(GameState game)
        {
            if (game.Phase != Phase.Victory || game.Endless) return false;
            game.Endless = true;
            if (game.Map != null) game.Cleared = Route.Count; // levels cleared beyond the boss count from here
            EnterShop(game);
            return true;
        }

        public static bool NextEncounter(GameState game)
        {
            if (game.Phase != Phase.Shop || ActiveCount(game) == 0) return false;
            Hooks.ShopClosed(game);
            if (game.Map != null && !game.Endless) { game.Phase = Phase.Map; return true; }
            int next = game.EncounterIndex + 1;
            if (!game.Endless && OfferAugment(game, next)) return true;
            game.EncounterIndex = next;
            StartEncounter(game);
            return true;
        }

        // Meta currency earned by a run: 1 per level cleared, +3 for beating the boss.
        public static int RunTokens(GameState game) => game.Cleared + (game.Phase == Phase.Victory ? 3 : 0);

        // Tokens to unlock a locked coin outside a run. Smart-bot runs average ~2.4 tokens, so a Rare costs ~4 runs.
        public static int TokenPrice(CoinDef coin)
        {
            switch (coin.Rarity) { case Rarity.Common: return 3; case Rarity.Uncommon: return 5; case Rarity.Rare: return 9; default: return 15; }
        }
    }
}
