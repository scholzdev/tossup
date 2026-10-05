namespace Tossup.Upgrades
{
    public sealed class CopperLiningUpgrade : Upgrade
    {
        public override string Id => "copper_lining";
        public override string Name => "Copper Lining";
        public override int Cost => 8;
        public override string Description => "Heads scores +1 point.";
        public override UpgradeType Type => UpgradeType.ScoreBonus;
        public override double Value => 1;
    }
}
