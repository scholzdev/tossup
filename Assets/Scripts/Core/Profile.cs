using System;
using System.Collections.Generic;
using System.Text;

namespace Tossup
{
    public sealed class CoinSet
    {
        public string Name;
        public List<string> Coins = new List<string>();
    }

    public sealed class Options
    {
        public bool ScreenShake = true, FastFlip, Fullscreen, SeenHelp;
        public string Language = "en";
        public double VolumeMaster = 80, VolumeMusic = 40, VolumeSfx = 80;

        public Options Copy() => (Options)MemberwiseClone();

        // The switches on the Options screen, by their save-file key.
        public bool GetSwitch(string key)
        {
            switch (key)
            {
                case "screen_shake": return ScreenShake;
                case "fast_flip": return FastFlip;
                case "fullscreen": return Fullscreen;
                case "seen_help": return SeenHelp;
            }
            throw new ArgumentException("unknown option " + key);
        }

        public void SetSwitch(string key, bool value)
        {
            switch (key)
            {
                case "screen_shake": ScreenShake = value; break;
                case "fast_flip": FastFlip = value; break;
                case "fullscreen": Fullscreen = value; break;
                case "seen_help": SeenHelp = value; break;
                default: throw new ArgumentException("unknown option " + key);
            }
        }

        public double GetVolume(string key)
        {
            switch (key)
            {
                case "volume_master": return VolumeMaster;
                case "volume_music": return VolumeMusic;
                case "volume_sfx": return VolumeSfx;
            }
            throw new ArgumentException("unknown volume " + key);
        }

        public void SetVolume(string key, double value)
        {
            switch (key)
            {
                case "volume_master": VolumeMaster = value; break;
                case "volume_music": VolumeMusic = value; break;
                case "volume_sfx": VolumeSfx = value; break;
                default: throw new ArgumentException("unknown volume " + key);
            }
        }
    }

    // Meta progression that survives between runs: tokens, coin unlocks, coin sets and options.
    public sealed class ProfileData
    {
        public double Tokens;
        public Dictionary<string, HashSet<string>> Unlocked = new Dictionary<string, HashSet<string>>();
        public HashSet<string> Collected = new HashSet<string>();
        public Dictionary<string, List<CoinSet>> Sets = new Dictionary<string, List<CoinSet>>();
        public Dictionary<string, int> ActiveSet = new Dictionary<string, int>();
        public HashSet<string> Wins = new HashSet<string>();
        public Dictionary<string, int> Stakes = new Dictionary<string, int>();
        public Dictionary<string, int> BestEndless = new Dictionary<string, int>();
        public Dictionary<string, double> CoinMastery = new Dictionary<string, double>();
        [NonSerialized] public bool MasteryDirty;
        public Options Options = new Options();
    }

    // Pure data + JSON serialization.
    public static class Profile
    {
        public const int SetCount = 3; // coin sets per character
        public static int RarityLimit(string coinId)
        {
            switch(Content.Coins[coinId].Rarity){case Rarity.Common:return 3;case Rarity.Uncommon:return 2;case Rarity.Rare:case Rarity.Epic:return 1;default:return 0;}
        }

        public static ProfileData New() => new ProfileData();

        public static double MasteryProgress(ProfileData profile, CoinDef coin) =>
            profile != null && coin?.Mastery != null && profile.CoinMastery.TryGetValue(coin.Id, out var progress)
                ? progress : 0;

        public static int MasteryLevel(ProfileData profile, CoinDef coin)
        {
            if (coin?.Mastery == null) return 0;
            double progress = MasteryProgress(profile, coin);
            int level = 0;
            foreach (var threshold in coin.Mastery.Thresholds)
                if (progress >= threshold) level++;
            return level;
        }

        public static void AddMastery(ProfileData profile, CoinDef coin, double amount)
        {
            if (profile == null || coin?.Mastery == null || amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount)) return;
            double old = MasteryProgress(profile, coin);
            double cap = coin.Mastery.Thresholds[2];
            double updated = Math.Min(cap, old + amount);
            if (updated <= old) return;
            profile.CoinMastery[coin.Id] = updated;
            profile.MasteryDirty = true;
        }

        public static bool CharacterUnlocked(ProfileData profile, string id)
        {
            int i=Content.CharacterOrder.IndexOf(id);return i==0||(i>0&&profile.Wins.Contains(Content.CharacterOrder[i-1]));
        }
        public static string RecordWin(ProfileData profile,string id){if(!profile.Wins.Add(id))return null;int i=Content.CharacterOrder.IndexOf(id);return i>=0&&i+1<Content.CharacterOrder.Count?Content.CharacterOrder[i+1]:null;}
        public static int MaxStake(ProfileData profile,string id) => Math.Max(1, Math.Min(Game.Stakes.Count, profile.Stakes.TryGetValue(id,out var n) ? n : profile.Wins.Contains(id) ? 2 : 1));
        public static int? RecordStakeWin(ProfileData profile,string id,int stake){int max=MaxStake(profile,id);if(stake>=max&&stake<Game.Stakes.Count){profile.Stakes[id]=stake+1;return stake+1;}return null;}
        public static bool RecordEndless(ProfileData profile,string id,int levels){if(levels<=0)return false;profile.BestEndless.TryGetValue(id,out var old);if(levels<=old)return false;profile.BestEndless[id]=levels;return true;}

        // Mark a coin as seen in the collection. Returns true if it was new.
        public static bool Collect(ProfileData profile, string coinId) => profile.Collected.Add(coinId);

        // Extra coin ids this character may sell in the shop (for Game.New), sorted.
        public static List<string> UnlockedList(ProfileData profile, string characterId)
        {
            var list = profile.Unlocked.TryGetValue(characterId, out var set) ? new List<string>(set) : new List<string>();
            list.Sort(string.CompareOrdinal);
            return list;
        }

        public static bool IsUnlocked(ProfileData profile, string characterId, string coinId) =>
            profile.Unlocked.TryGetValue(characterId, out var set) && set.Contains(coinId);

        // Unlock a coin of this character's locked list (done when it is bought in the shop). Returns true
        // if it was newly unlocked.
        public static bool Grant(ProfileData profile, string characterId, string coinId)
        {
            foreach (var entry in Content.Characters[characterId].Locked)
            {
                if (entry.Id != coinId || IsUnlocked(profile, characterId, coinId)) continue;
                if (!profile.Unlocked.TryGetValue(characterId, out var set)) profile.Unlocked[characterId] = set = new HashSet<string>();
                set.Add(coinId);
                return true;
            }
            return false;
        }

        // Coins this character may bring: its base pool plus unlocked coins.
        static HashSet<string> Available(ProfileData profile, string characterId)
        {
            var set = new HashSet<string>(Content.Characters[characterId].Pool.ConvertAll(c=>c.Id));
            if (profile.Unlocked.TryGetValue(characterId, out var unlocked)) set.UnionWith(unlocked);
            return set;
        }

        // The character's coin sets, created on first use: set 1 is the default deck, the others start empty.
        public static List<CoinSet> Sets(ProfileData profile, string characterId)
        {
            if (!profile.Sets.TryGetValue(characterId, out var sets))
            {
                var def = Content.Characters[characterId];
                sets = new List<CoinSet> { new CoinSet { Name = "SET 1", Coins = (def.Deck ?? new List<CoinDef> { def.Starter }).ConvertAll(c=>c.Id) } };
                profile.Sets[characterId] = sets;
            }
            while (sets.Count < SetCount) sets.Add(new CoinSet { Name = "SET " + (sets.Count + 1) });
            return sets;
        }

        public static int Active(ProfileData profile, string characterId)
        {
            int index = profile.ActiveSet.TryGetValue(characterId, out int i) ? i : 1;
            return Math.Max(1, Math.Min(SetCount, index));
        }

        public static void SetActive(ProfileData profile, string characterId, int index)
        {
            if (index >= 1 && index <= SetCount) profile.ActiveSet[characterId] = index;
        }

        // Can one more coin go into a set: it is available, the set has room, and this coin's copy limit allows it.
        public static bool CanAdd(ProfileData profile, string characterId, List<string> coins, string coinId, int max, int maxCopies)
        {
            if (coins.Count >= max || !Available(profile, characterId).Contains(coinId)) return false;
            int copies = 0;
            foreach (var id in coins) if (id == coinId) copies++;
            return copies < RarityLimit(coinId);
        }

        public static bool AddToSet(ProfileData profile, string characterId, int index, string coinId, int max, int maxCopies)
        {
            var coins = Sets(profile, characterId)[index - 1].Coins;
            if (!CanAdd(profile, characterId, coins, coinId, max, maxCopies)) return false;
            coins.Add(coinId);
            return true;
        }

        public static bool RemoveFromSet(ProfileData profile, string characterId, int index, int slot)
        {
            var coins = Sets(profile, characterId)[index - 1].Coins;
            if (slot < 0 || slot >= coins.Count) return false;
            coins.RemoveAt(slot);
            return true;
        }

        // The coins a new run starts with: the active set, limited to coins that are available, to max entries
        // and to the per-coin copy limit, so an old or hand-edited save can never produce a set
        // the game would reject. An empty (or unusable) set falls back to the default deck.
        public static List<string> Loadout(ProfileData profile, string characterId, int max, int maxCopies)
        {
            var def = Content.Characters[characterId];
            var ok = Available(profile, characterId);
            List<string> Pick(List<string> source)
            {
                return LimitedSet(source, max, ok);
            }
            var sets = Sets(profile, characterId);
            var picked = Pick(sets[Active(profile, characterId) - 1].Coins);
            if (picked.Count == 0) picked = Pick((def.Deck ?? new List<CoinDef> { def.Starter }).ConvertAll(c=>c.Id));
            return picked;
        }

        static List<string> SortedKeys<T>(Dictionary<string, T> map)
        {
            var keys = new List<string>(map.Keys);
            keys.Sort(string.CompareOrdinal);
            return keys;
        }

        public static string Encode(ProfileData profile)
        {
            var lines = new List<string> { "{", "  \"version\": 1,", "  \"tokens\": " + GameText.Num(profile.Tokens) + ",", "  \"unlocked\": {" };
            var unlockedKeys = SortedKeys(profile.Unlocked);
            for (int k = 0; k < unlockedKeys.Count; k++)
            {
                string characterId = unlockedKeys[k];
                var ids = UnlockedList(profile, characterId);
                for (int i = 0; i < ids.Count; i++) ids[i] = JsonData.Quote(ids[i]);
                lines.Add("    " + JsonData.Quote(characterId) + ": [" + string.Join(", ", ids) + "]" + (k + 1 < unlockedKeys.Count ? "," : ""));
            }
            lines.Add("  },");
            var collected = new List<string>(profile.Collected);
            collected.Sort(string.CompareOrdinal);
            for (int i = 0; i < collected.Count; i++) collected[i] = JsonData.Quote(collected[i]);
            lines.Add("  \"collected\": [" + string.Join(", ", collected) + "],");
            lines.Add("  \"sets\": {");
            var setKeys = SortedKeys(profile.Sets);
            for (int k = 0; k < setKeys.Count; k++)
            {
                string characterId = setKeys[k];
                lines.Add("    " + JsonData.Quote(characterId) + ": [");
                var sets = profile.Sets[characterId];
                for (int i = 0; i < sets.Count; i++)
                {
                    var coins = new List<string>();
                    foreach (var id in sets[i].Coins) coins.Add(JsonData.Quote(id));
                    lines.Add("      {\"name\": " + JsonData.Quote(sets[i].Name) + ", \"coins\": [" + string.Join(", ", coins) + "]}" + (i + 1 < sets.Count ? "," : ""));
                }
                lines.Add("    ]" + (k + 1 < setKeys.Count ? "," : ""));
            }
            lines.Add("  },");
            lines.Add("  \"activeSet\": {");
            var activeKeys = SortedKeys(profile.ActiveSet);
            for (int k = 0; k < activeKeys.Count; k++)
            {
                string characterId = activeKeys[k];
                lines.Add("    " + JsonData.Quote(characterId) + ": " + profile.ActiveSet[characterId] + (k + 1 < activeKeys.Count ? "," : ""));
            }
            var o = profile.Options;
            lines.Add("  },");
            var wins=new List<string>(profile.Wins);wins.Sort(string.CompareOrdinal);for(int i=0;i<wins.Count;i++)wins[i]=JsonData.Quote(wins[i]);
            lines.Add("  \"wins\": ["+string.Join(", ",wins)+"],");
            WriteIntMap(lines,"stakes",profile.Stakes);
            lines[lines.Count-1] += ",";
            WriteIntMap(lines,"bestEndless",profile.BestEndless);
            lines[lines.Count-1] += ",";
            lines.Add("  \"coinMastery\": {");
            var masteryKeys = SortedKeys(profile.CoinMastery);
            for (int k = 0; k < masteryKeys.Count; k++)
                lines.Add("    " + JsonData.Quote(masteryKeys[k]) + ": " + GameText.Num(profile.CoinMastery[masteryKeys[k]]) + (k + 1 < masteryKeys.Count ? "," : ""));
            lines.Add("  },");
            lines.Add("  \"options\": {");
            lines.Add("    \"fastFlip\": " + Bool(o.FastFlip) + ",");
            lines.Add("    \"fullscreen\": " + Bool(o.Fullscreen) + ",");
            lines.Add("    \"language\": " + JsonData.Quote(o.Language) + ",");
            lines.Add("    \"screenShake\": " + Bool(o.ScreenShake) + ",");
            lines.Add("    \"seenHelp\": " + Bool(o.SeenHelp) + ",");
            lines.Add("    \"volumeMaster\": " + GameText.Num(o.VolumeMaster) + ",");
            lines.Add("    \"volumeMusic\": " + GameText.Num(o.VolumeMusic) + ",");
            lines.Add("    \"volumeSfx\": " + GameText.Num(o.VolumeSfx));
            lines.Add("  }");
            lines.Add("}");
            return string.Join("\n", lines);
        }

        static bool BoolValue(JsonData value, bool fallback) => value != null && value.Kind == JsonKind.Boolean ? value.Boolean : fallback;
        static double NumberValue(JsonData value, double fallback) => value != null && value.Kind == JsonKind.Number ? value.Number : fallback;

        public static ProfileData Decode(string text)
        {
            JsonData data;
            try { data = string.IsNullOrEmpty(text) ? null : JsonData.Parse(text); }
            catch (FormatException) { data = null; }
            if (data == null || data.Kind != JsonKind.Object) return New();
            var profile = New();
            profile.Tokens = NumberValue(data["tokens"], 0);
            var unlocked = data["unlocked"];
            if (unlocked?.Kind == JsonKind.Object)
                foreach (var pair in unlocked.Object)
                    profile.Unlocked[pair.Key] = new HashSet<string>(Strings(pair.Value));
            profile.Collected = new HashSet<string>(Strings(data["collected"]));
            profile.Wins = new HashSet<string>(Strings(data["wins"]));
            ReadIntMap(data["stakes"],profile.Stakes);
            ReadIntMap(data["bestEndless"],profile.BestEndless);
            var mastery = data["coinMastery"];
            if (mastery?.Kind == JsonKind.Object)
                foreach (var pair in mastery.Object)
                    if (pair.Value.Kind == JsonKind.Number) profile.CoinMastery[pair.Key] = pair.Value.Number;
            var sets = data["sets"];
            if (sets?.Kind == JsonKind.Object)
            {
                foreach (var pair in sets.Object)
                {
                    var parsed = new List<CoinSet>();
                    if (pair.Value.Kind == JsonKind.Array)
                        foreach (var entry in pair.Value.Array)
                        {
                            if (entry.Kind != JsonKind.Object) continue;
                            var name = entry["name"];
                            parsed.Add(new CoinSet
                            {
                                Name = name?.Kind == JsonKind.String ? name.String : "SET " + (parsed.Count + 1),
                                Coins = Strings(entry["coins"]),
                            });
                        }
                    profile.Sets[pair.Key] = parsed;
                }
            }
            var active = data["activeSet"];
            if (active?.Kind == JsonKind.Object)
                foreach (var pair in active.Object)
                    if (pair.Value.Kind == JsonKind.Number) profile.ActiveSet[pair.Key] = (int)pair.Value.Number;
            var options = data["options"];
            if (options?.Kind == JsonKind.Object)
            {
                var o = profile.Options;
                o.ScreenShake = BoolValue(options["screenShake"], o.ScreenShake);
                o.FastFlip = BoolValue(options["fastFlip"], o.FastFlip);
                o.Fullscreen = BoolValue(options["fullscreen"], o.Fullscreen);
                o.SeenHelp = BoolValue(options["seenHelp"], o.SeenHelp);
                if (options["language"]?.Kind == JsonKind.String) o.Language = options["language"].String;
                o.VolumeMaster = NumberValue(options["volumeMaster"], o.VolumeMaster);
                o.VolumeMusic = NumberValue(options["volumeMusic"], o.VolumeMusic);
                o.VolumeSfx = NumberValue(options["volumeSfx"], o.VolumeSfx);
            }
            Sanitize(profile);
            return profile;
        }

        internal static List<string> LimitedSet(List<string> source, int max, HashSet<string> allowed = null)
        {
            var entries = new List<string>();
            foreach (var id in source)
                if (id != null && Content.Coins.ContainsKey(id) && (allowed == null || allowed.Contains(id)) && entries.Count < max) entries.Add(id);
            var result = new List<string>();
            var counts = new Dictionary<string, int>();
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                string id = entries[i];
                counts.TryGetValue(id, out int n);
                if (n >= RarityLimit(id)) continue;
                counts[id] = n + 1;
                result.Insert(0, id);
            }
            return result;
        }

        internal static void Sanitize(ProfileData p)
        {
            p.Tokens = double.IsNaN(p.Tokens) || double.IsInfinity(p.Tokens) || p.Tokens < 0 ? 0 : Math.Floor(p.Tokens);
            foreach (var id in new List<string>(p.Unlocked.Keys))
                if (!Content.Characters.ContainsKey(id)) p.Unlocked.Remove(id);
                else p.Unlocked[id].RemoveWhere(x => !Content.Coins.ContainsKey(x));
            p.Collected.RemoveWhere(x => !Content.Coins.ContainsKey(x));
            foreach (var id in new List<string>(p.CoinMastery.Keys))
            {
                if (!Content.Coins.TryGetValue(id, out var coin) || coin.Mastery == null) p.CoinMastery.Remove(id);
                else
                {
                    double value = p.CoinMastery[id];
                    p.CoinMastery[id] = double.IsNaN(value) || double.IsInfinity(value) || value < 0
                        ? 0 : Math.Min(value, coin.Mastery.Thresholds[2]);
                }
            }
            p.Wins.RemoveWhere(x => !Content.Characters.ContainsKey(x));
            foreach (var id in new List<string>(p.Stakes.Keys))
                if (!Content.Characters.ContainsKey(id)) p.Stakes.Remove(id);
                else p.Stakes[id] = Math.Max(1, Math.Min(Game.Stakes.Count, p.Stakes[id]));
            foreach (var id in new List<string>(p.BestEndless.Keys))
                if (!Content.Characters.ContainsKey(id) || p.BestEndless[id] <= 0) p.BestEndless.Remove(id);
            foreach (var id in new List<string>(p.ActiveSet.Keys))
                if (!Content.Characters.ContainsKey(id) || p.ActiveSet[id] < 1 || p.ActiveSet[id] > SetCount) p.ActiveSet.Remove(id);
            foreach (var id in new List<string>(p.Sets.Keys))
            {
                var sets = p.Sets[id];
                if (!Content.Characters.ContainsKey(id) || sets.Count < SetCount) { p.Sets.Remove(id); continue; }
                if (sets.Count > SetCount) sets.RemoveRange(SetCount, sets.Count - SetCount);
                for (int i = 0; i < sets.Count; i++)
                {
                    sets[i].Name = sets[i].Name ?? "SET " + (i + 1);
                    sets[i].Coins = LimitedSet(sets[i].Coins, Game.StartMax);
                }
            }
            var o = p.Options;
            o.VolumeMaster = ClampVolume(o.VolumeMaster, 80);
            o.VolumeMusic = ClampVolume(o.VolumeMusic, 40);
            o.VolumeSfx = ClampVolume(o.VolumeSfx, 80);
            if (o.Language != "en" && o.Language != "de") o.Language = "en";
        }

        static double ClampVolume(double value, double fallback) => double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Max(0, Math.Min(100, value));

        static List<string> Strings(JsonData list)
        {
            var result = new List<string>();
            if (list?.Kind == JsonKind.Array)
                foreach (var entry in list.Array)
                    if (entry.Kind == JsonKind.String) result.Add(entry.String);
            return result;
        }

        static string Bool(bool b) => b ? "true" : "false";

        static void WriteIntMap(List<string> lines,string name,Dictionary<string,int> map)
        {
            lines.Add("  "+JsonData.Quote(name)+": {");var keys=SortedKeys(map);
            for(int i=0;i<keys.Count;i++)lines.Add("    "+JsonData.Quote(keys[i])+": "+map[keys[i]]+(i+1<keys.Count?",":""));
            lines.Add("  }");
        }
        static void ReadIntMap(JsonData data,Dictionary<string,int> map){if(data?.Kind!=JsonKind.Object)return;foreach(var p in data.Object)if(p.Value.Kind==JsonKind.Number)map[p.Key]=(int)p.Value.Number;}

    }
}
