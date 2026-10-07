using System.Collections.Generic;

namespace Tossup.Augments
{
    // Kept only so a run saved before coin upgrades were removed can resume.
    public sealed class UpgradePressAugment : AugmentDef
    {
        public UpgradePressAugment()
        {
            Id = "upgrade_press";
            Name = "Retired Upgrade Press";
            Description = "This augment has been retired.";
            Tier = "silver";
        }

        public override bool Available(GameState game) => false;
        public override AugmentPending OnChosen(GameState game) => null;
        public override List<AugmentChoice> Choices(GameState game, AugmentPending pending) =>
            new List<AugmentChoice> { new AugmentChoice { Key = "continue", Title = "CONTINUE", Detail = "Continue the run." } };
        public override void ApplyChoice(GameState game, AugmentPending pending, AugmentChoice choice) { }
    }
}
