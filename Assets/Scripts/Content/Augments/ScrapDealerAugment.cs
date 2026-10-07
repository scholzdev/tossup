namespace Tossup.Augments
{
    public sealed class ScrapDealerAugment : AugmentDef
    {
        public ScrapDealerAugment()
        {
            Id = "scrap_dealer";
            Name = "Scrap Dealer";
            Description = "Discarding a coin grants 1 gold, but each discard adds 2 quota to the current level.";
            Tier = "silver";
        }

        public override void OnRunHook(GameState game, RunHookEvent evt)
        {
            var encounter = game.Encounter;
            if (evt != RunHookEvent.Discard || encounter == null) return;
            game.Player.Gold++;
            encounter.MaxQuota += 2;
            if (!encounter.Cleared) encounter.Quota += 2;
            Game.Log(game, "Scrap Dealer: +1 gold, quota +2.");
        }
    }
}
