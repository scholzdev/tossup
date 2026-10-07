using System;

namespace Tossup.Encounters
{
    public sealed class HouseClockEncounter : RunEncounterDef
    {
        public HouseClockEncounter()
        {
            Id = "house_clock";
            Name = "The House's Clock";
            Description = "Every third flip is inverted. Clearing a level pays 25% more.";
            InvertsFlip = flip => flip % 3 == 0;
            EncounterStart = (game, encounter) =>
            {
                if (encounter.Payout.HasValue)
                    encounter.Payout = Math.Floor(encounter.Payout.Value * 1.25 + .5);
            };
        }
    }
}
