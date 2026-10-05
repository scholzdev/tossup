namespace Tossup.Upgrades
{
    public sealed class SureStrikeUpgrade : Upgrade
    {
        public override string Id => "sure_strike";
        public override string Name => "Sure Strike";
        public override int Cost => 13;
        public override string Description => "+6% Heads chance.";
        public override UpgradeType Type => UpgradeType.Probability;
        public override double Value => 0.06;
    }
}
