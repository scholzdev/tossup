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
            "next_coin", "start_level", "shop_title", "logo", "title_scene", "title_play", "title_sets", "title_collection", "title_options",
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
            foreach (var id in Game.AugmentOrder) Ui.AugmentImages[id] = platform.LoadImage("augments/" + (id == "epic_windfall" ? "gold/" : "silver/") + id);
            foreach (var id in new[] { "house_clock", "dead_heat", "high_roller_table", "thin_market" }) Ui.EncounterImages[id] = platform.LoadImage("encounters/" + id);
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
            else if (game.Phase == Phase.Contract) ContractView.Draw();
            else if (game.Phase == Phase.Augment) AugmentView.Draw();
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
            if(Ui.EncounterReveal!=null)EncounterRevealView.Draw();
            if(Ui.Tutorial!=null)Tutorial.Draw();
            ConfirmDialog();
            if (Ui.EncounterReveal == null) PadNavigation.DrawFocus();
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
                if (!b.Disabled && b.Contains(mx, my)) { over = true; break; }
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
            PadNavigation.MouseUsed();
            if(Ui.EncounterReveal!=null){DismissEncounterReveal();return;}
            Ui.ToCanvas(x, y, out float cx, out float cy);
            for (int i = Ui.Buttons.Count - 1; i >= 0; i--)
            {
                var b = Ui.Buttons[i];
                if (b.Disabled || !b.Contains(cx, cy)) continue;
                ActivateButton(b);
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
            PadNavigation.MouseUsed();
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

        public static void ActivateButton(Button button)
        {
            if (button == null || button.Disabled || button.Action == null) return;
            Ui.Notice = "";
            Sound.Play("click");
            button.Action();
        }

        // key: LÖVE key names used by the rules UI (arrows, Q/E, I, Enter, Space, Escape, digits, F3/F5).
        public static void KeyPressed(string key, bool fromController = false)
        {
            if (fromController) PadNavigation.ControllerUsed();
            else PadNavigation.KeyboardUsed();
            if(Ui.EncounterReveal!=null){DismissEncounterReveal();return;}
            if (key == "f3")
            {
                Ui.DebugVisible = !Ui.DebugVisible;
                return;
            }
            if (Ui.Tutorial != null)
            {
                if (key == "escape") { Tutorial.Finish(); return; }
                if ((key == "space" || key == "return") && !Tutorial.Interactive) { Tutorial.Next(); return; }
            }
            if (Ui.Confirm != null)
            {
                if (key == "escape") Ui.Confirm = null;
                else PadNavigation.HandleKeyboardKey(key);
                return;
            }

            if (key == "escape")
            {
                var game = Ui.Game;
                if (game != null && !game.Paused) A.OpenMenu();
                else if (Ui.Screen != "title") A.Go("title");
                else if (game != null) game.Paused = false;
                return;
            }

            if (PadNavigation.HandleKeyboardKey(key)) return;

            var currentGame = Ui.Game;
            if (currentGame != null && !currentGame.Paused && currentGame.Phase == Phase.Encounter &&
                Ui.Tutorial == null && currentGame.Mulligan == null && Ui.FlipAnimation == null)
            {
                if (int.TryParse(key, out int slot) && slot >= 1 && slot <= 3 && !Ui.Holding && currentGame.Items.Count >= slot)
                {
                    A.UseItem(slot);
                    return;
                }
                if (key == "o" && currentGame.Encounter.Cleared && currentGame.Pending == null)
                {
                    A.OpenShop();
                    return;
                }
            }

            if ((currentGame == null || currentGame.Paused) && Ui.Screen == "select" &&
                int.TryParse(key, out int choice) && choice >= 1 && choice <= Content.CharacterOrder.Count)
            {
                A.SelectCharacter(Content.CharacterOrder[choice - 1]);
                return;
            }
            if (key == "space" && currentGame != null && !currentGame.Paused &&
                currentGame.Phase == Phase.Encounter && Ui.FlipAnimation == null)
                A.NextOrFlip();
        }

        public static bool DismissEncounterReveal()
        {
            if(Ui.EncounterReveal==null)return false;
            Ui.EncounterReveal=null;
            return true;
        }
    }
}
