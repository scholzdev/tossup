using System;

namespace Tossup
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CoinColorAttribute: Attribute
    {
        public string Color { get; }
        public CoinColorAttribute(string color)
        {
            if (color == null || color.Length != 6 || !uint.TryParse(color, System.Globalization.NumberStyles.HexNumber, null, out _))
                throw new ArgumentException("Coin colors must be six-digit RGB hex values without '#'.", nameof(color));
            Color = color.ToUpperInvariant();
        }
    }

    public enum Rarity { Common, Uncommon, Rare, Epic }
    public enum CoinType {
        // RGB hex without '#'; UI converts this metadata to its shared Rgba type.
        [CoinColor("BEC5CC")]
        Steel,
        [CoinColor("D94A59")]
        Blood,
        [CoinColor("E4B33E")]
        Greed,
        [CoinColor("BB6FE8")]
        Chaos,
        [CoinColor("41BFC4")]
        Rhythm,
        [CoinColor("61B879")]
        Fortune
    }
    public enum UpgradeType { ScoreBonus, Probability }
    public enum EffectType { AllOdds, Amplify, BankDiscard, ComboBonus, ComboShield, Energy, ExtraDraw, ExtraExchange, FetchBest, FortuneOdds, Gold, GoldLoss, NextHeads, NextMult, NextOdds, NextSwap, Penalty, Probability, Score, TypeBuff }
    public enum CharacterPerkType { StartEnergy, CoinDiscount, TailsGold, ExtraExchange, ComboStep, ComboShield }

    // Stable keys belong at persistence/localization/export boundaries.
    public static class DefinitionKeys
    {
        static readonly System.Collections.Generic.Dictionary<CoinType, string> coinColors = BuildCoinColors();

        static System.Collections.Generic.Dictionary<CoinType, string> BuildCoinColors()
        {
            var colors = new System.Collections.Generic.Dictionary<CoinType, string>();
            foreach (CoinType type in System.Enum.GetValues(typeof(CoinType)))
            {
                var field = typeof(CoinType).GetField(type.ToString());
                var attribute = (CoinColorAttribute)System.Attribute.GetCustomAttribute(field, typeof(CoinColorAttribute));
                if (attribute == null) throw new System.InvalidOperationException("Coin type has no color: " + type);
                colors.Add(type, attribute.Color);
            }
            return colors;
        }

        public static string CoinColorHex(CoinType type) => coinColors[type];
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
