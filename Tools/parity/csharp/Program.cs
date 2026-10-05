// Parity bot for the C# rules: the same scripted policy as Tools/parity/bot.lua, printing the same state
// dump after every action, so the two outputs can be diffed line by line.
// Usage: dotnet run -c Release -- <runs> <output file>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tossup;

static class Program
{
    const int MaxSteps = 500;
    static readonly string[] Chars = { "blade", "seer", "trader" };
    static readonly string[] RelicIds = { "magnet", "penny", "clock", "metronome", "baton" };
    static readonly string[] ItemIds = { "force_heads", "force_tails", "weighted", "double_down", "swap", "peek", "extra_draw" };

    static long botState;
    static StreamWriter output;

    static int Brand(int n)
    {
        botState = botState * 16807 % 2147483647;
        return 1 + (int)Math.Floor((double)botState / 2147483647 * n);
    }

    static void Print(string line) => output.Write(line + "\n");

    static string S(object v) => v == null ? "nil" : GameText.ToLuaString(v);
    static string S(double? v) => v.HasValue ? GameText.Num(v.Value) : "nil";
    static string S(int? v) => v.HasValue ? v.Value.ToString() : "nil";
    static string S(bool v) => v ? "true" : "false";
    static string List<T>(IEnumerable<T> items) => string.Join(",", items.Select(x => S(x)));

    static string PhaseName(Phase p) => p switch
    {
        Phase.Encounter => "ENCOUNTER",
        Phase.Shop => "SHOP",
        Phase.Victory => "VICTORY",
        _ => "GAME_OVER",
    };

    static void Dump(GameState g)
    {
        Print($"S phase={PhaseName(g.Phase)} gold={S(g.Player.Gold)} energy={S(g.Player.Energy)} maxe={S(g.Player.MaxEnergy)} idx={g.EncounterIndex} cleared={g.Cleared} rng={g.RngState} sel={S(g.SelectedUid)} endless={S(g.Endless)} xo={S(g.ExchangeOpen)} lost={S(g.LostWhy)}");
        var e = g.Encounter;
        if (e != null)
        {
            var buffs = e.Buffs.Select(b => b.Kind + ":" + S(b.Amount) + ":" + b.Left + ":" + S(b.Fresh));
            var bonus = e.Bonus.Keys.OrderBy(k => k).Select(k => k + ":" + S(e.Bonus[k]));
            Print($"E quota={S(e.Quota)} maxq={S(e.MaxQuota)} scored={S(e.Scored)} flips={e.Flips} streak={e.Streak} cside={S(e.ComboSide)} clen={S(e.ComboLen)} shield={S(e.Shield)} magnet={S(e.Magnet)} ret={e.Returned} disc={e.Discards} exch={e.Exchanges} dbl={e.Doubler} sp={S(e.SurplusPaid)} clr={S(e.Cleared)} step={S(e.ComboStep)} cap={S(e.ComboCap)}");
            Print("  buffs=" + string.Join(",", buffs) + " pile=" + List(e.Pile) + " queue=" + List(e.Queue) +
                " played=" + List(e.Played) + " discarded=" + List(e.Discarded.OrderBy(x => x)) + " bonus=" + string.Join(",", bonus));
        }
        var p = g.Pending;
        if (p != null) Print("P " + p.Uid + " " + S(p.Probability) + " " + S(p.Raw) + " " + S(p.Result) + " " + S(p.Altered) + " " + S(p.Forced));
        var d = g.Dealt;
        if (d != null) Print("D " + d.Uid + " " + S(d.Probability));
        var l = g.LastResult;
        if (l != null)
            Print("L " + l.Uid + " " + S(l.Final) + " " + S(l.Gained) + " " + S(l.Penalty) + " " + S(l.Combo?.Len) + " " + S(l.Combo?.Mult) + " " + S(l.Combo?.Side));
        var coins = g.Coins.Select(c => c.Uid + ":" + c.Id + ":" + S(c.Bonus) + ":" + S(c.Charge) + ":" + S(c.Debt) + ":" +
            S(c.Stack) + ":" + S(c.Anger) + ":" + S(c.Jackpot) + ":" + S(Game.Probability(g, c)));
        Print("C " + string.Join(" ", coins));
        if (g.Mulligan != null) Print("M " + List(g.Mulligan.Hand));
        if (g.Peek != null) Print("K " + List(g.Peek));
        if (g.Phase == Phase.Shop)
            Print("SHOP " + string.Join(",", g.ShopOffers.Select(x => x ?? "-")) + " | " + string.Join(",", g.ShopItems.Select(x => x ?? "-")) +
                " | " + S(g.ShopRelic) + " | " + g.RerollCost);
        Print("I " + List(g.Items) + " R " + List(g.Relics));
    }

    static (string, object) Step(GameState game)
    {
        if (game.Phase == Phase.Encounter)
        {
            if (game.Mulligan != null)
            {
                if (Brand(3) == 1)
                {
                    var picks = new List<int>();
                    foreach (int uid in game.Mulligan.Hand) if (Brand(3) == 1) picks.Add(uid);
                    return ("mdiscard", Game.MulliganDiscard(game, picks));
                }
                return ("mdone", Game.MulliganDone(game));
            }
            if (game.Pending != null)
            {
                int r = Brand(12);
                if (r == 1) return ("reroll", Game.Reroll(game));
                if (r == 2) return ("force", Game.Force(game, Brand(2) == 1 ? Side.Heads : Side.Tails));
                return ("resolve", Game.Resolve(game));
            }
            if (game.Dealt != null)
            {
                if (game.Encounter.Cleared && Brand(10) == 1) return ("endlevel", Game.EndLevel(game));
                int r = Brand(10);
                if (r == 1 && game.Items.Count > 0) return ("use", Game.UseItem(game, Brand(game.Items.Count) - 1));
                if (r == 2)
                {
                    var picks = new List<int>();
                    foreach (int uid in game.Encounter.Queue) if (Brand(2) == 1) picks.Add(uid);
                    return ("discard", Game.Discard(game, picks));
                }
                if (Game.CanFlip(game)) return ("flip", Game.Flip(game));
                return ("discard1", Game.Discard(game));
            }
            if (Game.CanExchange(game) && Brand(4) > 1) return ("exchange", Game.Exchange(game));
            if (game.Encounter.Cleared) return ("endlevel", Game.EndLevel(game));
            return ("giveup", Game.GiveUp(game));
        }
        if (game.Phase == Phase.Shop)
        {
            int r = Brand(9);
            if (r == 1) return ("buy", Game.Buy(game, Brand(4) - 1));
            if (r == 2) return ("buyitem", Game.BuyItem(game, Brand(2) - 1));
            if (r == 3) return ("buyrelic", Game.BuyRelic(game));
            if (r == 4) return ("rerollshop", Game.RerollShop(game));
            if (r == 5) return ("energy", Game.BuyEnergy(game));
            if (r == 6) return ("upgrade", Game.Upgrade(game, game.Coins[Brand(game.Coins.Count) - 1].Uid));
            if (r == 7 && Brand(3) == 1) return ("remove", Game.Remove(game, game.Coins[Brand(game.Coins.Count) - 1].Uid));
            return ("leave", Game.LeaveShop(game));
        }
        if (game.Phase == Phase.Victory)
        {
            if (Brand(2) == 1) return ("endless", Game.ContinueEndless(game));
            return ("stop", null);
        }
        return ("stop", null);
    }

    static int Main(string[] args)
    {
        int runs = args.Length > 0 ? int.Parse(args[0]) : 300;
        output = new StreamWriter(args.Length > 1 ? args[1] : "csharp.txt", false, new UTF8Encoding(false));
        var order = Content.CoinOrder;
        for (int run = 1; run <= runs; run++)
        {
            botState = run * 7919L + 13;
            long seed = run * 104729L + 7;
            string character = Chars[Brand(3) - 1];
            int n = Brand(10);
            var loadout = new List<string>();
            var copies = new Dictionary<string, int>();
            for (int k = 0; k < n; k++)
            {
                string id = order[Brand(order.Count) - 1];
                copies.TryGetValue(id, out int c);
                copies[id] = ++c;
                if (id == "normal" || c <= 3) loadout.Add(id);
            }
            Print("RUN " + run + " seed=" + seed + " char=" + character + " loadout=" + string.Join(",", loadout));
            GameState game;
            try { game = Game.New(seed, character, new List<string>(order), loadout, true); }
            catch (Exception ex) { Print("ERROR " + ex.Message); Print("END " + run); continue; }
            for (int k = Brand(4) - 1; k > 0; k--)
            {
                string id = RelicIds[Brand(5) - 1];
                if (!game.Relics.Contains(id)) Game.AddRelic(game, id);
            }
            for (int k = Brand(4) - 1; k > 0; k--) game.Items.Add(ItemIds[Brand(7) - 1]);
            Dump(game);
            for (int stepIndex = 1; stepIndex <= MaxSteps; stepIndex++)
            {
                string action;
                object result;
                try { (action, result) = Step(game); }
                catch (Exception ex) { Print("ERROR " + ex.GetType().Name + ": " + ex.Message); break; }
                Print("STEP " + stepIndex + " " + action + " " + S(result));
                Dump(game);
                if (action == "stop" || game.Phase == Phase.GameOver) break;
            }
            foreach (var line in game.Log) Print("LOG " + line);
            Print("END " + run);
        }
        output.Flush();
        output.Dispose();
        return 0;
    }
}
