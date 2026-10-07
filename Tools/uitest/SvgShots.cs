// Renders the real UI into SVG files, to look at a screen without Unity. Set UITEST_SVG=<folder> when running
// the test project: the fight screen is dumped for the main states (round start, mid round, flipping, ended
// round, hand sizes, won) after the layout proof. Coin art is embedded, so each .svg opens by itself in a browser.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Tossup;
using Tossup.UI;

sealed class SvgBackend : IGfxBackend
{
    readonly string resources;
    readonly StringBuilder body = new StringBuilder(), defs = new StringBuilder();
    readonly Dictionary<string, string> images = new Dictionary<string, string>();
    readonly Dictionary<string, string> clips = new Dictionary<string, string>();
    string clip = "";

    public SvgBackend(string resources) { this.resources = resources; }

    static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    static string Hex(Rgba c) => "#" + ((int)Math.Round(c.R * 255)).ToString("x2") + ((int)Math.Round(c.G * 255)).ToString("x2") + ((int)Math.Round(c.B * 255)).ToString("x2");

    public void Clip(float x, float y, float w, float h)
    {
        if (w < 0) { clip = ""; return; }
        string key = F(x) + "," + F(y) + "," + F(w) + "," + F(h);
        if (!clips.TryGetValue(key, out var id))
        {
            id = "c" + clips.Count;
            clips[key] = id;
            defs.Append("<clipPath id=\"" + id + "\"><rect x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(w) + "\" height=\"" + F(h) + "\"/></clipPath>");
        }
        clip = " clip-path=\"url(#" + id + ")\"";
    }

    public void Triangles(List<float> xy, Rgba color)
    {
        body.Append("<path fill=\"" + Hex(color) + "\" fill-opacity=\"" + F(color.A) + "\"" + clip + " d=\"");
        for (int i = 0; i < xy.Count; i += 6)
            body.Append("M" + F(xy[i]) + " " + F(xy[i + 1]) + "L" + F(xy[i + 2]) + " " + F(xy[i + 3]) + "L" + F(xy[i + 4]) + " " + F(xy[i + 5]) + "Z");
        body.Append("\"/>\n");
    }

    public void Image(Img image, float[] q, Rgba tint)
    {
        if (!images.TryGetValue(image.Key, out var id))
        {
            id = "i" + images.Count;
            images[image.Key] = id;
            string data = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(resources, image.Key + ".png")));
            defs.Append("<image id=\"" + id + "\" width=\"" + image.Width + "\" height=\"" + image.Height + "\" href=\"data:image/png;base64," + data + "\"/>");
        }
        float x = Math.Min(Math.Min(q[0], q[2]), Math.Min(q[4], q[6])), y = Math.Min(Math.Min(q[1], q[3]), Math.Min(q[5], q[7]));
        float r = Math.Max(Math.Max(q[0], q[2]), Math.Max(q[4], q[6])), b = Math.Max(Math.Max(q[1], q[3]), Math.Max(q[5], q[7]));
        body.Append("<use href=\"#" + id + "\" opacity=\"" + F(tint.A) + "\"" + clip + " transform=\"translate(" + F(x) + " " + F(y) + ") scale(" +
            F((r - x) / image.Width) + " " + F((b - y) / image.Height) + ")\"/>\n");
    }

    public void Text(PixFont font, string text, float x, float y, float sx, float sy, Rgba color)
    {
        if (string.IsNullOrEmpty(text)) return;
        body.Append("<text x=\"" + F(x) + "\" y=\"" + F(y + font.Ascent * sy) + "\" font-family=\"Menlo, Consolas, monospace\" font-weight=\"bold\" font-size=\"" +
            F(font.Size * sy * .85f) + "\" fill=\"" + Hex(color) + "\" textLength=\"" + F(font.GetWidth(text) * sx) + "\" lengthAdjust=\"spacingAndGlyphs\"" + clip +
            " xml:space=\"preserve\">" + System.Security.SecurityElement.Escape(text) + "</text>\n");
    }

    public string Finish() =>
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + (int)Ui.Width + "\" height=\"" + (int)Ui.Height + "\" viewBox=\"0 0 " + (int)Ui.Width + " " + (int)Ui.Height +
        "\"><defs>" + defs + "</defs><rect width=\"100%\" height=\"100%\" fill=\"#000\"/>\n" + body + "</svg>\n";
}

static class SvgShots
{
    static string dir, resources;
    static IGfxBackend previous;

    public static void Dump(string name)
    {
        var svg = new SvgBackend(resources);
        previous = Gfx.Backend;
        Gfx.Backend = svg;
        try { AppCore.Draw(); } finally { Gfx.Backend = previous; }
        File.WriteAllText(Path.Combine(dir, name + ".svg"), svg.Finish());
        Console.WriteLine("svg: " + Path.Combine(dir, name + ".svg"));
    }

    static void Fresh(string character, double seed, int stake = 1)
    {
        Ui.Confirm = null; Ui.FlipAnimation = null; Ui.ResolveTimer = 0; Ui.Deciding = false;
        Ui.SelectedCharacter = Content.Characters[character];
        Ui.StakePick[character] = stake;
        A.Start(seed);
        if (stake > 1) Ui.Game = Game.New(seed, character, Tossup.Profile.UnlockedList(Ui.Profile, character), A.Loadout(), stake, map: true);
        Game.ChooseNode(Ui.Game, Ui.Game.Map.FindIndex(n => n.Row == 0));
        Ui.EncounterReveal = null; Ui.Shake = 0; Ui.Game.Paused = false;
    }

    static void Flip(int index)
    {
        A.FlipCoin(Ui.Game.Encounter.Hand[index]);
        Settle();
    }

    static void Settle()
    {
        A.Update(Ui.FlipAnimation.Duration + .01);
        if (Ui.Deciding) A.Keep(); else A.Update(2); // keep the result, or let it apply by itself
    }

    public static void Run(HeadlessPlatform platform, string folder, string root)
    {
        dir = folder; resources = Path.Combine(root, "Assets", "Resources");
        Directory.CreateDirectory(dir);
        platform.WindowWidth = 1280; platform.WindowHeight = 800;
        Lang.Set("en");
        Ui.Profile.Options.SeenHelp = true;
        Ui.Profile.Options.FastFlip = false;

        Fresh("blade", 12345);
        AppCore.Update(0); Dump("01_round1_start");
        Flip(0); Flip(1);
        AppCore.Update(0); Dump("02_mid_round_after_2_flips");
        A.FlipCoin(Ui.Game.Encounter.Hand[0]); Ui.FlipAnimation.Elapsed = Ui.FlipAnimation.Duration * .5;
        Dump("03_flipping_in_place");
        Ui.FlipAnimation.Elapsed = Ui.FlipAnimation.Duration; A.Update(.001);
        Dump("04_landed_applying_or_deciding");
        A.Update(2);
        A.EndRound(); AppCore.Update(0);
        Dump("05_after_end_round_enemy_revealed");

        // a stage-8 run shows the enemy's round bonus and the 4-coin hand
        Fresh("blade", 777, 8);
        AppCore.Update(0); Dump("06_stage8_round1");
        A.EndRound(); Flip(0); AppCore.Update(0); Dump("07_stage8_round2");

        // a won fight
        for (int seed = 1; seed < 400; seed++)
        {
            Fresh("blade", seed);
            var g = Ui.Game;
            for (int guard = 0; guard < 200 && g.Phase == Phase.Encounter && !g.Encounter.Cleared; guard++)
            {
                if (g.Pending != null) { if (Ui.FlipAnimation != null) Settle(); else A.Update(5); continue; }
                int uid = g.Encounter.Hand.Find(u => Game.CanFlip(g, u));
                if (uid != 0) { A.FlipCoin(uid); Settle(); }
                else A.EndRound();
            }
            if (g.Phase == Phase.Encounter && g.Encounter.Cleared) { AppCore.Update(0); Dump("08_fight_won"); break; }
        }

        // a big hand, with buffs and a re-flip waiting
        Ui.Game = Game.NewSandbox(new SandboxConfig { Coins = new List<string> { "normal", "dagger", "sword", "hammer", "lunge", "normal", "dagger", "slug", "normal", "dagger", "sword", "normal" }, Seed = 6, Energy = 9 });
        Game.DrawCoins(Ui.Game, 7);
        Ui.Game.Encounter.Modifier = "lucky_day";
        Game.AddBuff(Ui.Game, "mult", 2, 2, true);
        Ui.Game.Items.AddRange(new[] { "energy_drink", "shortcut" });
        A.Update(0); Dump("09_hand_of_12_sandbox");
        Ui.Game = Game.NewSandbox(new SandboxConfig { Coins = new List<string> { "normal", "dagger", "sword", "hammer", "lunge", "slug" }, Seed = 6, Energy = 9 });
        A.FlipCoin(Ui.Game.Encounter.Hand[0]); A.Update(Ui.FlipAnimation.Duration + .01);
        Dump("10_deciding_reflip_or_keep");
        // tooltips and German on the stage-8 fight (enemy round bonus, hand of 4)
        Fresh("blade", 777, 8);
        A.EndRound(); AppCore.Update(0);
        platform.X = 150; platform.Y = 130; AppCore.Draw(); Dump("11_hover_enemy_pouch");
        platform.X = 700; platform.Y = 150; AppCore.Draw(); Dump("12_hover_enemy_bonus");
        platform.X = -50; platform.Y = -50;
        Lang.Set("de"); AppCore.Update(0); Dump("13_german_stage8_round2"); Lang.Set("en");
        Console.WriteLine("svg: dumped to " + dir);
    }
}
