namespace Tossup
{
    public enum Rarity { Common, Uncommon, Rare, Epic }
    public enum CoinType { Steel, Blood, Greed, Chaos, Rhythm, Fortune }
    public enum UpgradeType { ScoreBonus, Probability }
    public enum EffectType { AllOdds, Amplify, BankDiscard, ComboBonus, ComboShield, Energy, ExtraDraw, ExtraExchange, FetchBest, FortuneOdds, Gold, GoldLoss, NextHeads, NextMult, NextOdds, NextSwap, Penalty, Probability, Score, TypeBuff }

    // Stable keys belong at persistence/localization/export boundaries.
    public static class DefinitionKeys
    {
        public static string RarityCode(Rarity rarity) => rarity == Rarity.Common ? "N" : rarity == Rarity.Uncommon ? "R" : rarity == Rarity.Rare ? "SR" : "UR";
        public static string Key(System.Enum value)
        {
            string name=value.ToString();var result=new System.Text.StringBuilder();
            for(int i=0;i<name.Length;i++){if(i>0&&char.IsUpper(name[i]))result.Append('_');result.Append(char.ToLowerInvariant(name[i]));}
            return result.ToString();
        }
        public static object Parse(System.Type type, string key)
        {
            foreach(System.Enum value in System.Enum.GetValues(type))if(Key(value)==key)return value;
            throw new System.FormatException("Unknown "+type.Name+": "+key);
        }
    }
}
