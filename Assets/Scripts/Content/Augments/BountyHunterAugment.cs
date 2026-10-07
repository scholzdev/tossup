namespace Tossup.Augments
{
    public sealed class BountyHunterAugment : AugmentDef
    {
        public BountyHunterAugment()
        {
            Id = "bounty_hunter";
            Name = "Bounty Hunter";
            Description = "Winning a fight pays 10 more gold, but every enemy flips 1 more coin a round.";
            Tier = "silver";
        }

        public override void OnRunHook(GameState game, RunHookEvent evt)
        {
            var encounter = game.Encounter;
            if (evt != RunHookEvent.EncounterStart || encounter == null || encounter.EnemyId == null) return;
            encounter.EnemyDraw++;
            encounter.Payout = (encounter.Payout ?? 0) + 10;
        }
    }
}
