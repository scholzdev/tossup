namespace Tossup.Upgrades
{
    public sealed class BloodlettingUpgrade : Upgrade
    {
        public override string Id => "bloodletting";
        public override string Name => "Bloodletting";
        public override int Cost => 12;
        public override string Description => "Heads scores +2 points.";
        public override UpgradeType Type => UpgradeType.ScoreBonus;
        public override double Value => 2;
    }
}
