using System;
using System.Collections.Generic;

namespace Tossup.UI
{
    // Shared keyboard and gamepad focus over the buttons registered by the current UI frame.
    public static class PadNavigation
    {
        const float DeadZone = .6f;
        const float FocusRadiusSquared = 120 * 120;
        static float focusX = Ui.Width / 2, focusY = 400;
        static double repeatTimer;
        static bool hasFocus, active, needsDefaultFocus = true;
        static bool leftTriggerWasDown, rightTriggerWasDown, inspecting;
        static string context;

        public static string Device { get; private set; } = "keyboard";
        public static bool Inspecting => inspecting;
        public static bool Connected;

        public static void Update(double dt, float stickX, float stickY, float dpadX, float dpadY,
            float leftTrigger, float rightTrigger)
        {
            UpdateContext();
            repeatTimer = Math.Max(0, repeatTimer - dt);

            float x = Math.Abs(dpadX) > Math.Abs(stickX) ? dpadX : stickX;
            float y = Math.Abs(dpadY) > Math.Abs(stickY) ? dpadY : stickY;
            if (repeatTimer == 0 && (Math.Abs(x) > DeadZone || Math.Abs(y) > DeadZone))
            {
                ControllerUsed();
                Move(Math.Abs(x) > Math.Abs(y) ? Math.Sign(x) : 0,
                    Math.Abs(y) >= Math.Abs(x) ? Math.Sign(y) : 0);
                repeatTimer = .22;
            }

            bool leftDown = leftTrigger > .5f;
            bool rightDown = rightTrigger > .5f;
            if (leftDown && !leftTriggerWasDown) { ControllerUsed(); Step(-1); }
            if (rightDown && !rightTriggerWasDown) { ControllerUsed(); Step(1); }
            leftTriggerWasDown = leftDown;
            rightTriggerWasDown = rightDown;
        }

        public static void KeyboardUsed() => Device = "keyboard";

        public static void ControllerUsed()
        {
            Device = "controller";
            active = true;
            UpdateContext();
        }

        public static void MouseUsed()
        {
            active = false;
            inspecting = false;
        }

        public static bool HandleKeyboardKey(string key)
        {
            switch (key)
            {
                case "up": Move(0, -1); return true;
                case "down": Move(0, 1); return true;
                case "left": Move(-1, 0); return true;
                case "right": Move(1, 0); return true;
                case "return": Accept(); return true;
                case "q": Step(-1); return true;
                case "e": Step(1); return true;
                case "i": ToggleInspect(); return true;
                default: return false;
            }
        }

        public static void Move(int dx, int dy)
        {
            active = true;
            UpdateContext();
            var current = FocusedButton();
            if (dx != 0 && current?.Adjust != null)
            {
                current.Adjust(dx);
                return;
            }
            if (current == null)
            {
                var nearest = Nearest(focusX, focusY, out _);
                if (nearest != null) SetFocus(nearest);
                return;
            }

            Center(current, out float fx, out float fy);
            Button best = null;
            float bestScore = float.MaxValue;
            foreach (var button in Ui.Buttons)
            {
                if (button == current) continue;
                Center(button, out float cx, out float cy);
                float rx = cx - fx, ry = cy - fy;
                float along = rx * dx + ry * dy;
                if (along <= 1) continue;
                float score = along + 2.5f * Math.Abs(rx * dy - ry * dx);
                if (score < bestScore) { best = button; bestScore = score; }
            }

            if (best == null)
            {
                var order = new List<Button>(Ui.Buttons);
                order.Sort((a, b) => Math.Abs(a.Y - b.Y) > 12 ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));
                int index = order.IndexOf(current);
                if (index >= 0 && order.Count > 0)
                {
                    int step = dx + dy > 0 ? 1 : -1;
                    best = order[(index + step + order.Count) % order.Count];
                }
            }

            if (best != null) SetFocus(best);
        }

        public static void Step(int direction)
        {
            active = true;
            if (Ui.Confirm != null || Ui.Tutorial != null) return;
            if (Ui.Game != null && !Ui.Game.Paused)
            {
                inspecting = !inspecting;
                return;
            }

            switch (Ui.Screen)
            {
                case UiScreen.Select:
                    A.CycleCharacter(direction);
                    break;
                case UiScreen.Collection:
                    A.ChangeCollectionPage(direction);
                    break;
                case UiScreen.Options:
                    string[] tabs = { "game", "sound", "controls" };
                    int tab = Array.IndexOf(tabs, Ui.OptionsTab);
                    Ui.OptionsTab = tabs[((tab < 0 ? 0 : tab) + direction + tabs.Length) % tabs.Length];
                    break;
                case UiScreen.Sets:
                    var order = Content.CharacterOrder;
                    int index = order.IndexOf(Ui.SetsCharacter);
                    if (index >= 0 && order.Count > 0)
                    {
                        for (int step = 1; step <= order.Count; step++)
                        {
                            int candidateIndex = ((index + direction * step) % order.Count + order.Count) % order.Count;
                            string candidate = order[candidateIndex];
                            if (!Profile.CharacterUnlocked(Ui.Profile, candidate)) continue;
                            A.SetsPickCharacter(candidate);
                            break;
                        }
                    }
                    break;
            }
        }

        public static void ToggleInspect()
        {
            active = true;
            if (Ui.Confirm == null && Ui.Tutorial == null) inspecting = !inspecting;
        }

        public static void Accept()
        {
            active = true;
            var button = FocusedButton();
            if (button != null) AppCore.ActivateButton(button);
        }

        public static void ControllerPressed(int button)
        {
            ControllerUsed();
            if (Ui.EncounterReveal != null) { AppCore.KeyPressed("any", true); return; }
            switch (button)
            {
                case 0: Accept(); break;                         // A / Cross
                case 1: case 7: AppCore.KeyPressed("escape", true); break; // B / Circle / Start
                case 2: AppCore.KeyPressed("space", true); break; // X / Square
                case 3: ToggleInspect(); break;                  // Y / Triangle
                case 4: Step(-1); break;                         // left shoulder
                case 5: Step(1); break;                          // right shoulder
            }
        }

        public static Button FocusedButton()
        {
            EnsureDefaultFocus();
            if (!hasFocus) return null;
            var nearest = Nearest(focusX, focusY, out float distanceSquared);
            return distanceSquared <= FocusRadiusSquared ? nearest : null;
        }

        public static bool TryInspectPoint(out float x, out float y)
        {
            EnsureDefaultFocus();
            x = focusX;
            y = focusY;
            return inspecting && hasFocus;
        }

        public static void Inspect()
        {
            if (!inspecting || Ui.Confirm != null || Ui.Tutorial != null) return;
            var button = FocusedButton();
            if (button == null) return;
            Center(button, out float fx, out float fy);
            HoverRegion best = null; float bestGap = 160;
            foreach (var region in Ui.Regions)
            {
                if (fx < region.X || fx > region.X + region.W) continue;
                float gap = fy >= region.Y && fy <= region.Y + region.H ? 0 : fy - region.Y - region.H;
                if (gap >= 0 && gap < bestGap) { best = region; bestGap = gap; }
            }
            if (best == null) return;
            Ui.HoveredCoin = best.Coin; Ui.HoveredText = best.Text;
            Ui.HoverAnchorX = best.X + best.W / 2; Ui.HoverAnchorY = best.Y + best.H;
        }

        public static void DrawFocus()
        {
            if (Connected)
                foreach (var b in Ui.Buttons)
                {
                    if (b.Hotkey == null) continue;
                    float w = Math.Max(26, Ui.F16.GetWidth(b.Hotkey) + 14), x = b.X + (b.W - w) / 2, y = b.Y - (b.H < 40 ? 18 : 11);
                    var tint = b.Hotkey == "A" ? C.Green : b.Hotkey == "B" ? C.Red : b.Hotkey == "X" ? C.Blue : b.Hotkey == "Y" ? C.Gold : C.PanelLight;
                    D.Color(C.Black, .4f); Gfx.Rectangle(true, x, y + 2, w, 20, 5);
                    D.Color(tint, b.Disabled ? .55f : 1); Gfx.Rectangle(true, x, y, w, 20, 5);
                    D.Outline(x, y, w, 20, C.Face, 5); D.Centered(b.Hotkey, x, y + 2, w, Ui.F16, C.Ink);
                }
            if (!active) return;
            var button = FocusedButton();
            if (button == null) return;
            D.Color(C.Gold);
            Gfx.SetLineWidth(4);
            Gfx.Rectangle(false, button.X - 5, button.Y - 5, button.W + 10, button.H + 10, 8);
            Gfx.SetLineWidth(1);
        }

        static void UpdateContext()
        {
            var game = Ui.Game;
            string next = Ui.Screen + ":" + (game != null && !game.Paused ? game.Phase.ToString() : "-") + ":" +
                (Ui.Confirm != null ? "c" : "-") + ":" + (Ui.Tutorial != null ? Ui.Tutorial.Step.ToString() : "-");
            if (next != context)
            {
                context = next;
                needsDefaultFocus = true;
                hasFocus = false;
            }
        }

        static void EnsureDefaultFocus()
        {
            UpdateContext();
            if (!needsDefaultFocus || Ui.Buttons.Count == 0) return;
            DefaultFocus(out focusX, out focusY);
            hasFocus = true;
            needsDefaultFocus = false;
        }

        static void DefaultFocus(out float x, out float y)
        {
            var game = Ui.Game;
            if (Ui.Confirm != null) { x = Ui.Confirm.Single ? 810 : 962; y = 480; return; }
            if (Ui.Tutorial != null) { x = 810; y = 400; return; }
            if (game != null && !game.Paused)
            {
                if (game.Phase == Phase.Encounter) { x = EncounterView.EndRoundX + EncounterView.EndRoundW / 2; y = EncounterView.EndRoundY + EncounterView.EndRoundH / 2; return; }
                if (game.Phase == Phase.Shop) { x = 1110; y = 658; return; }
                x = 810; y = 614;
                return;
            }
            if (Ui.Screen == UiScreen.Select) { x = 810; y = 694; return; }
            Center(Ui.Buttons[0], out x, out y);
        }

        static Button Nearest(float x, float y, out float distanceSquared)
        {
            Button best = null;
            distanceSquared = float.MaxValue;
            foreach (var button in Ui.Buttons)
            {
                Center(button, out float cx, out float cy);
                float dx = cx - x, dy = cy - y;
                float d = dx * dx + dy * dy;
                if (d < distanceSquared) { best = button; distanceSquared = d; }
            }
            return best;
        }

        static void SetFocus(Button button)
        {
            Center(button, out focusX, out focusY);
            hasFocus = true;
        }

        static void Center(Button button, out float x, out float y)
        {
            x = button.X + button.W / 2;
            y = button.Y + button.H / 2;
        }
    }
}
