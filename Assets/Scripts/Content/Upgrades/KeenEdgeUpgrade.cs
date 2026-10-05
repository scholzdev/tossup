namespace Tossup.Upgrades
{
    public sealed class KeenEdgeUpgrade : Upgrade
    {
        public override string Id => "keen_edge";
        public override string Name => "Keen Edge";
        public override int Cost => 8;
        public override string Description => "Heads scores +1 point.";
        public override UpgradeType Type => UpgradeType.ScoreBonus;
        public override double Value => 1;
    }
}
