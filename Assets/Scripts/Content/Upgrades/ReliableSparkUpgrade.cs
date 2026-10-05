namespace Tossup.Upgrades
{
    public sealed class ReliableSparkUpgrade : Upgrade
    {
        public override string Id => "reliable_spark";
        public override string Name => "Reliable Spark";
        public override int Cost => 10;
        public override string Description => "+7% Heads chance.";
        public override UpgradeType Type => UpgradeType.Probability;
        public override double Value => 0.07;
    }
}
