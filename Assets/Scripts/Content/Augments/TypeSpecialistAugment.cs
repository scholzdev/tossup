using System;
using System.Collections.Generic;

namespace Tossup.Augments
{
    public sealed class TypeSpecialistAugment : AugmentDef
    {
        public override string ChoiceHeading => "CHOOSE A COIN TYPE";
        public override bool ChoicesAreTypes => true;
        public override string Detail(GameState game) => game.AugmentData.TryGetValue(Id, out var selected)
            ? Description + " [" + selected.ToUpperInvariant() + "]" : Description;
        public TypeSpecialistAugment()
        {
            Id = "type_specialist";
            Name = "Type Specialist";
            Description = "Choose a coin type you own. Each coin of that type scores +1 on its first Heads each level.";
            Tier = "silver";
        }

        public override bool Available(GameState game)
        {
            foreach (var coin in game.Coins)
                if (coin.Definition.Types.Count > 0) return true;
            return false;
        }

        public override AugmentPending OnChosen(GameState game) => new AugmentPending { Id = Id };

        public override List<AugmentChoice> Choices(GameState game, AugmentPending pending)
        {
            var result = new List<AugmentChoice>();
            var seen = new HashSet<CoinType>();
            foreach (var coin in game.Coins)
                foreach (var type in coin.Definition.Types)
                    if (seen.Add(type)) result.Add(new AugmentChoice { Key = DefinitionKeys.Key(type),
                        Title = type.ToString().ToUpperInvariant(), Detail = "First Heads per coin each level: +1 point" });
            result.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));
            return result;
        }

        public override void ApplyChoice(GameState game, AugmentPending pending, AugmentChoice choice) =>
            game.AugmentData[Id] = choice.Key;

        public override void OnResolve(GameState game, CoinInst coin, string result, Res resolution)
        {
            if (result == Side.Heads && game.AugmentData.TryGetValue(Id, out var type) &&
                Game.HasType(coin, type) && game.Encounter.TypeSpecialistPaid.Add(coin.Uid))
                resolution.Effects.Add(Effect.Score(1));
        }
    }
}
