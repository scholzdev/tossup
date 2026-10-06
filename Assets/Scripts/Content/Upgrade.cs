namespace Tossup
{
    public abstract class Upgrade
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract int Cost { get; }
        public abstract string Description { get; }
        // Legacy scalar fields remain available to old content. New upgrades should override
        // Changes so they can add effects to any outcome or introduce a targeted buff.
        public virtual UpgradeType Type => UpgradeType.ScoreBonus;
        public virtual double Value => 0;
        public virtual System.Collections.Generic.IReadOnlyList<UpgradeChange> Changes
        {
            get
            {
                if (Value == 0) return System.Array.Empty<UpgradeChange>();
                return Type == UpgradeType.Probability
                    ? new[] { UpgradeChange.AddHeadsProbability(Value) }
                    : new[] { UpgradeChange.AddEffect(OutcomeSide.Heads, Effect.Score(Value)) };
            }
        }
        public override string ToString() => Id;
    }
}
