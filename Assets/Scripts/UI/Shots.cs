using System;
using System.Collections.Generic;

namespace Tossup.UI
{
    // A scripted tour through every screen, used to capture screenshots for checking the port (run the
    // player with -tossup-shots <folder>). Each shot sets up the state, then is drawn with the given mouse
    // position (canvas pixels). Saves go to a throwaway folder in that mode.
    public sealed class Shot
    {
        public string Name;
        public Action Setup;
        public float MouseX = -50, MouseY = -50;
    }

    public static class Shots
    {
        // Play the current level with a simple policy until the shop opens (or the run ends).
        static void PlayToShop()
        {
            var g = Ui.Game;
            for (int guard = 0; guard < 400 && g.Phase == Phase.Encounter; guard++)
            {
                if (g.Mulligan != null) Game.MulliganDone(g);
                else if (g.Pending != null) Game.Resolve(g);
                else if (g.Dealt != null)
                {
                    if (g.Encounter.Cleared) Game.EndLevel(g);
                    else if (Game.CanFlip(g)) Game.Flip(g);
                    else Game.Discard(g);
                }
                else if (Game.CanExchange(g)) Game.Exchange(g);
                else if (g.Encounter.Cleared) Game.EndLevel(g);
                else break;
            }
            Ui.Holding = false;
            Ui.FlipAnimation = null;
            Ui.ResolveTimer = 0;
        }

        static void NewRun(string character, double seed)
        {
            Ui.SelectedCharacter = character;
            A.Start(seed);
            Ui.Shake = 0;
        }

        public static List<Shot> Script() => new List<Shot>
        {
            new Shot { Name = "01_title", Setup = () => { Lang.Set("en"); Ui.Game = null; A.Go("title"); } },
            new Shot { Name = "02_help", Setup = () => { Ui.HelpNext = "select"; A.Go("help"); } },
            new Shot { Name = "03_select", Setup = () => { Ui.SelectedCharacter = "seer"; A.Go("select"); } },
            new Shot { Name = "04_sets", Setup = () => A.OpenSets("trader") },
            new Shot { Name = "05_sets_tooltip", Setup = () => { }, MouseX = 583, MouseY = 294 },
            new Shot { Name = "06_collection", Setup = () => A.Go("collection") },
            new Shot { Name = "07_options", Setup = () => { Ui.OptionsTab = "game"; A.Go("options"); } },
            new Shot { Name = "08_options_sound", Setup = () => Ui.OptionsTab = "sound" },
            new Shot { Name = "09_confirm", Setup = A.ClearProgress, MouseX = 520, MouseY = 480 },
            new Shot
            {
                Name = "10_mulligan",
                Setup = () =>
                {
                    Ui.Confirm = null;
                    NewRun("blade", 12345);
                    Ui.Marked.Add(Ui.Game.Mulligan.Hand[1]);
                },
            },
            new Shot { Name = "11_dealt", Setup = () => { Game.MulliganDone(Ui.Game); Ui.Marked.Clear(); } },
            new Shot
            {
                Name = "12_flip",
                Setup = () =>
                {
                    A.FlipNextCoin();
                    Ui.FlipAnimation.Elapsed = Ui.FlipAnimation.Duration * .55;
                },
            },
            new Shot
            {
                Name = "13_result",
                Setup = () =>
                {
                    Ui.FlipAnimation = null;
                    Game.Resolve(Ui.Game);
                    Ui.Holding = true;
                },
            },
            new Shot { Name = "14_bank_tooltip", Setup = () => { }, MouseX = 150, MouseY = 270 },
            new Shot { Name = "15_shop", Setup = PlayToShop },
            new Shot { Name = "16_shop_tooltip", Setup = () => { }, MouseX = 395, MouseY = 270 },
            new Shot
            {
                Name = "17_game_over",
                Setup = () =>
                {
                    Ui.Game.Phase = Phase.GameOver;
                    Ui.Game.LostWhy = "out of coins, and not enough gold to exchange.";
                },
            },
            new Shot { Name = "18_victory", Setup = () => Ui.Game.Phase = Phase.Victory },
            new Shot { Name = "19_german_title", Setup = () => { Lang.Set("de"); Ui.Game = null; A.Go("title"); } },
            new Shot
            {
                Name = "20_german_round",
                Setup = () =>
                {
                    NewRun("seer", 777);
                    Game.MulliganDone(Ui.Game);
                    A.FlipNextCoin();
                    Ui.FlipAnimation = null;
                    Game.Resolve(Ui.Game);
                    Ui.Holding = true;
                    Ui.DebugVisible = true;
                },
            },
            new Shot { Name = "21_german_shop", Setup = () => { Ui.DebugVisible = false; PlayToShop(); } },
        };
    }
}
