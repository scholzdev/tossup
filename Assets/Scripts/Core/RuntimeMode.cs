using System;
using System.Collections.Generic;

namespace Tossup
{
    public static class RuntimeMode
    {
        public const string DeveloperModePreference = "developer-mode.json";
        public static bool Dev { get; private set; }
        public static bool Sandbox { get; private set; }
        static readonly Random masteryRandom = new Random();

        public static void Configure(bool dev, bool sandbox) { Dev = dev; Sandbox = sandbox; }
        public static void ConfigureFromEnvironment() => Configure(
            Environment.GetEnvironmentVariable("TOSSUP_DEV") == "1",
            Environment.GetEnvironmentVariable("TOSSUP_SANDBOX") == "1" || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TOSSUP_SANDBOX_SCENE")));

        public static void ApplyProfile(ProfileData profile)
        {
            if (!Dev && !Sandbox) return;
            foreach (var characterId in Content.CharacterOrder)
            {
                profile.Wins.Add(characterId);
                profile.Stakes[characterId] = Game.Stakes.Count;
                foreach (var entry in Content.Characters[characterId].Locked) Profile.Grant(profile, characterId, entry.Id);
                foreach (var coin in Content.Characters[characterId].Pool) profile.Collected.Add(coin.Id);
                foreach (var entry in Content.Characters[characterId].Locked) profile.Collected.Add(entry.Id);
            }
            if (Dev)
                foreach (var coin in Content.CoinOrder)
                {
                    profile.Collected.Add(coin.Id); // includes stamp-only coins that no pool lists
                    if (coin.Mastery != null)
                    {
                        int level = masteryRandom.Next(coin.Mastery.Thresholds.Length + 1);
                        // profile.CoinMastery[coin.Id] = coin.Mastery.Thresholds[coin.Mastery.Thresholds.Length - 1];
                        profile.CoinMastery[coin.Id] = level == 0 ? 0 : coin.Mastery.Thresholds[level - 1];
                    }
                }
            profile.Options.SeenHelp = true;
        }
    }

    public sealed class SandboxOdds { public double Heads, Tie; }

    public sealed class SandboxConfig
    {
        public List<string> Coins = new List<string>();
        public Dictionary<string, SandboxOdds> Odds = new Dictionary<string, SandboxOdds>();
        public string Character = "blade";
        public int Stake = 1;
        public double? Gold, Energy, Seed;
        public string Screen = "encounter";

        public static SandboxConfig Decode(string json)
        {
            var root = JsonData.Parse(json);
            if (root.Kind != JsonKind.Object) throw new FormatException("sandbox scene must be a JSON object");
            var result = new SandboxConfig();
            var coins = root["coins"];
            if (coins?.Kind != JsonKind.Array) throw new FormatException("sandbox coins must be an array");
            foreach (var item in coins.Array)
            {
                if (item.Kind != JsonKind.String || !Content.Coins.ContainsKey(item.String)) throw new FormatException("unknown sandbox coin");
                result.Coins.Add(item.String);
            }
            if (result.Coins.Count < 1 || result.Coins.Count > Game.DeckMax) throw new FormatException("sandbox needs 1-" + Game.DeckMax + " coins");
            if (root["character"]?.Kind == JsonKind.String) result.Character = root["character"].String;
            if (!Content.Characters.ContainsKey(result.Character)) throw new FormatException("unknown sandbox character");
            if (root["stake"]?.Kind == JsonKind.Number) result.Stake = (int)root["stake"].Number;
            if (root["gold"]?.Kind == JsonKind.Number) result.Gold = root["gold"].Number;
            if (root["energy"]?.Kind == JsonKind.Number) result.Energy = root["energy"].Number;
            if (root["seed"]?.Kind == JsonKind.Number) result.Seed = root["seed"].Number;
            if (root["screen"]?.Kind == JsonKind.String) result.Screen = root["screen"].String;
            if(result.Screen!="encounter"&&result.Screen!="shop"&&result.Screen!="title"&&result.Screen!="select"&&result.Screen!="collection"&&result.Screen!="sets"&&result.Screen!="options"&&result.Screen!="help")
                throw new FormatException("invalid sandbox screen");
            var odds = root["odds"];
            if (odds?.Kind == JsonKind.Object)
                foreach (var pair in odds.Object)
                {
                    if (!Content.Coins.ContainsKey(pair.Key) || pair.Value.Kind != JsonKind.Object) throw new FormatException("invalid sandbox odds");
                    var heads = pair.Value["heads"];
                    var tie = pair.Value["tie"];
                    double tieValue=tie?.Kind==JsonKind.Number?tie.Number:0;
                    if (heads?.Kind != JsonKind.Number || heads.Number < 0 || tieValue < 0 || heads.Number + tieValue > 1)
                        throw new FormatException("invalid sandbox odds");
                    result.Odds[pair.Key] = new SandboxOdds { Heads = heads.Number, Tie = tieValue };
                }
            return result;
        }
    }
}
