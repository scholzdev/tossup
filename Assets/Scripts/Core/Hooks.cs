using System;
using System.Collections.Generic;

namespace Tossup
{
    public sealed class CoinCtx
    {
        public GameState Game;
        public CoinInst Inst;
        public Action<string, Action<GameEvent>> On;
    }

    public sealed class RelicCtx
    {
        public GameState Game;
        public Action<string, Action<GameEvent>> On;
    }

    // Coin effect hooks. Routes a coin def's hooks onto the Signal bus, scoped to the coin that is
    // currently dealt/flipped.
    //
    // A coin def (Content/Coins.cs) may declare hooks in two styles, mixed freely:
    //   1. one delegate per hook:
    //        OnDeal(game, inst)          coin was dealt from the stack (before you flip or discard)
    //        OnFlip(game, inst, flip)    dice rolled; set flip.Result to change the outcome
    //        OnResolve(game, inst, res)  outcome final, effects NOT yet applied. res.Effects is a
    //                                    private copy: edit it, add to it, or replace it
    //        OnDiscard(game, inst)       coin discarded for the level
    //   2. Register(ctx): subscribe yourself, for several events or closure state:
    //        ctx.On("coin_resolve", e => ...). Handlers are torn down when the coin resolves or is discarded.
    //
    // Pure (no side effects), evaluated any time odds are shown, for every owned coin:
    //   OnOdds(game, inst, odds)     mutate odds.P
    // Persistent growth:
    //   Grow(inst, event)            "level" (each level start, every owned coin) | "flip" | "discard"
    //
    // Randomness inside a hook must use Rng.Random(game) / Rng.Int(game, a, b) to stay seeded.
    public static class Hooks
    {
        static List<Handle> active; // one coin is in play at a time, so a single handle list is enough

        public static void Unbind()
        {
            if (active == null) return;
            foreach (var handle in active) Signal.Off(handle);
            active = null;
        }

        public static void Bind(GameState game, CoinInst inst)
        {
            Unbind();
            var def = inst.Definition;
            var handles = new List<Handle>();
            active = handles;
            void On(string evt, Action<GameEvent> handler)
            {
                handles.Add(Signal.On(evt, e => { if (e.Inst == inst) handler(e); }));
            }
            if (def.Overrides(nameof(CoinDef.OnDeal))) On("coin_deal", e => def.OnDeal(e.Game, e.Inst));
            if (def.Overrides(nameof(CoinDef.OnFlip))) On("coin_flip", e => def.OnFlip(e.Game, e.Inst, e.Flip));
            if (def.Overrides(nameof(CoinDef.OnResolve))) On("coin_resolve", e => def.OnResolve(e.Game, e.Inst, e.Res));
            if (def.Overrides(nameof(CoinDef.OnDiscard))) On("coin_discard", e => def.OnDiscard(e.Game, e.Inst));
            def.Register(new CoinCtx { Game = game, Inst = inst, On = On });
        }

        public static void Grow(CoinInst inst, string evt) => inst.Definition.Grow(inst, evt);

        public static double Odds(GameState game, CoinInst inst, double p)
        {
            var def = inst.Definition;
            var odds = new Odds { P = p };
            def.OnOdds(game, inst, odds);
            return odds.P;
        }
    }

    // Relics are passive, run-long modifiers. Each relic def has Register(ctx); ctx.On(event, handler)
    // subscribes to the bus while the relic is owned. coin_outcome fires before the boss inversion:
    // set e.Result to change it.
    public static class Relics
    {
        static List<Handle> handles = new List<Handle>();

        public static void Unbind()
        {
            foreach (var handle in handles) Signal.Off(handle);
            handles = new List<Handle>();
        }

        // Rebind every owned relic of this game (call after the relic list changes).
        public static void Bind(GameState game)
        {
            Unbind();
            void On(string evt, Action<GameEvent> handler)
            {
                handles.Add(Signal.On(evt, e => { if (e.Game == game) handler(e); }));
            }
            foreach (var id in game.Relics) Content.Relics[id].Register(new RelicCtx { Game = game, On = On });
        }
    }

    // Consumable items: bought in the shop, used in the middle of a level while a coin is dealt.
    // Use returns false to refuse (the item is then not consumed). To change the coming flip, arm a
    // one-shot listener: Items.Arm("coin_flip", e => e.Flip.Result = "Heads"). Armed listeners are
    // dropped at encounter end and when a new game starts.
    public static class Items
    {
        public const int Max = 3;

        static List<Handle> armed = new List<Handle>();

        public static void Clear()
        {
            foreach (var handle in armed) Signal.Off(handle);
            armed = new List<Handle>();
        }

        // Run fn once on the next emit of the event.
        public static void Arm(string evt, Action<GameEvent> fn)
        {
            Handle handle = null;
            handle = Signal.On(evt, e =>
            {
                Signal.Off(handle);
                armed.Remove(handle);
                fn(e);
            });
            armed.Add(handle);
        }

        public static bool CanUse(GameState game) =>
            game.Phase == Phase.Encounter && game.Dealt != null && game.Pending == null;

        public static bool Use(GameState game, int slot)
        {
            if (slot < 0 || slot >= game.Items.Count || !CanUse(game)) return false;
            var id = game.Items[slot];
            if (!Content.Items[id].Use(game)) return false;
            game.Items.RemoveAt(slot);
            Game.Log(game, "Used " + Content.Items[id].Name + ".");
            return true;
        }
    }
}
