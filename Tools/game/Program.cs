using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Tossup;

static class Program
{
    static object Effects(IReadOnlyList<Effect> list) => list.Select(e => new { type=DefinitionKeys.Key(e.Type), amount=e.Amount, coins=e.Coins, kind=e.Kind.HasValue?DefinitionKeys.Key(e.Kind.Value):null }).ToArray();
    static object Buff(BuffSpec buff) => new
    {
        id = buff.Id,
        trigger = DefinitionKeys.Key(buff.Trigger),
        target = DefinitionKeys.Key(buff.Target.Kind),
        target_type = buff.Target.Type.HasValue ? DefinitionKeys.Key(buff.Target.Type.Value) : null,
        target_count = buff.Target.Count,
        applies_on = buff.AppliesOn.HasValue ? DefinitionKeys.Key(buff.AppliesOn.Value) : null,
        effect = Effects(new[] { buff.Effect })
    };

    static object CoinData(CoinDef coin) => new
    {
        id = coin.Id,
        name = coin.Name,
        description = coin.Description,
        special_rule = coin.SpecialRule,
        heads_description = coin.HeadsDescription,
        tails_description = coin.TailsDescription,
        edge_description = coin.EdgeDescription,
        rarity = DefinitionKeys.RarityCode(coin.Rarity),
        probability = coin.Probability,
        tie_probability = coin.TieProbability,
        cost = coin.Cost,
        energy_cost = coin.EnergyCost,
        coin_types = coin.Types.Select(type => DefinitionKeys.Key(type)),
        heads = Effects(coin.Heads),
        tails = Effects(coin.Tails),
        edge = Effects(coin.Edge),
        buffs = coin.Buffs.Select(Buff),
        hooks = new[]
        {
            ("on_deal", nameof(CoinDef.OnDeal)),
            ("on_discard", nameof(CoinDef.OnDiscard)),
            ("on_flip", nameof(CoinDef.OnFlip)),
            ("on_resolve", nameof(CoinDef.OnResolve)),
            ("on_odds", nameof(CoinDef.OnOdds)),
            ("grow", nameof(CoinDef.Grow)),
            ("register", nameof(CoinDef.Register))
        }.Where(hook => coin.HasHook(hook.Item2)).Select(hook => hook.Item1).ToArray()
    };

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
            coins=Content.CoinOrder.Select(CoinData).ToArray(),
            items=Content.Items.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(c=>new { id=c.Id,name=c.Name,description=c.Description,cost=c.Cost,@short=c.Short }).ToArray(),
            relics=Content.Relics.Values.OrderBy(x=>x.Id,StringComparer.Ordinal).Select(c=>new {id=c.Id,name=c.Name,description=c.Description,hooks=new[]{"register"}}).ToArray(),
            characters=Content.CharacterOrder.Select(id=> {var c=Content.Characters[id];return new {id,name=c.Name,description=c.Description,deck=c.Deck.Select(x=>x.Id),pool=c.Pool.Select(x=>x.Id),locked=c.Locked.Select(x=>x.Id).ToArray()};}).ToArray(),
            route=Game.Route.Select(c=>new{name=c.Name,payout=c.Payout,boss=c.Boss}).ToArray(),
            enemies=EnemyCatalog.Ordered.Select(d=>new{id=d.Id,name=d.Name,description=d.Description,pouch=d.Pouch,draw=d.Draw,round_points=d.RoundPoints,heads_bonus=d.HeadsBonus,min_level=d.MinLevel,max_level=d.MaxLevel,elite=d.Elite,boss=d.Boss}).ToArray(),
            stakes=Game.Stakes.Select(c=>new{info=c.Info,rules=c.Rules}).ToArray(),
            modifiers=Game.ModifierOrder.Select(id=>{var c=Game.Modifiers[id];return new{id,name=c.Name,description=c.Description};}).ToArray(),
            encounters=Game.Encounters.Select(c=>new{id=c.Id,name=c.Name,description=c.Description}).ToArray(),
            augments=Game.AugmentOrder.Select(id=>{var c=Game.AugmentDefs[id];return new{id,name=c.Name,description=c.Description,tier=c.Tier};}).ToArray(),
            constants=new Dictionary<string,object>{{"START_GOLD",Game.StartGold},{"START_MAX",Game.StartMax},{"DECK_MAX",Game.DeckMax},{"ROUNDS",Game.Rounds},{"HAND_SIZE",Game.HandSize},{"POUCH_SIZE",Game.PouchSize},{"SET_COPIES",Game.SetCopies},{"SURPLUS_RATE",Game.SurplusRate},{"COMBO_STEP",Game.ComboStep},{"COMBO_CAP",Game.ComboCap},{"RETURN_CAP",Game.ReturnCap},{"MAX_COPIES",Game.MaxCopies}},
            de, de_strings=flat
        };
        Console.WriteLine(JsonSerializer.Serialize(data,new JsonSerializerOptions { DefaultIgnoreCondition=System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
    }
}
