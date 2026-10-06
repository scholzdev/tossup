using System.Collections.Generic;

namespace Tossup.UI
{
    // Text lookup. Source strings in the code are English; Resources/locales/<code>.json maps them to
    // another language. Lang.T("BACK") -> "ZURÜCK" (German). Unknown strings come back unchanged, so English
    // needs no table. Lang.T("LEVEL %d / 4", n) formats after the lookup, so a translation can move the number.
    // Names and descriptions of coins, chips, prizes and characters live in the table's
    // coins/items/relics/characters groups.
    public static class Lang
    {
        public static string Current = "en";
        public static readonly Dictionary<string, string> Names = new Dictionary<string, string> { { "en", "ENGLISH" }, { "de", "DEUTSCH" } };
        public static readonly List<string> Order = new List<string> { "en", "de" };

        static readonly Dictionary<string, JsonData> tables = new Dictionary<string, JsonData>
        {
            { "en", new JsonData { Kind = JsonKind.Object, Object = new Dictionary<string, JsonData>() } }
        };

        public static void Load(string code, string jsonSource)
        {
            if (!string.IsNullOrEmpty(jsonSource)) tables[code] = JsonData.Parse(jsonSource);
        }

        public static void Set(string code)
        {
            if (code != null && tables.ContainsKey(code)) Current = code;
        }

        public static string T(string text, params object[] args)
        {
            if (text == null) return null;
            if (tables[Current].Object.TryGetValue(text, out var translated) && translated.Kind == JsonKind.String) text = translated.String;
            return args.Length > 0 ? GameText.Format(text, args) : text;
        }

        // A field (name/description/short) of a coin, chip, prize or character in the current language.
        static string Def(string group, string id, string key, string fallback)
        {
            var entry = tables[Current][group]?[id];
            var value = entry?[key];
            if (value != null && value.Kind == JsonKind.String) return value.String;
            return fallback;
        }

        public static string CoinName(string id) => Def("coins", id, "name", Content.Coins[id].Name);
        public static string CoinHeadsDescription(string id) => Def("coins", id, "heads_description", Content.Coins[id].HeadsDescription);
        public static string CoinTailsDescription(string id) => Def("coins", id, "tails_description", Content.Coins[id].TailsDescription);
        public static string CoinEdgeDescription(string id) => Def("coins", id, "edge_description", Content.Coins[id].EdgeDescription);
        public static string ModifierName(string id) => Def("modifiers", id, "name", Game.Modifiers[id].Name);
        public static string ModifierDescription(string id) => Def("modifiers", id, "description", Game.Modifiers[id].Description);
        public static string CoinDescription(string id) => Def("coins", id, "description", Content.Coins[id].Description);
        public static string ItemName(string id) => Def("items", id, "name", Content.Items[id].Name);
        public static string ItemShort(string id) => Def("items", id, "short", Content.Items[id].Short);
        public static string ItemDescription(string id) => Def("items", id, "description", Content.Items[id].Description);
        public static string RelicName(string id) => Def("relics", id, "name", Content.Relics[id].Name);
        public static string RelicDescription(string id) => Def("relics", id, "description", Content.Relics[id].Description);
        public static string CharacterName(string id) => Def("characters", id, "name", Content.Characters[id].Name);
        public static string CharacterDescription(string id) => Def("characters", id, "description", Content.Characters[id].Description);

        // string.upper, including the German umlauts.
        public static string Upper(string s) => s.ToUpperInvariant();

        // The characters of a string (for the vertical shop labels).
        public static List<string> Chars(string s)
        {
            var list = new List<string>();
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsHighSurrogate(s[i]) && i + 1 < s.Length) { list.Add(s.Substring(i, 2)); i++; }
                else list.Add(s[i].ToString());
            }
            return list;
        }
    }
}
