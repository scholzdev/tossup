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
            // This helper drives fixed screenshot states, so a losing seed must not erase the shop scene.
            if (g.Phase != Phase.Shop) Game.OpenSandboxShop(g);
            Ui.Holding = false;
            Ui.FlipAnimation = null;
            Ui.ResolveTimer = 0;
        }

        static void NewRun(string character, double seed)
        {
            Ui.Confirm = null;
            Ui.Holding = false;
            Ui.FlipAnimation = null;
            Ui.ResolveTimer = 0;
            // Screenshot saves are isolated; unlock the requested fixture character explicitly.
            int characterIndex = Content.CharacterOrder.IndexOf(character);
            if (characterIndex > 0) Ui.Profile.Wins.Add(Content.CharacterOrder[characterIndex - 1]);
            Ui.SelectedCharacter = character;
            A.Start(seed);
            Ui.EncounterReveal = null; // ordinary gameplay shots should show the screen behind the reveal
            Ui.Shake = 0;
        }

        public static List<Shot> Script() => new List<Shot>
        {
            new Shot { Name = "01_title", Setup = () => { Lang.Set("en"); Ui.Game = null; A.Go("title"); } },
            new Shot { Name = "02_help", Setup = () => { Ui.HelpNext = "select"; A.Go("help"); } },
            new Shot { Name = "03_select", Setup = () => { Ui.SelectedCharacter = "seer"; A.Go("select"); } },
            new Shot { Name = "04_sets", Setup = () => A.OpenSets("trader") },
            new Shot { Name = "05_sets_tooltip", Setup = () => { }, MouseX = 740, MouseY = 294 },
            new Shot { Name = "06_collection", Setup = () => A.Go("collection") },
            new Shot { Name = "07_options", Setup = () => { Ui.OptionsTab = "game"; A.Go("options"); } },
            new Shot { Name = "08_options_sound", Setup = () => Ui.OptionsTab = "sound" },
            new Shot { Name = "09_confirm", Setup = A.ClearProgress, MouseX = 658, MouseY = 480 },
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
            new Shot { Name = "16_shop_tooltip", Setup = () => { }, MouseX = 500, MouseY = 270 },
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
            new Shot { Name = "22_run_encounter_reveal", Setup = () => { Ui.SelectedCharacter="blade";A.Start(6601);AppCore.Update(.9); } },
            new Shot { Name = "23_contract", Setup = () => { NewRun("blade", 6602); Ui.EncounterReveal = null; Ui.Game.ContractsEnabled=true; Game.OfferContract(Ui.Game); } },
            new Shot { Name="24_edge", Setup=()=> {
                Lang.Set("en"); NewRun("blade",6); Game.MulliganDone(Ui.Game);
                Game.Flip(Ui.Game);Ui.Game.Pending.Result=Side.Tie;
                Ui.FlipAnimation=new FlipAnimation {Id=Game.GetCoin(Ui.Game,Ui.Game.Pending.Uid).Id,Outcome=Side.Tie,Duration=1.0,Elapsed=.995};
            } },
            new Shot { Name="25_upgraded_shop", Setup=()=> {
                NewRun("blade",12345);PlayToShop();
                Ui.Game.ShopOffers[0]=CoinCatalog.Normal;Ui.Game.ShopUpgrades[0]=UpgradeCatalog.LuckyDay;
                Ui.Game.Items.AddRange(new[]{"energy_drink","shortcut","safety_net"});Game.AddRelic(Ui.Game,"clock");
            }, MouseX=500,MouseY=270 },
            new Shot { Name="26_controller_inspect", Setup=()=> {
                PadNavigation.Connected=true;PadNavigation.ControllerUsed();PadNavigation.ToggleInspect();
                PadNavigation.Move(-1,0);PadNavigation.Move(0,-1);
            } },
            new Shot { Name="27_german_augment", Setup=()=> {
                PadNavigation.Connected=false;PadNavigation.MouseUsed();Lang.Set("de");NewRun("seer",6);
                Ui.Game.Phase=Phase.Augment;Ui.Game.AugmentLevel=3;Ui.Game.AugmentOptions=new List<string>{"upgrade_press","type_specialist","hedge_fund"};
            } },
            new Shot { Name="28_german_upgrade_choices", Setup=()=>Game.ChooseAugment(Ui.Game,"upgrade_press") },
            new Shot { Name="29_square_dance_tooltip", Setup=()=> {
                Ui.Game=null;Lang.Set("en");A.Go("collection");Ui.CollectionPage=1;Ui.CollectionSort="order";Ui.CollectionFilter="ALL";
                Ui.Profile.Collected.Add(CoinCatalog.SquareDance.Id);
            }, MouseX=1034,MouseY=422 },
            new Shot {Name="30_developer_reveal_start",Setup=()=> {
                RuntimeMode.Configure(true,false);Ui.SelectedCharacter="blade";A.Start(6601);AppCore.Update(.12);
            }},
            new Shot {Name="31_developer_reveal_later",Setup=()=> {
                AppCore.Update(.8);RuntimeMode.Configure(false,false);
            }},
            new Shot {Name="32_half_screen_continue_menu",Setup=()=> {
                Lang.Set("en");NewRun("blade",6603);A.OpenMenu();
            }},
            new Shot {Name="33_applying_result",Setup=()=> {
                Ui.Game.Paused=false;Ui.EncounterReveal=null;Game.MulliganDone(Ui.Game);
                A.FlipNextCoin();Ui.FlipAnimation=null;Ui.ResolveTimer=.9;Ui.Shake=0;
            }},
        };
    }
}
