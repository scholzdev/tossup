namespace Tossup.Encounters
{
    public sealed class HighRollerTableEncounter : RunEncounterDef
    {
        public HighRollerTableEncounter()
        {
            Id = "high_roller_table";
            Name = "High-Roller Table";
            Description = "Side bets use double the stake and payout. A Tie loses the stake.";
            SideBetMultiplier = 2;
            SideBetTieLoses = true;
        }
    }
}
