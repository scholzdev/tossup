namespace Tossup.Upgrades
{
    public sealed class BrightSideUpgrade : Upgrade
    {
        public override string Id => "bright_side";
        public override string Name => "Bright Side";
        public override int Cost => 9;
        public override string Description => "+8% Heads chance.";
        public override UpgradeType Type => UpgradeType.Probability;
        public override double Value => 0.08;
    }
}
