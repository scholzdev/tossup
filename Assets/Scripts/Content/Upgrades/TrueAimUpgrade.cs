namespace Tossup.Upgrades
{
    public sealed class TrueAimUpgrade : Upgrade
    {
        public override string Id => "true_aim";
        public override string Name => "True Aim";
        public override int Cost => 10;
        public override string Description => "+8% Heads chance.";
        public override UpgradeType Type => UpgradeType.Probability;
        public override double Value => 0.08;
    }
}
