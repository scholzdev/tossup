using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    // The Act 1 map: rows climb from the bottom to the boss. Nodes you can walk to next are lit; the node you
    // are on is outlined. A Mint or Back Room node shows its choices full screen until one is taken.
    public static class MapView
    {
        const float NodeW = 120, NodeH = 36, RowStep = 64, BottomY = 640, CenterX0 = 250, LaneStep = 165;

        static string Label(NodeKind kind) => kind == NodeKind.Event ? "BACK ROOM" : kind == NodeKind.Boss ? "THE HOUSE" : kind.ToString().ToUpperInvariant();

        static Rgba Tint(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Elite: return C.Red;
                case NodeKind.Shop: return C.Gold;
                case NodeKind.Mint: return C.Orange;
                case NodeKind.Altar: return C.Purple;
                case NodeKind.Event: return C.Green;
                case NodeKind.Boss: return C.Red;
                default: return C.Blue;
            }
        }

        static float NodeX(MapNode n) => n.Row == Game.MapRows ? CenterX0 + LaneStep * (Game.MapLanes - 1) / 2f : CenterX0 + n.Lane * LaneStep;
        static float NodeY(MapNode n) => BottomY - n.Row * RowStep;

        public static void Draw()
        {
            var g = Ui.Game;
            Frame(null);
            Text("ACT 1", 70, 62, Ui.F32, C.Gold);
            Button("MENU", 1120, 56, 100, 34, C.PanelLight, A.OpenMenu);
            ImageAt(Ui.UiImages["gold"], 1010, 100, 44);
            Text(GameText.Num(g.Player.Gold), 1062, 104, Ui.F32, C.Gold);
            if (g.MapPrompt != null) { DrawPrompt(g); return; }

            // edges first, so the nodes sit on top of them
            for (int i = 0; i < g.Map.Count; i++)
            {
                var a = g.Map[i];
                bool lit = g.MapAt == i;
                foreach (int next in a.Next)
                {
                    var b = g.Map[next];
                    Color(lit ? C.Gold : C.Line);
                    Gfx.SetLineWidth(lit ? 4 : 2);
                    Gfx.Line(NodeX(a), NodeY(a), NodeX(b), NodeY(b) + NodeH);
                }
            }
            Gfx.SetLineWidth(1);
            int currentRow = g.MapAt.HasValue ? g.Map[g.MapAt.Value].Row : -1;
            for (int i = 0; i < g.Map.Count; i++)
            {
                var n = g.Map[i];
                float x = NodeX(n) - NodeW / 2, y = NodeY(n);
                bool reachable = Game.MapReachable(g, i);
                int index = i;
                Button(Label(n.Kind), x, y, NodeW, NodeH, Tint(n.Kind), () => A.ChooseNode(index), reachable, face: Ui.F16);
                if (reachable) Outline(x - 5, y - 5, NodeW + 10, NodeH + 10, C.Gold);
                string enemy = reachable ? Game.NodeEnemy(g, i) : null;
                if (enemy != null) Centered(Lang.Upper(L(EnemyCatalog.ById[enemy].Name)), x - 20, y + NodeH + 6, NodeW + 40, Ui.F16, C.Face);
                if (g.MapAt == i) Outline(x - 5, y - 5, NodeW + 10, NodeH + 10, C.Face);
                else if (n.Row <= currentRow && !reachable) { Color(C.Black, .35f); Gfx.Rectangle(true, x, y, NodeW, NodeH); }
            }

            // the side panel: what each node means
            Box(920, 150, 300, 500, C.PanelDk);
            Outline(920, 150, 300, 500, C.Line);
            Centered(g.MapAt.HasValue ? "CHOOSE YOUR NEXT STOP" : "CHOOSE WHERE TO START", 920, 164, 300, Ui.F16, C.Gold);
            string[] help =
            {
                "TABLE|A fight", "ELITE|Harder fight, a relic", "SHOP|Coins, chips, prizes", "MINT|Melt a coin or gain",
                "ALTAR|Pick an augment", "BACK ROOM|Gold, a chip or a head start", "THE HOUSE|The boss",
            };
            for (int i = 0; i < help.Length; i++)
            {
                string[] parts = help[i].Split('|');
                float y = 204 + i * 44;
                Text(parts[0], 940, y, Ui.F16, parts[0] == "TABLE" ? C.Blue : parts[0] == "ELITE" || parts[0] == "THE HOUSE" ? C.Red : parts[0] == "SHOP" ? C.Gold : parts[0] == "MINT" ? C.Orange : parts[0] == "ALTAR" ? C.Purple : C.Green);
                Text(parts[1], 940, y + 20, Ui.F16, C.Muted);
            }
            Text(L("FIGHTS WON  %d", g.Cleared), 940, 520, Ui.F16, C.Face);
            Text(L("POUCH  %d / %d", g.Coins.Count, Game.DeckMax), 940, 544, Ui.F16, C.Face);
            var held = new List<string>();
            foreach (var id in g.Items) held.Add(Lang.ItemShort(id));
            foreach (var id in g.Relics) held.Add(Lang.Upper(Lang.RelicName(id)));
            if (held.Count > 0) Text(L("HELD  %s", string.Join(", ", held)), 940, 568, Ui.F16, C.Orange);
        }

        static void DrawPrompt(GameState g)
        {
            bool mint = g.MapPrompt == "mint";
            Centered(mint ? "THE MINT" : "THE BACK ROOM", 70, 150, 1140, Ui.F32, C.Gold);
            Centered(mint ? "CHOOSE ONE" : "CHOOSE ONE REWARD", 70, 196, 1140, Ui.F20, C.Muted);
            var selected = g.SelectedUid.HasValue ? Game.GetCoin(g, g.SelectedUid.Value) : null;
            const float gap = 30, y = 250;
            float w = mint ? 250 : 320, x0 = (1280 - (w * 3 + gap * 2)) / 2;
            if (mint)
            {
                x0 = (1280 - (w * 4 + gap * 3)) / 2;
                Button("REMOVE SELECTED COIN", x0, y, w, 72, C.Red, () => A.MapChoose("remove", selected.Uid), selected != null && g.Coins.Count > 1, face: Ui.F16);
                Button("STAMP NORMAL INTO GILDED", x0 + w + gap, y, w, 72, C.Purple, () => A.MapChoose("stamp", selected.Uid), selected != null && selected.Id == "normal", face: Ui.F16);
                Button("+1 MAX ENERGY", x0 + 2 * (w + gap), y, w, 72, C.Blue, () => A.MapChoose("energy"), g.Player.MaxEnergy < 5);
                Button("+8 GOLD", x0 + 3 * (w + gap), y, w, 72, C.Gold, () => A.MapChoose("gold"));
            }
            else
            {
                Button("+15 GOLD", x0, y, w, 72, C.Gold, () => A.MapChoose("gold"));
                Button("A RANDOM CHIP", x0 + w + gap, y, w, 72, C.Blue, () => A.MapChoose("item"), g.Items.Count < Items.Max);
                Button(L("NEXT ENEMY -%d POINTS", Game.BackRoomEdge), x0 + 2 * (w + gap), y, w, 72, C.Green, () => A.MapChoose("edge"));
            }
            if (!mint) return;
            Text(L("YOUR POUCH  %d / %d", g.Coins.Count, Game.DeckMax), 70, 400, Ui.F20, C.Gold);
            Text("CLICK A COIN TO SELECT IT", 340, 406, Ui.F16, C.Muted);
            PouchStrip(g, 70, 440, 17, 2, C.Card);
        }
    }
}
