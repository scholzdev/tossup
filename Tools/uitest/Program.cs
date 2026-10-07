// Headless UI test: runs the real UI code (AppCore, views, actions) without Unity. It plays the screenshot
// tour, then a long "monkey" session that clicks random buttons, presses random keys and hovers random spots,
// and fails on the first exception or on a non-finite coordinate.
// Usage: dotnet run -c Release -- [frames] [seed]
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tossup;
using Tossup.UI;

sealed class HeadlessBackend : IGfxBackend
{
    public long Primitives;
    public bool Capture;
    public readonly Dictionary<string, float[]> Images = new Dictionary<string, float[]>();
    public readonly List<(string Value, float X, float Y, float Width, float Height)> Texts =
        new List<(string, float, float, float, float)>();

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

    public void Image(Img image, float[] quad, Rgba tint)
    {
        if (image == null || quad.Length != 8) throw new InvalidOperationException("invalid image quad");
        foreach (float v in quad) Check(v);
        if (Capture) Images[image.Key] = (float[])quad.Clone();
        Primitives++;
    }
    public void Clip(float x, float y, float w, float h) { Check(x); Check(y); Check(w); Check(h); }

    public void Text(PixFont font, string text, float x, float y, float sx, float sy, Rgba color)
    {
        if (font == null) throw new InvalidOperationException("no font set");
        Check(x); Check(y);
        foreach (char c in text)
            if (c != ' ' && font.Advance(c) == 0) throw new InvalidOperationException($"glyph missing from the font: '{c}' (U+{(int)c:X4}) in \"{text}\"");
        if (Capture) Texts.Add((text, x, y, font.GetWidth(text) * sx, font.Height * sy));
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
    public bool Fullscreen;
    public float MusicVolume;

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
    public int WindowWidth { get; set; } = 1280;
    public int WindowHeight { get; set; } = 800;
    public double Time => Clock;
    public long UnixTime => 1_700_000_000;
    public int Random(int low, int high) => random.Next(low, high + 1);

    public Img LoadImage(string path)
    {
        string file = Path.Combine(resources, path + ".png");
        using var stream = File.OpenRead(file);
        var header = new byte[24];
        stream.ReadExactly(header);
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
    public void DeleteSave(string name) { string path = Path.Combine(saveDir, name); if (File.Exists(path)) File.Delete(path); }
    public void PlaySound(string name, float pitch, float volume)
    {
        if (Array.IndexOf(Sound.Names, name) < 0) throw new InvalidOperationException("unknown sound " + name);
        Sounds++;
    }
    public void SetMusicVolume(float volume) => MusicVolume = volume;
    public void SetCursor(string name) { }
    public void SetFullscreen(bool fullscreen) => Fullscreen = fullscreen;
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
        if (Environment.GetEnvironmentVariable("UITEST_SVG") is string svgFolder) { SvgShots.Run(platform, svgFolder, root); return 0; }
        LatestTests.Run();
        AuditRegressionTests.Run();
        LayoutProof(platform, backend);

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
        A.Go(UiScreen.Title);
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
                    platform.X = rng.Next(-20, platform.WindowWidth + 20);
                    platform.Y = rng.Next(-20, 820);
                }
                // keep runs flowing: leave the title/help screens quickly, sometimes switch language
                if (rng.Next(4000) == 0) A.CycleLanguage();
                if (Ui.Game == null && Ui.Screen == UiScreen.Title && rng.Next(3) == 0) A.Play();
                if (Ui.Screen == UiScreen.Select && (Ui.Game == null || Ui.Game.Paused) && rng.Next(3) == 0) A.Start();
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

    static void LayoutProof(HeadlessPlatform platform, HeadlessBackend backend)
    {
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("UI layout: " + message);
        }
        void Capture()
        {
            backend.Images.Clear(); backend.Texts.Clear(); AppCore.Draw();
        }
        void ClickCanvas(Button button)
        {
            Ui.Layout(out float scale, out float ox, out float oy);
            platform.X = ox + (button.X + button.W / 2) * scale;
            platform.Y = oy + (button.Y + button.H / 2) * scale;
            AppCore.MousePressed(platform.X, platform.Y);
        }
        backend.Capture = true;
        foreach (var size in new[] { (1620,800), (1280,800), (640,400), (1920,1080) })
        {
            platform.WindowWidth = size.Item1; platform.WindowHeight = size.Item2;
            Ui.Game = null; Ui.Confirm = null; Ui.Tutorial = null; Ui.EncounterReveal = null;
            Lang.Set("en"); A.Go(UiScreen.Title); Capture();
            Ui.Layout(out float scale, out float ox, out float oy);
            if (size == (1280,800)) Check(scale == 1 && ox == 0 && oy == 0, "the Lua-resolution view fills the full window");
            var quad = backend.Images["ui/title_scene"];
            Check(quad[0] <= 0 && quad[1] <= 0 && quad[4] >= Ui.Width && quad[5] >= Ui.Height,
                "title artwork covers the canvas without side bars");
            var scene = Ui.UiImages["title_scene"];
            Check(Math.Abs((quad[4]-quad[0])/scene.Width-(quad[5]-quad[1])/scene.Height) < .001,
                "title artwork keeps its aspect ratio");
            var menu = Ui.Buttons.Find(b => b.Label == "COLLECTION");
            Check(menu.X == 100 && menu.W == 340 && menu.H == 52,
                "main menu controls match the Lua-sized layout");
            ClickCanvas(menu); Check(Ui.Screen == UiScreen.Collection, "resized pointer activates the visible control");
            Ui.ToCanvas(platform.X, platform.Y, out float cx, out float cy);
            Check(menu.Contains(cx,cy), "drawing and pointer transforms agree after resizing");
        }
        platform.WindowWidth = 1280; platform.WindowHeight = 800;
        Ui.Game = null;
        A.OpenSets("trader"); Capture();
        int traderCoins = Content.Characters["trader"].Pool.Count + Content.Characters["trader"].Locked.Count;
        Check(traderCoins > 32, "the trader exercises catalog paging");
        var firstPageCoins = Ui.Regions.FindAll(r => r.Coin != null && r.X >= 520 && r.X < 1220);
        Check(firstPageCoins.Count == 32 && firstPageCoins.TrueForAll(r => r.Y + r.H < 690),
            "the first catalog page fits every coin above its paging controls");
        var nextCatalogPage = Ui.Buttons.Find(b => b.Label == ">" && b.X > 1100 && b.Y > 680);
        Check(nextCatalogPage != null && !nextCatalogPage.Disabled, "overflow coins have a next-page control");
        ClickCanvas(nextCatalogPage); Capture();
        var secondPageCoins = Ui.Regions.FindAll(r => r.Coin != null && r.X >= 520 && r.X < 1220);
        Check(Ui.SetsCatalogPage == 2 && secondPageCoins.Count == Math.Min(32, traderCoins - 32),
            "the remaining character coins are reachable on the second page");
        Ui.Game = Game.NewSandbox(new SandboxConfig { Coins = new List<string> {"dagger","normal"}, Seed = 6, Energy = 99 });
        Ui.FlipAnimation = null; Ui.ResolveTimer = 0;
        A.FlipCoin(Ui.Game.Encounter.Hand[0]); Ui.Game.Pending.Result = Side.Tails; Ui.FlipAnimation.Outcome = Side.Tails;
        string flippedId = Game.GetCoin(Ui.Game, Ui.Game.Pending.Uid).Id;
        A.Update(Ui.FlipAnimation.Duration); Capture();
        Check(Ui.Deciding && backend.Texts.Exists(t => t.Value == "RE-FLIP (1 ENERGY)") && backend.Texts.Exists(t => t.Value == "KEEP"), "an affordable re-flip pauses on the landed result");
        A.Update(2); Check(Ui.Game.Pending != null, "the result does not auto-resolve while a re-flip is offered");
        A.Reflip(); Check(Ui.FlipAnimation != null && Ui.Game.Pending.Rerolled && !Ui.Deciding, "Re-flip replays the landing once");
        Ui.Game.Pending.Result = Side.Tails; Ui.FlipAnimation.Outcome = Side.Tails;
        A.Update(Ui.FlipAnimation.Duration); Capture(); // the one re-flip is spent, so it resolves by itself again
        var applying = backend.Texts.Find(t => t.Value == "APPLYING...");
        var landed = backend.Texts.FindLast(t => t.Value == "TAILS");
        var coinQuad = backend.Images["coins/" + flippedId];
        Check(applying.Value != null && Math.Abs(applying.X+applying.Width/2-(coinQuad[0]+coinQuad[4])/2) <= 1,
            "applying feedback is centered under the landed coin");
        Check(landed.Y+landed.Height <= applying.Y, "the outcome and applying feedback do not overlap");
        A.Update(.91); Capture();
        Check(Ui.Game.Pending == null && !backend.Texts.Exists(t => t.Value == "APPLYING..."),
            "applying feedback gives way to the resolved result");
        Ui.Shake = 0;
        // the hand: one clickable card per coin, inside the hand panel, for any hand size
        Ui.Game = Game.NewSandbox(new SandboxConfig { Coins = new List<string> {"normal","dagger","sword"}, Seed = 6 }); Capture();
        const float cardsY = EncounterView.CardsY - 1;
        var handTarget = Ui.Regions.Find(r => r.Coin?.Id == "dagger" && r.Y >= cardsY);
        var handIcon = backend.Images["coins/dagger"];
        Check(handTarget != null && handTarget.H >= 150 && handTarget.W >= 200 && handIcon[4]-handIcon[0] >= 48, "hand cards are large with readable coin icons");
        for (int count = 1; count <= 12; count++)
        {
            var deck = new List<string>(); for (int i=0;i<12;i++) deck.Add("normal");
            Ui.Game = Game.NewSandbox(new SandboxConfig { Coins = deck, Seed = 6 });
            var e = Ui.Game.Encounter;
            if (e.Hand.Count > count) { e.Pouch.InsertRange(0, e.Hand.GetRange(count, e.Hand.Count - count)); e.Hand.RemoveRange(count, e.Hand.Count - count); }
            else Game.DrawCoins(Ui.Game, count - e.Hand.Count);
            Capture();
            var cards = Ui.Regions.FindAll(r => r.Coin != null && r.Y >= cardsY);
            Check(cards.Count == count && cards.TrueForAll(r => r.X >= EncounterView.InnerX && r.X + r.W <= EncounterView.InnerRight + .5f && r.W >= 60 && r.H >= Ui.F20.Height + 4 &&
                r.Y + r.H <= EncounterView.FooterY),
                "every hand coin fits its panel for hand size " + count);
            cards.Sort((a, b) => a.X.CompareTo(b.X));
            for (int i = 1; i < cards.Count; i++) Check(cards[i].X >= cards[i-1].X + cards[i-1].W, "hand cards do not overlap for hand size " + count);
            Check(Ui.Buttons.FindAll(b => b.Label == "HAND COIN").Count == count && Ui.Buttons.Exists(b => b.Label == "END ROUND"), "each hand coin and the round button can be clicked");
        }
        Ui.Game.Encounter.BankDiscards = 1; Ui.DiscardMode = true;
        for (int i=0;i<6;i++) Game.AddBuff(Ui.Game,"mult",2,2,true);
        Capture();
        var discardToggle = Ui.Buttons.Find(b => b.Label == "DISCARD MODE: ON");
        Check(discardToggle != null, "the discard toggle shows while a discard is armed");
        foreach (var text in backend.Texts.FindAll(t => t.Y >= EncounterView.StatusY - 2 && t.Y < EncounterView.StatusY + 26 && t.X < discardToggle.X))
            Check(text.X >= EncounterView.InnerX && text.X + text.Width <= discardToggle.X, "buffs stay on their line, left of the discard toggle");
        Check(backend.Texts.Exists(t => t.Value.StartsWith("BUFF x2")), "active buffs are listed");
        Ui.DiscardMode = false;
        Ui.Game = Game.NewSandbox(new SandboxConfig { Coins = new List<string>{"normal","normal","normal"}, Seed = 6 });
        Capture();
        foreach (int uid in new List<int>(Ui.Game.Encounter.Hand)) { Game.Flip(Ui.Game, uid); Game.Resolve(Ui.Game); }
        Capture();
        Check(Ui.Regions.FindAll(r => r.Coin != null && r.Y >= cardsY).Count == 0 && Ui.Buttons.FindAll(b => b.Label == "HAND COIN").Count == 0 &&
            backend.Texts.Exists(t => t.Value == "NO COINS IN HAND  -  END THE ROUND") && Ui.Buttons.Exists(b => b.Label == "END ROUND"),
            "an empty hand shows a compact empty state and the round can still end");
        Check(Ui.Game.Encounter.RoundLog.Count == 3 && backend.Texts.FindAll(t => t.Value == "HEADS" || t.Value == "TAILS").Count == 3,
            "flipped coins keep their cards, showing the side they landed on");
        // the scoreboard: played rounds show their points, the current round is marked, totals follow
        var board = Game.NewSandbox(new SandboxConfig { Coins = new List<string>{"normal","normal","normal","normal","normal","normal"}, Seed = 6 });
        Ui.Game = board;
        var be = board.Encounter;
        be.EnemyId = "pickpocket"; be.EnemyDraw = 4; be.EnemyRoundPoints = 1; be.EnemyPouch = new List<string>(EnemyCatalog.ById["pickpocket"].Pouch);
        A.FlipCoin(be.Hand[0]); A.Update(5); if (Ui.Deciding) A.Keep(); A.Update(5); A.EndRound();
        Capture();
        Check(be.Round == 2 && be.RoundScores.Count == 1 && be.EnemyRoundScores.Count == 1 &&
            Math.Abs(be.RoundScores[0] - be.Scored) < 1e-9 && Math.Abs(be.EnemyRoundScores[0] - be.EnemyScore) < 1e-9 &&
            Math.Abs(be.EnemyScore - (be.EnemyFlips.Sum(f => f.Points) + be.EnemyRoundPoints)) < 1e-9,
            "the model keeps per-round points that add up to the totals");
        Check(backend.Texts.Exists(t => t.Value == GameText.Num(be.EnemyRoundScores[0])) && backend.Texts.Exists(t => t.Value == "R5") &&
            backend.Texts.Exists(t => t.Value == "ROUND 2 / 5") && backend.Texts.Exists(t => t.Value == "TOTAL") && backend.Texts.Exists(t => t.Value == "LAST FLIPS  -  ROUND 1"),
            "the scoreboard lists the finished round and the enemy's revealed flips");
        Check(RunSave.Decode(RunSave.Encode(board)) is GameState saved && saved.Encounter.RoundScores.Count == 1 &&
            saved.Encounter.EnemyRoundScores[0] == be.EnemyRoundScores[0] && saved.Encounter.RoundStartScored == be.RoundStartScored, "per-round points survive a save");
        Ui.Game = null;
        backend.Capture = false;
        foreach (var shot in Shots.Script())
        {
            shot.Setup(); AppCore.Update(0); AppCore.Draw();
            foreach (var button in Ui.Buttons)
                Check(button.X >= 0 && button.Y >= 0 && button.X+button.W <= Ui.Width+1 && button.Y+button.H <= Ui.Height+1,
                    shot.Name + " has a clickable outside the canvas: " + button.Label);
        }
        Ui.Game = null; Ui.Confirm = null; Ui.EncounterReveal = null; Ui.FlipAnimation = null;
        Ui.ResolveTimer = 0; PadNavigation.MouseUsed(); Lang.Set("en");
        Console.WriteLine("layout: full-width art, Lua-sized menu, resized input, result feedback and tour bounds passed");
    }

    static Button Find(string label)
    {
        for (int i = Ui.Buttons.Count - 1; i >= 0; i--)
            if (Ui.Buttons[i].Label != null && Ui.Buttons[i].Label.StartsWith(label)) return Ui.Buttons[i];
        return null;
    }

    static List<Button> All(string label) => Ui.Buttons.FindAll(b => b.Label != null && b.Label.StartsWith(label));

    static readonly string[] MapLabels = { "TABLE", "ELITE", "SHOP", "MINT", "ALTAR", "BACK ROOM", "THE HOUSE", "REMOVE SELECTED COIN",
        "+1 MAX ENERGY", "+8 GOLD", "+15 GOLD", "A RANDOM CHIP", "NEXT ENEMY -5 POINTS" };

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
        Ui.Profile.Options.SeenHelp = true;
        A.DeleteRun();
        Ui.Game = null;
        Ui.Confirm = null;
        Ui.SelectedCharacter = Content.Characters["blade"];
        A.Go(UiScreen.Title);
        int runs = 0, shops = 0, wins = 0, losses = 0, endless = 0, items = 0, bought = 0, rounds = 0, maxLevel = 0;
        Phase? lastPhase = null;
        for (int frame = 0; frame < frames; frame++)
        {
            try
            {
                platform.Clock += .1;
                AppCore.Update(.1);
                AppCore.Draw();
                var g = Ui.Game;
                if (Ui.EncounterReveal != null) { AppCore.KeyPressed("return"); continue; }
                if (g != null && g.Phase != lastPhase)
                {
                    if (g.Phase == Phase.Shop) shops++;
                    if (g.Phase == Phase.Victory) wins++;
                    if (g.Phase == Phase.GameOver) losses++;
                    lastPhase = g.Phase;
                }
                if (g != null) maxLevel = Math.Max(maxLevel, g.EncounterIndex);
                if (Environment.GetEnvironmentVariable("UITEST_TRACE") != null && frame % 5000 == 0)
                    Console.WriteLine($"[{frame}] screen={Ui.Screen} phase={g?.Phase} paused={g?.Paused} level={g?.EncounterIndex} flips={g?.Encounter?.Flips} round={g?.Encounter?.Round} hand={g?.Encounter?.Hand.Count} score={g?.Encounter?.Scored}:{g?.Encounter?.EnemyScore} cleared={g?.Encounter?.Cleared} " +
                        $"pending={g?.Pending != null} anim={Ui.FlipAnimation != null} timer={Ui.ResolveTimer:F2} buttons=" +
                        string.Join("|", Ui.Buttons.ConvertAll(b => b.Label ?? "?")));
                if (Ui.Confirm != null) { Click(platform, Find("CANCEL") ?? Find("OK")); continue; }
                if (g == null || g.Paused)
                {
                    if (Ui.Screen == UiScreen.Help) Click(platform, Find("GOT IT") ?? Find("BACK"));
                    else if (Ui.Screen == UiScreen.Select) { Click(platform, Find("START RUN")); runs++; lastPhase = null; }
                    else if (Ui.Screen == UiScreen.Title) Click(platform, Find("NEW RUN") ?? Find("PLAY"));
                    else Click(platform, Find("BACK"));
                    continue;
                }
                if (g.Phase == Phase.Encounter)
                {
                    if (g.Encounter.Cleared || rng.Next(40) == 0 && g.Encounter.Cleared)
                    {
                        Click(platform, Find("OPEN SHOP") ?? Find("CONTINUE"));
                        continue;
                    }
                    if (rng.Next(12) == 0 && Click(platform, Find("ITEM"))) { items++; continue; }
                    Button bankCombo = All("BANK ").Find(b => b.Label != "BANK COIN");
                    if (bankCombo != null && rng.Next(4) == 0) { Click(platform, bankCombo); continue; }
                    if (Find("RE-FLIP") != null) { Click(platform, rng.Next(3) == 0 ? Find("RE-FLIP") : Find("KEEP")); continue; }
                    if (Find("DISCARD MODE") != null && rng.Next(4) == 0) { Click(platform, Find("DISCARD MODE")); continue; }
                    var cards = All("HAND COIN");
                    if (cards.Count > 0 && rng.Next(8) > 0) { Click(platform, cards[rng.Next(cards.Count)]); continue; }
                    if (Click(platform, Find("END ROUND"))) { rounds++; continue; }
                    if (Find("START AGAIN") != null) { Click(platform, Find("START AGAIN")); runs++; lastPhase = null; continue; }
                    continue;
                }
                if (g.Phase == Phase.Map)
                {
                    var choices = Ui.Buttons.FindAll(b => !b.Disabled && b.Label != null && Array.IndexOf(MapLabels, b.Label) >= 0);
                    if (choices.Count == 0) throw new InvalidOperationException("the map offers nothing to click (prompt=" + g.MapPrompt + ")");
                    Click(platform, choices[rng.Next(choices.Count)]);
                    continue;
                }
                if (g.Phase == Phase.Shop)
                {
                    int r = rng.Next(10);
                    if (r < 3 && Click(platform, All("BUY").Count > 0 ? All("BUY")[rng.Next(All("BUY").Count)] : null)) bought++;
                    else if (r == 3) Click(platform, Find("REROLL"));
                    else if (r == 4) { var coins = All("COIN"); if (coins.Count > 0) Click(platform, coins[rng.Next(coins.Count)]); }
                    else if (r == 5 && rng.Next(3) == 0) { var coins = All("COIN"); if (coins.Count > 0) Click(platform, coins[rng.Next(coins.Count)]); Click(platform, Find("REMOVE")); }
                    else if (r >= 7) Click(platform, Find("NEXT ROUND") ?? Find("BACK TO MAP"));
                    continue;
                }
                if (g.Phase == Phase.Augment)
                {
                    if (!Click(platform, All("CHOOSE AUGMENT").Count > 0 ? All("CHOOSE AUGMENT")[rng.Next(All("CHOOSE AUGMENT").Count)] : null))
                        Click(platform, All("CHOOSE").Count > 0 ? All("CHOOSE")[rng.Next(All("CHOOSE").Count)] : null);
                    continue;
                }
                // finish screen (a GAME OVER notice over the round comes first)
                if (Click(platform, Find("OK"))) continue;
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
            $"{losses} losses, deepest level {maxLevel}, {items} chips used, {bought} shop buys, {rounds} rounds ended");
        return true;
    }
}
