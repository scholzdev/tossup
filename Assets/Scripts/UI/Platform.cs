namespace Tossup.UI
{
    // Everything the game needs from the engine, so the UI code itself has no UnityEngine dependency (and
    // can be driven headless by Tools/uitest).
    public interface IPlatform
    {
        // mouse position in window pixels, origin top left
        float MouseX { get; }
        float MouseY { get; }
        int WindowWidth { get; }
        int WindowHeight { get; }
        double Time { get; } // seconds since start (love.timer.getTime)
        long UnixTime { get; } // os.time()
        int Random(int low, int high); // inclusive; presentation only (screen shake)

        Img LoadImage(string path); // e.g. "coins/normal"
        PixFont LoadFont(int size);
        string LoadText(string path); // Unity Resources path without an extension, e.g. "locales/de"

        string ReadSave(string name); // null if missing
        void WriteSave(string name, string text);
        void AppendSave(string name, string text);

        void PlaySound(string name, float pitch, float volume);
        void SetMusicVolume(float volume);
        void SetCursor(string name); // "arrow" | "click"
        void SetFullscreen(bool fullscreen);
        void Quit();
    }
}
