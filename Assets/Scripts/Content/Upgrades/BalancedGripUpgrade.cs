namespace Tossup.Upgrades
{
    public sealed class BalancedGripUpgrade : Upgrade
    {
        public override string Id => "balanced_grip";
        public override string Name => "Balanced Grip";
        public override int Cost => 13;
        public override string Description => "+6% Heads chance.";
        public override UpgradeType Type => UpgradeType.Probability;
        public override double Value => 0.06;
    }
}
