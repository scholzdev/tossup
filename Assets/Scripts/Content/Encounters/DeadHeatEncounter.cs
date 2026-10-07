namespace Tossup.Encounters
{
    public sealed class DeadHeatEncounter : RunEncounterDef
    {
        public DeadHeatEncounter()
        {
            Id = "dead_heat";
            Name = "Dead Heat";
            Description = "A Tie breaks your combo and burns its pot. Banking a combo grants 2 extra gold.";
            BreaksComboOnTie = true;
            ComboBankBonus = 2;
        }
    }
}
