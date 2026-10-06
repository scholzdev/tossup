using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Tossup;

static class Program
{
    static object Effects(IReadOnlyList<Effect> list) => list.Select(e => new { type=DefinitionKeys.Key(e.Type), amount=e.Amount, coins=e.Coins, kind=e.Kind.HasValue?DefinitionKeys.Key(e.Kind.Value):null }).ToArray();
    static string root;
    public static int Main(string[] args)
    {
        try
        {
            root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","..",".."));
            string command = args.Length>0 ? args[0] : "content";
            if(command=="content") ContentDump();
            else if(command=="sim") Simulator.Run(args.Skip(1).ToArray());
            else if(command=="import" && args.Length==3) Console.WriteLine(LegacySave.ImportDirectory(args[1],args[2]) ? "imported" : "no absent Unity saves to import");
            else throw new ArgumentException("usage: game_tools.sh content | sim [options] | import <Lua save directory> <Unity save directory>");
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
    static void ContentDump()
    {
        using var deDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"Assets","Resources","locales","de.json")));
        var de = new Dictionary<string,JsonElement>(); var flat = new Dictionary<string,JsonElement>();
        foreach(var p in deDoc.RootElement.EnumerateObject()) (p.Value.ValueKind==JsonValueKind.Object ? de : flat)[p.Name]=p.Value;
        object data = new {
            version=JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(root,"Assets","Resources","version.json"))),
            coins=Content.CoinOrder.Select(c=> { return new { id=c.Id, name=c.Name,description=c.Description,heads_description=c.HeadsDescription,rarity=DefinitionKeys.RarityCode(c.Rarity),probability=c.Probability,tie_probability=c.TieProbability,cost=c.Cost,energy_cost=c.EnergyCost,coin_types=c.Types.Select(type=>DefinitionKeys.Key(type)),heads=Effects(c.Heads),tails=Effects(c.Tails),upgrades=c.Upgrades.ToDictionary(u=>u.Id,u=>new {name=u.Name,description=u.Description,cost=u.Cost,heads_score=u.Type==UpgradeType.ScoreBonus?u.Value:0,heads_probability=u.Type==UpgradeType.Probability?u.Value:0}),hooks=new[]{("on_deal",nameof(CoinDef.OnDeal)),("on_discard",nameof(CoinDef.OnDiscard)),("on_flip",nameof(CoinDef.OnFlip)),("on_resolve",nameof(CoinDef.OnResolve)),("on_odds",nameof(CoinDef.OnOdds)),("grow",nameof(CoinDef.Grow)),("register",nameof(CoinDef.Register))}.Where(x=>c.HasHook(x.Item2)).Select(x=>x.Item1).ToArray()}; }).ToArray(),
            items=Content.Items.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(c=>new { id=c.Id,name=c.Name,description=c.Description,cost=c.Cost,@short=c.Short }).ToArray(),
            relics=Content.Relics.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(c=>new {id=c.Id,name=c.Name,description=c.Description,hooks=new[]{"register"}}).ToArray(),
            characters=Content.CharacterOrder.Select(id=> {var c=Content.Characters[id];return new {id,name=c.Name,description=c.Description,deck=c.Deck.Select(x=>x.Id),pool=c.Pool.Select(x=>x.Id),locked=c.Locked.Select(x=>x.Id).ToArray()};}).ToArray(),
            route=Game.Route.Select(c=>new{name=c.Name,per_coin=c.PerCoin,payout=c.Payout,boss=c.Boss}).ToArray(),
            stakes=Game.Stakes.Select(c=>new{info=c.Info,rules=c.Rules}).ToArray(),
            modifiers=Game.ModifierOrder.Select(id=>{var c=Game.Modifiers[id];return new{id,name=c.Name,description=c.Description};}).ToArray(),
            contracts=Game.ContractOrder.Select(id=>{var c=Game.Contracts[id];return new{id,name=c.Name,description=c.Description,drawback=c.Drawback,reward=c.Reward,reward_text=c.RewardText,heads_penalty=c.HeadsPenalty};}).ToArray(),
            encounters=Game.EncounterOrder.Select(id=>{var c=Game.Encounters[id];return new{id,name=c.Name,description=c.Description};}).ToArray(),
            augments=Game.AugmentOrder.Select(id=>{var c=Game.AugmentDefs[id];return new{id,name=c.Name,description=c.Description,tier=c.Tier};}).ToArray(),
            constants=new Dictionary<string,object>{{"START_GOLD",Game.StartGold},{"START_MAX",Game.StartMax},{"DECK_MAX",Game.DeckMax},{"SLOT_COST",Game.SlotCost},{"SLOT_STEP",Game.SlotStep},{"EXCHANGE_BASE",Game.ExchangeBase},{"EXCHANGE_STEP",Game.ExchangeStep},{"EXCHANGE_GAIN",Game.ExchangeGain},{"EXCHANGE_MAX",Game.ExchangeMax},{"SURPLUS_RATE",Game.SurplusRate},{"COMBO_STEP",Game.ComboStep},{"COMBO_CAP",Game.ComboCap},{"RETURN_CAP",Game.ReturnCap},{"MAX_COPIES",3},{"VISIBLE",Game.Visible},{"MULLIGAN",Game.MulliganSize}},
            de, de_strings=flat
        };
        Console.WriteLine(JsonSerializer.Serialize(data,new JsonSerializerOptions { DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
    }
}
