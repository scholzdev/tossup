namespace Tossup.Upgrades
{
    public sealed class SteadyHandUpgrade : Upgrade
    {
        public override string Id => "steady_hand";
        public override string Name => "Steady Hand";
        public override int Cost => 9;
        public override string Description => "+6% Heads chance.";
        public override UpgradeType Type => UpgradeType.Probability;
        public override double Value => 0.06;
    }
}
