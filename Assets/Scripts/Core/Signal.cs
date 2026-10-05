using System;
using System.Collections.Generic;

namespace Tossup
{
    // One event shape for every bus event; each event fills only the fields it needs.
    //   encounter_start{Encounter}  encounter_end{Won}
    //   coin_deal{Inst}  coin_flip{Inst, Flip}  coin_outcome{Inst, Flips, Result}  coin_resolve{Inst, Res}
    //   effect_applied{Inst, Effect, Text}  coin_resolved{Inst, Res}  coin_discard{Inst}
    public sealed class GameEvent
    {
        public GameState Game;
        public CoinInst Inst;
        public FlipState Flip;
        public Res Res;
        public Effect Effect;
        public string Text;
        public Encounter Encounter;
        public bool Won;
        public int Flips;
        public string Result;
    }

    public sealed class Handle
    {
        public string Event;
        public Action<GameEvent> Callback;
        public bool Active = true;
    }

    // Mutable event bus. An emitted event passes through every listener in registration order;
    // listeners may mutate it, and gameplay reads the final result after Emit.
    public static class Signal
    {
        static Dictionary<string, List<Handle>> listeners = new Dictionary<string, List<Handle>>();

        public static Handle On(string name, Action<GameEvent> callback)
        {
            var handle = new Handle { Event = name, Callback = callback };
            if (!listeners.TryGetValue(name, out var list)) listeners[name] = list = new List<Handle>();
            list.Add(handle);
            return handle;
        }

        public static void Off(Handle handle)
        {
            if (!handle.Active) return;
            handle.Active = false;
            if (listeners.TryGetValue(handle.Event, out var list)) list.Remove(handle);
        }

        public static GameEvent Emit(string name, GameEvent e)
        {
            if (!listeners.TryGetValue(name, out var list)) return e;
            var snapshot = list.ToArray(); // listeners added/removed mid-emit only affect later emits
            foreach (var handle in snapshot)
                if (handle.Active) handle.Callback(e);
            return e;
        }

        public static void ClearAll() => listeners = new Dictionary<string, List<Handle>>();
    }
}
