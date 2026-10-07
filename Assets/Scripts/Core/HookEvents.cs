using System;

namespace Tossup
{
    public enum ShopPurchaseKind
    {
        Coin,
        Item,
        Relic,
        Energy,
        Reroll,
        CoinRemoval,
    }

    // The definition that owns a hook is invoked once per owned coin instance or relic.
    // Coin is null for relic hooks; Relic is null for coin hooks.
    public sealed class HookContext
    {
        public GameState Game { get; }
        public CoinInst Coin { get; }
        public ItemDef Item { get; }
        public RelicDef Relic { get; }
        public double DeltaTime { get; }
        public double ElapsedSeconds => Game?.Encounter?.ElapsedSeconds ?? 0;
        public CoinMasteryState Mastery => new CoinMasteryState(Game, Coin?.Definition);

        internal HookContext(GameState game, CoinInst coin, double deltaTime = 0)
        { Game = game; Coin = coin; DeltaTime = deltaTime; }

        internal HookContext(GameState game, RelicDef relic, double deltaTime = 0)
        { Game = game; Relic = relic; DeltaTime = deltaTime; }

        internal HookContext(GameState game, ItemDef item, double deltaTime = 0)
        { Game = game; Item = item; DeltaTime = deltaTime; }
    }

    public sealed class ShopPurchase
    {
        public GameState Game { get; }
        public ShopPurchaseKind Kind { get; }
        public string Id { get; }
        public int Cost { get; set; }
        public bool Cancelled { get; set; }

        internal ShopPurchase(GameState game, ShopPurchaseKind kind, string id, int cost)
        { Game = game; Kind = kind; Id = id; Cost = cost; }
    }

    public sealed class CoinHookEvents
    {
        public event Action<HookContext, Odds> Odds;
        public event Action<HookContext> Deal;
        public event Action<HookContext, FlipState> Flip;
        public event Action<HookContext, Res> Resolve;
        public event Action<HookContext> Discard;
        public event Action<HookContext, CoinGrowthEvent> Grow;
        public event Action<HookContext, Buff> BuffCreated;

        internal bool HasOdds => Odds != null;
        internal bool HasDeal => Deal != null;
        internal bool HasFlip => Flip != null;
        internal bool HasResolve => Resolve != null;
        internal bool HasDiscard => Discard != null;
        internal bool HasGrow => Grow != null;

        internal void RaiseOdds(HookContext context, Odds odds) => Odds?.Invoke(context, odds);
        internal void RaiseDeal(HookContext context) => Deal?.Invoke(context);
        internal void RaiseFlip(HookContext context, FlipState flip) => Flip?.Invoke(context, flip);
        internal void RaiseResolve(HookContext context, Res res) => Resolve?.Invoke(context, res);
        internal void RaiseDiscard(HookContext context) => Discard?.Invoke(context);
        internal void RaiseGrow(HookContext context, CoinGrowthEvent evt) => Grow?.Invoke(context, evt);
        internal void RaiseBuffCreated(HookContext context, Buff buff) => BuffCreated?.Invoke(context, buff);
    }

    public sealed class ShopHookEvents
    {
        public event Action<HookContext> Opened;
        public event Action<HookContext> Closed;
        public event Action<HookContext, ShopPurchase> BeforePurchase;
        public event Action<HookContext, ShopPurchase> AfterPurchase;

        internal void RaiseOpened(HookContext context) => Opened?.Invoke(context);
        internal void RaiseClosed(HookContext context) => Closed?.Invoke(context);
        internal void RaiseBeforePurchase(HookContext context, ShopPurchase purchase) => BeforePurchase?.Invoke(context, purchase);
        internal void RaiseAfterPurchase(HookContext context, ShopPurchase purchase) => AfterPurchase?.Invoke(context, purchase);
    }

    public sealed class EncounterHookEvents
    {
        public event Action<HookContext> Start;
        public event Action<HookContext, bool> End;

        internal void RaiseStart(HookContext context) => Start?.Invoke(context);
        internal void RaiseEnd(HookContext context, bool won) => End?.Invoke(context, won);
    }

    // Run-wide views of coin lifecycle signals. Unlike On.Coins, these fire for each coin
    // event in the run and are delivered to every currently owned definition.
    public sealed class GameCoinHookEvents
    {
        public event Action<HookContext, GameEvent> Deal;
        public event Action<HookContext, GameEvent> Flip;
        public event Action<HookContext, GameEvent> Outcome;
        public event Action<HookContext, GameEvent> Resolve;
        public event Action<HookContext, GameEvent> Resolved;
        public event Action<HookContext, GameEvent> Discard;

        internal void Raise(GameSignal signal, HookContext context, GameEvent e)
        {
            switch (signal)
            {
                case GameSignal.CoinDeal: Deal?.Invoke(context, e); break;
                case GameSignal.CoinFlip: Flip?.Invoke(context, e); break;
                case GameSignal.CoinOutcome: Outcome?.Invoke(context, e); break;
                case GameSignal.CoinResolve: Resolve?.Invoke(context, e); break;
                case GameSignal.CoinResolved: Resolved?.Invoke(context, e); break;
                case GameSignal.CoinDiscard: Discard?.Invoke(context, e); break;
            }
        }
    }

    public sealed class GameEffectHookEvents
    {
        public event Action<HookContext, GameEvent> Applied;
        internal void RaiseApplied(HookContext context, GameEvent e) => Applied?.Invoke(context, e);
    }

    public sealed class GameBuffHookEvents
    {
        public event Action<HookContext, GameEvent> Applied;
        internal void RaiseApplied(HookContext context, GameEvent e) => Applied?.Invoke(context, e);
    }

    public sealed class GameHookEvents
    {
        public ShopHookEvents Shop { get; } = new ShopHookEvents();
        public EncounterHookEvents Encounter { get; } = new EncounterHookEvents();
        public GameCoinHookEvents Coins { get; } = new GameCoinHookEvents();
        public GameEffectHookEvents Effects { get; } = new GameEffectHookEvents();
        public GameBuffHookEvents Buffs { get; } = new GameBuffHookEvents();
    }

    public sealed class TimeHookEvents
    {
        public event Action<HookContext> Tick;
        internal void RaiseTick(HookContext context) => Tick?.Invoke(context);
    }

    // Each definition owns its own events. Runtime dispatch supplies the active run/instance,
    // so definitions remain shared immutable content and never capture run-specific state.
    public sealed class OnHooks
    {
        public CoinHookEvents Coins { get; } = new CoinHookEvents();
        public GameHookEvents Game { get; } = new GameHookEvents();
        public TimeHookEvents Time { get; } = new TimeHookEvents();
    }
}
