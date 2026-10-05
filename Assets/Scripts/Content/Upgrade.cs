namespace Tossup
{
    public abstract class Upgrade
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract int Cost { get; }
        public abstract string Description { get; }
        public abstract UpgradeType Type { get; }
        public abstract double Value { get; }
        public override string ToString() => Id;
    }
}
