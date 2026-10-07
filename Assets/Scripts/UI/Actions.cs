using System;
using System.Collections.Generic;

namespace Tossup.UI
{
    // Player actions and per-frame flow (flip animation -> hold -> resolve).
    public static class A
    {
        const string ProfileFile = "profile.json";
        const string RunFile = "run.json";
        const string DevRunFile = "run-dev.json";
        static string savedKey;
        static bool? savedRunExists;
        static string CurrentRunFile => RuntimeMode.Dev ? DevRunFile : RunFile;

        static void ApplyOptions()
        {
            Ui.Platform.SetFullscreen(Ui.Profile.Options.Fullscreen);
            Lang.Set(Ui.Profile.Options.Language);
            Sound.Apply(Ui.Profile.Options);
        }

        public static void LoadProfile()
        {
            string json = Ui.Platform.ReadSave(ProfileFile);
            Ui.Profile = json != null ? Tossup.Profile.Decode(json) : Tossup.Profile.New();
            RuntimeMode.ApplyProfile(Ui.Profile);
            ApplyOptions();
        }

        static void SaveProfile()
        {
            if (RuntimeMode.Dev || RuntimeMode.Sandbox) return;
            Ui.Platform.WriteSave(ProfileFile, Tossup.Profile.Encode(Ui.Profile));
            Ui.Profile.MasteryDirty = false;
        }

        public static bool HasSavedRun()
        {
            if(RuntimeMode.Sandbox)return false;
            if(!savedRunExists.HasValue)savedRunExists=Ui.Platform.ReadSave(CurrentRunFile)!=null;
            return savedRunExists.Value;
        }
        public static void SaveRun()
        {
            if (!RuntimeMode.Sandbox && Ui.Game != null && !Ui.Game.Tutorial && Ui.Game.Sandbox == null && RunSave.IsSafePoint(Ui.Game))
            {Ui.Platform.WriteSave(CurrentRunFile, RunSave.Encode(Ui.Game));savedRunExists=true;}
        }
        public static void DeleteRun() { Ui.Platform.DeleteSave(CurrentRunFile); savedRunExists=false;savedKey = "over"; }

        public static bool LoadRun()
        {
            string text=Ui.Platform.ReadSave(CurrentRunFile);savedRunExists=text!=null;
            var game = RunSave.Decode(text);
            if (game == null) { DeleteRun(); return false; }
            game.Paused = false;
            Ui.Game = game;
            game.MasteryProfile = !RuntimeMode.Sandbox && !game.Tutorial && game.Sandbox == null ? Ui.Profile : null;
            Ui.DiscardMode = false;
            Ui.SelectedCharacter = Content.Characters[game.CharacterId];
            Ui.EncounterReveal = null;
            Ui.FlipAnimation = null;
            Ui.Deciding = false;
            Ui.ResolveTimer = 0;
            Ui.Tutorial = null;
            savedKey = null;
            return true;
        }

        public static void Go(UiScreen screen)
        {
            Ui.Screen = screen;
            Ui.Confirm = null;
            Ui.CollectionPage = 1;
        }

        // "Play" / "New Run": the very first time, show How To Play before the character screen.
        public static void Play()
        {
            if (RuntimeMode.Sandbox && Ui.SandboxConfig != null) { StartSandbox(Ui.SandboxConfig); return; }
            if (HasSavedRun())
            {
                Ui.Confirm = new Confirm { Title = "NEW RUN", Text = "YOUR SAVED RUN WILL BE REPLACED.", Ok = () => { DeleteRun(); Go(UiScreen.Select); } };
                return;
            }
            if (Ui.Profile.Options.SeenHelp)
            {
                Go(UiScreen.Select);
                return;
            }
            Ui.Profile.Options.SeenHelp = true;
            SaveProfile();
            Tutorial.Start();
        }

        public static void ToggleDeveloperMode()
        {
            bool enable = !RuntimeMode.Dev;
            Action changeMode = () =>
            {
                Ui.Platform.WriteSave(RuntimeMode.DeveloperModePreference, enable ? "1" : "0");
                Ui.Game = null;
                RuntimeMode.Configure(enable, false);
                savedRunExists = null;
                savedKey = null;
                LoadProfile();
                if (enable) Play();
                else Go(UiScreen.Title);
            };

            if (Ui.Game != null && Ui.Game.Phase != Phase.GameOver && Ui.Game.Phase != Phase.Victory)
            {
                Ui.Confirm = new Confirm
                {
                    Title = enable ? "DEVELOPER MODE" : "EXIT DEVELOPER MODE",
                    Text = "THE FLIP IN PROGRESS IS LOST. YOU CONTINUE FROM YOUR LAST FLIP.",
                    Ok = changeMode,
                };
                return;
            }

            changeMode();
        }

        public static void StartTutorial()
        {
            Ui.Profile.Options.SeenHelp = true;
            SaveProfile();
            if(Ui.Game!=null&&Ui.Game.Phase!=Phase.GameOver&&Ui.Game.Phase!=Phase.Victory)
                Ui.Confirm=new Confirm{Title="TUTORIAL",Text="THE FLIP IN PROGRESS IS LOST. YOU CONTINUE FROM YOUR LAST FLIP.",Ok=Tutorial.Start};
            else Tutorial.Start();
        }

        // Quitting asks first (popup) while a run is in progress, because runs are not saved.
        public static void Quit()
        {
            var g = Ui.Game;
            bool running = g != null && !RuntimeMode.Sandbox && !g.Tutorial && g.Sandbox == null && g.Phase != Phase.GameOver && g.Phase != Phase.Victory;
            if (running)
            {
                Ui.Confirm = new Confirm { Title = "QUIT", Text = "THE FLIP IN PROGRESS IS LOST. YOU CONTINUE FROM YOUR LAST FLIP.", Ok = Ui.Platform.Quit };
                return;
            }
            Ui.Platform.Quit();
        }

        // One line per finished run, to match tester feedback with seeds (runs.log in the save folder).
        static void LogRun(GameState game)
        {
            var coins = new List<string>();
            foreach (var owned in game.Coins) coins.Add(owned.Id);
            string result = game.Endless ? "ENDLESS" : game.Phase == Phase.Victory ? "WIN" : "LOSS";
            Ui.Platform.AppendSave("runs.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " v" + BuildInfo.Number + "-" + BuildInfo.Build + " seed=" + game.Seed +
                " char=" + game.CharacterId + " stake=" + game.Stake + " result=" + result + " cleared=" + game.Cleared + " gold=" + (long)game.Player.Gold +
                " why=" + (game.LostWhy ?? "-") + " heads=" + game.RunHeads + "/" + game.RunFlips + " expected=" + GameText.Num(Math.Round(game.RunExpectedHeads, 1)) + " coins=" + string.Join(",", coins) + "\n");
        }

        // Pause the run (if any) and show the title screen.
        public static void OpenMenu()
        {
            if (Ui.Game != null) Ui.Game.Paused = true;
            Go(UiScreen.Title);
        }

        public static void ToggleOption(string key)
        {
            var options = Ui.Profile.Options;
            options.SetSwitch(key, !options.GetSwitch(key));
            if (key == "fullscreen") ApplyOptions();
            SaveProfile();
        }

        // A volume slider (0-100). Persist each update so changing sound settings survives an early exit.
        public static void SetVolume(string key, double value)
        {
            Ui.Profile.Options.SetVolume(key, Math.Max(0, Math.Min(100, Math.Floor(value + .5))));
            Sound.Apply(Ui.Profile.Options);
            SaveProfile();
        }

        public static void SaveOptions() => SaveProfile();

        public static void RestoreSoundDefaults()
        {
            var current = Ui.Profile.Options;
            var defaults = new Options();
            current.VolumeMaster = defaults.VolumeMaster;
            current.VolumeMusic = defaults.VolumeMusic;
            current.VolumeSfx = defaults.VolumeSfx;
            Sound.Apply(current);
            SaveOptions();
        }

        // Cycle through the available languages (English, Deutsch).
        public static void CycleLanguage()
        {
            var order = Lang.Order;
            int i = order.IndexOf(Lang.Current);
            Ui.Profile.Options.Language = order[(i + 1) % order.Count];
            Lang.Set(Ui.Profile.Options.Language);
            SaveProfile();
        }

        // Spend tokens to unlock a locked coin for one character (same unlock as buying it in the shop).
        public static void UnlockWithTokens(string characterId, CoinDef coin)
        {
            int price = Game.TokenPrice(coin);
            if (RuntimeMode.Sandbox || Ui.Profile.Tokens < price || !Tossup.Profile.Grant(Ui.Profile, characterId, coin.Id)) return;
            Ui.Profile.Tokens -= price;
            SaveProfile();
        }

        public static void ContinueEndless() => Game.ContinueEndless(Ui.Game);

        // Delete unlocks, collection, coin sets and tokens (options stay); also ends a run in progress. Asks first.
        public static void ClearProgress()
        {
            Ui.Confirm = new Confirm { Title = "CLEAR PROGRESS", Text = "YOUR DATA WILL BE PERMANENTLY DELETED.", Ok = DoClearProgress };
        }

        public static void DoClearProgress()
        {
            var options = Ui.Profile.Options;
            Ui.Profile = Tossup.Profile.New();
            Ui.Profile.Options = options;
            Ui.Game = null;
            Ui.DiscardMode = false;
            Ui.SandboxConfig = null;
            DeleteRun();
            Ui.SetDraft = null;
            Ui.FlipAnimation = null;
            Ui.Deciding = false;
            SaveProfile();
        }

        public static void SetFilter(CollectionRarityFilter rarity)
        {
            Ui.CollectionFilter = rarity;
            Ui.CollectionPage = 1;
        }

        public static void ChangeCollectionPage(int delta) => Ui.CollectionPage = Math.Max(1, Ui.CollectionPage + delta);

        // The coins the selected character starts a run with (its active coin set).
        public static List<CoinDef> Loadout() =>
            Tossup.Profile.Loadout(Ui.Profile, Ui.SelectedCharacter.Id, Game.StartMax, Game.MaxCopies).ConvertAll(id=>Content.Coins[id]);
        public static int Stake()
        {
            int top=Tossup.Profile.MaxStake(Ui.Profile,Ui.SelectedCharacter.Id);return Math.Max(1,Math.Min(top,Ui.StakePick.TryGetValue(Ui.SelectedCharacter.Id,out var n)?n:top));
        }
        public static void CycleStake(int delta){Ui.StakePick[Ui.SelectedCharacter.Id]=Math.Max(1,Math.Min(Tossup.Profile.MaxStake(Ui.Profile,Ui.SelectedCharacter.Id),Stake()+delta));}

        // ---- coin set editor: edits go to a draft and only reach the profile when Save is pressed

        static List<string> SavedSet() => Tossup.Profile.Sets(Ui.Profile, Ui.SetsCharacter)[Ui.SetsIndex - 1].Coins;

        // The coins of the open set as currently edited (a copy of the saved set until something changes).
        public static List<string> SetDraftCoins()
        {
            var d = Ui.SetDraft;
            if (d == null || d.Character != Ui.SetsCharacter || d.Index != Ui.SetsIndex)
            {
                d = new SetDraft { Character = Ui.SetsCharacter, Index = Ui.SetsIndex, Coins = new List<string>(SavedSet()) };
                Ui.SetDraft = d;
            }
            return d.Coins;
        }

        public static bool SetDirty()
        {
            var draft = SetDraftCoins();
            var saved = SavedSet();
            if (draft.Count != saved.Count) return true;
            for (int i = 0; i < draft.Count; i++) if (draft[i] != saved[i]) return true;
            return false;
        }

        public static void OpenSets(string characterId)
        {
            Ui.SetsReturn = Ui.Screen == UiScreen.Select ? UiScreen.Select : UiScreen.Title;
            Ui.SetsCharacter = characterId ?? Ui.SelectedCharacter.Id;
            Ui.SetsIndex = Tossup.Profile.Active(Ui.Profile, Ui.SetsCharacter);
            Ui.SetsCatalogPage = 1;
            Ui.SetDraft = null;
            Go(UiScreen.Sets);
        }

        public static void BackFromSets() => Go(Ui.SetsReturn);

        public static void SetsPickCharacter(string id)
        {
            if (!Tossup.Profile.CharacterUnlocked(Ui.Profile, id)) return;
            Ui.SetsCharacter = id;
            Ui.SetsIndex = Tossup.Profile.Active(Ui.Profile, id);
            Ui.SetsCatalogPage = 1;
            Ui.SetDraft = null;
        }

        public static void ChangeSetsCatalogPage(int delta) =>
            Ui.SetsCatalogPage = Math.Max(1, Ui.SetsCatalogPage + delta);

        public static void SetsPickSet(int index)
        {
            Ui.SetsIndex = index;
            Ui.SetDraft = null; // unsaved edits are dropped when you switch sets
            Tossup.Profile.SetActive(Ui.Profile, Ui.SetsCharacter, index); // the play screen opens on the set you last looked at
            SaveProfile();
        }

        public static void AddCoinToSet(string coinId)
        {
            var coins = SetDraftCoins();
            if (Tossup.Profile.CanAdd(Ui.Profile, Ui.SetsCharacter, coins, coinId, Game.StartMax, Game.MaxCopies)) coins.Add(coinId);
        }

        public static void RemoveCoinFromSet(int slot)
        {
            var coins = SetDraftCoins();
            if (slot >= 0 && slot < coins.Count) coins.RemoveAt(slot);
        }

        public static void ClearSet() =>
            Ui.SetDraft = new SetDraft { Character = Ui.SetsCharacter, Index = Ui.SetsIndex, Coins = new List<string>() };

        public static void SaveSet()
        {
            Tossup.Profile.Sets(Ui.Profile, Ui.SetsCharacter)[Ui.SetsIndex - 1].Coins = new List<string>(SetDraftCoins());
            SaveProfile();
        }

        // Step through the selected character's sets on the play screen.
        public static void CycleActiveSet(int delta)
        {
            int count = Tossup.Profile.SetCount;
            int index = ((Tossup.Profile.Active(Ui.Profile, Ui.SelectedCharacter.Id) - 1 + delta) % count + count) % count + 1;
            Tossup.Profile.SetActive(Ui.Profile, Ui.SelectedCharacter.Id, index);
            SaveProfile();
        }

        public static void Start(double? seed = null)
        {
            if(RuntimeMode.Sandbox&&Ui.SandboxConfig!=null){StartSandbox(Ui.SandboxConfig);return;}
            var p = Ui.Platform;
            if(!Tossup.Profile.CharacterUnlocked(Ui.Profile,Ui.SelectedCharacter.Id))return;
            Ui.Game = Game.New(seed ?? p.UnixTime + Math.Floor(p.Time * 1000000), Ui.SelectedCharacter.Id,
                Tossup.Profile.UnlockedList(Ui.Profile, Ui.SelectedCharacter.Id), Loadout(), Stake(), map: true);
            Ui.DiscardMode = false;
            Ui.FlipAnimation = null;
            Ui.Deciding = false;
            Ui.ResolveTimer = 0;
            Ui.Notice = "";
            Ui.EncounterReveal = Ui.Game.RunEncounter == null ? null : new EncounterReveal { Elapsed = 0 };
            Ui.Game.Tutorial = false;
            Ui.Game.MasteryProfile = Ui.Profile;
            savedKey = null;
        }

        public static void StartSandbox(SandboxConfig config)
        {
            RuntimeMode.Configure(RuntimeMode.Dev, true);
            RuntimeMode.ApplyProfile(Ui.Profile);
            var game = Game.NewSandbox(config);
            Ui.SandboxConfig = config;
            Ui.Game = game;
            Ui.DiscardMode = false;
            Ui.SelectedCharacter = Content.Characters[game.CharacterId];
            Ui.SetsCharacter = game.CharacterId;
            Ui.Tutorial = null;
            Ui.EncounterReveal = null;
            Ui.FlipAnimation = null;
            Ui.Deciding = false;
            Ui.ResolveTimer = 0;
            Ui.Screen = Enum.Parse<UiScreen>(config.Screen, true);
            game.Paused = Ui.Screen != UiScreen.Encounter && Ui.Screen != UiScreen.Shop;
            if (Ui.Screen == UiScreen.Shop) Game.OpenSandboxShop(game);
            savedKey = null;
        }

        public static void SelectCharacter(string id) => Ui.SelectedCharacter = Content.Characters[id];

        // Step to the previous/next character (wraps around).
        public static void CycleCharacter(int delta)
        {
            var order = Content.CharacterOrder;
            int i = order.IndexOf(Ui.SelectedCharacter.Id);
            if (i < 0) return;
            SelectCharacter(order[((i + delta) % order.Count + order.Count) % order.Count]);
        }

        public static void ChooseNode(int index) => Game.ChooseNode(Ui.Game, index);
        public static void MapChoose(string option, int uid = 0) => Game.MapChoose(Ui.Game, option, uid);
        public static void ChooseAugment(string id) => Game.ChooseAugment(Ui.Game, id);
        public static void ChooseAugmentOption(string key) => Game.ChooseAugmentOption(Ui.Game, key);

        public static void UseItem(int slot)
        {
            if (Ui.FlipAnimation == null) Game.UseItem(Ui.Game, slot);
        }

        static void Land()
        {
            var game = Ui.Game;
            Ui.FlipAnimation = new FlipAnimation
            {
                Id = Game.GetCoin(game, game.Pending.Uid).Id, Outcome = game.Pending.Result, Elapsed = 0,
                Duration = Ui.Profile.Options.FastFlip ? .8 : 1.6,
            };
        }

        // The previous flip is kept as it landed (the player moved on): apply it now.
        static bool SettlePending()
        {
            var game = Ui.Game;
            if (Ui.FlipAnimation != null) return false;
            if (game.Pending != null)
            {
                Ui.Deciding = false;
                Ui.ResolveTimer = 0;
                Game.Resolve(game);
            }
            return true;
        }

        // Flip a coin of your hand. Clicking the next coin while the last one is still showing keeps its result.
        public static void FlipCoin(int uid)
        {
            Ui.Notice = "";
            if (!SettlePending()) return;
            if (Game.Flip(Ui.Game, uid)) Land();
        }

        // Keyboard shortcut: flip the first coin in the hand that you can pay for.
        public static void FlipFirst()
        {
            var game = Ui.Game;
            if (game?.Encounter == null) return;
            foreach (int uid in game.Encounter.Hand)
                if (Game.FlipCost(game, uid) <= game.Player.Energy) { FlipCoin(uid); return; }
        }

        // Spend 1 energy to re-roll the pending result and replay the landing.
        public static void Reflip()
        {
            var game = Ui.Game;
            if (!Ui.Deciding || Ui.FlipAnimation != null || !Game.Reroll(game)) return;
            Ui.Deciding = false; // one re-flip per coin
            Land();
        }

        public static void SelectCoin(CoinInst item)
        {
            Ui.Notice = "";
            Game.Select(Ui.Game, item.Uid);
        }

        // Keep the landed result (instead of re-flipping it) and apply it.
        public static void Keep() { if (Ui.Deciding) SettlePending(); }

        // The round is over: the enemy flips, your hand refills.
        public static void EndRound()
        {
            if (!SettlePending()) return;
            Ui.DiscardMode = false;
            Game.EndRound(Ui.Game);
        }

        public static void DiscardCoin(int uid)
        {
            if (Game.DiscardBank(Ui.Game, uid) && Ui.Game.Encounter.BankDiscards < 1) Ui.DiscardMode = false;
        }

        public static bool ToggleDiscardMode()
        {
            var game = Ui.Game;
            if (game == null || game.Encounter == null || game.Encounter.BankDiscards < 1) return false;
            Ui.DiscardMode = !Ui.DiscardMode;
            return true;
        }

        // Leave the fight you won: to the map (or the shop).
        public static void OpenShop()
        {
            if (Ui.FlipAnimation == null) Game.EndLevel(Ui.Game);
        }

        public static void BankCombo() => Game.BankCombo(Ui.Game);

        public static void Update(double dt)
        {
            var game = Ui.Game;
            Ui.Shake = Math.Max(0, Ui.Shake - dt);
            if (Ui.EncounterReveal != null && game != null && !game.Paused)
                Ui.EncounterReveal.Elapsed = Math.Min(3.4, Ui.EncounterReveal.Elapsed + dt);
            if (game != null && Ui.EncounterReveal == null)
                Game.AdvanceClock(game, dt);
            Tutorial.Update();
            if (game?.Pending == null) Ui.Deciding = false;
            if (game == null || game.Phase != Phase.Encounter || game.Encounter == null || game.Encounter.BankDiscards < 1) Ui.DiscardMode = false;
            if (game != null && !RuntimeMode.Sandbox && !game.Tutorial && game.Sandbox==null && game.Purchased.Count > 0) // buying a locked coin in the shop unlocks it for good
            {
                bool unlockedNow = false;
                foreach (var id in game.Purchased) unlockedNow = Tossup.Profile.Grant(Ui.Profile, game.CharacterId, id) || unlockedNow;
                game.Purchased.Clear();
                if (unlockedNow) SaveProfile();
            }
            if (game != null && !RuntimeMode.Sandbox && !game.Tutorial && game.Sandbox == null) // anything that has been in your deck counts as collected
            {
                bool fresh = false;
                foreach (var owned in game.Coins) fresh = Tossup.Profile.Collect(Ui.Profile, owned.Id) || fresh;
                if (fresh) SaveProfile();
            }
            if (Ui.Profile?.MasteryDirty == true) SaveProfile();
            if (game != null && !RuntimeMode.Sandbox && !game.Tutorial && game.Sandbox == null && (game.Phase == Phase.GameOver || game.Phase == Phase.Victory) && game.TokensPaid == null)
            {
                game.TokensPaid = Game.RunTokens(game);
                LogRun(game);
                Ui.Profile.Tokens += game.TokensPaid.Value;
                SaveProfile();
            }
            if(game!=null&&!RuntimeMode.Sandbox&&!game.Tutorial&&game.Sandbox==null&&game.Phase==Phase.Victory&&!game.WinRecorded)
            {
                game.WinRecorded=true;game.UnlockedStake=Tossup.Profile.RecordStakeWin(Ui.Profile,game.CharacterId,game.Stake);game.UnlockedCharacter=Tossup.Profile.RecordWin(Ui.Profile,game.CharacterId);SaveProfile();
            }
            if(game!=null&&!RuntimeMode.Sandbox&&!game.Tutorial&&game.Sandbox==null&&game.Endless&&game.Phase==Phase.GameOver&&!game.EndlessRecorded){game.EndlessRecorded=true;game.EndlessRecord=Tossup.Profile.RecordEndless(Ui.Profile,game.CharacterId,game.Cleared-Game.Route.Count);SaveProfile();}
            if (game != null && !RuntimeMode.Sandbox && !game.Tutorial && game.Sandbox==null && game.Endless && game.Phase == Phase.GameOver && !game.EndlessLogged)
            {
                game.EndlessLogged = true;
                LogRun(game); // an endless run is logged again when it ends, with the levels cleared beyond the boss
            }
            if (game != null && !RuntimeMode.Sandbox && !game.Tutorial && game.Sandbox == null)
            {
                bool safe = RunSave.IsSafePoint(game);
                if (safe)
                {
                    string key = game.Phase + ":" + game.EncounterIndex + ":" + game.Player.Gold + ":" + game.Coins.Count + ":" +
                        game.MapAt + game.MapPrompt + ":" + game.Items.Count + ":" + game.Relics.Count + ":" + game.RerollCost + ":" + game.RngState + ":" + game.Augments.Count;
                    if (game.Encounter != null && game.Phase == Phase.Encounter) key += ":" + game.Encounter.Round + ":" + game.Encounter.RoundFlips + ":" + game.Encounter.Cleared;
                    if (game.AugmentPending != null) key += ":" + game.AugmentPending.Id + ":" + game.AugmentPending.RewardId;
                    if (key != savedKey) { savedKey = key; SaveRun(); }
                }
                else if ((game.Phase == Phase.GameOver || game.Phase == Phase.Victory) && savedKey != "over") DeleteRun();
            }
            if (Ui.ResolveTimer > 0 && game != null && !game.Paused)
            {
                Ui.ResolveTimer -= dt;
                if (Ui.ResolveTimer <= 0)
                {
                    Game.Resolve(game);
                }
            }
            if (Ui.FlipAnimation != null && game != null && !game.Paused)
            {
                Ui.FlipAnimation.Elapsed += dt;
                if (Ui.FlipAnimation.Elapsed >= Ui.FlipAnimation.Duration)
                {
                    Ui.FlipAnimation = null;
                    Ui.Shake = Ui.Profile.Options.ScreenShake ? .3 : 0;
                    if (Game.CanReroll(game)) Ui.Deciding = true; // wait for the player; no auto-resolve
                    else Ui.ResolveTimer = .5;
                }
            }
        }
    }
}
