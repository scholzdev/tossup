using System.Collections.Generic;

namespace Tossup
{
    public class AugmentDef
    {
        public string Id, Name, Description, Tier;
        public virtual string ChoiceHeading => Name.ToUpperInvariant();
        public virtual bool ChoicesAreTypes => false;
        public virtual string Detail(GameState game) => Description;
        public virtual bool Available(GameState game) => true;
        public virtual AugmentPending OnChosen(GameState game) => null;
        public virtual List<AugmentChoice> Choices(GameState game, AugmentPending pending) => new List<AugmentChoice>();
        public virtual void ApplyChoice(GameState game, AugmentPending pending, AugmentChoice choice) { }
        public virtual void OnRunHook(GameState game, RunHookEvent evt) { }
        public virtual int BankBonus(GameState game) => 0;
        public virtual void OnResolve(GameState game, CoinInst coin, string result, Res resolution) { }

        protected static void ReplaceCoin(GameState game, CoinInst old, string id)
        {
            int index = game.Coins.IndexOf(old);
            var replacement = Game.NewCoin(game, id);
            game.Coins[index] = replacement;
            game.SelectedUid = replacement.Uid;
        }
    }
}
