using System;

namespace Tossup
{
    [Flags]
    public enum MasterySides { None = 0, Heads = 1, Tails = 2, Edge = 4, All = Heads | Tails | Edge }

    // A coin chooses its own progress hook and reward hooks. This describes only
    // the three account-wide thresholds shown to the player.
    public sealed class CoinMastery
    {
        public string ProgressDescription { get; }
        public double[] Thresholds { get; }
        public string[] Rewards { get; }
        public MasterySides[] RewardSides { get; }

        public CoinMastery(string progressDescription, double first, double second, double third,
            string firstReward, string secondReward, string thirdReward,
            MasterySides firstSide = MasterySides.None, MasterySides secondSide = MasterySides.None,
            MasterySides thirdSide = MasterySides.None)
        {
            if (string.IsNullOrWhiteSpace(progressDescription) || first <= 0 || second <= first || third <= second)
                throw new ArgumentException("Mastery thresholds must be positive and increasing.");
            ProgressDescription = progressDescription;
            // Mastery progress is earned per coin, so halve the original grind for every coin.
            Thresholds = new[] { Math.Ceiling(first / 2), Math.Ceiling(second / 2), Math.Ceiling(third / 2) };
            Rewards = new[] { firstReward, secondReward, thirdReward };
            RewardSides = new[] { firstSide, secondSide, thirdSide };
        }

        public System.Collections.Generic.IEnumerable<int> OutcomeRewardLevels(OutcomeSide side, int level)
        {
            var flag = side == OutcomeSide.Heads ? MasterySides.Heads :
                side == OutcomeSide.Tails ? MasterySides.Tails : MasterySides.Edge;
            for (int i = 0; i < Math.Min(level, Rewards.Length); i++)
                if ((RewardSides[i] & flag) != 0) yield return i;
        }
    }

    public sealed class CoinMasteryState
    {
        readonly GameState game;
        readonly ProfileData profile;
        readonly CoinDef definition;

        internal CoinMasteryState(GameState game, CoinDef definition)
        { this.game = game; profile = game?.MasteryProfile; this.definition = definition; }

        public double Progress => Profile.MasteryProgress(profile, definition);
        public int Level => Profile.MasteryLevel(profile, definition);
        public void Add(double amount)
        {
            int before = Level;
            Profile.AddMastery(profile, definition, amount);
            int after = Level;
            if (after > before && game != null)
                Game.Log(game, definition.Name + " mastery level " + after + " unlocked: " + definition.Mastery.Rewards[after - 1]);
        }
    }
}
