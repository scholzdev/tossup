using System;
using System.Collections.Generic;

namespace Tossup.Augments
{
    public sealed class ReforgerAugment : AugmentDef
    {
        public override string ChoiceHeading => "CHOOSE A COIN TO REFORGE";
        public ReforgerAugment()
        {
            Id = "reforger";
            Name = "Reforger";
            Description = "Reforge one coin now into a random different coin of the same rarity.";
            Tier = "silver";
        }

        static bool HasAlternative(CoinInst owned)
        {
            foreach (var candidate in Content.CoinOrder)
                if (candidate.Id != owned.Id && candidate.Rarity == owned.Definition.Rarity) return true;
            return false;
        }

        public override bool Available(GameState game)
        {
            foreach (var coin in game.Coins) if (HasAlternative(coin)) return true;
            return false;
        }

        public override AugmentPending OnChosen(GameState game) => new AugmentPending { Id = Id };

        public override List<AugmentChoice> Choices(GameState game, AugmentPending pending)
        {
            var result = new List<AugmentChoice>();
            foreach (var coin in game.Coins)
                if (HasAlternative(coin))
                    result.Add(new AugmentChoice { Key = coin.Uid.ToString(), CoinId = coin.Id,
                        Detail = "Random coin of the same rarity." });
            return result;
        }

        public override void ApplyChoice(GameState game, AugmentPending pending, AugmentChoice choice)
        {
            var old = Game.GetCoin(game, int.Parse(choice.Key));
            var ids = new List<string>();
            foreach (var coin in Content.CoinOrder)
                if (coin.Id != old.Id && coin.Rarity == old.Definition.Rarity) ids.Add(coin.Id);
            ReplaceCoin(game, old, ids[Rng.Int(game, 1, ids.Count) - 1]);
        }
    }
}
