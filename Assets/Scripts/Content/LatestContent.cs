using System;
using System.Collections.Generic;

namespace Tossup
{
    // Content added after the first Unity port. Kept separate so the data-heavy catalogue stays reviewable.
    public static partial class Content
    {
        static Effect K(string type, double amount = 0, int? coins = null, string kind = null) =>
            new Effect(type, amount, coins) { Kind = kind };

        static CoinUpgradeDef U(string id, string name, int cost, string description, double score = 0, double odds = 0) =>
            new CoinUpgradeDef { Id = id, Name = name, Cost = cost, Description = description, HeadsScore = score, HeadsProbability = odds };

        static void Latest(Dictionary<string, CoinDef> map, string id, double probability, string[] types = null,
            double tie = 0, int? cost = null, int? energy = null)
        {
            var c = map[id];
            c.Probability = probability;
            c.TieProbability = tie;
            if (types != null) c.CoinTypes = new List<string>(types);
            if (cost.HasValue) c.Cost = cost;
            if (energy.HasValue) c.EnergyCost = energy.Value;
        }

        static void AddUpgrade(CoinDef coin, CoinUpgradeDef upgrade) => coin.Upgrades[upgrade.Id] = upgrade;

        static void ApplyLatestCoins(Dictionary<string, CoinDef> map)
        {
            // Current balance values and type tags.
            Latest(map, "normal", .65, cost: 5);
            Latest(map, "copper", .30, new[] { "greed" }, cost: 10);
            Latest(map, "sword", .35, new[] { "steel" }, cost: 12);
            map["sword"].Heads=L(E("score",3));
            Latest(map, "lucky", .30, new[] { "fortune" }, cost: 10);
            Latest(map, "cursed", .25, new[] { "chaos" });
            Latest(map, "loaded", .59, new[] { "fortune", "greed" }, .23, 12);
            map["loaded"].Tails = L(E("gold_loss", 8));
            Latest(map, "dagger", .67, new[] { "steel" }, cost: 12);
            map["dagger"].Heads = L(E("score", 2));
            Latest(map, "hammer", .25, new[] { "steel" }, cost: 22, energy: 2);
            Latest(map, "blood", .37, new[] { "blood" }, .09, 22, 1);
            map["blood"].Heads = L(E("score", 10)); map["blood"].Tails = L(E("penalty", 6));
            Latest(map, "spark", .70, cost: 9);
            Latest(map, "focus", .65, new[] { "fortune" }, cost: 10);
            map["focus"].Heads = L(E("next_odds", .35, 1));
            Latest(map, "snowball", .30, cost: 24, energy: 1);
            map["snowball"].Grow=(inst,evt)=>{if(evt=="flip")inst.Stack=Math.Min(8,inst.Stack+1);};
            map["snowball"].OnResolve=(g,inst,res)=>{if(res.Result==Side.Heads)foreach(var effect in res.Effects)if(effect.Type=="score")effect.Amount+=inst.Stack;};
            Latest(map, "gambler", .35, new[] { "chaos" }, cost: 22, energy: 1);
            Latest(map, "momentum", .35, new[] { "rhythm" });
            Latest(map, "echo", .60);
            Latest(map, "vampire", .35, new[] { "blood" });
            Latest(map, "miser", .40, new[] { "greed" });
            Latest(map, "fuse", .30, new[] { "steel" }, cost: 10);
            Latest(map, "phoenix", .25);
            Latest(map, "contrarian", .65, new[] { "chaos" });
            Latest(map, "chain", .60, new[] { "rhythm" });
            Latest(map, "bank", .40, new[] { "fortune", "greed" });
            Latest(map, "lucky_seven", .30);
            Latest(map, "hourglass", .30, cost: 10);
            Latest(map, "capacitor", .35, energy: 1); map["capacitor"].Name = "Flux Capacitor";
            Latest(map, "martyr", .40, new[] { "blood" }, energy: 1);
            map["martyr"].OnResolve=(g,i,r)=>{if(r.Result==Side.Heads&&g.Encounter.Tails>0)r.Effects.Add(E("score",g.Encounter.Tails));};
            Latest(map, "bounty", .40, new[] { "greed" }, energy: 1);
            Latest(map, "jester", .50, new[] { "chaos" }, energy: 1);
            Latest(map, "flock", .25);
            Latest(map, "megaphone", .65, new[] { "rhythm" }, cost: 20, energy: 1);
            Latest(map, "cheerleader", .70, new[] { "rhythm" });
            Latest(map, "mirror", .65); Latest(map, "twin", .65); Latest(map, "pot", .25);
            map["pot"].OnResolve=(g,i,r)=>{if(r.Result==Side.Heads)r.Effects.Add(E("score",Math.Min(8,g.Encounter.Flips)));};
            Latest(map, "domino", .60); Latest(map, "hot_hand", .70); Latest(map, "anchor", .70);
            Latest(map, "bettor", .35, energy: 1); Latest(map, "cash_out", .30); Latest(map, "cold_streak", .20);
            Latest(map, "amplifier", .70, energy: 1); Latest(map, "true_echo", .50); Latest(map, "doubler", .30);

            AddUpgrade(map["normal"], U("lucky_day", "Lucky Day", 8, "Heads scores +1 point.", 1));
            AddUpgrade(map["normal"], U("mathematician", "Mathematician", 10, "+10% Heads chance.", odds: .10));
            AddUpgrade(map["copper"], U("copper_lining", "Copper Lining", 8, "Heads scores +1 point.", 1));
            AddUpgrade(map["copper"], U("bright_side", "Bright Side", 9, "+8% Heads chance.", odds: .08));
            AddUpgrade(map["sword"], U("keen_edge", "Keen Edge", 8, "Heads scores +1 point.", 1));
            AddUpgrade(map["sword"], U("true_aim", "True Aim", 10, "+8% Heads chance.", odds: .08));
            AddUpgrade(map["dagger"], U("deep_cut", "Deep Cut", 8, "Heads scores +1 point.", 1));
            AddUpgrade(map["dagger"], U("steady_hand", "Steady Hand", 9, "+6% Heads chance.", odds: .06));
            AddUpgrade(map["hammer"], U("heavy_head", "Heavy Head", 12, "Heads scores +2 points.", 2));
            AddUpgrade(map["hammer"], U("balanced_grip", "Balanced Grip", 13, "+6% Heads chance.", odds: .06));
            AddUpgrade(map["blood"], U("bloodletting", "Bloodletting", 12, "Heads scores +2 points.", 2));
            AddUpgrade(map["blood"], U("sure_strike", "Sure Strike", 13, "+6% Heads chance.", odds: .06));
            AddUpgrade(map["focus"], U("clear_mind", "Clear Mind", 10, "+8% Heads chance.", odds: .08));
            AddUpgrade(map["focus"], U("follow_through", "Follow Through", 10, "Heads scores +2 points.", 2));
            AddUpgrade(map["loaded"], U("safer_bet", "Safer Bet", 10, "+7% Heads chance.", odds: .07));
            AddUpgrade(map["loaded"], U("gilded_face", "Gilded Face", 9, "Heads scores +2 points.", 2));
            AddUpgrade(map["spark"], U("hot_spark", "Hot Spark", 9, "Heads scores +2 points.", 2));
            AddUpgrade(map["spark"], U("reliable_spark", "Reliable Spark", 10, "+7% Heads chance.", odds: .07));

            Add(map, new CoinDef { Id="compost", Name="Compost", Description="Tails: quota +2; once per level, Fortune coins gain +11% Heads for the run (max +55%).", Rarity="N", Cost=10, Probability=.47, Heads=L(E("score",2)), Tails=L(E("penalty",2),E("fortune_odds",.11)) });
            Add(map, new CoinDef { Id="square_dance", Name="Square Dance", Description="Heads: 2 points times the square of Square Dance copies in your deck.", Rarity="N", Cost=14, Probability=.27, OnResolve=(g,i,r)=> { if(r.Result==Side.Heads){ int n=0; foreach(var c in g.Coins) if(c.Id==i.Id)n++; r.Effects.Add(E("score",2*n*n)); } } });
            Add(map, new CoinDef { Id="whetstone", Name="Whetstone", Description="Heads: next 2 Steel coins gain 3 points on Heads, but add 2 quota on Tails.", Rarity="R", Cost=22, Probability=.67, Heads=L(K("type_buff",0,2,"steel")) });
            Add(map, new CoinDef { Id="counterfeiter", Name="Counterfeiter", Description="Heads: next 2 Greed coins double all gold gained. Each Tails also loses up to 3 gold.", Rarity="R", Cost=25, Probability=.61, Heads=L(K("type_buff",0,2,"greed")) });
            Add(map, new CoinDef { Id="blood_pact", Name="Blood Pact", Description="Heads: next Blood coin gains 8 points on Heads or adds 4 quota on Tails.", Rarity="SR", Cost=32, Probability=.58, Heads=L(K("type_buff",0,1,"blood")) });
            Add(map, new CoinDef { Id="doppelganger", Name="Doppelganger", Description="Heads: next Chaos coin applies its resolved effects twice.", Rarity="SR", Cost=34, Probability=.53, Heads=L(K("type_buff",0,1,"chaos")) });
            Add(map, new CoinDef { Id="jackpot", Name="Jackpot", Description="Heads: 25 points. Only 15% Heads.", Rarity="SR", Cost=18, EnergyCost=1, Probability=.15, Heads=L(E("score",25)) });
            Add(map, new CoinDef { Id="mimic", Name="Mimic", Description="Heads: copies the Heads effects of a random other coin in your deck.", Rarity="UR", Probability=.30, CoinTypes=new List<string>{"chaos"}, Tails=L(E("score",1)), OnResolve=Mimic });
            Add(map, new CoinDef { Id="good_dog", Name="Good Dog", Description="Heads: 2 points; return the highest-scoring coin played this level once.", Rarity="UR", Cost=28, Probability=.43, Heads=L(E("score",2),E("fetch_best")) });
            Add(map, new CoinDef { Id="orchestra", Name="Orchestra", Description="Heads: 2 points per different coin in your deck.", Rarity="R", Probability=.25, CoinTypes=new List<string>{"rhythm"}, Tails=L(E("gold",1)), OnResolve=Orchestra });
            Add(map, new CoinDef { Id="conductor", Name="Conductor", Description="Heads: next 3 Rhythm coins gain points from the combo.", Rarity="R", Cost=24, Probability=.73, Heads=L(K("type_buff",0,3,"rhythm")) });
            Add(map, new CoinDef { Id="lifeline", Name="Lifeline", Description="Heads: 1 point, and one extra exchange this level.", Rarity="R", Probability=.70, CoinTypes=new List<string>{"blood"}, Heads=L(E("score",1),E("extra_exchange",1)) });
            Add(map, new CoinDef { Id="horoscope", Name="Horoscope", Description="Heads: 1 point, all coins +7% Heads this level. Tails: +3%.", Rarity="R", Probability=.70, CoinTypes=new List<string>{"fortune"}, Heads=L(E("score",1),E("all_odds",.07)), Tails=L(E("all_odds",.03)) });
            Add(map, new CoinDef { Id="crystal_ball", Name="Crystal Ball", Description="Heads: 5 points. Tails: discard one of the next three coins.", Rarity="SR", Probability=.30, Heads=L(E("score",5)), Tails=L(E("bank_discard",1)) });
        }

        static void Add(Dictionary<string, CoinDef> map, CoinDef coin) => map[coin.Id] = coin;

        static void Mimic(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            var choices = new List<CoinInst>();
            foreach (var coin in game.Coins) if (coin != inst && Coins[coin.Id].Heads.Count > 0) choices.Add(coin);
            if (choices.Count == 0) return;
            var pick = choices[Rng.Int(game, 1, choices.Count) - 1];
            foreach (var effect in Coins[pick.Id].Heads) res.Effects.Add(effect.Copy());
            Game.Log(game, "Mimic copies " + Coins[pick.Id].Name + ".");
        }

        static void Orchestra(GameState game, CoinInst inst, Res res)
        {
            if (res.Result != Side.Heads) return;
            var ids = new HashSet<string>(); foreach (var coin in game.Coins) ids.Add(coin.Id);
            res.Effects.Add(E("score", 2 * ids.Count));
        }

        static void ApplyLatestItems(Dictionary<string, ItemDef> map)
        {
            map["energy_drink"] = new ItemDef { Id="energy_drink", Name="Energy Drink", Short="+2 NRG", Cost=8, Description="Gain 2 energy", Use=g=> { g.Player.Energy += 2; return true; } };
            map["shortcut"] = new ItemDef { Id="shortcut", Name="Shortcut", Short="+3 PTS", Cost=12, Description="Score 3 points at once", Use=g=> { Game.ApplyEffect(g,null,E("score",3)); return true; } };
            map["safety_net"] = new ItemDef { Id="safety_net", Name="Safety Net", Short="SHIELD", Cost=9, Description="The next combo break is prevented", Use=g=> { Game.ApplyEffect(g,null,E("combo_shield",1)); return true; } };
            map["lucky_charm"] = new ItemDef { Id="lucky_charm", Name="Lucky Charm", Short="CHARM", Cost=10, Description="The next 2 coins +20% Heads", Use=g=> { Game.AddBuff(g,"odds",.2,2,true); if(g.Dealt!=null)g.Dealt.Probability=Math.Min(1,g.Dealt.Probability+.2); return true; } };
        }

        static void ApplyLatestCharacters(Dictionary<string, CharacterDef> c)
        {
            c["blade"].Deck=S("normal","sword","dagger");
            c["blade"].Locked=new List<LockedCoin>{K("hammer",3),K("blood",4),K("vampire",4),K("chain",5),K("cursed",5),K("fuse",5),K("focus",6),K("martyr",6),K("snowball",8),K("spark",3),K("jackpot",5),K("lifeline",4),K("megaphone",5),K("pot",4),K("hot_hand",4),K("cash_out",5),K("doubler",6),K("amplifier",6),K("compost",4),K("square_dance",4),K("good_dog",6),K("whetstone",4),K("blood_pact",5),K("conductor",4)};
            c["seer"].Deck=S("normal","normal","dagger","focus","spark"); c["seer"].Pool=S("normal","dagger","cursed","gambler","spark","focus","lucky","compost");
            c["seer"].Locked=new List<LockedCoin>{K("horoscope",4),K("crystal_ball",5),K("mimic",6),K("contrarian",4),K("lucky_seven",4),K("blood",4),K("hourglass",5),K("jester",5),K("echo",6),K("phoenix",6),K("mirror",5),K("domino",6),K("twin",5),K("cold_streak",4),K("anchor",4),K("true_echo",6),K("amplifier",6),K("square_dance",4),K("good_dog",6),K("blood_pact",5),K("doppelganger",6)};
            c["trader"].Deck=S("normal","loaded","dagger"); c["trader"].Pool=S("normal","copper","loaded","dagger","sword","square_dance");
            c["trader"].Locked=new List<LockedCoin>{K("spark",3),K("bank",4),K("miser",4),K("hammer",4),K("bounty",5),K("flock",5),K("momentum",5),K("capacitor",6),K("cheerleader",4),K("megaphone",5),K("orchestra",4),K("lifeline",4),K("jackpot",5),K("bettor",5),K("anchor",4),K("doubler",6),K("true_echo",6),K("compost",4),K("good_dog",6),K("counterfeiter",4),K("conductor",4)};
        }
    }
}
