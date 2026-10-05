using System;
using System.Collections.Generic;

namespace Tossup
{
    public sealed class StageDef
    {
        public string Name;
        public double PerCoin;
        public double? Payout;
        public bool Boss, Inverts;
        public int? Endless;
    }

    // The rules. Pure C# (no UnityEngine), so the whole game can be simulated and tested outside Unity.
    public static partial class Game
    {
        public const int Visible = 3; // coins shown in the bank; the first one is the coin you are about to play
        public const int MulliganSize = 5; // coins drawn at the start of a level, from which you may discard
        public const int StartMax = 5; // starting deck slots; more can be bought in shops
        public const int DeckMax = 10; // the shop cannot grow the deck past this: buying is refused when it is full
        public const int SlotCost = 5, SlotStep = 2;
        public const int ExchangeBase = 7; // gold for the first exchange of a level (empty stack): played coins come back
        public const int ExchangeStep = 5; // every further exchange in the same level costs this much more
        public const int ExchangeGain = 3; // played coins that come back into the stack in exchange
        public const int ExchangeMax = 3;
        public const double ComboStep = 0.25, ComboCap = 3; // combo: x1 + 0.25 per extra same result in a row, up to x3
        public const int ReturnCap = 3; // "extra draw" effects (a coin returning to the pile) per level
        public const int StartGold = 5;
        public const double SurplusRate = .5; // gold per point scored beyond the quota (rounded down in total)
        public const int MaxCopies = 3; // copies of one coin in a set; the plain Normal coin is exempt (up to the set size)

        public static readonly List<StageDef> Route = new List<StageDef>
        {
            new StageDef { Name = "Opening", PerCoin = 0.7, Payout = 25 },
            new StageDef { Name = "Second Chance", PerCoin = 1.4, Payout = 30 },
            new StageDef { Name = "High Stakes", PerCoin = 2.5, Payout = 20 },
            new StageDef { Name = "Rising Tide", PerCoin = 3.2, Payout = 35 },
            new StageDef { Name = "Double Down", PerCoin = 3.9, Payout = 40 },
            new StageDef { Name = "Last Call", PerCoin = 4.5, Payout = 45 },
            new StageDef { Name = "Final Table", PerCoin = 5.2, Payout = 50 },
            new StageDef { Name = "The House", PerCoin = 6.0, Boss = true },
        };

        public static void Log(GameState game, string message) => game.Log.Add(message);

        static string N(double v) => GameText.Num(v);
        static string CoinName(CoinInst inst) => Content.Coins[inst.Id].Name;

        static CoinInst NewCoin(GameState game, string id)
        {
            if (!Content.Coins.ContainsKey(id)) throw new GameRuleException("unknown coin: " + id);
            game.NextUid++;
            return new CoinInst { Uid = game.NextUid, Id = id, Bonus = 0 };
        }

        public static CoinInst GetCoin(GameState game, int uid)
        {
            foreach (var item in game.Coins)
                if (item.Uid == uid) return item;
            return null;
        }

        public static int ActiveCount(GameState game) => game.Coins.Count;

        static CoinInst AddToDeck(GameState game, string id)
        {
            var item = NewCoin(game, id);
            game.Coins.Add(item);
            game.SelectedUid = item.Uid;
            return item;
        }

        static List<string> Offers(GameState game, List<string> pool, int count)
        {
            var choices = new List<string>();
            var remaining = new List<string>(pool);
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
        static List<string> UsablePool(GameState game)
        {
            var pool = new List<string>();
            var seen = new HashSet<string>();
            foreach (var id in Content.Characters[game.CharacterId].Pool) { pool.Add(id); seen.Add(id); }
            foreach (var id in game.Unlocked)
                if (seen.Add(id)) pool.Add(id);
            return pool;
        }

        // Every coin the shop may offer this character, including coins that are still locked: buying a
        // locked coin in the shop is what unlocks it (see game.Purchased).
        static List<string> ShopPool(GameState game)
        {
            var pool = UsablePool(game);
            var seen = new HashSet<string>(pool);
            foreach (var entry in Content.Characters[game.CharacterId].Locked)
                if (seen.Add(entry.Id)) pool.Add(entry.Id);
            return pool;
        }

        // Four distinct coin offers from the whole coin list.
        static List<string> ShopStock(GameState game) => Offers(game, ShopPool(game), 4);

        // Draw pile: every owned coin that is not discarded this level and not already waiting in the bank.
        static List<int> ShuffleDeck(GameState game)
        {
            var pile = new List<int>();
            var e = game.Encounter;
            var held = new HashSet<int>();
            if (e != null) foreach (var uid in e.Queue) held.Add(uid);
            if (game.Mulligan != null) foreach (var uid in game.Mulligan.Hand) held.Add(uid);
            if (game.Pending != null) held.Add(game.Pending.Uid); // still being flipped
            foreach (var item in game.Coins)
                if ((e == null || !e.Discarded.Contains(item.Uid)) && !held.Contains(item.Uid)) pile.Add(item.Uid);
            for (int i = pile.Count; i >= 2; i--)
            {
                int j = Rng.Int(game, 1, i);
                (pile[i - 1], pile[j - 1]) = (pile[j - 1], pile[i - 1]);
            }
            return pile;
        }

        public static double Probability(GameState game, CoinInst item)
        {
            if (game.Sandbox != null && game.Sandbox.Odds.TryGetValue(item.Id, out var sandboxOdds)) return sandboxOdds.Heads;
            double bonus = 0, magnet = 0, boost = 0;
            var e = game.Encounter;
            if (game.Phase == Phase.Encounter && e != null)
            {
                if (e.Bonus.TryGetValue(item.Uid, out double b)) bonus = b;
                magnet = e.Magnet;
                foreach (var buff in e.Buffs)
                    if (buff.Kind == "odds" && !buff.Fresh) boost += buff.Amount;
            }
            var def = Content.Coins[item.Id];
            double upgrade = item.Upgrade != null && def.Upgrades.TryGetValue(item.Upgrade, out var up) ? up.HeadsProbability : 0;
            double fortune = HasType(item, "fortune") ? game.FortuneBonus : 0;
            double p = def.Probability + item.Bonus + bonus + magnet + boost + upgrade + fortune;
            p = Hooks.Odds(game, item, p);
            if (e != null && e.Contract != null && Contracts.TryGetValue(e.Contract.Id, out var contract)) p -= contract.HeadsPenalty;
            return Math.Max(0, Math.Min(1 - def.TieProbability, p));
        }

        public static double TieProbability(GameState game, CoinInst item)
        {
            if (game.Sandbox != null && game.Sandbox.Odds.TryGetValue(item.Id, out var sandboxOdds)) return sandboxOdds.Tie;
            return Content.Coins[item.Id].TieProbability;
        }

        // Top the bank up to Visible coins from the draw pile. The pile is never reshuffled: a level lasts
        // exactly as long as the coins in your stack (bank + pile).
        static void Refill(GameState game)
        {
            var e = game.Encounter;
            while (e.Queue.Count < Visible)
            {
                if (e.Pile.Count == 0 && game.Reshuffle) e.Pile = ShuffleDeck(game);
                if (e.Pile.Count == 0) return;
                e.Queue.Add(e.Pile[0]);
                e.Pile.RemoveAt(0);
            }
        }

        public static int CoinsLeft(GameState game) => game.Encounter.Queue.Count + game.Encounter.Pile.Count;

        // A level of the run. Beyond the route (after the boss) the levels are endless: each asks 0.5 more
        // points per coin than the one before, pays more, and inverts every 5th flip like The House.
        public static StageDef Stage(int level)
        {
            if (level >= 1 && level <= Route.Count) return Route[level - 1];
            int k = level - Route.Count;
            return new StageDef
            {
                Name = "Endless", Endless = k, PerCoin = Route[Route.Count - 1].PerCoin + 0.5 * k,
                Payout = 40 + 5 * k, Inverts = true,
            };
        }

        // A level's quota scales with the size of your deck, because a level lasts exactly as long as your
        // stack: a bigger deck means more flips.
        public static double QuotaFor(int level, int coinCount, double mult = 1) =>
            Math.Max(1, Math.Floor(Stage(level).PerCoin * coinCount * mult + .5));

        static void StartEncounter(GameState game)
        {
            var stage = Stage(game.EncounterIndex);
            double quota = DeckQuota(game) + game.NextLevelQuotaBonus;
            game.NextLevelQuotaBonus = 0;
            game.Encounter = null; // discards from the previous level must not carry over
            var pile = ShuffleDeck(game);
            game.Encounter = new Encounter
            {
                Name = stage.Name, Quota = quota, MaxQuota = quota, Boss = stage.Boss, Inverts = stage.Boss || stage.Inverts,
                Endless = stage.Endless, Payout = stage.Payout, ComboStep = ComboStep, ComboCap = ComboCap, Pile = pile,
            };
            game.Player.Energy = game.Player.MaxEnergy;
            ApplyModifier(game);
            TriggerRunHook(game, "encounter_start");
            game.Pending = null;
            game.LastResult = null;
            game.Phase = Phase.Encounter;
            Log(game, "Encounter " + game.EncounterIndex + ": " + stage.Name + " (quota " + N(quota) + ")");
            foreach (var owned in game.Coins) Hooks.Grow(owned, "level");
            Signal.Emit("encounter_start", new GameEvent { Game = game, Encounter = game.Encounter });
            // mulligan: draw a hand to look at; the UI lets the player discard before play starts
            var hand = new List<int>();
            int draw = Math.Min(MulliganSize, game.Encounter.Pile.Count);
            for (int k = 0; k < draw; k++)
            {
                hand.Add(game.Encounter.Pile[0]);
                game.Encounter.Pile.RemoveAt(0);
            }
            game.Mulligan = new Mulligan { Hand = hand };
            game.Dealt = null;
            if (!game.ManualMulligan) MulliganDone(game);
        }

        // Deal the coin at the front of the bank; the player may discard it or flip it.
        static void Deal(GameState game, int? preferredUid = null)
        {
            var e = game.Encounter;
            Refill(game);
            if (preferredUid.HasValue && !e.Queue.Contains(preferredUid.Value) && e.Pile.Remove(preferredUid.Value))
                e.Queue.Add(preferredUid.Value);
            if (e.Queue.Count == 0)
            {
                game.Dealt = null;
                Hooks.Unbind();
                StackEmpty(game);
                return;
            }
            int uid = preferredUid.HasValue && e.Queue.Contains(preferredUid.Value) ? preferredUid.Value : e.Queue[0];
            var inst = GetCoin(game, uid);
            game.Dealt = new FlipState { Uid = uid };
            game.SelectedUid = uid;
            Log(game, CoinName(inst) + " #" + uid + " dealt.");
            game.Peek = null;
            Hooks.Bind(game, inst);
            Signal.Emit("coin_deal", new GameEvent { Game = game, Inst = inst });
            game.Dealt.Probability = Probability(game, inst); // on_deal may have changed the odds
            game.Dealt.TieProbability = TieProbability(game, inst);
        }

        // Discard coins from the opening hand (free). They stay out of play for the level, and at least
        // one coin must be kept. Returns how many were discarded.
        public static int MulliganDiscard(GameState game, IList<int> uids)
        {
            var m = game.Mulligan;
            var e = game.Encounter;
            if (m == null) return 0;
            int count = 0;
            foreach (int uid in uids)
            {
                for (int index = 0; index < m.Hand.Count; index++)
                {
                    if (m.Hand[index] == uid && m.Hand.Count > 1)
                    {
                        m.Hand.RemoveAt(index);
                        e.Discarded.Add(uid);
                        e.Discards++;
                        var inst = GetCoin(game, uid);
                        Log(game, CoinName(inst) + " #" + uid + " discarded from the opening hand.");
                        Hooks.Bind(game, inst);
                        Signal.Emit("coin_discard", new GameEvent { Game = game, Inst = inst });
                        Hooks.Unbind();
                        Hooks.Grow(inst, "discard");
                        count++;
                        break;
                    }
                }
            }
            return count;
        }

        // Keep the remaining hand as the bank and start the level.
        public static bool MulliganDone(GameState game)
        {
            var m = game.Mulligan;
            if (m == null) return false;
            game.Encounter.Queue = m.Hand;
            game.Mulligan = null;
            Deal(game);
            return true;
        }

        // unlocked: extra coin ids the character may use. loadout: coin ids to start with (at most
        // StartMax, from the character's pool plus unlocked coins); defaults to the character's deck.
        // manualMulligan: the UI sets this and calls MulliganDone itself.
        public static GameState New(double seed, string characterId = null, List<string> unlocked = null,
            List<string> loadout = null, bool manualMulligan = false, int stake = 1, bool applyRunEncounter = true, SandboxConfig sandbox = null)
        {
            characterId = characterId ?? "blade";
            if (!Content.Characters.ContainsKey(characterId)) throw new GameRuleException("unknown character: " + characterId);
            long normalized = Rng.Seed(seed);
            var game = new GameState
            {
                Seed = normalized, RngState = normalized, CharacterId = characterId, Phase = Phase.Encounter,
                Player = new Player { Gold = StartGold, Energy = 3, MaxEnergy = 3 },
                Unlocked = unlocked ?? new List<string>(), EncounterIndex = 1, Slots = StartMax, Stake = Math.Max(1, Math.Min(8, stake)), Sandbox=sandbox,
            };
            var def = Content.Characters[characterId];
            game.Player.Gold = RuntimeMode.Dev ? 5000 : Rule(game, "start_gold", StartGold);
            game.ManualMulligan = manualMulligan;
            if (loadout != null && sandbox == null)
            {
                if (loadout.Count < 1 || loadout.Count > StartMax)
                    throw new GameRuleException("loadout must have 1-" + StartMax + " coins");
                var allowed = new HashSet<string>(UsablePool(game));
                var copies = new Dictionary<string, int>();
                foreach (var id in loadout)
                {
                    if (!allowed.Contains(id)) throw new GameRuleException("coin not available to this character: " + id);
                    string rarity=Content.Coins[id].Rarity;copies.TryGetValue(rarity, out int n);copies[rarity] = n + 1;
                    if (copies[rarity] > Profile.RarityLimit(id)) throw new GameRuleException("too many " + rarity + " coins");
                }
            }
            var ids = sandbox?.Coins ?? loadout ?? def.Deck ?? new List<string> { def.Starter };
            foreach (var id in ids) game.Coins.Add(NewCoin(game, id));
            game.SelectedUid = game.Coins[0].Uid;
            Log(game, "Seed: " + normalized);
            Hooks.Unbind();
            Relics.Bind(game);
            Items.Clear();
            if (applyRunEncounter && !RuntimeMode.Sandbox)
            {
                game.RunEncounterId = EncounterOrder[Rng.Int(game, 1, EncounterOrder.Count) - 1];
                TriggerRunHook(game, "run_start");
            }
            StartEncounter(game);
            if(game.RunEncounterId!=null) Log(game, "Run Encounter: " + Encounters[game.RunEncounterId].Name + ".");
            return game;
        }

        public static GameState NewSandbox(SandboxConfig config)
        {
            if (config == null) throw new GameRuleException("sandbox scene is missing");
            RuntimeMode.Configure(RuntimeMode.Dev, true);
            var game = New(config.Seed ?? DateTime.UtcNow.Ticks, config.Character, null, null, false, config.Stake, false, config);
            game.ContractsEnabled = false;
            game.Slots = Math.Max(game.Slots, game.Coins.Count);
            if (config.Gold.HasValue) game.Player.Gold = config.Gold.Value;
            if (config.Energy.HasValue) game.Player.Energy = game.Player.MaxEnergy = config.Energy.Value;
            if (game.Dealt != null)
            {
                var item = GetCoin(game, game.Dealt.Uid);
                game.Dealt.Probability = Probability(game, item);
                game.Dealt.TieProbability = TieProbability(game, item);
            }
            return game;
        }

        public static void OpenSandboxShop(GameState game) => EnterShop(game);

        public static bool Select(GameState game, int uid)
        {
            if (GetCoin(game, uid) == null) return false;
            game.SelectedUid = uid;
            if (game.Phase == Phase.Encounter && game.Pending == null && game.Mulligan == null && game.Encounter != null &&
                (game.Encounter.Queue.Contains(uid) || game.Encounter.Pile.Contains(uid)) && (game.Dealt == null || game.Dealt.Uid != uid))
                Deal(game, uid);
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
            Signal.Emit("coin_outcome", outcome);
            string final = outcome.Result;
            flip.Altered = final != before ? "RELIC" : null; // shown in the UI so a changed side is never a mystery
            int interval = (int)Rule(game, "boss_every", 5);
            bool stageInvert = final != Side.Tie && (e.Boss || e.Inverts) && nth % interval == 0;
            bool clockInvert = final != Side.Tie && game.RunEncounterId == "house_clock" && nth % 3 == 0 && !stageInvert;
            if (stageInvert || clockInvert)
            {
                final = Side.Other(final);
                flip.Altered = (flip.Altered != null ? flip.Altered + " + " : "") + (clockInvert ? "THE HOUSE'S CLOCK" : "THE HOUSE");
            }
            flip.Result = final;
        }

        // Buffs: "the next N coins ..." effects. kind: "mult" (x amount on score and gold), "odds" (+amount
        // Heads), "swap" (use the other side's effects), "heads" (guaranteed Heads). A buff made while a coin
        // resolves is "fresh" and starts with the following coin; every resolved coin uses up one coin.
        public static void AddBuff(GameState game, string kind, double amount, int? coins, bool immediate = false)
        {
            game.Encounter.Buffs.Add(new Buff { Kind = kind, Amount = amount, Left = coins ?? 1, Fresh = !immediate });
        }

        static Buff BuffActive(GameState game, string kind)
        {
            foreach (var buff in game.Encounter.Buffs)
                if (buff.Kind == kind && !buff.Fresh) return buff;
            return null;
        }

        static void TickBuffs(GameState game, CoinInst item)
        {
            var kept = new List<Buff>();
            foreach (var buff in game.Encounter.Buffs)
            {
                if (buff.Fresh) buff.Fresh = false;
                else if (!IsTypeBuff(buff.Kind) || HasType(item, buff.Kind)) buff.Left--;
                if (buff.Left > 0) kept.Add(buff);
            }
            game.Encounter.Buffs = kept;
        }

        // Roll the dice for a pending flip, then let on_flip hooks change the outcome.
        static void Roll(GameState game, CoinInst item, FlipState flip)
        {
            double value = Rng.Random(game);
            flip.Raw = value < flip.Probability ? Side.Heads : value < flip.Probability + flip.TieProbability ? Side.Tie : Side.Tails;
            flip.Result = flip.Raw;
            flip.Forced = false;
            flip.Altered = null;
            if (game.Tutorial && game.TutorialHeads > 0)
            {
                game.TutorialHeads--;
                flip.Raw = flip.Result = Side.Heads;
                flip.Forced = true;
                flip.Altered = "TUTORIAL";
            }
            if (BuffActive(game, "heads") != null) { flip.Result = Side.Heads; flip.Altered = "BUFF"; }
            Signal.Emit("coin_flip", new GameEvent { Game = game, Inst = item, Flip = flip });
            Finalize(game, item, flip);
        }

        public static bool Flip(GameState game)
        {
            if (game.Phase != Phase.Encounter || game.Pending != null || game.Dealt == null) return false;
            if (!CanFlip(game)) return false;
            int uid = game.Dealt.Uid;
            double probability = game.Dealt.Probability, tieProbability = game.Dealt.TieProbability;
            var item = GetCoin(game, uid);
            game.Player.Energy -= Math.Min(FlipCost(game, uid), game.Player.Energy);
            game.Pending = new FlipState { Uid = uid, Probability = probability, TieProbability = tieProbability };
            game.Dealt = null;
            game.Encounter.Queue.Remove(uid); // the selected coin leaves the bank at once
            game.Encounter.Played.Add(uid);
            Refill(game);
            Roll(game, item, game.Pending);
            Log(game, CoinName(item) + " #" + uid + " rolled " + game.Pending.Raw +
                (game.Pending.Result != game.Pending.Raw ? " → " + game.Pending.Result : "") + ".");
            return true;
        }

        // Energy a coin costs to flip (0 for most coins).
        public static int FlipCost(GameState game, int uid) => Content.Coins[GetCoin(game, uid).Id].EnergyCost;

        // Can the dealt coin be flipped right now? A coin you cannot pay for must be discarded (free) --
        // unless it is the last usable coin, which always flips so a level can never dead-end.
        public static bool CanFlip(GameState game)
        {
            if (game.Phase != Phase.Encounter || game.Pending != null || game.Dealt == null) return false;
            var e = game.Encounter;
            return FlipCost(game, game.Dealt.Uid) <= game.Player.Energy || game.Coins.Count - e.Discards <= 1;
        }

        // Discard coins from the bank for the rest of the level. Free. With no list the front coin goes.
        // At least one coin must stay usable. Returns how many were discarded.
        public static int Discard(GameState game, IList<int> uids = null)
        {
            var e = game.Encounter;
            if (game.Phase != Phase.Encounter || game.Pending != null || game.Mulligan != null || game.Dealt == null) return 0;
            var inBank = new HashSet<int>(e.Queue);
            foreach (var uid in e.Pile) inBank.Add(uid);
            var targets = new List<int>();
            var seen = new HashSet<int>();
            foreach (int uid in uids ?? new List<int> { game.Dealt.Uid })
                if (inBank.Contains(uid) && seen.Add(uid)) targets.Add(uid);
            if (targets.Count == 0 || game.Coins.Count - e.Discards - targets.Count < 1) return 0;
            int front = game.Dealt.Uid;
            Hooks.Unbind();
            foreach (int uid in targets)
            {
                e.Queue.Remove(uid); e.Pile.Remove(uid); e.DealHooksFired.Remove(uid);
                e.Discarded.Add(uid);
                e.Discards++;
                var inst = GetCoin(game, uid);
                Log(game, CoinName(inst) + " #" + uid + " discarded for this level.");
                Hooks.Bind(game, inst); // so the coin's own on_discard hook runs even if it was not the front coin
                Signal.Emit("coin_discard", new GameEvent { Game = game, Inst = inst });
                Hooks.Unbind();
                Hooks.Grow(inst, "discard");
                TriggerRunHook(game, "discard");
            }
            if (seen.Contains(front)) Deal(game);
            else
            {
                Refill(game);
                Hooks.Bind(game, GetCoin(game, front)); // the dealt coin stays dealt; restore its hooks
            }
            return targets.Count;
        }

        public static bool Reroll(GameState game)
        {
            var result = game.Phase == Phase.Encounter ? game.Pending : null;
            if (result == null || game.Player.Energy < 1) return false;
            game.Player.Energy -= 1;
            Roll(game, GetCoin(game, result.Uid), result);
            Log(game, CoinName(GetCoin(game, result.Uid)) + " rerolled: " + result.Result);
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
                case "gold":
                    double gold = effect.Amount * e.GoldMult;
                    p.Gold += gold;
                    return "+" + N(gold) + " gold";
                case "gold_loss":
                    double lost = Math.Min(p.Gold, effect.Amount); p.Gold -= lost; return "-" + N(lost) + " gold";
                case "score":
                    e.Quota = Math.Max(0, e.Quota - effect.Amount); // quota is what is still missing
                    e.Scored += effect.Amount; // points earned this level (may overshoot on the last flip)
                    return "+" + N(effect.Amount) + " points";
                case "energy":
                    p.Energy += effect.Amount;
                    return "+" + N(effect.Amount) + " energy";
                case "all_odds":
                    e.Magnet += effect.Amount; return "all coins +" + N(Math.Floor(effect.Amount * 100 + .5)) + "% Heads";
                case "fortune_odds":
                    if (item.CompostLevel == game.EncounterIndex) return "Fortune already fertilized this level";
                    item.CompostLevel = game.EncounterIndex; game.FortuneBonus = Math.Min(.55, game.FortuneBonus + effect.Amount);
                    return "Fortune +" + N(Math.Floor(game.FortuneBonus * 100 + .5)) + "% Heads for the run";
                case "type_buff":
                    AddBuff(game, effect.Kind, 0, effect.Coins); return "next " + (effect.Coins ?? 1) + " " + effect.Kind + " coins";
                case "bank_discard":
                    e.BankDiscards += (int)effect.Amount; return "discard one";
                case "extra_exchange":
                    e.ExtraExchanges += (int)effect.Amount; return "+" + N(effect.Amount) + " exchange";
                case "amplify":
                    // every active buff lasts one coin longer and gets stronger (x2 -> x3, +20% -> +40% Heads)
                    foreach (var buff in e.Buffs)
                    {
                        buff.Left++;
                        if (buff.Kind == "mult") buff.Amount += 1;
                        else if (buff.Kind == "odds") buff.Amount = Math.Min(.6, buff.Amount * 2);
                    }
                    return "buffs amplified";
                case "combo_bonus":
                    e.ComboLen += effect.Amount;
                    return "combo +" + N(effect.Amount);
                case "combo_shield":
                    e.Shield += effect.Amount;
                    return "combo shield";
                case "next_mult":
                    AddBuff(game, "mult", effect.Amount, effect.Coins);
                    return "next " + (effect.Coins ?? 1) + " coins x" + N(effect.Amount);
                case "next_odds":
                    AddBuff(game, "odds", effect.Amount, effect.Coins);
                    return "next " + (effect.Coins ?? 1) + " coins +" + N(Math.Floor(effect.Amount * 100 + .5)) + "% Heads";
                case "next_swap":
                    AddBuff(game, "swap", 0, effect.Coins);
                    return "next coin uses its other side";
                case "next_heads":
                    AddBuff(game, "heads", 0, effect.Coins);
                    return "next coin lands Heads";
                case "penalty":
                    e.Quota += effect.Amount; // a penalty moves the goalposts
                    e.MaxQuota += effect.Amount;
                    return "quota +" + N(effect.Amount);
                case "extra_draw":
                    {
                        // a played coin goes back into the draw pile (the flipping coin itself, or a random played
                        // one when no coin is flipping), so it can be played again. Capped per level.
                        int back = 0;
                        for (int k = 0; k < (int)effect.Amount; k++)
                        {
                            if (e.Returned >= ReturnCap) break;
                            int uid;
                            if (item != null) uid = item.Uid;
                            else
                            {
                                var candidates = new List<int>();
                                foreach (int played in e.Played)
                                    if (!e.Discarded.Contains(played)) candidates.Add(played);
                                if (candidates.Count == 0) break;
                                uid = candidates[Rng.Int(game, 1, candidates.Count) - 1];
                            }
                            e.Played.Remove(uid);
                            e.Pile.Insert(Rng.Int(game, 1, e.Pile.Count + 1) - 1, uid);
                            e.Returned++;
                            back++;
                        }
                        return back > 0 ? back + " coin back in the pile" : "no coin could return";
                    }
                case "fetch_best":
                    if (item.FetchedLevel == game.EncounterIndex) return "already fetched this level";
                    int bestUid = 0; double bestScore = 0;
                    foreach (int uid in e.Played) if (uid != item.Uid && e.BestScores.TryGetValue(uid, out var score) && score > bestScore) { bestUid = uid; bestScore = score; }
                    if (bestUid == 0 || e.Returned >= ReturnCap || !e.Played.Remove(bestUid)) return "no scored coin to fetch";
                    e.Pile.Insert(Rng.Int(game, 1, e.Pile.Count + 1) - 1, bestUid); e.Returned++; item.FetchedLevel = game.EncounterIndex;
                    return CoinName(GetCoin(game, bestUid)) + " fetched back into the pile";
                case "probability":
                    e.Bonus.TryGetValue(item.Uid, out double bonus);
                    e.Bonus[item.Uid] = bonus + effect.Amount;
                    return "+" + N(Math.Floor(effect.Amount * 100 + .5)) + "% Heads this encounter";
            }
            throw new GameRuleException("unknown effect: " + effect.Type);
        }

        // Stock the shop after a cleared level: four coins and one relic, all from the seeded RNG.
        static void EnterShop(GameState game)
        {
            game.Phase = Phase.Shop;
            game.RerollCost = game.Shop.RefreshCost;
            game.RerollStep = 2;
            SetShopStock(game);
            game.ShopRelic = null;
            var owned = new HashSet<string>(game.Relics);
            var unowned = new List<string>();
            foreach (var id in Content.Relics.Keys)
                if (!owned.Contains(id)) unowned.Add(id);
            unowned.Sort(string.CompareOrdinal); // stable order so the seeded pick is deterministic
            if (unowned.Count > 0) game.ShopRelic = unowned[Rng.Int(game, 1, unowned.Count) - 1];
            var itemIds = new List<string>(Content.Items.Keys);
            itemIds.Sort(string.CompareOrdinal);
            game.ShopItems = Offers(game, itemIds, 2);
        }

        // The level is over for good: the player opened the shop (or the boss fell). Only possible once the
        // quota is met. The payout was already given when the quota was met.
        public static bool EndLevel(GameState game)
        {
            var e = game.Encounter;
            if (game.Phase != Phase.Encounter || !e.Cleared || game.Pending != null || game.Mulligan != null) return false;
            if (e.ComboPot > 0) BankComboPot(game, false);
            Hooks.Unbind();
            game.Dealt = null;
            Items.Clear();
            Signal.Emit("encounter_end", new GameEvent { Game = game, Won = true });
            if (e.Boss) game.Phase = Phase.Victory;
            else EnterShop(game);
            if (e.Contract?.Result == "COMPLETE" && Contracts[e.Contract.Id].RewardAction != null) Contracts[e.Contract.Id].RewardAction(game, e);
            return true;
        }

        public static bool Resolve(GameState game)
        {
            if (game.Phase != Phase.Encounter || game.Pending == null) return false;
            var e = game.Encounter;
            var result = game.Pending;
            var item = GetCoin(game, result.Uid);
            var def = Content.Coins[item.Id];
            e.Flips++;
            string final = result.Result; // already decided (relics, boss inversion) when the coin was flipped
            result.Final = final;
            if (e.SideBetSide != null)
            {
                if (final == Side.Tie && game.RunEncounterId != "high_roller_table") { game.Player.Gold += e.SideBetCost; e.SideBetOutcome = "PUSH"; Log(game, "Side bet pushed; stake returned."); }
                else if (final == e.SideBetSide) { game.Player.Gold += e.SideBetPayout; e.SideBetOutcome = "WON"; Log(game, "Side bet won: +" + N(e.SideBetPayout) + " gold."); }
                else { e.SideBetOutcome = "LOST"; Log(game, "Side bet lost."); if (HasAugment(game, "hedge_fund")) game.NextLevelQuotaBonus += 2; }
            }
            if (final == Side.Heads) e.Streak++;
            else if (final == Side.Tails) e.Streak = 0; // a Tie holds the streak
            // combo: consecutive identical results. A shield (Anchor) lets one different result pass without breaking it.
            string previousComboSide = e.ComboSide;
            bool tieBreaks = e.Contract?.Id == "hot_streak" || game.RunEncounterId == "dead_heat";
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
            bool swap = BuffActive(game, "swap") != null;
            var sideEffects = final == Side.Tie ? TieEffects(item.Id) : (swap ? final != Side.Heads : final == Side.Heads) ? def.Heads : def.Tails;
            foreach (var effect in sideEffects) res.Effects.Add(effect.Copy());
            if (final == Side.Heads && item.Upgrade != null && def.Upgrades.TryGetValue(item.Upgrade, out var upgrade) && upgrade.HeadsScore != 0)
                res.Effects.Add(new Effect("score", upgrade.HeadsScore));
            Signal.Emit("coin_resolve", new GameEvent { Game = game, Inst = item, Res = res });
            ApplyTypeBuffs(game, item, final, comboBroke, res.Effects);
            if (final == Side.Heads && game.AugmentData.TryGetValue("type_specialist", out var specialist) && HasType(item, specialist) && e.TypeSpecialistPaid.Add(item.Uid))
                res.Effects.Add(new Effect("score", 1));
            if(final==Side.Tails)e.Tails++;
            result.BaseEffects = new List<Effect>(); // what this coin did before multipliers (True Echo repeats it)
            foreach (var effect in res.Effects) result.BaseEffects.Add(effect.Copy());
            double multiplier = 1;
            string messagesAllIn = null;
            foreach (var buff in e.Buffs)
                if (buff.Kind == "mult" && !buff.Fresh) multiplier *= buff.Amount;
            double combo = Math.Min(e.ComboCap, 1 + e.ComboStep * (Math.Max(e.ComboLen, 1) - 1));
            if (e.PushUsed && HasAugment(game, "all_in"))
            {
                if (final == previousComboSide && !e.AllInPaid) { combo *= 2; e.AllInPaid = true; messagesAllIn = "All-In push succeeded: combo payout doubled."; }
                else { double pushLoss=Math.Min(2,game.Player.Gold);game.Player.Gold-=pushLoss;messagesAllIn="All-In push failed: -"+N(pushLoss)+" gold."; }
            }
            if (res.CashOut) combo *= combo; // Cash Out spends the combo twice (then resets it)
            if (e.Contract?.Id == "bank_once") combo = Math.Min(combo, 2);
            result.Combo = new Combo { Len = e.ComboLen, Mult = combo, Side = e.ComboSide };
            multiplier *= combo;
            if (multiplier != 1) // buffs ("next coins pay double") and the combo multiply points and gold
            {
                foreach (var effect in res.Effects)
                    if (effect.Type == "score" || effect.Type == "gold") effect.Amount = Math.Floor(effect.Amount * multiplier + .5);
            }
            var messages = new List<string>();
            if (messagesAllIn != null) messages.Add(messagesAllIn);
            double scoredBefore = e.Scored, quotaTotalBefore = e.MaxQuota;
            int greed = HasType(item, "greed") ? ActiveTypeBuffs(e, "greed") : 0;
            for (int i = 0; i < res.Effects.Count; i++)
            {
                var effect = res.Effects[i];
                double goldBefore = game.Player.Gold;
                string text = ApplyEffect(game, item, effect);
                messages.Add(text);
                Signal.Emit("effect_applied", new GameEvent { Game = game, Inst = item, Effect = effect, Text = text });
                double gainedGold = Math.Max(0, game.Player.Gold - goldBefore);
                if (greed > 0 && gainedGold > 0) { double extra = gainedGold * (Math.Pow(2, greed) - 1); game.Player.Gold += extra; messages.Add("+" + N(extra) + " greed gold"); }
            }
            if (final == Side.Tails && e.Contract?.Id == "clean_run") messages.Add("contract: " + ApplyEffect(game, item, new Effect("penalty", 2)));
            if (greed > 0 && final == Side.Tails) { double loss = Math.Min(game.Player.Gold, 3 * greed); game.Player.Gold -= loss; messages.Add("-" + N(loss) + " greed gold"); }
            e.ComboPot = ComboPot(e.ComboLen);
            e.PushUsed = false;
            if (res.CashOut) { int banked = BankComboPot(game); if (banked > 0) messages.Add("banked " + banked + " combo gold"); }
            TickBuffs(game, item);
            result.Gained = e.Scored - scoredBefore; // for the UI: what this flip was worth
            result.Penalty = e.MaxQuota - quotaTotalBefore;
            e.BestScores.TryGetValue(item.Uid, out var previousBest); e.BestScores[item.Uid] = Math.Max(previousBest, result.Gained.Value);
            Signal.Emit("coin_resolved", new GameEvent { Game = game, Inst = item, Res = res });
            Hooks.Unbind();
            Hooks.Grow(item, "flip");
            Log(game, def.Name + " #" + item.Uid + ": " + final + (final != result.Raw ? " (raw " + result.Raw + ")" : "") +
                " → " + (messages.Count > 0 ? string.Join(", ", messages) : "nothing"));
            game.LastResult = result;
            game.Pending = null;
            if (e.Quota <= 0 && !e.Cleared)
            {
                e.Cleared = true;
                game.Cleared++;
                Log(game, "Quota met! " + e.Name + " cleared.");
                SettleContract(game);
                if (!e.Boss)
                {
                    game.Player.Gold += e.Payout ?? 0;
                    Log(game, "+" + N(e.Payout ?? 0) + " gold level payout.");
                }
            }
            if (e.Cleared) // keep flipping after the quota: every 1/SurplusRate extra points pay one gold
            {
                double owed = Math.Floor(Math.Max(0, e.Scored - e.MaxQuota) * SurplusRate);
                if (owed > e.SurplusPaid)
                {
                    game.Player.Gold += owed - e.SurplusPaid;
                    Log(game, "+" + N(owed - e.SurplusPaid) + " gold for extra points.");
                    e.SurplusPaid = owed;
                }
            }
            if (e.Cleared && e.Boss) EndLevel(game); // the boss ends the run the moment its quota is met
            else Deal(game); // deals the next coin, or handles an empty stack
            return true;
        }

        static void LoseLevel(GameState game, string why)
        {
            game.Phase = Phase.GameOver;
            game.ExchangeOpen = false;
            game.Dealt = null;
            game.LostWhy = why;
            Log(game, "Defeat: " + why);
            Hooks.Unbind();
            Items.Clear();
            Signal.Emit("encounter_end", new GameEvent { Game = game, Won = false });
        }

        // Gold an exchange costs right now: it rises with every exchange already made this level.
        public static int ExchangeCost(GameState game) => ExchangeBase + ExchangeStep * game.Encounter.Exchanges;

        // The coins that were played this level and could come back (discarded coins never do).
        static List<int> Returnable(GameState game)
        {
            var e = game.Encounter;
            var list = new List<int>();
            foreach (int uid in e.Played)
                if (!e.Discarded.Contains(uid)) list.Add(uid);
            return list;
        }

        // With an empty stack, pay gold to get up to ExchangeGain of the coins you already played this
        // level back into the stack.
        public static bool CanExchange(GameState game)
        {
            if (game.Phase != Phase.Encounter || game.Pending != null || game.Mulligan != null || game.Dealt != null) return false;
            if (CoinsLeft(game) > 0) return false;
            if (ExchangesLeft(game) <= 0) return false;
            return game.Player.Gold >= ExchangeCost(game) && Returnable(game).Count >= 1;
        }

        public static bool Exchange(GameState game)
        {
            if (!CanExchange(game)) return false;
            var e = game.Encounter;
            int cost = ExchangeCost(game);
            game.Player.Gold -= cost;
            var back = Returnable(game);
            for (int i = back.Count; i >= 2; i--) // seeded shuffle, then take the first ExchangeGain
            {
                int j = Rng.Int(game, 1, i);
                (back[i - 1], back[j - 1]) = (back[j - 1], back[i - 1]);
            }
            var taken = new HashSet<int>();
            for (int i = 0; i < Math.Min(ExchangeGain, back.Count); i++)
            {
                taken.Add(back[i]);
                e.Pile.Add(back[i]);
            }
            var kept = new List<int>();
            foreach (int uid in e.Played)
                if (!taken.Contains(uid)) kept.Add(uid);
            e.Played = kept;
            e.Exchanges++;
            game.ExchangeOpen = false;
            Log(game, "Paid " + cost + " gold: " + e.Pile.Count + " coins are back in the stack.");
            Deal(game);
            return true;
        }

        // The stack ran dry. Cleared level: the shop is the only way on (unless an exchange is possible, then
        // the player chooses). Uncleared: exchange if possible (the player decides), otherwise the run ends.
        public static void StackEmpty(GameState game)
        {
            var e = game.Encounter;
            bool canExchange = CanExchange(game);
            if (e.Cleared)
            {
                if (!canExchange) EndLevel(game);
            }
            else if (canExchange) game.ExchangeOpen = true;
            else LoseLevel(game, ExchangesLeft(game) <= 0 ? "out of coins, and all exchanges are used." : "out of coins, and not enough gold to exchange.");
        }

        // Give up instead of exchanging (only while the stack is empty and the quota is unmet).
        public static bool GiveUp(GameState game)
        {
            if (game.Phase != Phase.Encounter || game.Encounter.Cleared || game.Dealt != null || game.Pending != null ||
                game.Mulligan != null || CoinsLeft(game) > 0) return false;
            LoseLevel(game, "gave up.");
            return true;
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
            int cost = id == null ? 0 : Price(game, Content.Items[id].Cost);
            if (id == null || game.Items.Count >= Items.Max || game.Player.Gold < cost) return false;
            game.ShopItems[index] = null;
            game.Player.Gold -= cost;
            game.Items.Add(id);
            Log(game, "Bought " + Content.Items[id].Name + " for " + cost + " gold.");
            return true;
        }

        public static bool UseItem(GameState game, int slot) => Items.Use(game, slot);

        public const int RelicCost = 25;

        public static bool BuyRelic(GameState game)
        {
            int cost = Price(game, RelicCost);
            if (game.Phase != Phase.Shop || game.ShopRelic == null || game.Player.Gold < cost) return false;
            game.Player.Gold -= cost;
            AddRelic(game, game.ShopRelic);
            Log(game, "Bought relic " + Content.Relics[game.ShopRelic].Name + " for " + cost + " gold.");
            game.ShopRelic = null;
            return true;
        }

        public static int CoinCost(string id) => Content.Coins[id].Cost ?? 15;

        public static bool Buy(GameState game, int index)
        {
            string id = game.Phase == Phase.Shop && index >= 0 && index < game.ShopOffers.Count ? game.ShopOffers[index] : null;
            int cost = id == null ? -1 : CoinOfferCost(game, index);
            if (id == null || game.Player.Gold < cost) return false;
            if (game.Coins.Count >= game.Slots) return false;
            string upgrade = index < game.ShopUpgrades.Count ? game.ShopUpgrades[index] : null;
            game.ShopOffers[index] = null;
            game.Purchased.Add(id); // the UI turns purchases of locked coins into permanent unlocks
            game.Player.Gold -= cost;
            var bought = AddToDeck(game, id); bought.Upgrade = upgrade;
            Log(game, "Bought " + Content.Coins[id].Name + " for " + cost + " gold.");
            return true;
        }

        // Rerolling the coin offers costs 4 gold, 2 more each time within one shop visit.
        public static bool RerollShop(GameState game)
        {
            int cost = game.RerollCost;
            if (game.Phase != Phase.Shop || game.Player.Gold < cost) return false;
            game.Player.Gold -= cost;
            game.RerollCost = cost + game.RerollStep;
            SetShopStock(game);
            Log(game, "Shop rerolled for " + cost + " gold.");
            return true;
        }

        public static bool BuyEnergy(GameState game)
        {
            if (game.Phase != Phase.Shop || game.Player.Gold < 20 || game.Player.MaxEnergy >= 5) return false;
            game.Player.Gold -= 20;
            game.Player.MaxEnergy += 1;
            Log(game, "Bought +1 maximum energy for 20 gold.");
            return true;
        }

        public static bool Remove(GameState game, int uid)
        {
            if (game.Phase != Phase.Shop || game.Player.Gold < 8 || game.Coins.Count <= 1) return false;
            for (int index = 0; index < game.Coins.Count; index++)
            {
                var item = game.Coins[index];
                if (item.Uid != uid) continue;
                game.Coins.RemoveAt(index);
                game.Player.Gold -= 8;
                game.SelectedUid = game.Coins[0].Uid;
                Log(game, "Removed " + CoinName(item) + " for 8 gold.");
                return true;
            }
            return false;
        }

        public static bool Upgrade(GameState game, int uid)
        {
            if (game.Phase != Phase.Shop || game.Player.Gold < 10) return false;
            var item = GetCoin(game, uid);
            if (item == null || Probability(game, item) >= 1) return false;
            game.Player.Gold -= 10;
            var def = Content.Coins[item.Id];
            item.Bonus = Math.Min(1 - def.Probability, item.Bonus + .10);
            Log(game, def.Name + " upgraded to " + N(Math.Floor((def.Probability + item.Bonus) * 100 + .5)) + "% Heads.");
            return true;
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
            EnterShop(game);
            return true;
        }

        public static bool NextEncounter(GameState game)
        {
            if (game.Phase != Phase.Shop || ActiveCount(game) == 0) return false;
            int next = game.EncounterIndex + 1;
            if (!game.Endless && OfferAugment(game, next)) return true;
            game.EncounterIndex = next;
            StartEncounter(game);
            return true;
        }

        // Meta currency earned by a run: 1 per level cleared, +3 for beating the boss.
        public static int RunTokens(GameState game) => game.Cleared + (game.Phase == Phase.Victory ? 3 : 0);
    }
}
