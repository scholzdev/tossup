namespace Tossup.Upgrades
{
    public sealed class FollowThroughUpgrade : Upgrade
    {
        public override string Id => "follow_through";
        public override string Name => "Follow Through";
        public override int Cost => 10;
        public override string Description => "Heads scores +2 points.";
        public override UpgradeType Type => UpgradeType.ScoreBonus;
        public override double Value => 2;
    }
}
