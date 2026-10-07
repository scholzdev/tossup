using System;

namespace Tossup.Augments
{
    public sealed class HedgeFundAugment : AugmentDef
    {
        public HedgeFundAugment()
        {
            Id = "hedge_fund";
            Name = "Hedge Fund";
            Description = "A winning side bet pays 25% extra. Losing one adds 2 quota to the next level.";
            Tier = "silver";
        }

        public override int SideBetPayout(GameState game, int payout) => (int)Math.Floor(payout * 1.25 + .5);
        public override void OnSideBetLost(GameState game) => game.NextLevelQuotaBonus += 2;
    }
}
