using System;
using System.Collections.Generic;

namespace Tossup
{
    // Act 1 map (Slay the Spire style): MapRows rows of up to MapLanes nodes, then the boss. Walking from row 0
    // upward the player picks one of the nodes the current one connects to. Everything here is seeded (Rng),
    // so one seed always gives the same map.
    public static partial class Game
    {
        // MapStarts paths begin apart and stay apart up to row MapSplitRow; from there MapPaths walks (the starts
        // plus branches off them) wander and may merge.
        public const int BackRoomEdge = 5; // points the Back Room gives you in the next fight
        public const int MapRows = 8, MapLanes = 4, MapPaths = 6, MapStarts = 3, MapSplitRow = 3;

        // Weights in percent, in NodeKind order (Boss is never rolled).
        static readonly NodeKind[] RolledKinds = { NodeKind.Table, NodeKind.Elite, NodeKind.Shop, NodeKind.Mint, NodeKind.Event, NodeKind.Altar };
        static readonly int[] RolledWeights = { 45, 12, 13, 10, 12, 8 };

        public static List<MapNode> BuildMap(GameState g)
        {
            // edges[row, lane]: lanes reached in the next row; null = no node here.
            var edges = new List<int>[MapRows, MapLanes];
            // the separate starts: path p keeps to lanes p..p+1 and right of the path before it, so they never meet
            // or cross (ponytail: relies on MapLanes == MapStarts + 1)
            var at = new int[MapStarts];
            for (int row = 0; row <= MapSplitRow; row++)
            {
                int taken = -1;
                for (int p = 0; p < MapStarts; p++)
                {
                    var options = new List<int>();
                    for (int to = p; to <= p + 1; to++)
                        if (to > taken && (row == 0 || Math.Abs(to - at[p]) <= 1)) options.Add(to);
                    int lane = options[Rng.Int(g, 1, options.Count) - 1]; // p + 1 always qualifies
                    if (row > 0) edges[row - 1, at[p]].Add(lane);
                    edges[row, lane] = new List<int>();
                    at[p] = taken = lane;
                }
            }
            // from the split row on: each start walks on, and the extra walks branch off the starts
            for (int path = 0; path < MapPaths; path++)
            {
                int lane = at[path % MapStarts];
                for (int row = MapSplitRow; row < MapRows; row++)
                {
                    if (edges[row, lane] == null) edges[row, lane] = new List<int>();
                    if (row == MapRows - 1) break;
                    var options = new List<int>();
                    for (int to = lane - 1; to <= lane + 1; to++)
                        if (to >= 0 && to < MapLanes && !Crosses(edges, row, lane, to)) options.Add(to);
                    int next = options[Rng.Int(g, 1, options.Count) - 1]; // straight on is never a crossing, so never empty
                    if (!edges[row, lane].Contains(next)) edges[row, lane].Add(next);
                    lane = next;
                }
            }

            var map = new List<MapNode>();
            var index = new int[MapRows, MapLanes];
            for (int row = 0; row < MapRows; row++)
                for (int lane = 0; lane < MapLanes; lane++)
                    if (edges[row, lane] != null) { index[row, lane] = map.Count; map.Add(new MapNode { Row = row, Lane = lane }); }
            foreach (var node in map)
            {
                if (node.Row == MapRows - 1) continue;
                var lanes = edges[node.Row, node.Lane];
                lanes.Sort();
                foreach (int to in lanes) node.Next.Add(index[node.Row + 1, to]);
            }
            int bossIndex = map.Count;
            foreach (var node in map) if (node.Row == MapRows - 1) node.Next.Add(bossIndex);
            map.Add(new MapNode { Row = MapRows, Lane = MapLanes / 2, Kind = NodeKind.Boss });

            for (int i = 0; i < bossIndex; i++)
            {
                var node = map[i];
                if (node.Row == 0) node.Kind = NodeKind.Table;
                else if (node.Row == MapRows - 1) node.Kind = NodeKind.Mint; // a rest stop before the boss
                else
                {
                    bool shopFollowsShop = false;
                    foreach (var parent in map)
                        if (parent.Row == node.Row - 1 && parent.Next.Contains(i) && parent.Kind == NodeKind.Shop) shopFollowsShop = true;
                    node.Kind = RollKind(g, node.Row, !shopFollowsShop);
                }
            }
            return map;
        }

        // Would the edge (row, lane) -> (row + 1, to) cross an edge that is already drawn?
        static bool Crosses(List<int>[,] edges, int row, int lane, int to)
        {
            if (to == lane) return false;
            int neighbour = to; // the neighbour whose edge would run the other way is the lane we step to
            return neighbour >= 0 && neighbour < MapLanes && edges[row, neighbour] != null && edges[row, neighbour].Contains(lane);
        }

        static NodeKind RollKind(GameState g, int row, bool shopAllowed)
        {
            int roll = Rng.Int(g, 1, 100);
            for (int i = 0; i < RolledKinds.Length; i++)
            {
                roll -= RolledWeights[i];
                if (roll > 0) continue;
                if (RolledKinds[i] == NodeKind.Elite && row < 3) break;
                if (RolledKinds[i] == NodeKind.Shop && !shopAllowed) break;
                return RolledKinds[i];
            }
            return NodeKind.Table;
        }

        public static bool MapReachable(GameState g, int index) =>
            g != null && g.Map != null && g.Phase == Phase.Map && g.MapPrompt == null && index >= 0 && index < g.Map.Count &&
            (g.MapAt.HasValue ? g.Map[g.MapAt.Value].Next.Contains(index) : g.Map[index].Row == 0);

        public static bool ChooseNode(GameState g, int index)
        {
            if (!MapReachable(g, index) || ActiveCount(g) == 0) return false;
            g.MapAt = index;
            var node = g.Map[index];
            g.PendingEnemyId = NodeEnemy(g, index);
            Log(g, "Map: " + node.Kind + ".");
            switch (node.Kind)
            {
                case NodeKind.Table: StartMapFight(g, false); break;
                case NodeKind.Elite: StartMapFight(g, true); break;
                case NodeKind.Boss: g.EncounterIndex = Route.Count; StartEncounter(g); break; // The House
                case NodeKind.Shop: EnterShop(g); break;
                case NodeKind.Mint: g.MapPrompt = "mint"; break;
                case NodeKind.Event: g.MapPrompt = "backroom"; break;
                case NodeKind.Altar: OpenAltar(g); break;
            }
            return true;
        }

        // The enemy at a fight node; null for other nodes. Pure, so the map can show it beforehand.
        public static string NodeEnemy(GameState g, int index)
        {
            var kind = g.Map[index].Kind;
            if (kind == NodeKind.Boss) return PickEnemy(g, Route.Count, false, true, index + 1);
            if (kind != NodeKind.Table && kind != NodeKind.Elite) return null;
            return PickEnemy(g, Math.Max(1, Math.Min(Route.Count - 1, g.Cleared + 1)), kind == NodeKind.Elite, false, index + 1);
        }

        // Difficulty follows the fights won so far, not the row: skipping fights does not make the run harder.
        static void StartMapFight(GameState g, bool elite)
        {
            g.EncounterIndex = Math.Max(1, Math.Min(Route.Count - 1, g.Cleared + 1));
            StartEncounter(g);
            if (!elite) return;
            var e = g.Encounter;
            e.Name = "Elite " + e.Name;
            if (e.Modifier == null && g.Sandbox == null) // an elite always carries a modifier
            {
                string id = ModifierOrder[Rng.Int(g, 1, ModifierOrder.Count) - 1];
                e.Modifier = id;
                Modifiers[id].Apply(g, e);
            }
        }

        // EndLevel of a map fight: back to the map; an elite also drops a relic.
        static void ReturnToMap(GameState g)
        {
            if (g.Map[g.MapAt.Value].Kind == NodeKind.Elite)
            {
                string relic = PickUnownedRelic(g);
                if (relic != null) { AddRelic(g, relic); Log(g, "Elite reward: relic " + Content.Relics[relic].Name + "."); }
            }
            g.Phase = Phase.Map;
        }

        static void OpenAltar(GameState g)
        {
            var pool = AugmentPool(g);
            if (g.Sandbox == null && g.Augments.Count < 2 && pool.Count >= 3)
            {
                g.AugmentOptions = Offers(g, pool, 3);
                g.Phase = Phase.Augment;
                return;
            }
            g.Player.Gold += 12; // ponytail: an altar with nothing left to offer pays gold instead
            Log(g, "The altar has nothing left to offer: +12 gold.");
        }

        // Answers a Mint ("remove" or "stamp" a coin by uid, "energy", "gold") or the Back Room ("gold", "item", "edge").
        public static bool MapChoose(GameState g, string option, int uid = 0)
        {
            if (g == null || g.Phase != Phase.Map || g.MapPrompt == null) return false;
            if (g.MapPrompt == "mint")
            {
                if (option == "remove")
                {
                    var coin = GetCoin(g, uid);
                    if (coin == null || g.Coins.Count <= 1) return false;
                    g.Coins.Remove(coin);
                    g.SelectedUid = g.Coins[0].Uid;
                    Log(g, "The Mint melted down " + CoinName(coin) + ".");
                }
                else if (option == "energy")
                {
                    if (g.Player.MaxEnergy >= 5) return false;
                    g.Player.MaxEnergy += 1;
                    Log(g, "The Mint gave +1 maximum energy.");
                }
                else if (option == "gold") { g.Player.Gold += 8; Log(g, "The Mint paid 8 gold."); }
                else if (option == "stamp") { if (!StampNormal(g, uid, "gilded")) return false; }
                else return false;
            }
            else
            {
                if (option == "gold") { g.Player.Gold += 15; Log(g, "Back Room: +15 gold."); }
                else if (option == "item")
                {
                    if (g.Items.Count >= Items.Max) return false;
                    var ids = new List<string>(Content.Items.Keys);
                    ids.Sort(string.CompareOrdinal);
                    string id = ids[Rng.Int(g, 1, ids.Count) - 1];
                    g.Items.Add(id);
                    Log(g, "Back Room: got the chip " + Content.Items[id].Name + ".");
                }
                else if (option == "edge")
                {
                    g.NextFightEdge -= BackRoomEdge; // negative: the next fight starts with you ahead
                    Log(g, "Back Room: the next enemy starts " + BackRoomEdge + " points behind.");
                }
                else return false;
            }
            g.MapPrompt = null;
            return true;
        }
    }
}
