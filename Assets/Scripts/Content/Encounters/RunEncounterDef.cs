using System;

namespace Tossup
{
    public class RunEncounterDef
    {
        public string Id, Name, Description;
        public Action<GameState> RunStart;
        public Action<GameState, Encounter> EncounterStart;
        public Func<int, bool> InvertsFlip;
        public Func<GameState, CoinDef, int> CoinDiscount;
        public int SideBetMultiplier = 1;
        public bool SideBetTieLoses;
        public bool BreaksComboOnTie;
        public int ComboBankBonus;
    }
}
