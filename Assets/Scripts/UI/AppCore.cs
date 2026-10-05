using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    // The game loop callbacks (LÖVE's load/draw/update/input), engine independent. Unity's TossupApp
    // forwards to these; Tools/uitest drives them headless.
    public static class AppCore
    {
        static readonly string[] UiImageNames =
        {
            "next_round", "reroll", "gold", "energy", "coins_left", "open_shop", "exchange", "give_up", "flip", "discard",
            "next_coin", "start_level", "shop_title", "logo", "title_play", "title_sets", "title_collection", "title_options",
            "title_help", "title_play_de", "title_sets_de", "title_collection_de", "title_options_de", "title_help_de",
        };

        static readonly Dictionary<string, Action> Screens = new Dictionary<string, Action>
        {
            { "title", TitleView.Draw },
            { "select", SelectView.Draw },
            { "collection", CollectionView.Draw },
            { "sets", SetsView.Draw },
            { "options", OptionsView.Draw },
            { "help", HelpView.Draw },
        };

        public static void Load(IPlatform platform)
        {
            Ui.Platform = platform;
            Lang.Load("de", platform.LoadText("locales/de"));
            Ui.F16 = platform.LoadFont(16);
            Ui.F20 = platform.LoadFont(20);
            Ui.F32 = platform.LoadFont(32);
            Ui.F48 = platform.LoadFont(48);
            foreach (var id in Content.CoinOrder) Ui.CoinImages[id] = platform.LoadImage("coins/" + id);
            Ui.CoinImages["back"] = platform.LoadImage("coins/back");
            foreach (var id in Content.ItemOrder) Ui.ItemImages[id] = platform.LoadImage("items/" + id);
            foreach (var id in Content.RelicOrder) Ui.RelicImages[id] = platform.LoadImage("relics/" + id);
            foreach (var name in UiImageNames) Ui.UiImages[name] = platform.LoadImage("ui/" + name);
            Ui.UiImages["title_shop"] = Ui.UiImages["shop_title"];
            foreach (var id in Content.CharacterOrder) Ui.CharacterImages[id] = platform.LoadImage("characters/" + id);
            A.LoadProfile();
            Ui.CursorCurrent = "arrow";
            platform.SetCursor("arrow");
        }

        static void DrawGame()
        {
            var game = Ui.Game;
            if (game.Phase == Phase.Encounter) EncounterView.Draw();
            else if (game.Phase == Phase.Shop) ShopView.Draw();
            else FinishView.Draw(); // Victory and GameOver
            if (Ui.Notice != "") Text(Ui.Notice, 300, 762, Ui.F16, C.Red);
            if (Ui.DebugVisible)
            {
                Box(944, 177, 300, 101, C.Ink);
                Text("DEBUG / F3", 955, 184, Ui.F16, C.Gold);
                Text("RNG " + game.RngState, 955, 207, Ui.F16);
                Text("FLIPS " + game.Encounter.Flips, 955, 231, Ui.F16);
                Text("LAST " + (game.LastRng.HasValue ? GameText.Format("%.5f", game.LastRng.Value) : "-"), 955, 255, Ui.F16);
            }
        }

        // Draws one frame onto the 1280x800 canvas. The backend has already cleared the window with the felt
        // colour (which fills the bars around the 16:10 canvas).
        public static void Draw()
        {
            Ui.Buttons = new List<Button>();
            Ui.HoveredCoin = null;
            Ui.HoveredText = null;
            Gfx.Reset();
            Gfx.Push();
            if (Ui.Shake > 0)
            {
                var p = Ui.Platform;
                Gfx.Translate((float)(p.Random(-6, 6) * Ui.Shake / .3), (float)(p.Random(-6, 6) * Ui.Shake / .3));
            }
            Color(C.Felt);
            Gfx.Rectangle(true, 0, 0, 1280, 800);
            if (Ui.Game == null || Ui.Game.Paused) Screens[Ui.Screen]();
            else DrawGame();
            CoinTooltip();
            TextTooltip();
            ConfirmDialog();
            Gfx.Pop();
        }

        public static void Update(double dt)
        {
            A.Update(dt);
            Sound.Watch();
            // pick the cursor from what the last frame drew under the mouse
            Ui.Mouse(out float mx, out float my);
            bool over = false;
            foreach (var b in Ui.Buttons)
                if (b.Contains(mx, my)) { over = true; break; }
            string want = over ? "click" : "arrow";
            if (want != Ui.CursorCurrent)
            {
                Ui.Platform.SetCursor(want);
                Ui.CursorCurrent = want;
            }
        }

        // Left button only; x, y in window pixels.
        public static void MousePressed(float x, float y)
        {
            Ui.ToCanvas(x, y, out float cx, out float cy);
            for (int i = Ui.Buttons.Count - 1; i >= 0; i--)
            {
                var b = Ui.Buttons[i];
                if (!b.Contains(cx, cy)) continue;
                Ui.Notice = "";
                Sound.Play("click");
                b.Action();
                if (b.Drag != null)
                {
                    Ui.Dragging = b;
                    b.Drag(cx);
                }
                return;
            }
        }

        public static void MouseMoved(float x, float y)
        {
            if (Ui.Dragging == null) return;
            Ui.ToCanvas(x, y, out float cx, out _);
            Ui.Dragging.Drag(cx);
        }

        public static void MouseReleased()
        {
            var slider = Ui.Dragging;
            Ui.Dragging = null;
            slider?.Release?.Invoke();
        }

        // key: LÖVE key names ("f3", "escape", "space", "return", "left", "right", "1".."9").
        public static void KeyPressed(string key)
        {
            var game = Ui.Game;
            if (key == "f3")
            {
                Ui.DebugVisible = !Ui.DebugVisible;
                return;
            }
            if (Ui.Confirm != null) // a popup is open
            {
                if (key == "escape") Ui.Confirm = null;
                return;
            }
            if (key == "escape")
            {
                if (game != null && !game.Paused) A.OpenMenu();
                else if (Ui.Screen != "title") A.Go("title");
                else if (game != null) game.Paused = false;
                return;
            }
            if (game == null || game.Paused)
            {
                if (Ui.Screen == "select")
                {
                    if (int.TryParse(key, out int choice) && choice >= 1 && choice <= Content.CharacterOrder.Count)
                        A.SelectCharacter(Content.CharacterOrder[choice - 1]);
                    if (key == "left") A.CycleCharacter(-1);
                    if (key == "right") A.CycleCharacter(1);
                    if (key == "return") A.Start();
                }
                else if (Ui.Screen == "collection")
                {
                    if (key == "left") A.ChangeCollectionPage(-1);
                    if (key == "right") A.ChangeCollectionPage(1);
                }
                return;
            }
            if (game.Phase == Phase.Encounter && Ui.FlipAnimation != null) return;
            if (key == "space" && game.Phase == Phase.Encounter) A.NextOrFlip();
            else if (key == "return" && game.Phase == Phase.Shop) Game.LeaveShop(game);
        }
    }
}
