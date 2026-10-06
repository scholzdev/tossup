using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Tossup.UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tossup
{
    // The Unity host: one camera, one component. It loads the assets from Resources, feeds input and time to
    // AppCore, plays audio, stores the save files and draws the canvas after the camera has cleared the screen.
    // Works in any scene: if the open scene has no TossupApp, one is created at startup.
    [RequireComponent(typeof(Camera))]
    public sealed class TossupApp : MonoBehaviour, IPlatform
    {
        public const int DefaultWindowWidth = 1280;
        public const int DefaultWindowHeight = 800;
        static int WindowPixelScale => Application.platform == RuntimePlatform.OSXPlayer ? 2 : 1;
        static int DefaultPixelWidth => DefaultWindowWidth * WindowPixelScale;
        static int DefaultPixelHeight => DefaultWindowHeight * WindowPixelScale;
        static void SetDefaultWindowResolution() =>
            Screen.SetResolution(DefaultPixelWidth, DefaultPixelHeight, FullScreenMode.Windowed);
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
        string saveDir;
        Vector3 lastMouse;
        bool loaded;

        // -tossup-shots <folder>: capture the scripted screenshot tour and quit
        string shotsDir;
        bool shotMode;
        string sandboxScenePath;
        bool devMode;
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
            cam.backgroundColor = new Color(C.Felt.R, C.Felt.G, C.Felt.B, 1); // fills the bars around the design canvas
            cam.cullingMask = 0;
            cam.orthographic = true;
            cam.allowHDR = false;
            cam.allowMSAA = false;
            var cameraData = cam.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.requiresColorOption = CameraOverrideOption.Off;
            cameraData.requiresDepthOption = CameraOverrideOption.Off;
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;

            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-tossup-shots") shotsDir = args[i + 1];
            devMode = Environment.GetEnvironmentVariable("TOSSUP_DEV") == "1";
            sandboxScenePath = Environment.GetEnvironmentVariable("TOSSUP_SANDBOX_SCENE");
            for(int i=0;i<args.Length;i++)
            {
                if(args[i]=="-tossup-dev")devMode=true;
                if(args[i]=="-tossup-sandbox"&&i+1<args.Length)sandboxScenePath=args[++i];
            }
            bool sandboxMode=Environment.GetEnvironmentVariable("TOSSUP_SANDBOX")=="1"||!string.IsNullOrEmpty(sandboxScenePath);
            shotMode = shotsDir != null;
            saveDir = shotMode ? Path.Combine(Application.temporaryCachePath, "shots-save") : Application.persistentDataPath;
            if (shotMode && Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
            Directory.CreateDirectory(saveDir);

            if (!shotMode) devMode |= ReadSave(RuntimeMode.DeveloperModePreference) == "1";
            RuntimeMode.Configure(devMode,sandboxMode);
            if (!shotMode && !RuntimeMode.Dev && !RuntimeMode.Sandbox) ImportLegacySaves();
            Application.logMessageReceived += RecordCrash;
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

            AppCore.Load(this);
            if (sandboxMode && string.IsNullOrEmpty(sandboxScenePath))
            {
                Ui.SandboxConfig = SandboxConfig.Decode(Resources.Load<TextAsset>("sandbox/default").text);
                A.StartSandbox(Ui.SandboxConfig);
            }
            if(!string.IsNullOrEmpty(sandboxScenePath))
            {
                try
                {
                    if(Path.GetExtension(sandboxScenePath)!=".json")throw new FormatException("sandbox scene must be a JSON file");
                    Ui.SandboxConfig=SandboxConfig.Decode(File.ReadAllText(sandboxScenePath));A.StartSandbox(Ui.SandboxConfig);
                }
                catch(Exception ex){Debug.LogError("Could not load Tossup sandbox JSON: "+ex.Message);}
            }
            if (music.clip != null && !shotMode) music.Play();
            loaded = true;
            lastMouse = Input.mousePosition;
            if (shotMode) StartCoroutine(CaptureShots());
        }

        void ImportLegacySaves()
        {
            string configured = Environment.GetEnvironmentVariable("TOSSUP_LEGACY_SAVE_DIR");
            var candidates = new List<string>();
            if (!string.IsNullOrEmpty(configured)) candidates.Add(configured);
            string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
                candidates.Add(Path.Combine(userDirectory,"Library","Application Support","LOVE","tossup"));
            else if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"LOVE","tossup"));
            else
            {
                string dataDirectory = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                candidates.Add(Path.Combine(string.IsNullOrEmpty(dataDirectory) ? Path.Combine(userDirectory,".local","share") : dataDirectory,"love","tossup"));
            }
            foreach (string directory in candidates)
                if (Directory.Exists(directory))
                    try { if (LegacySave.ImportDirectory(directory,saveDir)) Debug.Log("Imported Lua progress from " + directory); }
                    catch (Exception error) { Debug.LogWarning("Lua save import failed: " + error.Message); }
        }

        void RecordCrash(string message, string trace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            try { File.AppendAllText(Path.Combine(saveDir,"crash.log"),DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " v" + BuildInfo.Number + "-" + BuildInfo.Build + "\n" + message + "\n" + trace + "\n\n"); }
            catch (IOException) { } // Logging must never replace the original exception.
            catch (UnauthorizedAccessException) { }
        }
        void OnDestroy()
        {
            Application.logMessageReceived -= RecordCrash;
            if (Gfx.Backend == backend) Gfx.Backend = null;
            backend?.Dispose();
        }

        void Update()
        {
            if (!loaded || shotMode) return;
            int pixelScale = WindowPixelScale;
            if (!Application.isEditor && Screen.fullScreenMode == FullScreenMode.Windowed &&
                (Screen.width < 640 * pixelScale || Screen.height < 400 * pixelScale))
                Screen.SetResolution(Math.Max(640 * pixelScale, Screen.width),
                    Math.Max(400 * pixelScale, Screen.height), FullScreenMode.Windowed);
            bool revealWasOpen = Ui.EncounterReveal != null;
            if (Input.GetMouseButtonDown(0)) AppCore.MousePressed(MouseX, MouseY);
            if (Input.mousePosition != lastMouse)
            {
                lastMouse = Input.mousePosition;
                AppCore.MouseMoved(MouseX, MouseY);
            }
            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2)) AppCore.MouseReleased();
            // Unity's anyKeyDown includes mouse buttons. The Start click may
            // create the reveal above; only an already-open reveal can consume it.
            if (revealWasOpen && Ui.EncounterReveal != null && Input.anyKeyDown) AppCore.KeyPressed("any");
            bool consumedRevealInput = revealWasOpen && Ui.EncounterReveal == null;
            if (!consumedRevealInput && Input.GetKeyDown(KeyCode.F3)) AppCore.KeyPressed("f3");
            if (!consumedRevealInput && Input.GetKeyDown(KeyCode.F5) && RuntimeMode.Sandbox)
            {
                try
                {
                    if(!string.IsNullOrEmpty(sandboxScenePath)&&Path.GetExtension(sandboxScenePath)!=".json")throw new FormatException("sandbox scene must be a JSON file");
                    string json=string.IsNullOrEmpty(sandboxScenePath)?Resources.Load<TextAsset>("sandbox/default").text:File.ReadAllText(sandboxScenePath);
                    Ui.SandboxConfig=SandboxConfig.Decode(json);A.StartSandbox(Ui.SandboxConfig);
                }
                catch(Exception ex){Debug.LogError("Could not reload Tossup sandbox JSON: "+ex.Message);}
            }
            if (!consumedRevealInput)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) AppCore.KeyPressed("escape");
                if (Input.GetKeyDown(KeyCode.Space)) AppCore.KeyPressed("space");
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) AppCore.KeyPressed("return");
                if (Input.GetKeyDown(KeyCode.UpArrow)) AppCore.KeyPressed("up");
                if (Input.GetKeyDown(KeyCode.DownArrow)) AppCore.KeyPressed("down");
                if (Input.GetKeyDown(KeyCode.LeftArrow)) AppCore.KeyPressed("left");
                if (Input.GetKeyDown(KeyCode.RightArrow)) AppCore.KeyPressed("right");
                if (Input.GetKeyDown(KeyCode.Q)) AppCore.KeyPressed("q");
                if (Input.GetKeyDown(KeyCode.E)) AppCore.KeyPressed("e");
                if (Input.GetKeyDown(KeyCode.I)) AppCore.KeyPressed("i");
                if (Input.GetKeyDown(KeyCode.O)) AppCore.KeyPressed("o");
                for (int n = 1; n <= 9; n++)
                    if (Input.GetKeyDown(KeyCode.Alpha0 + n)) AppCore.KeyPressed(n.ToString());

                if (Input.GetKeyDown(KeyCode.JoystickButton0)) PadNavigation.ControllerPressed(0);
                if (Input.GetKeyDown(KeyCode.JoystickButton1)) PadNavigation.ControllerPressed(1);
                if (Input.GetKeyDown(KeyCode.JoystickButton2)) PadNavigation.ControllerPressed(2);
                if (Input.GetKeyDown(KeyCode.JoystickButton3)) PadNavigation.ControllerPressed(3);
                if (Input.GetKeyDown(KeyCode.JoystickButton4)) PadNavigation.ControllerPressed(4);
                if (Input.GetKeyDown(KeyCode.JoystickButton5)) PadNavigation.ControllerPressed(5);
                if (Input.GetKeyDown(KeyCode.JoystickButton7)) PadNavigation.ControllerPressed(7);

                PadNavigation.Connected = Array.Exists(Input.GetJoystickNames(), name => !string.IsNullOrEmpty(name));
                PadNavigation.Update(UnityEngine.Time.unscaledDeltaTime,
                    Input.GetAxisRaw("Tossup Pad Horizontal"), Input.GetAxisRaw("Tossup Pad Vertical"),
                    Input.GetAxisRaw("Tossup Pad DPad Horizontal"), Input.GetAxisRaw("Tossup Pad DPad Vertical"),
                    Input.GetAxisRaw("Tossup Pad Left Trigger"), Input.GetAxisRaw("Tossup Pad Right Trigger"));
            }
            else PadNavigation.Update(UnityEngine.Time.unscaledDeltaTime, 0, 0, 0, 0, 0, 0);
            AppCore.Update(UnityEngine.Time.unscaledDeltaTime);
        }

        internal UnityGfxBackend PrepareCanvas()
        {
            if (!loaded) return null;
            backend.BeginFrame();
            try { AppCore.Draw(); }
            finally { backend.EndFrame(); }
            return backend;
        }

        IEnumerator CaptureShots()
        {
            Directory.CreateDirectory(shotsDir);
            SetDefaultWindowResolution();
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
        public void DeleteSave(string name)
        {
            string path = Path.Combine(saveDir, name);
            if (File.Exists(path)) File.Delete(path);
        }

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
            // The platform pointer stays compact over small chips and run modifiers.
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
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
                SetDefaultWindowResolution();
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
