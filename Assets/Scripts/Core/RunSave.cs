using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace Tossup
{
    // Versioned JSON snapshots of the pure game model. Reflection is deliberately confined to the
    // model's public data fields, keeping the format independent of Unity and free of executable data.
    public static class RunSave
    {
        public const int Version = 1;

        public static string Encode(GameState game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (!IsSafePoint(game)) throw new InvalidOperationException("runs are saved only at safe points");
            return "{\"version\":" + Version + ",\"game\":" + StateJson.Encode(game) + "}";
        }

        public static bool IsSafePoint(GameState game) => game != null && (game.Phase == Phase.Shop || game.Phase == Phase.Augment ||
            game.Phase == Phase.Contract || (game.Phase == Phase.Encounter && game.Mulligan == null));

        public static GameState Decode(string text)
        {
            try
            {
                var root = JsonData.Parse(text);
                if (root.Kind != JsonKind.Object || root["version"]?.Kind != JsonKind.Number || (int)root["version"].Number != Version)
                    return null;
                var game = (GameState)StateJson.Decode(root["game"], typeof(GameState));
                if (!Valid(game)) return null;
                Hooks.Unbind();
                Items.Clear();
                Relics.Bind(game);
                var flip = game.Pending ?? game.Dealt;
                if (flip != null)
                {
                    var coin = Game.GetCoin(game, flip.Uid);
                    if (coin == null) return null;
                    Hooks.Bind(game, coin);
                }
                return game;
            }
            catch { return null; }
        }

        static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        static bool Valid(GameState game)
        {
            if (game == null || !Content.Characters.ContainsKey(game.CharacterId) || game.Stake < 1 || game.Stake > Game.Stakes.Count ||
                game.RngState < 1 || game.Coins == null || game.Coins.Count < 1 || game.Coins.Count > Game.DeckMax ||
                game.Player == null || !Finite(game.Player.Gold) || !Finite(game.Player.Energy) || !Finite(game.Player.MaxEnergy)) return false;
            if(!IsSafePoint(game))return false;
            var uids = new HashSet<int>();
            foreach (var coin in game.Coins)
                if (coin == null || !Content.Coins.ContainsKey(coin.Id) || coin.Uid < 1 || !uids.Add(coin.Uid) || !Finite(coin.Bonus) ||
                    (coin.Upgrade != null && !Content.Coins[coin.Id].Upgrades.ContainsKey(coin.Upgrade))) return false;
            foreach (var id in game.Relics) if (!Content.Relics.ContainsKey(id)) return false;
            foreach (var id in game.Items) if (!Content.Items.ContainsKey(id)) return false;
            foreach (var id in game.ShopItems) if (id != null && !Content.Items.ContainsKey(id)) return false;
            foreach (var id in game.ShopOffers) if (id != null && !Content.Coins.ContainsKey(id)) return false;
            if (game.RunEncounterId != null && !Game.Encounters.ContainsKey(game.RunEncounterId)) return false;
            if (game.EncounterIndex < 1 || game.Slots < 1 || game.Slots > Game.DeckMax || game.NextUid < game.Coins.Count) return false;
            if (game.Encounter == null || !Finite(game.Encounter.Quota) || !Finite(game.Encounter.MaxQuota)) return false;
            foreach (var uid in game.Encounter.Queue) if (!uids.Contains(uid)) return false;
            foreach (var uid in game.Encounter.Pile) if (!uids.Contains(uid)) return false;
            foreach (var uid in game.Encounter.Played) if (!uids.Contains(uid)) return false;
            if (game.Dealt != null && !uids.Contains(game.Dealt.Uid)) return false;
            if (game.Pending != null && !uids.Contains(game.Pending.Uid)) return false;
            if (game.SelectedUid.HasValue && !uids.Contains(game.SelectedUid.Value)) return false;
            return true;
        }
    }

    static class StateJson
    {
        public static string Encode(object value)
        {
            var output = new StringBuilder();
            Write(output, value, value?.GetType() ?? typeof(object));
            return output.ToString();
        }

        static void Write(StringBuilder output, object value, Type declared)
        {
            if (value == null) { output.Append("null"); return; }
            Type type = value.GetType();
            if (type == typeof(string) || type.IsEnum) { output.Append(JsonData.Quote(value.ToString())); return; }
            if (type == typeof(bool)) { output.Append((bool)value ? "true" : "false"); return; }
            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(number) || double.IsInfinity(number)) throw new InvalidOperationException("cannot save a non-finite number");
                output.Append(number.ToString("R", CultureInfo.InvariantCulture)); return;
            }
            if (type.IsPrimitive) { output.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            if (value is IDictionary dictionary)
            {
                output.Append('{'); bool first = true;
                var entries = new List<DictionaryEntry>();
                foreach (DictionaryEntry entry in dictionary) entries.Add(entry);
                entries.Sort((a, b) => string.CompareOrdinal(Convert.ToString(a.Key, CultureInfo.InvariantCulture), Convert.ToString(b.Key, CultureInfo.InvariantCulture)));
                foreach (var entry in entries)
                {
                    if (!first) output.Append(','); first = false;
                    output.Append(JsonData.Quote(Convert.ToString(entry.Key, CultureInfo.InvariantCulture))).Append(':');
                    Write(output, entry.Value, entry.Value?.GetType() ?? typeof(object));
                }
                output.Append('}'); return;
            }
            if (value is IEnumerable sequence)
            {
                output.Append('['); bool first = true;
                foreach (var item in sequence)
                {
                    if (!first) output.Append(','); first = false;
                    Write(output, item, item?.GetType() ?? typeof(object));
                }
                output.Append(']'); return;
            }
            output.Append('{');
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) output.Append(',');
                output.Append(JsonData.Quote(fields[i].Name)).Append(':');
                Write(output, fields[i].GetValue(value), fields[i].FieldType);
            }
            output.Append('}');
        }

        public static object Decode(JsonData data, Type type)
        {
            if (data == null || data.Kind == JsonKind.Null)
            {
                if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null) return null;
                throw new FormatException("null for " + type.Name);
            }
            Type nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) return Decode(data, nullable);
            if (type == typeof(string)) return data.Kind == JsonKind.String ? data.String : throw new FormatException("expected string");
            if (type == typeof(bool)) return data.Kind == JsonKind.Boolean ? data.Boolean : throw new FormatException("expected boolean");
            if (type.IsEnum) return data.Kind == JsonKind.String ? Enum.Parse(type, data.String) : throw new FormatException("expected enum");
            if (type == typeof(double)) return Number(data);
            if (type == typeof(float)) return (float)Number(data);
            if (type == typeof(int)) return checked((int)Number(data));
            if (type == typeof(long)) return checked((long)Number(data));

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (data.Kind != JsonKind.Array) throw new FormatException("expected array");
                var list = (IList)Activator.CreateInstance(type); Type element = type.GetGenericArguments()[0];
                foreach (var item in data.Array) list.Add(Decode(item, element));
                return list;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                if (data.Kind != JsonKind.Array) throw new FormatException("expected array");
                object set = Activator.CreateInstance(type); MethodInfo add = type.GetMethod("Add"); Type element = type.GetGenericArguments()[0];
                foreach (var item in data.Array) add.Invoke(set, new[] { Decode(item, element) });
                return set;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                if (data.Kind != JsonKind.Object) throw new FormatException("expected object");
                var dictionary = (IDictionary)Activator.CreateInstance(type); Type[] args = type.GetGenericArguments();
                foreach (var pair in data.Object)
                {
                    object key = args[0] == typeof(string) ? pair.Key : args[0] == typeof(int) ? int.Parse(pair.Key, CultureInfo.InvariantCulture) : throw new FormatException("unsupported dictionary key");
                    dictionary.Add(key, Decode(pair.Value, args[1]));
                }
                return dictionary;
            }
            if (data.Kind != JsonKind.Object) throw new FormatException("expected object");
            object result = Activator.CreateInstance(type);
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (data.Object.TryGetValue(field.Name, out var fieldData)) field.SetValue(result, Decode(fieldData, field.FieldType));
            return result;
        }

        static double Number(JsonData data)
        {
            if (data.Kind != JsonKind.Number || double.IsNaN(data.Number) || double.IsInfinity(data.Number)) throw new FormatException("expected finite number");
            return data.Number;
        }
    }
}
