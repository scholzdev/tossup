namespace Tossup.Augments
{
    public sealed class DeepPocketsAugment : AugmentDef
    {
        public DeepPocketsAugment()
        {
            Id = "deep_pockets";
            Name = "Deep Pockets";
            Description = "Your hand holds 1 more coin, but every enemy starts 3 points ahead.";
            Tier = "silver";
        }

        public override void OnRunHook(GameState game, RunHookEvent evt)
        {
            var encounter = game.Encounter;
            if (evt != RunHookEvent.EncounterStart || encounter == null) return;
            encounter.HandSize++;
            encounter.EnemyScore += 3;
        }
    }
}
