namespace Tossup.Upgrades
{
    public sealed class DeepCutUpgrade : Upgrade
    {
        public override string Id => "deep_cut";
        public override string Name => "Deep Cut";
        public override int Cost => 8;
        public override string Description => "Heads scores +1 point.";
        public override UpgradeType Type => UpgradeType.ScoreBonus;
        public override double Value => 1;
    }
}
