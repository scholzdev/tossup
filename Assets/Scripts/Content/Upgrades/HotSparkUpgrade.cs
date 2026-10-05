namespace Tossup.Upgrades
{
    public sealed class HotSparkUpgrade : Upgrade
    {
        public override string Id => "hot_spark";
        public override string Name => "Hot Spark";
        public override int Cost => 9;
        public override string Description => "Heads scores +2 points.";
        public override UpgradeType Type => UpgradeType.ScoreBonus;
        public override double Value => 2;
    }
}
