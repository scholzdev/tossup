using System;
using System.Collections.Generic;

namespace Tossup
{
    public enum GameSignal
    {
        EncounterStart,
        EncounterEnd,
        CoinDeal,
        CoinFlip,
        CoinOutcome,
        CoinResolve,
        EffectApplied,
        CoinResolved,
        CoinDiscard,
        BuffApplied,
    }

    // One payload shape for every bus signal; each signal fills only the fields it needs.
    //   EncounterStart{Encounter}  EncounterEnd{Won}
    //   CoinDeal{Inst}  CoinFlip{Inst, Flip}  CoinOutcome{Inst, Flips, Result}  CoinResolve{Inst, Res}
    //   EffectApplied{Inst, Effect, Text}  CoinResolved{Inst, Res}  CoinDiscard{Inst}
    public sealed class GameEvent
    {
        public GameState Game;
        public CoinInst Inst;
        public FlipState Flip;
        public Res Res;
        public Effect Effect;
        public Buff Buff;
        public double ScoreDelta, GoldDelta, EnergyDelta, PenaltyDelta;
        public int ReturnedDelta;
        public int BuffsAffected;
        public string Text;
        public Encounter Encounter;
        public bool Won;
        public bool Final;
        public int Flips;
        public string Result;
    }

    public sealed class Handle
    {
        public GameSignal Event;
        public Action<GameEvent> Callback;
        public bool Active = true;
    }

    // Mutable event bus. An emitted event passes through every listener in registration order;
    // listeners may mutate it, and gameplay reads the final result after Emit.
    public static class Signal
    {
        static Dictionary<GameSignal, List<Handle>> listeners = new Dictionary<GameSignal, List<Handle>>();

        public static Handle On(GameSignal signal, Action<GameEvent> callback)
        {
            var handle = new Handle { Event = signal, Callback = callback };
            if (!listeners.TryGetValue(signal, out var list)) listeners[signal] = list = new List<Handle>();
            list.Add(handle);
            return handle;
        }

        public static void Off(Handle handle)
        {
            if (!handle.Active) return;
            handle.Active = false;
            if (listeners.TryGetValue(handle.Event, out var list)) list.Remove(handle);
        }

        public static GameEvent Emit(GameSignal signal, GameEvent e)
        {
            if (listeners.TryGetValue(signal, out var list))
            {
                var snapshot = list.ToArray(); // listeners added/removed mid-emit only affect later emits
                foreach (var handle in snapshot)
                    if (handle.Active) handle.Callback(e);
            }
            // Definition-owned hooks run after bus listeners and before gameplay consumes the event.
            Hooks.GameEvent(signal, e);
            return e;
        }

        public static void ClearAll() => listeners = new Dictionary<GameSignal, List<Handle>>();
    }
}
