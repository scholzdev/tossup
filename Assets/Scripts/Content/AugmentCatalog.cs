using System.Collections.Generic;
using Tossup.Augments;

namespace Tossup
{
    public static class AugmentCatalog
    {
        public static readonly BankersCutAugment BankersCut = new BankersCutAugment();
        public static readonly AllInAugment AllIn = new AllInAugment();
        public static readonly HedgeFundAugment HedgeFund = new HedgeFundAugment();
        public static readonly ScrapDealerAugment ScrapDealer = new ScrapDealerAugment();
        public static readonly EpicWindfallAugment EpicWindfall = new EpicWindfallAugment();
        public static readonly ReforgerAugment Reforger = new ReforgerAugment();
        public static readonly TypeSpecialistAugment TypeSpecialist = new TypeSpecialistAugment();
        public static readonly UpgradePressAugment UpgradePress = new UpgradePressAugment();

        public static readonly List<AugmentDef> Ordered = new List<AugmentDef>
        {
            BankersCut, AllIn, HedgeFund, ScrapDealer, EpicWindfall, Reforger, TypeSpecialist,
        };

        public static readonly Dictionary<string, AugmentDef> ById = Index();
        public static readonly List<string> Ids = IdList();

        static Dictionary<string, AugmentDef> Index()
        {
            var result = new Dictionary<string, AugmentDef>();
            foreach (var augment in Ordered) result.Add(augment.Id, augment);
            result.Add(UpgradePress.Id, UpgradePress); // decode old saved runs
            return result;
        }

        static List<string> IdList()
        {
            var result = new List<string>();
            foreach (var augment in Ordered) result.Add(augment.Id);
            return result;
        }
    }
}
