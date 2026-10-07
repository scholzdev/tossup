namespace Tossup.Augments
{
    public sealed class BankersCutAugment : AugmentDef
    {
        public BankersCutAugment()
        {
            Id = "bankers_cut";
            Name = "Banker's Cut";
            Description = "Banked combo pots grant 2 extra gold. Each bank gives the next enemy a 2 point head start.";
            Tier = "silver";
        }

        public override int BankBonus(GameState game)
        {
            game.NextFightEdge += 2;
            return 2;
        }
    }
}
