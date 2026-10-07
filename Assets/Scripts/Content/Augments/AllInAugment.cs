using System;

namespace Tossup.Augments
{
    public sealed class AllInAugment : AugmentDef
    {
        public AllInAugment()
        {
            Id = "all_in";
            Name = "All-In";
            Description = "The first successful push each level doubles its combo payout. A failed push costs 2 gold.";
            Tier = "silver";
        }

        public override string OnPush(GameState game, string result, string previousSide, ref double combo)
        {
            if (!game.Encounter.PushUsed) return null;
            if (result == previousSide && !game.Encounter.AllInPaid)
            {
                combo *= 2;
                game.Encounter.AllInPaid = true;
                return "All-In push succeeded: combo payout doubled.";
            }
            double loss = Math.Min(2, game.Player.Gold);
            game.Player.Gold -= loss;
            return "All-In push failed: -" + GameText.Num(loss) + " gold.";
        }
    }
}
