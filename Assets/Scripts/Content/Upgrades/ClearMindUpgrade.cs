namespace Tossup.Upgrades
{
    public sealed class ClearMindUpgrade : Upgrade
    {
        public override string Id => "clear_mind";
        public override string Name => "Clear Mind";
        public override int Cost => 10;
        public override string Description => "+8% Heads chance.";
        public override UpgradeType Type => UpgradeType.Probability;
        public override double Value => 0.08;
    }
}
