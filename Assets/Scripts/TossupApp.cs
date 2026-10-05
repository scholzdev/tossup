using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tossup.UI;
using UnityEngine;

namespace Tossup
{
    // The Unity host: one camera, one component. It loads the assets from Resources, feeds input and time to
    // AppCore, plays audio, stores the save files and draws the canvas after the camera has cleared the screen.
    // Works in any scene: if the open scene has no TossupApp, one is created at startup.
    [RequireComponent(typeof(Camera))]
    public sealed class TossupApp : MonoBehaviour, IPlatform
    {
        const string FontPath = "fonts/m6x11plus";
        const int SfxVoices = 16;
        // m6x11plus metrics (unitsPerEm 1024, ascender 768, descender -256) at FreeType's rounding, as LÖVE uses them
        static int AscentFor(int size) => (int)Math.Ceiling(768.0 * size / 1024);
        static int HeightFor(int size) => (int)Math.Round(1024.0 * size / 1024);

        UnityGfxBackend backend;
        Font font;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> voices = new List<AudioSource>();
        int nextVoice;
        AudioSource music;
        Texture2D cursorArrow, cursorClick;
        string saveDir;
        Vector3 lastMouse;
        bool loaded;

        // -tossup-shots <folder>: capture the scripted screenshot tour and quit
        string shotsDir;
        bool shotMode;
        float shotMouseX = -50, shotMouseY = -50;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindObjectOfType<TossupApp>() != null) return;
            var go = new GameObject("Tossup");
            go.AddComponent<Camera>();
            go.AddComponent<TossupApp>();
        }

        void Awake()
        {
            var cam = GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(C.Felt.R, C.Felt.G, C.Felt.B, 1); // fills the bars around the 16:10 canvas
            cam.cullingMask = 0;
            cam.orthographic = true;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-tossup-shots") shotsDir = args[i + 1];
            shotMode = shotsDir != null;
            saveDir = shotMode ? Path.Combine(Application.temporaryCachePath, "shots-save") : Application.persistentDataPath;
            if (shotMode && Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
            Directory.CreateDirectory(saveDir);

            backend = new UnityGfxBackend();
            Gfx.Backend = backend;
            font = Resources.Load<Font>(FontPath);
            foreach (var name in Sound.Names)
            {
                var clip = Resources.Load<AudioClip>("sfx/" + name);
                if (clip != null) clips[name] = clip; // missing file: that sound just stays quiet
            }
            for (int i = 0; i < SfxVoices; i++)
            {
                var voice = gameObject.AddComponent<AudioSource>();
                voice.playOnAwake = false;
                voices.Add(voice);
            }
            music = gameObject.AddComponent<AudioSource>();
            music.clip = Resources.Load<AudioClip>("music/theme");
            music.loop = true;
            music.playOnAwake = false;
            cursorArrow = Resources.Load<Texture2D>("ui/cursor_arrow");
            cursorClick = Resources.Load<Texture2D>("ui/cursor_click");

            AppCore.Load(this);
            if (music.clip != null && !shotMode) music.Play();
            loaded = true;
            lastMouse = Input.mousePosition;
            if (shotMode) StartCoroutine(CaptureShots());
        }

        void Update()
        {
            if (!loaded || shotMode) return;
            if (Input.GetMouseButtonDown(0)) AppCore.MousePressed(MouseX, MouseY);
            if (Input.mousePosition != lastMouse)
            {
                lastMouse = Input.mousePosition;
                AppCore.MouseMoved(MouseX, MouseY);
            }
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)) AppCore.MouseReleased();
            if (Input.GetKeyDown(KeyCode.F3)) AppCore.KeyPressed("f3");
            if (Input.GetKeyDown(KeyCode.Escape)) AppCore.KeyPressed("escape");
            if (Input.GetKeyDown(KeyCode.Space)) AppCore.KeyPressed("space");
            if (Input.GetKeyDown(KeyCode.Return)) AppCore.KeyPressed("return");
            if (Input.GetKeyDown(KeyCode.LeftArrow)) AppCore.KeyPressed("left");
            if (Input.GetKeyDown(KeyCode.RightArrow)) AppCore.KeyPressed("right");
            for (int n = 1; n <= 9; n++)
                if (Input.GetKeyDown(KeyCode.Alpha0 + n)) AppCore.KeyPressed(n.ToString());
            AppCore.Update(UnityEngine.Time.unscaledDeltaTime);
        }

        void OnPostRender()
        {
            if (!loaded) return;
            backend.BeginFrame();
            try { AppCore.Draw(); }
            finally { backend.EndFrame(); }
        }

        IEnumerator CaptureShots()
        {
            Directory.CreateDirectory(shotsDir);
            Screen.SetResolution(1280, 800, FullScreenMode.Windowed);
            for (int k = 0; k < 10; k++) yield return null; // let the window settle
            foreach (var shot in Shots.Script())
            {
                shotMouseX = shot.MouseX;
                shotMouseY = shot.MouseY;
                shot.Setup();
                AppCore.Update(0);
                yield return null; // draw once so hover state and buttons exist
                yield return null;
                yield return new WaitForEndOfFrame();
                var image = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(shotsDir, shot.Name + ".png"), image.EncodeToPNG());
                Destroy(image);
            }
            File.WriteAllText(Path.Combine(shotsDir, "done.txt"), "ok " + Screen.width + "x" + Screen.height);
            Quit();
        }

        // ---- IPlatform

        public float MouseX => shotMode ? ShotToWindowX(shotMouseX) : Input.mousePosition.x;
        public float MouseY => shotMode ? ShotToWindowY(shotMouseY) : Screen.height - Input.mousePosition.y;
        public int WindowWidth => Screen.width;
        public int WindowHeight => Screen.height;
        public double Time => shotMode ? 1.0 : UnityEngine.Time.realtimeSinceStartupAsDouble;
        public long UnixTime => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public int Random(int low, int high) => UnityEngine.Random.Range(low, high + 1);

        float ShotToWindowX(float x)
        {
            Ui.Layout(out float scale, out float ox, out _);
            return ox + x * scale;
        }

        float ShotToWindowY(float y)
        {
            Ui.Layout(out float scale, out _, out float oy);
            return oy + y * scale;
        }

        public Img LoadImage(string path)
        {
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) throw new FileNotFoundException("missing image Resources/" + path);
            return new Img { Key = path, Width = texture.width, Height = texture.height, Native = texture };
        }

        public PixFont LoadFont(int size)
        {
            // the sizes the game uses are baked from LÖVE's own rasterizer (Tools/fontbake): identical text
            var atlas = Resources.Load<Texture2D>("fonts/baked/font_" + size);
            if (atlas != null)
            {
                var img = new Img { Key = "fonts/baked/font_" + size, Width = atlas.width, Height = atlas.height, Native = atlas };
                var baked = PixFont.Baked(LoadText("fonts/baked/metrics"), size, img);
                if (baked != null) return baked;
            }
            // any other size: Unity's font engine. Warm its atlas with every character the texts use, so it
            // rarely rebuilds mid-frame
            font.RequestCharactersInTexture(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ÄÖÜäöüß→", size);
            return new PixFont(size, AscentFor(size), HeightFor(size), (f, c) =>
            {
                font.RequestCharactersInTexture(c.ToString(), f.Size);
                return font.GetCharacterInfo(c, out CharacterInfo info, f.Size) ? info.advance : 0;
            }, font);
        }

        public string LoadText(string path) => Resources.Load<TextAsset>(path)?.text;

        public string ReadSave(string name)
        {
            string path = Path.Combine(saveDir, name);
            if (File.Exists(path)) return File.ReadAllText(path);
            return null;
        }

        public void WriteSave(string name, string text) => File.WriteAllText(Path.Combine(saveDir, name), text);
        public void AppendSave(string name, string text) => File.AppendAllText(Path.Combine(saveDir, name), text);

        public void PlaySound(string name, float pitch, float volume)
        {
            if (shotMode || !clips.TryGetValue(name, out var clip)) return;
            var voice = voices[nextVoice]; // a ring of voices, so quick events overlap instead of cutting each other off
            nextVoice = (nextVoice + 1) % voices.Count;
            voice.Stop();
            voice.clip = clip;
            voice.pitch = pitch;
            voice.volume = volume;
            voice.Play();
        }

        public void SetMusicVolume(float volume)
        {
            if (music != null) music.volume = volume;
        }

        public void SetCursor(string name)
        {
            var texture = name == "click" ? cursorClick : cursorArrow;
            if (texture != null) Cursor.SetCursor(texture, new Vector2(2, 2), CursorMode.Auto);
        }

        public void SetFullscreen(bool fullscreen)
        {
            if (Application.isEditor || shotMode) return;
            if (fullscreen)
            {
                if (Screen.fullScreenMode != FullScreenMode.FullScreenWindow)
                    Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
            }
            else if (Screen.fullScreenMode != FullScreenMode.Windowed)
                Screen.SetResolution(1280, 800, FullScreenMode.Windowed);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
