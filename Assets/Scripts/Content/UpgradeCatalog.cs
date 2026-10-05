using System.Collections.Generic;
using Tossup.Upgrades;

namespace Tossup
{
    public static class UpgradeCatalog
    {
        public static readonly SaferBetUpgrade SaferBet = new SaferBetUpgrade();
        public static readonly GildedFaceUpgrade GildedFace = new GildedFaceUpgrade();
        public static readonly KeenEdgeUpgrade KeenEdge = new KeenEdgeUpgrade();
        public static readonly TrueAimUpgrade TrueAim = new TrueAimUpgrade();
        public static readonly HotSparkUpgrade HotSpark = new HotSparkUpgrade();
        public static readonly ReliableSparkUpgrade ReliableSpark = new ReliableSparkUpgrade();
        public static readonly DeepCutUpgrade DeepCut = new DeepCutUpgrade();
        public static readonly SteadyHandUpgrade SteadyHand = new SteadyHandUpgrade();
        public static readonly CopperLiningUpgrade CopperLining = new CopperLiningUpgrade();
        public static readonly BrightSideUpgrade BrightSide = new BrightSideUpgrade();
        public static readonly LuckyDayUpgrade LuckyDay = new LuckyDayUpgrade();
        public static readonly MathematicianUpgrade Mathematician = new MathematicianUpgrade();
        public static readonly BloodlettingUpgrade Bloodletting = new BloodlettingUpgrade();
        public static readonly SureStrikeUpgrade SureStrike = new SureStrikeUpgrade();
        public static readonly HeavyHeadUpgrade HeavyHead = new HeavyHeadUpgrade();
        public static readonly BalancedGripUpgrade BalancedGrip = new BalancedGripUpgrade();
        public static readonly ClearMindUpgrade ClearMind = new ClearMindUpgrade();
        public static readonly FollowThroughUpgrade FollowThrough = new FollowThroughUpgrade();
        public static readonly IReadOnlyList<Upgrade> Ordered = new Upgrade[]
        {
            SaferBet,
            GildedFace,
            KeenEdge,
            TrueAim,
            HotSpark,
            ReliableSpark,
            DeepCut,
            SteadyHand,
            CopperLining,
            BrightSide,
            LuckyDay,
            Mathematician,
            Bloodletting,
            SureStrike,
            HeavyHead,
            BalancedGrip,
            ClearMind,
            FollowThrough
        };
        public static readonly IReadOnlyDictionary<string, Upgrade> ById = Index();
        static Dictionary<string, Upgrade> Index()
        {
            var result = new Dictionary<string, Upgrade>();
            foreach (var upgrade in Ordered) result.Add(upgrade.Id, upgrade);
            return result;
        }
    }
}
