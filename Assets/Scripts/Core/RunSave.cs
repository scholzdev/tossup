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

        public static bool IsSafePoint(GameState game) => game != null && game.Pending == null && (game.Phase == Phase.Shop || game.Phase == Phase.Augment ||
            (game.Phase == Phase.Contract || game.Phase == Phase.Encounter) && game.Mulligan != null && game.Encounter != null && game.Encounter.Flips == 0);

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

        static bool Valid(GameState g)
        {
            if (g == null || g.CharacterId == null || !Content.Characters.ContainsKey(g.CharacterId) ||
                g.Stake < 1 || g.Stake > Game.Stakes.Count || g.RngState < 1 || g.RngState >= 2147483647 ||
                g.Coins == null || g.Coins.Count < 1 || g.Coins.Count > Game.DeckMax ||
                g.Player == null || !Finite(g.Player.Gold) || g.Player.Gold < 0 || !Finite(g.Player.Energy) || !Finite(g.Player.MaxEnergy) ||
                !IsSafePoint(g) || g.EncounterIndex < 1 || g.Cleared < 0 || g.Slots < 1 || g.Slots > Game.DeckMax ||
                !Finite(g.FortuneBonus) || g.FortuneBonus < 0 || g.FortuneBonus > .55 ||
                !Finite(g.NextLevelQuotaBonus) || g.NextLevelQuotaBonus < 0 || g.RerollStep < 1 || g.RerollStep > 2 || g.RerollCost < 0) return false;
            var uids = new HashSet<int>();
            if (g.LastResult != null && g.LastResult.CoinId != null && !Content.Coins.ContainsKey(g.LastResult.CoinId)) return false;
            foreach (var c in g.Coins)
                if (c == null || c.Id == null || !Content.Coins.ContainsKey(c.Id) || c.Uid < 1 || c.Uid > g.NextUid || !uids.Add(c.Uid) ||
                    !Finite(c.Bonus) || !Finite(c.Charge) || !Finite(c.Debt) || !Finite(c.Stack) || !Finite(c.Anger) ||
                    c.CompostLevel < 0 || c.FetchedLevel < 0 || (c.Upgrade != null && !c.Definition.TryGetUpgrade(c.Upgrade.Id,out _))) return false;
            if (g.SelectedUid.HasValue && !uids.Contains(g.SelectedUid.Value)) return false;
            if (!Ids(g.Relics, Content.Relics.ContainsKey) || !Ids(g.Items, Content.Items.ContainsKey) ||
                !Ids(g.Unlocked, Content.Coins.ContainsKey) || !Ids(g.Purchased, Content.Coins.ContainsKey) ||
                !Ids(g.ShopItems, Content.Items.ContainsKey, true) || g.ShopOffers == null || g.ShopOffers.Exists(c => c != null && (!Content.Coins.TryGetValue(c.Id,out var known) || known != c)) ||
                g.Log == null || g.Log.Exists(x => x == null) || g.ShopUpgrades == null || g.ShopUpgrades.Count > g.ShopOffers.Count ||
                g.Shop == null || g.Shop.RefreshCost < 0 || g.Shop.CoinOfferCount < 1 || g.Shop.CoinOfferCount > 99 ||
                !Finite(g.Shop.CoinPriceDiscount) || g.Shop.CoinPriceDiscount < 0 ||
                g.ShopRelic != null && !Content.Relics.ContainsKey(g.ShopRelic) ||
                g.RunEncounterId != null && !Game.Encounters.ContainsKey(g.RunEncounterId)) return false;
            for (int i = 0; i < g.ShopUpgrades.Count; i++)
                if (g.ShopUpgrades[i] != null && (g.ShopOffers[i] == null || !g.ShopOffers[i].TryGetUpgrade(g.ShopUpgrades[i].Id,out _))) return false;
            if (!Ids(g.Augments, Game.AugmentDefs.ContainsKey) || g.Augments.Count > 2 || new HashSet<string>(g.Augments).Count != g.Augments.Count || g.AugmentData == null) return false;
            if (g.AugmentData.TryGetValue("type_specialist", out var kind) && (kind == null || !g.Augments.Contains("type_specialist") || !KnownType(kind))) return false;
            if (g.Phase == Phase.Augment)
            {
                if (!g.AugmentLevel.HasValue || g.AugmentLevel != g.EncounterIndex + 1 || (g.AugmentLevel != 3 && g.AugmentLevel != 6)) return false;
                if (g.AugmentPending != null)
                {
                    var pending = g.AugmentPending;
                    if (g.AugmentOptions != null || pending.Id == null || !g.Augments.Contains(pending.Id) ||
                        (pending.Id != "epic_windfall" && pending.Id != "reforger" && pending.Id != "type_specialist" && pending.Id != "upgrade_press") ||
                        (pending.Id == "epic_windfall" && (pending.RewardId == null || !Content.Coins.ContainsKey(pending.RewardId) || Content.Coins[pending.RewardId].Rarity != Rarity.Epic)) ||
                        (pending.Id != "epic_windfall" && pending.RewardId != null) || Game.AugmentChoices(g).Count == 0) return false;
                }
                else if (!Ids(g.AugmentOptions, Game.AugmentDefs.ContainsKey) || g.AugmentOptions.Count != 3 ||
                    new HashSet<string>(g.AugmentOptions).Count != 3 || g.AugmentOptions.Exists(g.Augments.Contains)) return false;
            }
            else if (g.AugmentLevel != null || g.AugmentOptions != null || g.AugmentPending != null) return false;
            // The completed level is display-only in the shop; removed coins may still appear in its history.
            if (g.Phase == Phase.Shop) return g.Dealt == null && g.Pending == null;
            var e = g.Encounter;
            if (e == null || e.Name == null || !Finite(e.ElapsedSeconds) || e.ElapsedSeconds < 0 ||
                !Finite(e.Quota) || !Finite(e.MaxQuota) || !Finite(e.Scored) || !Finite(e.SurplusPaid) ||
                !Finite(e.Magnet) || !Finite(e.ComboLen) || !Finite(e.Shield) || !Finite(e.ComboStep) || !Finite(e.ComboCap) ||
                !Finite(e.ComboPot) || e.ComboPot < 0 || !Finite(e.BestComboLen) || e.BestComboLen < 0 || !Finite(e.GoldMult) ||
                (e.Payout.HasValue && !Finite(e.Payout.Value)) || e.Flips < 0 || e.Discards < 0 || e.Returned < 0 || e.BankDiscards < 0 ||
                e.Modifier != null && !Game.Modifiers.ContainsKey(e.Modifier) || !ValidSide(e.ComboSide)) return false;
            if (e.Contract != null && (e.Contract.Id == null || !Game.Contracts.ContainsKey(e.Contract.Id) ||
                (e.Contract.Result != null && e.Contract.Result != "COMPLETE" && e.Contract.Result != "MISSED") || !Finite(e.Contract.StartGold))) return false;
            if (e.ContractOptions != null && (g.Phase != Phase.Contract || e.Contract != null || !Ids(e.ContractOptions, Game.Contracts.ContainsKey) ||
                e.ContractOptions.Count != 3 || new HashSet<string>(e.ContractOptions).Count != 3)) return false;
            if (g.Phase == Phase.Contract && (!g.ContractsEnabled || e.ContractOptions == null || g.Mulligan == null)) return false;
            if (!UidList(e.Queue, uids) || !UidList(e.Pile, uids) || !UidList(e.Played, uids) || !UidList(e.Discarded, uids) ||
                !UidList(e.DealHooksFired, uids) || !UidList(e.TypeSpecialistPaid, uids) || !UidMap(e.Bonus, uids) || !UidMap(e.BestScores, uids) ||
                e.Buffs == null || e.Buffs.Exists(x => x == null || x.Kind == null || !Finite(x.Amount) || x.Left < 1)) return false;
            var live = new HashSet<int>();
            foreach (var uid in e.Queue) if (!live.Add(uid)) return false;
            foreach (var uid in e.Pile) if (!live.Add(uid)) return false;
            foreach (var uid in e.Played) if (!live.Add(uid)) return false;
            if (g.Mulligan != null && (!UidList(g.Mulligan.Hand, uids) || new HashSet<int>(g.Mulligan.Hand).Count != g.Mulligan.Hand.Count)) return false;
            if (g.Dealt != null && (!uids.Contains(g.Dealt.Uid) || !Finite(g.Dealt.Probability) || !Finite(g.Dealt.TieProbability) ||
                g.Dealt.Probability < 0 || g.Dealt.TieProbability < 0 || g.Dealt.Probability + g.Dealt.TieProbability > 1)) return false;
            return true;
        }

        static bool KnownType(string kind)
        {
            foreach (var coin in Content.Coins.Values) if (Array.Exists(new List<CoinType>(coin.Types).ToArray(),type=>DefinitionKeys.Key(type)==kind)) return true;
            return false;
        }

        static bool ValidSide(string side) => side == null || side == Side.Heads || side == Side.Tails || side == Side.Tie;
        static bool Ids(IEnumerable<string> ids, Func<string, bool> known, bool allowNull = false)
        {
            if (ids == null) return false;
            foreach (var id in ids) if (id == null ? !allowNull : !known(id)) return false;
            return true;
        }
        static bool UidList(IEnumerable<int> ids, HashSet<int> uids)
        {
            if (ids == null) return false;
            foreach (var id in ids) if (!uids.Contains(id)) return false;
            return true;
        }
        static bool UidMap(Dictionary<int, double> map, HashSet<int> uids)
        {
            if (map == null) return false;
            foreach (var pair in map) if (!uids.Contains(pair.Key) || !Finite(pair.Value)) return false;
            return true;
        }
    }

    static class StateJson
    {
        // The runtime carries definitions. Version-1 files continue to store the stable ID.
        static string FieldName(FieldInfo field) => field.DeclaringType == typeof(CoinInst) && field.Name == "Definition" ? "Id" : field.Name;
        public static string Encode(object value)
        {
            var output = new StringBuilder();
            Write(output, value, value?.GetType() ?? typeof(object));
            return output.ToString();
        }

        static void Write(StringBuilder output, object value, Type declared)
        {
            if (value == null) { output.Append("null"); return; }
            if (value is Upgrade upgrade) { output.Append(JsonData.Quote(upgrade.Id)); return; }
            if (value is CoinDef coin) { output.Append(JsonData.Quote(coin.Id)); return; }
            Type type = value.GetType();
            if (type == typeof(EffectType) || type == typeof(CoinType)) { output.Append(JsonData.Quote(DefinitionKeys.Key((Enum)value))); return; }
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
            Array.Sort(fields, (a, b) => string.CompareOrdinal(FieldName(a), FieldName(b)));
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) output.Append(',');
                output.Append(JsonData.Quote(FieldName(fields[i]))).Append(':');
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
            if (type == typeof(Upgrade))
                return data.Kind == JsonKind.String && UpgradeCatalog.ById.TryGetValue(data.String,out var upgrade) ? upgrade : throw new FormatException("unknown upgrade definition");
            if (type == typeof(CoinDef))
                return data.Kind == JsonKind.String && Content.Coins.TryGetValue(data.String,out var coin) ? coin : throw new FormatException("unknown coin definition");
            if (type == typeof(string)) return data.Kind == JsonKind.String ? data.String : throw new FormatException("expected string");
            if (type == typeof(bool)) return data.Kind == JsonKind.Boolean ? data.Boolean : throw new FormatException("expected boolean");
            if (type.IsEnum)
            {
                if (type == typeof(EffectType) || type == typeof(CoinType))
                    return data.Kind == JsonKind.String ? DefinitionKeys.Parse(type,data.String) : throw new FormatException("expected named enum");
                if (data.Kind != JsonKind.String || !Array.Exists(Enum.GetNames(type), x => x == data.String)) throw new FormatException("expected named enum");
                return Enum.Parse(type, data.String);
            }
            if (type == typeof(double)) return Number(data);
            if (type == typeof(float)) return (float)Number(data);
            if (type == typeof(int)) { double n = Number(data); if (n != Math.Floor(n)) throw new FormatException("expected integer"); return checked((int)n); }
            if (type == typeof(long)) { double n = Number(data); if (n != Math.Floor(n)) throw new FormatException("expected integer"); return checked((long)n); }

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
                if (data.Object.TryGetValue(FieldName(field), out var fieldData)) field.SetValue(result, Decode(fieldData, field.FieldType));
            return result;
        }

        static double Number(JsonData data)
        {
            if (data.Kind != JsonKind.Number || double.IsNaN(data.Number) || double.IsInfinity(data.Number)) throw new FormatException("expected finite number");
            return data.Number;
        }
    }
}
