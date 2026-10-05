namespace Tossup.Upgrades
{
    public sealed class LuckyDayUpgrade : Upgrade
    {
        public override string Id => "lucky_day";
        public override string Name => "Lucky Day";
        public override int Cost => 8;
        public override string Description => "Heads scores +1 point.";
        public override UpgradeType Type => UpgradeType.ScoreBonus;
        public override double Value => 1;
    }
}
