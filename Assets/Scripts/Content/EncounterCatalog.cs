using System.Collections.Generic;
using Tossup.Encounters;

namespace Tossup
{
    public static class EncounterCatalog
    {
        public static readonly HouseClockEncounter HouseClock = new HouseClockEncounter();
        public static readonly DeadHeatEncounter DeadHeat = new DeadHeatEncounter();
        public static readonly ThinMarketEncounter ThinMarket = new ThinMarketEncounter();
        public static readonly UpgradeEncounter Upgrade = new UpgradeEncounter();
        public static readonly GuildHallEncounter GuildHall = new GuildHallEncounter();
        public static readonly TypeMarketEncounter TypeMarket = new TypeMarketEncounter();

        public static readonly List<RunEncounterDef> Ordered = new List<RunEncounterDef>
        {
            HouseClock, DeadHeat, ThinMarket, Upgrade, GuildHall, TypeMarket,
        };
    }
}
