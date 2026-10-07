namespace Tossup.Augments
{
    public sealed class BankersCutAugment : AugmentDef
    {
        public BankersCutAugment()
        {
            Id = "bankers_cut";
            Name = "Banker's Cut";
            Description = "Banked combo pots grant 2 extra gold. Each bank adds 2 quota to the next level.";
            Tier = "silver";
        }

        public override int BankBonus(GameState game)
        {
            game.NextLevelQuotaBonus += 2;
            return 2;
        }
    }
}
