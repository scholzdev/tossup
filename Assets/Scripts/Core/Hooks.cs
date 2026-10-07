using System;
using System.Collections.Generic;

namespace Tossup
{
    public sealed class CoinCtx
    {
        public GameState Game;
        public CoinInst Inst;
        public Action<GameSignal, Action<GameEvent>> On;
    }

    public sealed class RelicCtx
    {
        public GameState Game;
        public Action<GameSignal, Action<GameEvent>> On;
    }

    // Coin and run hooks. A definition owns its event subscriptions; dispatch supplies the active
    // run and owner instance, so handlers do not capture mutable run state in shared content.
    //
    // Hooks are separate += events grouped by domain: On.Coins.Odds, On.Game.Shop.BeforePurchase,
    // On.Game.Encounter.Start, On.Time.Tick, and so on. Legacy virtual coin hooks are bridged by CoinDef.
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
            var context = new HookContext(game, inst);
            void On(GameSignal signal, Action<GameEvent> handler)
            {
                handles.Add(Signal.On(signal, e => { if (e.Inst == inst) handler(e); }));
            }
            On(GameSignal.CoinDeal, e => def.On.Coins.RaiseDeal(context));
            On(GameSignal.CoinFlip, e => def.On.Coins.RaiseFlip(context, e.Flip));
            On(GameSignal.CoinResolve, e => def.On.Coins.RaiseResolve(context, e.Res));
            On(GameSignal.CoinDiscard, e => def.On.Coins.RaiseDiscard(context));
            def.Register(new CoinCtx { Game = game, Inst = inst, On = On });
        }

        public static void Grow(GameState game, CoinInst inst, CoinGrowthEvent evt)
        {
            var context = new HookContext(game, inst);
            inst.Definition.On.Coins.RaiseGrow(context, evt);
        }

        public static void Odds(GameState game, CoinInst inst, Odds odds)
        {
            inst.Definition.On.Coins.RaiseOdds(new HookContext(game, inst), odds);
        }

        public static void Tick(GameState game, double dt) => ForEachOwner(game,
            (context, on) => on.Time.RaiseTick(context), dt);

        internal static void GameEvent(GameSignal signal, GameEvent e)
        {
            if (e?.Game == null) return;
            ForEachOwner(e.Game, (context, on) =>
            {
                switch (signal)
                {
                    case GameSignal.EncounterStart:
                        on.Game.Encounter.RaiseStart(context);
                        break;
                    case GameSignal.EncounterEnd:
                        on.Game.Encounter.RaiseEnd(context, e.Won);
                        break;
                    case GameSignal.EffectApplied:
                        on.Game.Effects.RaiseApplied(context, e);
                        break;
                    case GameSignal.BuffApplied:
                        on.Game.Buffs.RaiseApplied(context, e);
                        break;
                    default:
                        on.Game.Coins.Raise(signal, context, e);
                        break;
                }
            });
        }

        public static void ShopOpened(GameState game) => ForEachOwner(game,
            (context, on) => on.Game.Shop.RaiseOpened(context));

        public static void ShopClosed(GameState game) => ForEachOwner(game,
            (context, on) => on.Game.Shop.RaiseClosed(context));

        public static bool BeforePurchase(GameState game, ShopPurchase purchase)
        {
            ForEachOwner(game, (context, on) => on.Game.Shop.RaiseBeforePurchase(context, purchase));
            purchase.Cost = Math.Max(0, purchase.Cost);
            return !purchase.Cancelled;
        }

        public static void AfterPurchase(GameState game, ShopPurchase purchase) => ForEachOwner(game,
            (context, on) => on.Game.Shop.RaiseAfterPurchase(context, purchase));

        static void ForEachOwner(GameState game, Action<HookContext, OnHooks> invoke, double dt = 0)
        {
            if (game == null) return;
            foreach (var coin in game.Coins.ToArray())
                invoke(new HookContext(game, coin, dt), coin.Definition.On);
            foreach (var id in game.Items.ToArray())
                if (Content.Items.TryGetValue(id, out var item)) invoke(new HookContext(game, item, dt), item.On);
            foreach (var id in game.Relics.ToArray())
                if (Content.Relics.TryGetValue(id, out var relic)) invoke(new HookContext(game, relic, dt), relic.On);
        }
    }

    // Relics are passive, run-long modifiers. Each relic def has Register(ctx); ctx.On(event, handler)
    // subscribes to the bus while the relic is owned. GameSignal.CoinOutcome fires before the boss inversion:
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
            void On(GameSignal signal, Action<GameEvent> handler)
            {
                handles.Add(Signal.On(signal, e => { if (e.Game == game) handler(e); }));
            }
            foreach (var id in game.Relics) Content.Relics[id].Register(new RelicCtx { Game = game, On = On });
        }
    }

    // Consumable items: bought in the shop, used between flips during a fight.
    // Use returns false to refuse (the item is then not consumed). To change the next flip, arm a
    // one-shot listener: Items.Arm(GameSignal.CoinFlip, e => e.Flip.Result = Side.Heads). Armed listeners are
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
        public static void Arm(GameSignal signal, Action<GameEvent> fn)
        {
            Handle handle = null;
            handle = Signal.On(signal, e =>
            {
                Signal.Off(handle);
                armed.Remove(handle);
                fn(e);
            });
            armed.Add(handle);
        }

        public static bool CanUse(GameState game) =>
            game.Phase == Phase.Encounter && game.Encounter != null && !game.Encounter.Cleared && game.Pending == null;

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
