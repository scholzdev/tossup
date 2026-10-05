using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    // The Lua title screen: show the original three-coin scene behind the menu panel on the left.
    public static class TitleView
    {
        public static void Draw()
        {
            var scene = Ui.UiImages["title_scene"];
            Color(C.White);
            float sceneScale = Math.Max(Ui.Width / scene.Width, Ui.Height / scene.Height);
            Gfx.Draw(scene, (Ui.Width - scene.Width * sceneScale) / 2,
                (Ui.Height - scene.Height * sceneScale) / 2, sceneScale, sceneScale);
            Color(C.Ink, .88f);
            Gfx.Rectangle(true, 40, 24, Ui.Width / 2, 752, 10);
            Outline(40, 24, Ui.Width / 2, 752, C.Gold, 10);
            var logo = Ui.UiImages["logo"];
            Color(C.White);
            Gfx.Draw(logo, 165, 38, 560f / logo.Width, 560f / logo.Width);
            Centered("BEAT THE QUOTA", 40, 214, Ui.Width / 2, Ui.F32, C.Muted);

            var entries = new List<(string, Rgba, Action)>();
            Action newRun = () =>
            {
                if (Ui.Game != null || A.HasSavedRun())
                {
                    Ui.Confirm = new Confirm
                    {
                        Title = "NEW RUN", Text = "YOUR SAVED RUN WILL BE REPLACED.",
                        Ok = () => { if (A.HasSavedRun()) A.DeleteRun(); A.Play(); },
                    };
                }
                else A.Play();
            };
            if (Ui.Game != null)
            {
                entries.Add(("CONTINUE", C.Blue, () => Ui.Game.Paused = false));
                entries.Add(("NEW RUN", C.Gold, newRun));
            }
            else if(A.HasSavedRun())
            {
                entries.Add(("CONTINUE",C.Blue,()=>A.LoadRun()));
                entries.Add(("NEW RUN",C.Gold,newRun));
            }
            else entries.Add((RuntimeMode.Sandbox&&Ui.SandboxConfig!=null?"RELOAD SANDBOX":"PLAY", C.Blue, A.Play));
            entries.Add(("COIN SETS", C.Gold, () => A.OpenSets(Ui.SelectedCharacter)));
            entries.Add(("COLLECTION", C.Green, () => A.Go("collection")));
            var small = new List<(string, Rgba, Action)>
            {
                ("TUTORIAL", C.Green, A.StartTutorial),
                ("HELP", C.Green, () => { Ui.HelpNext = null; A.Go("help"); }),
                ("OPTIONS", C.PanelLight, () => A.Go("options")),
            };
            Text("v" + BuildInfo.Number + (RuntimeMode.Dev ? ".dev" : "") + " (" + BuildInfo.Build + ")", 80, 750, Ui.F16, C.Muted);
            float y = 260;
            for (int i = 0; i < entries.Count; i++)
            {
                Button(entries[i].Item1, 80, y, 730, 70, entries[i].Item2, entries[i].Item3, face: Ui.F48);
                y += 82;
            }
            y += 10;
            for (int i = 0; i < small.Count; i++)
                Button(small[i].Item1, 80 + i * 250, y, 230, 56, small[i].Item2, small[i].Item3, face: Ui.F32);
            Button("QUIT", 80, y + 74, 730, 64, C.Red, A.Quit, face: Ui.F48);
        }
    }

    // Play screen: pick a character and one of its coin sets, then start. Sets are edited in the Coin Sets screen.
    public static class SelectView
    {
        const float SlotSize = 100, Gap = 40;

        public static void Draw()
        {
            Frame(Title("play"), "BACK", () => A.Go("title"));

            string characterId = Ui.SelectedCharacter;
            bool locked=!Profile.CharacterUnlocked(Ui.Profile,characterId);
            int active = Profile.Active(Ui.Profile, characterId);
            var set = Profile.Sets(Ui.Profile, characterId)[active - 1];

            // left: who you are, with arrows to change character
            Button("<", 89, 160, 58, 48, C.Green, () => A.CycleCharacter(-1));
            Box(157, 160, 349, 48, C.PanelDk);
            Outline(157, 160, 349, 48, C.Line);
            Centered(Lang.Upper(Lang.CharacterName(characterId)), 157, 170, 349, Ui.F32, C.Face);
            Button(">", 516, 160, 58, 48, C.Green, () => A.CycleCharacter(1));
            Box(89, 220, 486, 400, C.Card);
            Outline(89, 220, 486, 400, C.Line);
            var portrait = Ui.CharacterImages[characterId];
            float scale = Math.Min(450f / portrait.Width, 380f / portrait.Height);
            float width = portrait.Width * scale, height = portrait.Height * scale;
            Color(C.White, locked ? .22f : 1f);
            Gfx.Draw(portrait, 89 + (486 - width) / 2, 226 + (388 - height) / 2, scale, scale);
            if(locked){Centered("LOCKED",89,440,486,Ui.F32,C.Face);int ci=Content.CharacterOrder.IndexOf(characterId);string required=ci>0?Lang.CharacterName(Content.CharacterOrder[ci-1]):"";Centered("WIN A RUN WITH: "+Lang.Upper(required),89,632,486,Ui.F16,C.Orange);}
            else {Centered(Lang.Upper(Lang.CharacterDescription(characterId)), 89, 632, 486, Ui.F16, C.Muted);if(Ui.Profile.BestEndless.TryGetValue(characterId,out var best))Centered("BEST ENDLESS: "+best,89,596,486,Ui.F16,C.Gold);}

            // right: the coin set you will play
            Box(613, 160, 932, 460, C.PanelDk);
            Outline(613, 160, 932, 460, C.Line);
            Centered("COIN SET", 613, 174, 932, Ui.F16, C.Gold);
            Button("<", 645, 200, 63, 44, C.PanelLight, () => A.CycleActiveSet(-1));
            Box(721, 200, 638, 44, C.Card);
            var coins = A.Loadout();
            Centered(L("%s  /  %d COINS", set.Name, coins.Count), 721, 210, 638, Ui.F20, C.Face);
            Button(">", 1372, 200, 63, 44, C.PanelLight, () => A.CycleActiveSet(1));

            const int columns = 5;
            float x0 = 613 + (932 - (columns * (SlotSize + Gap) - Gap)) / 2;
            for (int i = 0; i < Game.StartMax; i++)
            {
                float x = x0 + (i % columns) * (SlotSize + Gap);
                float y = 270 + (i / columns) * (SlotSize + Gap + 4);
                Box(x - 6, y - 6, SlotSize + 12, SlotSize + 12, C.SlotDk);
                Outline(x - 6, y - 6, SlotSize + 12, SlotSize + 12, C.Line);
                if (i < coins.Count)
                {
                    CoinImage(coins[i], x, y, SlotSize);
                    CoinHover(coins[i], x, y, SlotSize, SlotSize);
                }
            }
            if (set.Coins.Count == 0) Centered("THIS SET IS EMPTY  -  THE DEFAULT DECK IS USED", 613, 490, 932, Ui.F16, C.Orange);
            int stake=A.Stake(),top=Profile.MaxStake(Ui.Profile,characterId);
            Button("<",645,486,63,52,C.PanelLight,()=>A.CycleStake(-1),stake>1&&!locked);
            Centered("STAGE "+stake+" / "+Game.Stakes.Count,721,490,638,Ui.F20,stake==top?C.Gold:C.Face);
            Centered(Game.Stakes[stake-1].Text,721,518,638,Ui.F16,C.Muted);
            Button(">",1372,486,63,52,C.PanelLight,()=>A.CycleStake(1),stake<top&&!locked);
            Button("EDIT COIN SETS", 853, 555, 451, 52, C.Gold, () => A.OpenSets(Ui.SelectedCharacter),!locked);

            IconButton("START RUN", Ui.UiImages["start_level"], 595, 660, 430, 68, C.Green, () => A.Start(),!locked);
        }
    }

    // Coin Sets: every character with all of its coins. Build up to three sets of up to 5 coins per
    // character. Locked coins are unlocked by buying them in the shop during a run.
    public static class SetsView
    {
        const float SlotSize = 80, SlotGap = 22;
        static readonly Dictionary<string, string> Labels = new Dictionary<string, string> { { "blade", "BLADE" }, { "seer", "SEER" }, { "trader", "TRADER" } };
        static readonly Dictionary<string, int> RarityRank = new Dictionary<string, int> { { "N", 1 }, { "R", 2 }, { "SR", 3 }, { "UR", 4 } };

        static void Padlock(float cx, float cy)
        {
            Color(C.Face);
            Gfx.SetLineWidth(3);
            Gfx.ArcLine(cx, cy - 2, 6, (float)Math.PI, (float)(2 * Math.PI));
            Gfx.SetLineWidth(1);
            Gfx.Rectangle(true, cx - 9, cy - 2, 23, 14, 2);
            Color(C.Ink);
            Gfx.Circle(true, cx, cy + 5, 2);
        }

        public static void Draw()
        {
            Frame(Title("sets"), "BACK", A.BackFromSets);

            // every character
            for (int i = 0; i < Content.CharacterOrder.Count; i++)
            {
                string id = Content.CharacterOrder[i];
                Button(L(Labels[id]), 430 + i * 266, 148, 240, 44, id == Ui.SetsCharacter ? C.Gold : C.PanelLight,
                    () => A.SetsPickCharacter(id), Profile.CharacterUnlocked(Ui.Profile, id));
            }

            string characterId = Ui.SetsCharacter;
            var def = Content.Characters[characterId];
            var sets = Profile.Sets(Ui.Profile, characterId);
            var coins = A.SetDraftCoins();
            bool dirty = A.SetDirty();

            // left: the set being edited
            Box(89, 212, 544, 528, C.PanelDk);
            Outline(89, 212, 544, 528, C.Line);
            for (int i = 1; i <= Profile.SetCount; i++)
            {
                int index = i;
                Button(sets[i - 1].Name, 106 + (i - 1) * 170, 226, 159, 40, Ui.SetsIndex == i ? C.Blue : C.PanelLight, () => A.SetsPickSet(index));
            }
            Centered(L("%d / %d COINS", coins.Count, Game.StartMax), 89, 280, 544, Ui.F20, C.Gold);
            float x0 = 89 + (544 - (5 * (SlotSize + SlotGap) - SlotGap)) / 2;
            for (int i = 0; i < Game.StartMax; i++)
            {
                float x = x0 + (i % 5) * (SlotSize + SlotGap);
                float y = 322 + (i / 5) * (SlotSize + SlotGap + 8);
                Box(x - 4, y - 4, SlotSize + 8, SlotSize + 8, C.SlotDk);
                Outline(x - 4, y - 4, SlotSize + 8, SlotSize + 8, C.Line);
                if (i >= coins.Count) continue;
                CoinImage(coins[i], x, y, SlotSize);
                CoinHover(coins[i], x, y, SlotSize, SlotSize);
                int slot = i;
                AddButton(x, y, SlotSize, SlotSize, () => A.RemoveCoinFromSet(slot));
            }
            Centered(dirty ? "UNSAVED CHANGES" : "CLICK A COIN HERE TO REMOVE IT", 89, 496, 544, Ui.F16, dirty ? C.Orange : C.Muted);
            Button(dirty ? "SAVE SET" : "SAVED", 127, 530, 468, 48, dirty ? C.Blue : C.PanelLight, A.SaveSet, dirty);
            Button("CLEAR SET", 127, 592, 468, 44, C.Red, A.ClearSet, coins.Count > 0);
            Centered("PER SET: COMMON 3  -  UNCOMMON 2", 89, 680, 544, Ui.F16, C.Muted);
            Centered("RARE 1  -  EPIC 1", 89, 704, 544, Ui.F16, C.Muted);

            // right: all of this character's coins
            Box(658, 212, 886, 528, C.PanelDk);
            Outline(658, 212, 886, 528, C.Line);
            Centered(L("%s  -  CLICK A COIN TO ADD IT  -  LOCKED COINS COME FROM THE SHOP", Lang.Upper(Lang.CharacterName(characterId))),
                658, 226, 886, Ui.F16, C.Gold);
            var entries = new List<(string id, bool locked, int order)>();
            foreach (var coin in def.Pool) entries.Add((coin.Id, false, entries.Count));
            foreach (var entry in def.Locked) entries.Add((entry.Id, !Profile.IsUnlocked(Ui.Profile, characterId, entry.Id), entries.Count));
            entries.Sort((a, b) =>
            {
                int rankA = RarityRank.TryGetValue(DefinitionKeys.RarityCode(Content.Coins[a.id].Rarity), out int aRank) ? aRank : 9;
                int rankB = RarityRank.TryGetValue(DefinitionKeys.RarityCode(Content.Coins[b.id].Rarity), out int bRank) ? bRank : 9;
                return rankA != rankB ? rankA.CompareTo(rankB) : a.order.CompareTo(b.order);
            });
            const int columns = 8;
            const float step = 104;
            float gx = 658 + (886 - ((columns - 1) * step + SlotSize)) / 2;
            for (int i = 0; i < entries.Count; i++)
            {
                var (id, locked, _) = entries[i];
                float x = gx + (i % columns) * step;
                float y = 262 + (i / columns) * (SlotSize + 40);
                CoinImage(id, x, y, SlotSize);
                if (locked)
                {
                    Color(C.SlotDk, .7f);
                    Gfx.Circle(true, x + SlotSize / 2, y + SlotSize / 2, SlotSize / 2);
                    Padlock(x + SlotSize / 2, y + SlotSize / 2 - 4);
                    Centered("SHOP", x, y + SlotSize + 2, SlotSize, Ui.F16, C.Muted);
                }
                else
                {
                    int n = 0;
                    foreach (var owned in coins) if (owned == id) n++;
                    Centered(n > 0 ? "x" + n : "", x, y + SlotSize + 2, SlotSize, Ui.F16, C.Green);
                    AddButton(x, y, SlotSize, SlotSize, () => A.AddCoinToSet(id));
                }
                CoinHover(id, x, y, SlotSize, SlotSize, null, locked);
            }
        }
    }

    public static class CollectionView
    {
        const int Columns = 5, Rows = 3, PerPage = Columns * Rows;
        const float CellW = 224, Icon = 108;

        // rarity code (in the coin data) -> display name and tab colour
        static readonly (string key, string label, Rgba fill)[] Tabs =
        {
            ("ALL", "All", new Rgba(.13f, .27f, .50f)),
            ("N", "Common", new Rgba(.15f, .19f, .20f)),
            ("R", "Uncommon", new Rgba(.07f, .24f, .20f)),
            ("SR", "Rare", new Rgba(.28f, .16f, .04f)),
            ("UR", "Epic", new Rgba(.20f, .05f, .14f)),
        };
        static readonly Dictionary<string, int> Rank = new Dictionary<string, int> { { "N", 1 }, { "R", 2 }, { "SR", 3 }, { "UR", 4 } };
        static readonly (string key, string label)[] Sorts = { ("rarity", "Rarity"), ("name", "Name"), ("order", "Default") };

        static string SortLabel()
        {
            foreach (var s in Sorts) if (s.key == Ui.CollectionSort) return s.label;
            return "";
        }

        static void CycleSort()
        {
            for (int i = 0; i < Sorts.Length; i++)
            {
                if (Sorts[i].key != Ui.CollectionSort) continue;
                Ui.CollectionSort = Sorts[(i + 1) % Sorts.Length].key;
                Ui.CollectionPage = 1;
                return;
            }
        }

        static List<string> VisibleIds()
        {
            var ids = new List<(string id, int index)>();
            for (int index = 0; index < Content.CoinOrder.Count; index++)
            {
                string id = Content.CoinOrder[index].Id;
                if (Ui.CollectionFilter == "ALL" || DefinitionKeys.RarityCode(Content.Coins[id].Rarity) == Ui.CollectionFilter) ids.Add((id, index));
            }
            ids.Sort((a, b) =>
            {
                if (Ui.CollectionSort == "rarity")
                {
                    int ra = Rank.TryGetValue(DefinitionKeys.RarityCode(Content.Coins[a.id].Rarity), out int x) ? x : 9;
                    int rb = Rank.TryGetValue(DefinitionKeys.RarityCode(Content.Coins[b.id].Rarity), out int y) ? y : 9;
                    if (ra != rb) return ra.CompareTo(rb);
                }
                else if (Ui.CollectionSort == "name")
                {
                    int byName = string.CompareOrdinal(Content.Coins[a.id].Name, Content.Coins[b.id].Name);
                    if (byName != 0) return byName;
                }
                return a.index.CompareTo(b.index);
            });
            var list = new List<string>();
            foreach (var entry in ids) list.Add(entry.id);
            return list;
        }

        // A dark pill with light text (the shared Button draws dark text, which is unreadable here).
        static void Pill(string label, float x, float y, float w, float h, Rgba fill, Action action, bool selected = false)
        {
            Ui.Mouse(out float mx, out float my);
            bool hover = mx >= x && mx <= x + w && my >= y && my <= y + h;
            Box(x, y + (hover ? -2 : 0), w, h, fill);
            if (selected) Outline(x, y, w, h, C.White);
            Centered(label, x, y + (h - Ui.F20.Height) / 2f + (hover ? -2 : 0), w, Ui.F20, C.White);
            AddButton(x, y, w, h, action);
        }

        public static void Draw()
        {
            Frame(Title("collection"), "BACK", () => A.Go("title"));

            // sort + rarity filters
            Text("SORT", 89, 168, Ui.F16, C.Muted);
            Pill(SortLabel(), 152, 156, 152, 40, Tabs[0].fill, CycleSort);
            Color(C.White);
            Gfx.Rectangle(true, 321, 152, 3.8f, 48);
            for (int i = 0; i < Tabs.Length; i++)
            {
                var tab = Tabs[i];
                float x = 347 + i * 159;
                bool on = Ui.CollectionFilter == tab.key;
                Pill(tab.label, x, 156 + (on ? 3 : 0), 147, 40, tab.fill, () => A.SetFilter(tab.key), on);
            }

            // coin grid, five across
            var ids = VisibleIds();
            int pages = Math.Max(1, (int)Math.Ceiling(ids.Count / (double)PerPage));
            Ui.CollectionPage = Math.Min(Ui.CollectionPage, pages);
            int first = (Ui.CollectionPage - 1) * PerPage;
            float x0 = Ui.Width / 2 - Columns * CellW / 2;
            for (int slot = 0; slot < PerPage; slot++)
            {
                if (first + slot >= ids.Count) break;
                string id = ids[first + slot];
                float x = x0 + (slot % Columns) * CellW;
                float y = 224 + (slot / Columns) * 154;
                bool collected = Ui.Profile.Collected.Contains(id);
                var image = Ui.CoinImages[id];
                if (collected) Color(C.White);
                else Gfx.SetColor(0, 0, 0, .8f);
                Gfx.Draw(image, x + (CellW - Icon) / 2, y, Icon / image.Width, Icon / image.Height);
                Box(x + 11, y + 112, CellW - 22, 32, C.Card);
                Outline(x + 11, y + 112, CellW - 22, 32, C.Line);
                Centered(collected ? Lang.CoinName(id) : "Uncollected", x + 11, y + 118, CellW - 22, Ui.F20, collected ? C.White : C.Muted);
                if (collected) CoinHover(id, x + (CellW - Icon) / 2, y, Icon, 144);
            }

            // paging inside the frame
            bool canPrev = Ui.CollectionPage > 1, canNext = Ui.CollectionPage < pages;
            Button("<", 89, 380, 63, 90, C.Green, () => A.ChangeCollectionPage(-1), canPrev);
            Button(">", 1468, 380, 63, 90, C.Green, () => A.ChangeCollectionPage(1), canNext);
            Centered(Ui.CollectionPage + "/" + pages, 557, 700, 506, Ui.F32, C.White);
            Centered(L("COLLECTED %d / %d", Ui.Profile.Collected.Count, Content.CoinOrder.Count), 1114, 710, 405, Ui.F16, C.Muted);
        }
    }

    // Options: game settings, sound levels, and the keyboard/controller reference.
    public static class OptionsView
    {
        static readonly (string key, string label, string hint)[] Rows =
        {
            ("screen_shake", "SCREEN SHAKE", "Shake the screen when a coin lands."),
            ("fast_flip", "FAST FLIP", "Half-length coin flip animation."),
            ("fullscreen", "FULLSCREEN", "Switch between windowed and fullscreen."),
        };
        static readonly (string key, string label, string hint)[] Sliders =
        {
            ("volume_master", "MASTER VOLUME", "Everything at once."),
            ("volume_music", "MUSIC", "The background loop."),
            ("volume_sfx", "SOUND EFFECTS", "Flips, scores, buttons."),
        };
        static readonly (string label, string[] keys)[] KeyboardControls =
        {
            ("MENUS", Array.Empty<string>()),
            ("Move the focus", new[] { "ARROWS" }),
            ("Press the focused button", new[] { "ENTER" }),
            ("Back, close, menu", new[] { "ESC" }),
            ("Previous / next page, tab, character", new[] { "Q", "E" }),
            ("Choose a character (play screen)", new[] { "1", "2", "3" }),
            ("IN A LEVEL", Array.Empty<string>()),
            ("Flip / next coin", new[] { "SPACE" }),
            ("Inspect: show the details of the focused item", new[] { "I", "Q", "E" }),
            ("Use chip 1 / 2 / 3", new[] { "1", "2", "3" }),
            ("Open the shop (quota met)", new[] { "O" }),
            ("Debug info", new[] { "F3" }),
        };
        static readonly (string label, string[] keys)[] ControllerControls =
        {
            ("MENUS", Array.Empty<string>()),
            ("Move the focus", new[] { "D-PAD", "STICK" }),
            ("Press the focused button", new[] { "A" }),
            ("Back, close, menu", new[] { "B", "START" }),
            ("Previous / next page, tab, character", new[] { "LB", "RB", "LT", "RT" }),
            ("IN A LEVEL", Array.Empty<string>()),
            ("Flip / next coin", new[] { "X" }),
            ("Inspect: show the details of the focused item", new[] { "LB", "RB", "LT", "RT" }),
            ("Use a chip or open the shop", new[] { "D-PAD", "A" }),
            ("Menu", new[] { "START" }),
        };

        static void Panel(float y)
        {
            Box(354, y, 911, 78, C.PanelDk);
            Outline(354, y, 911, 78, C.Line);
        }

        // A slider is a clickable strip: pressing or dragging sets the value from the mouse x; releasing saves it.
        static void Slider(string key, string label, string hint, float y)
        {
            Panel(y);
            Text(label, 390, y + 10, Ui.F32, C.Face);
            Text(hint, 390, y + 46, Ui.F16, C.Muted);
            const float x = 759, w = 354;
            float value = (float)Ui.Profile.Options.GetVolume(key);
            Box(x, y + 30, w, 16, C.SlotDk, 8);
            Color(C.Gold);
            Gfx.Rectangle(true, x, y + 30, w * value / 100, 16, 8);
            Box(x + w * value / 100 - 8, y + 24, 20, 28, C.White, 4);
            Text(GameText.Num(value) + "%", 1154, y + 24, Ui.F32, C.Gold);
            Ui.Buttons.Add(new Button
            {
                X = x - 10, Y = y + 16, W = w + 20, H = 44, Action = () => { },
                Drag = mouseX => A.SetVolume(key, (mouseX - x) / w * 100),
                Adjust = direction =>
                {
                    A.SetVolume(key, Ui.Profile.Options.GetVolume(key) + direction * 5);
                    A.SaveOptions();
                    Sound.Play("score");
                },
                Release = () => { A.SaveOptions(); Sound.Play("score"); },
            });
        }

        static void DrawControls()
        {
            string view = Ui.ControlsView ?? PadNavigation.Device;
            bool controller = view == "controller";
            Button("KEYBOARD", 354, 198, 443, 40, !controller ? C.Gold : C.PanelLight, () => Ui.ControlsView = "keyboard");
            Button("CONTROLLER", 823, 198, 443, 40, controller ? C.Gold : C.PanelLight, () => Ui.ControlsView = "controller");
            var rows = controller ? ControllerControls : KeyboardControls;
            float y = 252;
            foreach (var row in rows)
            {
                if (row.keys.Length == 0)
                {
                    y += 6;
                    Text(row.label, 367, y, Ui.F20, C.Gold);
                    y += 30;
                    continue;
                }

                Text(row.label, 390, y + 6, Ui.F16, C.Face);
                float x = 886;
                foreach (var key in row.keys)
                {
                    Rgba tint = key == "A" ? C.Green : key == "B" ? C.Red : key == "X" ? C.Blue : C.PanelLight;
                    float width = Ui.F16.GetWidth(key) + 22;
                    Box(x, y, width, 28, tint, 5);
                    Outline(x, y, width, 28, C.Face, 5);
                    Text(key, x + 11, y + 6, Ui.F16, C.Ink);
                    x += width + 8;
                }
                y += 36;
            }
        }

        public static void Draw()
        {
            Frame(Title("options"), "BACK", () => A.Go("title"));
            Button("GAME", 354, 146, 291, 44, Ui.OptionsTab == "game" ? C.Gold : C.PanelLight, () => Ui.OptionsTab = "game");
            Button("SOUND", 664, 146, 291, 44, Ui.OptionsTab == "sound" ? C.Gold : C.PanelLight, () => Ui.OptionsTab = "sound");
            Button("CONTROLS", 975, 146, 291, 44, Ui.OptionsTab == "controls" ? C.Gold : C.PanelLight, () => Ui.OptionsTab = "controls");

            if (Ui.OptionsTab == "controls") DrawControls();
            else if (Ui.OptionsTab == "sound")
            {
                for (int i = 0; i < Sliders.Length; i++) Slider(Sliders[i].key, Sliders[i].label, Sliders[i].hint, 214 + i * 96);
                Button("RESTORE DEFAULTS", 557, 510, 506, 48, C.PanelLight, () =>
                {
                    A.RestoreSoundDefaults();
                    Sound.Play("score");
                });
            }
            else
            {
                for (int i = 0; i < Rows.Length; i++)
                {
                    var row = Rows[i];
                    float y = 214 + i * 86;
                    bool on = Ui.Profile.Options.GetSwitch(row.key);
                    Panel(y);
                    Text(row.label, 390, y + 10, Ui.F32, C.Face);
                    Text(row.hint, 390, y + 46, Ui.F16, C.Muted);
                    Button(on ? "ON" : "OFF", 1088, y + 16, 142, 46, on ? C.Green : C.PanelLight, () => A.ToggleOption(row.key));
                }
                // language: the button shows the current language's own name
                float ly = 214 + Rows.Length * 86;
                Panel(ly);
                Text("LANGUAGE", 390, ly + 10, Ui.F32, C.Face);
                Text("Language of all texts.", 390, ly + 46, Ui.F16, C.Muted);
                Button(Lang.Names[Lang.Current], 1038, ly + 16, 192, 46, C.Gold, A.CycleLanguage);
                // clear progress: opens a confirmation popup
                ly += 86;
                Panel(ly);
                Text("CLEAR PROGRESS", 390, ly + 10, Ui.F32, C.Face);
                Text("Resets unlocks, collection, sets and tokens. Options stay.", 390, ly + 46, Ui.F16, C.Muted);
                Button("CLEAR", 1038, ly + 16, 192, 46, C.Red, A.ClearProgress);
            }
            if (Ui.OptionsTab != "controls") Centered("F3 SHOWS DEBUG INFO IN A RUN", 0, 715, 1620, Ui.F16, C.Muted);
        }
    }

    // How to play: one screen with the whole loop. Shown once on the first Play, and from the title menu.
    public static class HelpView
    {
        static readonly (string title, string body)[] Sections =
        {
            ("THE GOAL", "Beat the quota before your coins run out. Bad flips can raise it. A run has eight levels: one Encounter changes the run, and Augments appear before levels 3 and 6. The House is the final level."),
            ("YOUR COINS", "You play with a small stack of coins. Each coin has a Heads chance and a Heads and a Tails effect. Every coin is played once per level; nothing is reshuffled."),
            ("EACH LEVEL", "The entire remaining coin bank stays visible. Click any coin to choose what to play next; choosing is free. Coins with an energy cost spend it when flipped."),
            ("ENERGY", "Strong coins cost ENERGY to flip (shown as E1, E2). You get 3 per level; Spark, Copper and Flux Capacitor give more. If you cannot pay, choose a different coin. Your last coin can still flip for up to 2 gold."),
            ("QUOTA MET", "You are paid gold at once and the level stays open: every 2 extra points pay 1 more gold. Press OPEN SHOP (top right) when you want to move on."),
            ("OUT OF COINS", "If the quota is not met, pay gold to EXCHANGE: 3 of your played coins come back (only a limited number of times per level). If you cannot, the run is over."),
            ("THE SHOP", "Buy COINS (larger and stronger decks raise the next quota), CHIPS (one-use helpers, used mid-level), and a PRIZE (lasts the run). Odds Tuner adds Heads chance; Coin Removal drops a weak coin."),
        };

        public static void Draw()
        {
            Frame(Title("help"), "BACK", () => A.Go("title"));
            for (int i = 0; i < Sections.Length; i++)
            {
                int col = i % 2, row = i / 2;
                float x = 89 + col * 734, y = 160 + row * 128;
                Box(x, y, 709, 116, C.PanelDk);
                Outline(x, y, 709, 116, C.Line);
                Text(Sections[i].title, x + 16, y + 10, Ui.F20, C.Gold);
                Gfx.SetFont(Ui.F16);
                Color(C.Face);
                Gfx.Printf(L(Sections[i].body), x + 16, y + 38, 668);
            }
            // the 8th cell of the grid: controls
            Box(823, 544, 709, 116, C.PanelDk);
            Outline(823, 544, 709, 116, C.Line);
            Text("CONTROLS", 843, 554, Ui.F20, C.Gold);
            Gfx.SetFont(Ui.F16);
            Color(C.Face);
            Gfx.Printf(L("Space = Flip / Next Coin.  Click = select a coin.  Esc = menu (your run waits).  Hover a coin for details. The run is saved at each level start and in the shop; Continue resumes it, a level in progress restarts."),
                843, 582, 668);
            if (Ui.HelpNext != null)
            {
                string next = Ui.HelpNext;
                IconButton("GOT IT", Ui.UiImages["start_level"], 595, 684, 430, 56, C.Green, () => A.Go(next));
            }
        }
    }

    // End of run: full-screen like the shop, with the outcome, what you reached and a way back in.
    public static class FinishView
    {
        public static void Draw()
        {
            var g = Ui.Game;
            bool won = g.Phase == Phase.Victory;
            Box(0, 0, 1620, 800, C.FeltDark);
            Box(46, 36, 1529, 728, C.Screen);
            Outline(46, 36, 1529, 728, C.Gold);

            // headline, drawn at double size in the outcome colour with a shadow
            string headline = L(g.Endless ? "ENDLESS RUN OVER" : won ? "THE HOUSE FALLS" : "RUN OVER");
            Gfx.SetFont(Ui.F48);
            float width = Ui.F48.GetWidth(headline) * 2;
            Color(C.Black, .35f);
            Gfx.Print(headline, (float)Math.Floor(Ui.Width / 2 - width / 2) + 4, 90 + 4, 2, 2);
            Color(won ? C.Green : C.Red);
            Gfx.Print(headline, (float)Math.Floor(Ui.Width / 2 - width / 2), 90, 2, 2);

            // the character and their starting coin
            Box(481, 206, 291, 300, C.PanelDk);
            Outline(481, 206, 291, 300, C.Line);
            var portrait = Ui.CharacterImages[g.CharacterId];
            float scale = Math.Min(210f / portrait.Width, 240f / portrait.Height);
            Color(C.White);
            Gfx.Draw(portrait, 481 + (291 - portrait.Width * scale) / 2, 214 + (250 - portrait.Height * scale) / 2, scale, scale);
            Centered(Lang.Upper(Lang.CharacterName(g.CharacterId)), 481, 476, 291, Ui.F20, C.Gold);

            // what the run reached
            Box(823, 206, 316, 300, C.PanelDk);
            Outline(823, 206, 316, 300, C.Line);
            Centered(g.Endless ? "ENDLESS MODE" : won ? "YOU WON THE RUN" : "TRY A NEW SET", 823, 222, 316, Ui.F20, C.Face);
            Centered((g.Endless ? g.Cleared - Game.Route.Count : g.Cleared).ToString(), 823, 276, 316, Ui.F48, won ? C.Green : C.Gold);
            Centered(g.Endless ? "ENDLESS LEVELS CLEARED" : "LEVELS CLEARED OF " + Game.Route.Count, 823, 336, 316, Ui.F16, C.Muted);
            ImageAt(Ui.UiImages["gold"], 873, 386, 44);
            Text(GameText.Num(g.Player.Gold), 944, 392, Ui.F32, C.Gold);
            Text("GOLD LEFT", 873, 440, Ui.F16, C.Muted);
            Text(L("SEED %s", g.Seed), 873, 468, Ui.F16, C.Muted);
            if (g.EndlessRecord) Centered("NEW RECORD!", 481, 514, 658, Ui.F20, C.Gold);
            if (won && !g.Endless)
            {
                float y = 514;
                if (g.UnlockedCharacter != null)
                {
                    Centered(L("NEW CHARACTER UNLOCKED: %s", Lang.Upper(Lang.CharacterName(g.UnlockedCharacter))), 481, y, 658, Ui.F20, C.Gold);
                    y += 32;
                }
                if (g.UnlockedStake.HasValue) Centered(L("STAGE %d UNLOCKED", g.UnlockedStake.Value), 481, y, 658, Ui.F20, C.Gold);
            }
            if (!won && g.LostWhy != null)
            {
                Gfx.SetFont(Ui.F16);
                Color(C.Red);
                string why = L(g.LostWhy);
                string capitalized = why.Length == 0 ? why : Lang.Upper(why.Substring(0, 1)) + why.Substring(1);
                Gfx.Printf(capitalized, 481, g.EndlessRecord ? 546 : 520, 658, Align.Center);
            }

            if (won && !g.Endless)
            {
                // the boss fell: keep going through endless levels or leave for the menu
                IconButton("ENDLESS MODE", Ui.UiImages["next_coin"], 595, 584, 430, 60, C.Gold, A.ContinueEndless);
                IconButton("BACK TO MENU", Ui.UiImages["give_up"], 595, 652, 430, 56, C.Blue, A.OpenMenu);
                Button("NEW RUN", 658, 718, 304, 36, C.Green, () => A.Start());
            }
            else
            {
                IconButton("NEW RUN", Ui.UiImages["start_level"], 595, 600, 430, 64, C.Green, () => A.Start());
                Button("BACK TO MENU", 658, 686, 304, 44, C.PanelLight, A.OpenMenu);
            }
            Button("MENU", 1418, 56, 127, 34, C.PanelLight, A.OpenMenu);
        }
    }
}
