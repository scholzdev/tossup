namespace Tossup.Upgrades
{
    public sealed class GildedFaceUpgrade : Upgrade
    {
        public override string Id => "gilded_face";
        public override string Name => "Gilded Face";
        public override int Cost => 9;
        public override string Description => "Heads scores +2 points.";
        public override UpgradeType Type => UpgradeType.ScoreBonus;
        public override double Value => 2;
    }
}
