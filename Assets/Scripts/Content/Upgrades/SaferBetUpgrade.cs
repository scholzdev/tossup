namespace Tossup.Upgrades
{
    public sealed class SaferBetUpgrade : Upgrade
    {
        public override string Id => "safer_bet";
        public override string Name => "Safer Bet";
        public override int Cost => 10;
        public override string Description => "+7% Heads chance.";
        public override UpgradeType Type => UpgradeType.Probability;
        public override double Value => 0.07;
    }
}
