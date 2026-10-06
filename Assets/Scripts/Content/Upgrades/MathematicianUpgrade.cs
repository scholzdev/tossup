namespace Tossup.Upgrades
{
    public sealed class MathematicianUpgrade : Upgrade
    {
        public override string Id => "mathematician";
        public override string Name => "Mathematician";
        public override int Cost => 10;
        public override string Description => "+3% Heads chance.";
        public override UpgradeType Type => UpgradeType.Probability;
        public override double Value => 0.1;
    }
}
