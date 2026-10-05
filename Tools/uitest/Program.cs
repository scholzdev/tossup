// Headless UI test: runs the real UI code (AppCore, views, actions) without Unity. It plays the screenshot
// tour, then a long "monkey" session that clicks random buttons, presses random keys and hovers random spots,
// and fails on the first exception or on a non-finite coordinate.
// Usage: dotnet run -c Release -- [frames] [seed]
using System;
using System.Collections.Generic;
using System.IO;
using Tossup;
using Tossup.UI;

sealed class HeadlessBackend : IGfxBackend
{
    public long Primitives;

    static void Check(float v)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) throw new InvalidOperationException("non-finite coordinate");
    }

    public void Triangles(List<float> xy, Rgba color)
    {
        if (xy.Count % 6 != 0) throw new InvalidOperationException("triangle list is not a multiple of 3 points");
        foreach (var v in xy) Check(v);
        Primitives++;
    }

    public void Image(Img image, float x0, float y0, float x1, float y1, Rgba tint)
    {
        if (image == null) throw new InvalidOperationException("null image");
        Check(x0); Check(y0); Check(x1); Check(y1);
        Primitives++;
    }

    public void Text(PixFont font, string text, float x, float y, float sx, float sy, Rgba color)
    {
        if (font == null) throw new InvalidOperationException("no font set");
        Check(x); Check(y);
        foreach (char c in text)
            if (c != ' ' && font.Advance(c) == 0) throw new InvalidOperationException($"glyph missing from the font: '{c}' (U+{(int)c:X4}) in \"{text}\"");
        Primitives++;
    }
}

sealed class HeadlessPlatform : IPlatform
{
    readonly string resources, saveDir;
    readonly Dictionary<int, int> advances = new Dictionary<int, int>();
    readonly Random random = new Random(1);
    public float X = -50, Y = -50;
    public double Clock;
    public int Quits, Sounds;

    public HeadlessPlatform(string root)
    {
        resources = Path.Combine(root, "Assets", "Resources");
        saveDir = Path.Combine(Path.GetTempPath(), "tossup-uitest");
        if (Directory.Exists(saveDir)) Directory.Delete(saveDir, true);
        Directory.CreateDirectory(saveDir);
        foreach (var line in File.ReadAllLines(Path.Combine(root, "Tools", "uitest", "advances.txt")))
        {
            var parts = line.Split(' ');
            advances[int.Parse(parts[0])] = int.Parse(parts[1]);
        }
    }

    public float MouseX => X;
    public float MouseY => Y;
    public int WindowWidth => 1280;
    public int WindowHeight => 800;
    public double Time => Clock;
    public long UnixTime => 1_700_000_000;
    public int Random(int low, int high) => random.Next(low, high + 1);

    public Img LoadImage(string path)
    {
        string file = Path.Combine(resources, path + ".png");
        using var stream = File.OpenRead(file);
        var header = new byte[24];
        stream.Read(header, 0, 24);
        int w = header[16] << 24 | header[17] << 16 | header[18] << 8 | header[19];
        int h = header[20] << 24 | header[21] << 16 | header[22] << 8 | header[23];
        return new Img { Key = path, Width = w, Height = h };
    }

    public PixFont LoadFont(int size)
    {
        var baked = PixFont.Baked(LoadText("fonts/baked/metrics"), size, LoadImage("fonts/baked/font_" + size));
        if (baked != null) return baked;
        return new PixFont(size, (int)Math.Ceiling(768.0 * size / 1024), size,
            (f, c) => advances.TryGetValue(c, out int a) ? (int)Math.Round(a * (double)f.Size / 1024) : 0);
    }

    public string LoadText(string path)
    {
        string json = Path.Combine(resources, path + ".json");
        return File.ReadAllText(File.Exists(json) ? json : Path.Combine(resources, path + ".txt"));
    }
    public string ReadSave(string name) => File.Exists(Path.Combine(saveDir, name)) ? File.ReadAllText(Path.Combine(saveDir, name)) : null;
    public void WriteSave(string name, string text) => File.WriteAllText(Path.Combine(saveDir, name), text);
    public void AppendSave(string name, string text) => File.AppendAllText(Path.Combine(saveDir, name), text);
    public void PlaySound(string name, float pitch, float volume)
    {
        if (Array.IndexOf(Sound.Names, name) < 0) throw new InvalidOperationException("unknown sound " + name);
        Sounds++;
    }
    public void SetMusicVolume(float volume) { }
    public void SetCursor(string name) { }
    public void SetFullscreen(bool fullscreen) { }
    public void Quit() => Quits++;
}

static class Program
{
    static int Main(string[] args)
    {
        int frames = args.Length > 0 ? int.Parse(args[0]) : 200000;
        int seed = args.Length > 1 ? int.Parse(args[1]) : 7;
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var platform = new HeadlessPlatform(root);
        var backend = new HeadlessBackend();
        Gfx.Backend = backend;
        AppCore.Load(platform);
        LatestTests.Run();

        // 1. the screenshot tour
        foreach (var shot in Shots.Script())
        {
            platform.X = shot.MouseX;
            platform.Y = shot.MouseY;
            shot.Setup();
            AppCore.Update(0);
            AppCore.Draw();
            AppCore.Draw();
        }
        Console.WriteLine($"tour: {Shots.Script().Count} shots drawn");

        // 2. monkey session
        var rng = new Random(seed);
        string[] keys = { "space", "space", "space", "return", "escape", "left", "right", "1", "2", "3", "f3" };
        var stats = new Dictionary<string, int>();
        int runs = 0, shops = 0, wins = 0, losses = 0;
        Phase? lastPhase = null;
        GameState lastGame = null;
        A.Go("title");
        Ui.Game = null;
        for (int frame = 0; frame < frames; frame++)
        {
            try
            {
                double dt = rng.NextDouble() * .12;
                platform.Clock += dt;
                AppCore.Update(dt);
                AppCore.Draw();
                if (Ui.Game != lastGame) { runs++; lastGame = Ui.Game; }
                if (Ui.Game != null && Ui.Game.Phase != lastPhase)
                {
                    if (Ui.Game.Phase == Phase.Shop) shops++;
                    if (Ui.Game.Phase == Phase.Victory) wins++;
                    if (Ui.Game.Phase == Phase.GameOver) losses++;
                    lastPhase = Ui.Game.Phase;
                }
                int r = rng.Next(100);
                if (r < 55 && Ui.Buttons.Count > 0)
                {
                    // prefer buttons that move the game on, but click anything
                    var b = Ui.Buttons[rng.Next(Ui.Buttons.Count)];
                    platform.X = b.X + b.W / 2;
                    platform.Y = b.Y + b.H / 2;
                    AppCore.Draw(); // hover frame, as a real mouse would produce
                    platform.X = b.X + b.W / 2;
                    platform.Y = b.Y + b.H / 2;
                    AppCore.MousePressed(platform.X, platform.Y);
                    if (Ui.Dragging != null)
                    {
                        AppCore.MouseMoved(platform.X + rng.Next(-200, 200), platform.Y);
                        AppCore.MouseReleased();
                    }
                    stats["click"] = stats.GetValueOrDefault("click") + 1;
                }
                else if (r < 70)
                {
                    string key = keys[rng.Next(keys.Length)];
                    AppCore.KeyPressed(key);
                    stats["key"] = stats.GetValueOrDefault("key") + 1;
                }
                else if (r < 85)
                {
                    platform.X = rng.Next(-20, 1300);
                    platform.Y = rng.Next(-20, 820);
                }
                // keep runs flowing: leave the title/help screens quickly, sometimes switch language
                if (rng.Next(4000) == 0) A.CycleLanguage();
                if (Ui.Game == null && Ui.Screen == "title" && rng.Next(3) == 0) A.Play();
                if (Ui.Screen == "select" && (Ui.Game == null || Ui.Game.Paused) && rng.Next(3) == 0) A.Start();
                if (Ui.Game != null && Ui.Game.Paused && rng.Next(4) == 0) Ui.Game.Paused = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAILED at frame {frame}: {ex}");
                Console.WriteLine($"screen={Ui.Screen} phase={Ui.Game?.Phase} seed={Ui.Game?.Seed}");
                return 1;
            }
        }
        Console.WriteLine($"monkey: {frames} frames, {runs} runs, {shops} shop visits, {wins} wins, {losses} losses, " +
            $"{stats.GetValueOrDefault("click")} clicks, {stats.GetValueOrDefault("key")} keys, {platform.Sounds} sounds, " +
            $"{platform.Quits} quit requests, {backend.Primitives} primitives");

        // 3. a player: clicks the buttons a person would, so runs go deep (shops, the boss, endless levels)
        return Player(platform, frames, seed) ? 0 : 1;
    }

    static Button Find(string label)
    {
        for (int i = Ui.Buttons.Count - 1; i >= 0; i--)
            if (Ui.Buttons[i].Label != null && Ui.Buttons[i].Label.StartsWith(label)) return Ui.Buttons[i];
        return null;
    }

    static List<Button> All(string label) => Ui.Buttons.FindAll(b => b.Label != null && b.Label.StartsWith(label));

    static bool Click(HeadlessPlatform platform, Button b)
    {
        if (b == null) return false;
        platform.X = b.X + b.W / 2;
        platform.Y = b.Y + b.H / 2;
        AppCore.MousePressed(platform.X, platform.Y);
        AppCore.MouseReleased();
        return true;
    }

    static bool Player(HeadlessPlatform platform, int frames, int seed)
    {
        var rng = new Random(seed * 31 + 1);
        Lang.Set("en");
        Ui.Game = null;
        Ui.Confirm = null;
        Ui.SelectedCharacter = "blade";
        A.Go("title");
        int runs = 0, shops = 0, wins = 0, losses = 0, endless = 0, items = 0, bought = 0, exchanges = 0, maxLevel = 0;
        Phase? lastPhase = null;
        for (int frame = 0; frame < frames; frame++)
        {
            try
            {
                platform.Clock += .1;
                AppCore.Update(.1);
                AppCore.Draw();
                var g = Ui.Game;
                if (g != null && g.Phase != lastPhase)
                {
                    if (g.Phase == Phase.Shop) shops++;
                    if (g.Phase == Phase.Victory) wins++;
                    if (g.Phase == Phase.GameOver) losses++;
                    lastPhase = g.Phase;
                }
                if (g != null) maxLevel = Math.Max(maxLevel, g.EncounterIndex);
                if (Environment.GetEnvironmentVariable("UITEST_TRACE") != null && frame % 5000 == 0)
                    Console.WriteLine($"[{frame}] screen={Ui.Screen} phase={g?.Phase} paused={g?.Paused} level={g?.EncounterIndex} flips={g?.Encounter?.Flips} left={(g?.Encounter!=null?Game.CoinsLeft(g):-1)} quota={g?.Encounter?.Quota} cleared={g?.Encounter?.Cleared} mull={g?.Mulligan != null} dealt={g?.Dealt != null} " +
                        $"pending={g?.Pending != null} hold={Ui.Holding} anim={Ui.FlipAnimation != null} timer={Ui.ResolveTimer:F2} buttons=" +
                        string.Join("|", Ui.Buttons.ConvertAll(b => b.Label ?? "?")));
                if (Ui.Confirm != null) { Click(platform, Find("CANCEL")); continue; }
                if (g == null || g.Paused)
                {
                    if (Ui.Screen == "help") Click(platform, Find("GOT IT") ?? Find("BACK"));
                    else if (Ui.Screen == "select") { Click(platform, Find("START RUN")); runs++; lastPhase = null; }
                    else if (Ui.Screen == "title") Click(platform, Find("NEW RUN") ?? Find("PLAY"));
                    else Click(platform, Find("BACK"));
                    continue;
                }
                if (g.Phase == Phase.Encounter)
                {
                    if (g.Mulligan != null)
                    {
                        if (rng.Next(4) == 0) Click(platform, All("CARD").Count > 0 ? All("CARD")[rng.Next(All("CARD").Count)] : null);
                        else if (rng.Next(3) == 0 && Click(platform, Find("DISCARD"))) { }
                        else Click(platform, Find("START LEVEL"));
                        continue;
                    }
                    if (rng.Next(6) == 0 && Click(platform, Find("OPEN SHOP"))) continue;
                    if (rng.Next(12) == 0 && Click(platform, Find("ITEM"))) { items++; continue; }
                    Button bankCombo=All("BANK ").Find(b=>b.Label!="BANK COIN");
                    if (bankCombo != null || Find("PUSH") != null)
                    {
                        if (rng.Next(3) == 0) Click(platform, bankCombo); else Click(platform, Find("PUSH"));
                        continue;
                    }
                    if (Click(platform, Find("FLIP")) || Click(platform, Find("NEXT COIN"))) continue;
                    if (rng.Next(3) == 0 && Click(platform, Find("DISCARD"))) continue;
                    var pay = Find("PAY") ?? Find("BUY MORE COINS");
                    if (pay != null && rng.Next(4) > 0) { Click(platform, pay); exchanges++; continue; }
                    if (Find("START AGAIN") != null) { Click(platform, Find("START AGAIN")); runs++; lastPhase = null; continue; }
                    Click(platform, Find("DISCARD"));
                    continue;
                }
                if (g.Phase == Phase.Shop)
                {
                    int r = rng.Next(10);
                    if (r < 3 && Click(platform, All("BUY").Count > 0 ? All("BUY")[rng.Next(All("BUY").Count)] : null)) bought++;
                    else if (r == 3) Click(platform, Find("REROLL"));
                    else if (r == 4) { var coins = All("COIN"); if (coins.Count > 0) Click(platform, coins[rng.Next(coins.Count)]); Click(platform, Find("UPGRADE")); }
                    else if (r == 5 && rng.Next(3) == 0) { var coins = All("COIN"); if (coins.Count > 0) Click(platform, coins[rng.Next(coins.Count)]); Click(platform, Find("REMOVE")); }
                    else if (r >= 7) Click(platform, Find("NEXT ROUND"));
                    continue;
                }
                if (g.Phase == Phase.Contract)
                {
                    Click(platform, All("TAKE CONTRACT").Count > 0 ? All("TAKE CONTRACT")[rng.Next(All("TAKE CONTRACT").Count)] : Find("SKIP CONTRACT"));
                    continue;
                }
                if (g.Phase == Phase.Augment)
                {
                    if (!Click(platform, All("CHOOSE AUGMENT").Count > 0 ? All("CHOOSE AUGMENT")[rng.Next(All("CHOOSE AUGMENT").Count)] : null))
                        Click(platform, All("CHOOSE").Count > 0 ? All("CHOOSE")[rng.Next(All("CHOOSE").Count)] : null);
                    continue;
                }
                // finish screen
                if (g.Phase == Phase.Victory && Find("ENDLESS MODE") != null && rng.Next(2) == 0) { Click(platform, Find("ENDLESS MODE")); endless++; }
                else { Click(platform, Find("NEW RUN")); runs++; lastPhase = null; }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PLAYER FAILED at frame {frame}: {ex}");
                Console.WriteLine($"screen={Ui.Screen} phase={Ui.Game?.Phase} seed={Ui.Game?.Seed}");
                return false;
            }
        }
        Console.WriteLine($"player: {frames} frames, {runs} runs, {shops} shop visits, {wins} boss wins, {endless} into endless, " +
            $"{losses} losses, deepest level {maxLevel}, {items} chips used, {bought} shop buys, {exchanges} exchanges");
        return true;
    }
}
