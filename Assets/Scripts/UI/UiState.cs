using System;
using System.Collections.Generic;

namespace Tossup.UI
{
    // A clickable area registered while drawing; clicks are matched against the last frame's list.
    public sealed class Button
    {
        public float X, Y, W, H;
        public string Label; // the untranslated caption, for tests
        public Action Action;
        public Action<float> Drag; // sliders follow the mouse until it is released
        public Action Release;

        public bool Contains(float x, float y) => x >= X && x <= X + W && y >= Y && y <= Y + H;
    }

    public sealed class HoveredCoin
    {
        public string Id;
        public double Probability;
        public bool Locked;
    }

    public sealed class HoveredText
    {
        public string Title, Body;
    }

    public sealed class Confirm
    {
        public string Title, Text;
        public Action Ok;
    }

    public sealed class FlipAnimation
    {
        public string Id, Outcome;
        public double Elapsed, Duration;
    }

    public sealed class TutorialState { public int Step = 1; }
    public sealed class EncounterReveal { public double Elapsed; }

    public sealed class SetDraft
    {
        public string Character;
        public int Index;
        public List<string> Coins;
    }

    // Shared mutable UI state. Game rules live in Core/Game.cs; this is presentation only.
    // The game is drawn on a fixed 1280x800 canvas that is scaled and centred to fit the window.
    public static class Ui
    {
        public const float Width = 1280, Height = 800;

        public static IPlatform Platform;

        public static string SelectedCharacter = "blade";
        public static string Screen = "title"; // title | select | sets | collection | options | help
        public static string SetsCharacter = "blade"; // coin set editor: which character and which of its sets is open
        public static int SetsIndex = 1;
        public static SetDraft SetDraft; // unsaved edits of the open coin set
        public static HashSet<int> Marked = new HashSet<int>(); // coins marked for discarding in the opening hand
        public static int CollectionPage = 1;
        public static readonly Dictionary<string,int> StakePick = new Dictionary<string,int>();
        public static string CollectionFilter = "ALL";
        public static string CollectionSort = "rarity";
        public static GameState Game;
        public static ProfileData Profile; // meta progression (tokens, unlocks), loaded in AppCore.Load
        public static List<Button> Buttons = new List<Button>();
        public static bool DebugVisible;
        public static string Notice = "";
        public static PixFont F16, F20, F32, F48;
        public static readonly Dictionary<string, Img> CoinImages = new Dictionary<string, Img>();
        public static readonly Dictionary<string, Img> ItemImages = new Dictionary<string, Img>();
        public static readonly Dictionary<string, Img> RelicImages = new Dictionary<string, Img>();
        public static readonly Dictionary<string, Img> UiImages = new Dictionary<string, Img>();
        public static readonly Dictionary<string, Img> CharacterImages = new Dictionary<string, Img>();
        public static readonly Dictionary<string, Img> AugmentImages = new Dictionary<string, Img>();
        public static readonly Dictionary<string, Img> EncounterImages = new Dictionary<string, Img>();
        public static HoveredCoin HoveredCoin;
        public static HoveredText HoveredText;
        public static Button Dragging; // the slider being dragged
        public static string OptionsTab = "game"; // game | sound
        public static Confirm Confirm; // a modal popup: Clear Progress, Quit during a run
        public static string HelpNext; // where the How To Play "continue" button goes on a first run
        public static string CursorCurrent;
        public static FlipAnimation FlipAnimation;
        public static double Shake;
        public static bool Holding; // landed coin stays in view until the player asks for the next one
        public static double ResolveTimer; // hold on the flipped result before effects apply
        public static TutorialState Tutorial;
        public static EncounterReveal EncounterReveal;
        public static SandboxConfig SandboxConfig;

        public static void Layout(out float scale, out float ox, out float oy)
        {
            float w = Platform.WindowWidth, h = Platform.WindowHeight;
            scale = Math.Min(w / Width, h / Height);
            ox = (w - Width * scale) / 2;
            oy = (h - Height * scale) / 2;
        }

        // Window coordinates -> canvas coordinates.
        public static void ToCanvas(float x, float y, out float cx, out float cy)
        {
            Layout(out float scale, out float ox, out float oy);
            cx = (x - ox) / scale;
            cy = (y - oy) / scale;
        }

        public static void Mouse(out float mx, out float my) => ToCanvas(Platform.MouseX, Platform.MouseY, out mx, out my);
    }
}
