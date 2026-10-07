using System.Collections.Generic;

namespace Tossup.Augments
{
    public sealed class EpicWindfallAugment : AugmentDef
    {
        public override string ChoiceHeading => "CHOOSE A COIN TO REPLACE";
        public EpicWindfallAugment()
        {
            Id = "epic_windfall";
            Name = "Epic Windfall";
            Description = "Gain a random Epic coin. If your bank is full, choose a coin to replace.";
            Tier = "gold";
        }

        public override bool Available(GameState game)
        {
            if (game.EncounterIndex + 1 != 6) return false;
            foreach (var coin in Content.CoinOrder)
                if (coin.Rarity == Rarity.Epic) return true;
            return false;
        }

        public override AugmentPending OnChosen(GameState game)
        {
            var ids = new List<string>();
            foreach (var coin in Content.CoinOrder)
                if (coin.Rarity == Rarity.Epic) ids.Add(coin.Id);
            string reward = ids[Rng.Int(game, 1, ids.Count) - 1];
            if (game.Coins.Count < game.Slots)
            {
                Game.AddToDeck(game, Content.Coins[reward]);
                return null;
            }
            return new AugmentPending { Id = Id, RewardId = reward };
        }

        public override List<AugmentChoice> Choices(GameState game, AugmentPending pending)
        {
            var result = new List<AugmentChoice>();
            foreach (var coin in game.Coins)
                result.Add(new AugmentChoice { Key = coin.Uid.ToString(), CoinId = coin.Id,
                    Detail = "Replace this coin." });
            return result;
        }

        public override void ApplyChoice(GameState game, AugmentPending pending, AugmentChoice choice) =>
            ReplaceCoin(game, Game.GetCoin(game, int.Parse(choice.Key)), pending.RewardId);
    }
}
