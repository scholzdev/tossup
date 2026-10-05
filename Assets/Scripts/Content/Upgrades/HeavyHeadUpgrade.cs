namespace Tossup.Upgrades
{
    public sealed class HeavyHeadUpgrade : Upgrade
    {
        public override string Id => "heavy_head";
        public override string Name => "Heavy Head";
        public override int Cost => 12;
        public override string Description => "Heads scores +2 points.";
        public override UpgradeType Type => UpgradeType.ScoreBonus;
        public override double Value => 2;
    }
}
