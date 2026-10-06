using System;
using System.Collections.Generic;
using static Tossup.UI.D;

namespace Tossup.UI
{
    // The Lua title screen: show the original three-coin scene behind the menu panel on the left.
    public static class TitleView
    {
        static int versionClicks;
        static double lastVersionClick;

        public static bool HandleVersionClick(float x, float y)
        {
            if (Ui.Screen != "title" || Ui.Confirm != null)
            {
                versionClicks = 0;
                return false;
            }

            string version = "v" + BuildInfo.Number + (RuntimeMode.Dev ? ".dev" : "") + " (" + BuildInfo.Build + ")";
            if (x < 88 || x > 104 + Ui.F16.GetWidth(version) || y < 618 || y > 650)
            {
                versionClicks = 0;
                return false;
            }

            double now = Ui.Platform.Time;
            if (versionClicks == 0 || now - lastVersionClick > 1.25) versionClicks = 0;
            lastVersionClick = now;
            if (++versionClicks == 3)
            {
                versionClicks = 0;
                A.ToggleDeveloperMode();
            }
            return true;
        }

        public static void Draw()
        {
            var scene = Ui.UiImages["title_scene"];
            Color(C.White);
            Gfx.Draw(scene, 0, 0, Ui.Width / scene.Width, Ui.Height / scene.Height);
            Color(C.Ink, .88f);
            Gfx.Rectangle(true, 70, 70, 400, 590, 10);
            Outline(70, 70, 400, 590, C.Gold);
            var logo = Ui.UiImages["logo"];
            Color(C.White);
            Gfx.Draw(logo, 120, 96, 300f / logo.Width, 300f / logo.Width);
            Centered("BEAT THE QUOTA", 70, 190, 400, Ui.F20, C.Muted);

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
            Text("v" + BuildInfo.Number + (RuntimeMode.Dev ? ".dev" : "") + " (" + BuildInfo.Build + ")", 96, 626, Ui.F16, C.Muted);
            float y = 240;
            for (int i = 0; i < entries.Count; i++)
            {
                Button(entries[i].Item1, 100, y, 340, 52, entries[i].Item2, entries[i].Item3);
                y += 62;
            }
            y += 10;
            for (int i = 0; i < small.Count; i++)
                Button(small[i].Item1, 100 + i * 114, y, 112, 44, small[i].Item2, small[i].Item3);
            Button("QUIT", 100, y + 62, 340, 44, C.Red, A.Quit);
        }
    }

    // Play screen: pick a character and one of its coin sets, then start. Sets are edited in the Coin Sets screen.
    public static class SelectView
    {
        const float SlotSize = 80, Gap = 22;

        public static void Draw()
        {
            Frame(Title("play"), "BACK", () => A.Go("title"));

            string characterId = Ui.SelectedCharacter;
            bool locked=!Profile.CharacterUnlocked(Ui.Profile,characterId);
            int active = Profile.Active(Ui.Profile, characterId);
            var set = Profile.Sets(Ui.Profile, characterId)[active - 1];

            // left: who you are, with arrows to change character
            Button("<", 70, 160, 46, 48, C.Green, () => A.CycleCharacter(-1));
            Box(124, 160, 276, 48, C.PanelDk);
            Outline(124, 160, 276, 48, C.Line);
            Centered(Lang.Upper(Lang.CharacterName(characterId)), 124, 170, 276, Ui.F32, C.Face);
            Button(">", 408, 160, 46, 48, C.Green, () => A.CycleCharacter(1));
            Box(70, 220, 384, 400, C.Card);
            Outline(70, 220, 384, 400, C.Line);
            var portrait = Ui.CharacterImages[characterId];
            float scale = Math.Min(350f / portrait.Width, 270f / portrait.Height);
            float width = portrait.Width * scale, height = portrait.Height * scale;
            Color(C.White, locked ? .22f : 1f);
            Gfx.Draw(portrait, 70 + (384 - width) / 2, 226 + (270 - height) / 2, scale, scale);
            var perks = Content.Characters[characterId].Perks;
            for (int i = 0; i < Math.Min(2, perks.Count); i++)
            {
                float perkY = 500 + i * 48;
                Box(84, perkY, 356, 42, C.PanelDk);
                Outline(84, perkY, 356, 42, C.Line);
                Text(Lang.Upper(Lang.T(perks[i].Name)), 94, perkY + 2, Ui.F16, C.Gold);
                Text(Lang.T(perks[i].Description), 94, perkY + 21, Ui.F16, C.Muted);
            }
            if(locked){Centered("LOCKED",70,440,384,Ui.F32,C.Face);int ci=Content.CharacterOrder.IndexOf(characterId);string required=ci>0?Lang.CharacterName(Content.CharacterOrder[ci-1]):"";Centered("WIN A RUN WITH: "+Lang.Upper(required),70,632,384,Ui.F16,C.Orange);}
            else {Centered(Lang.Upper(Lang.CharacterDescription(characterId)), 70, 632, 384, Ui.F16, C.Muted);if(Ui.Profile.BestEndless.TryGetValue(characterId,out var best))Centered("BEST ENDLESS: "+best,70,596,384,Ui.F16,C.Gold);}

            // right: the coin set you will play
            Box(484, 160, 736, 460, C.PanelDk);
            Outline(484, 160, 736, 460, C.Line);
            Centered("COIN SET", 484, 174, 736, Ui.F16, C.Gold);
            Button("<", 510, 200, 50, 44, C.PanelLight, () => A.CycleActiveSet(-1));
            Box(570, 200, 504, 44, C.Card);
            var coins = A.Loadout();
            Centered(L("%s  /  %d COINS", set.Name, coins.Count), 570, 210, 504, Ui.F20, C.Face);
            Button(">", 1084, 200, 50, 44, C.PanelLight, () => A.CycleActiveSet(1));

            const int columns = 5;
            float x0 = 484 + (736 - (columns * (SlotSize + Gap) - Gap)) / 2;
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
            if (set.Coins.Count == 0) Centered("THIS SET IS EMPTY  -  THE DEFAULT DECK IS USED", 484, 490, 736, Ui.F16, C.Orange);
            int stake=A.Stake(),top=Profile.MaxStake(Ui.Profile,characterId);
            Button("<",510,486,50,52,C.PanelLight,()=>A.CycleStake(-1),stake>1&&!locked);
            Centered("STAGE "+stake+" / "+Game.Stakes.Count,570,490,504,Ui.F20,stake==top?C.Gold:C.Face);
            Centered(Game.Stakes[stake-1].Text,570,518,504,Ui.F16,C.Muted);
            Button(">",1084,486,50,52,C.PanelLight,()=>A.CycleStake(1),stake<top&&!locked);
            Button("EDIT COIN SETS", 674, 555, 356, 52, C.Gold, () => A.OpenSets(Ui.SelectedCharacter),!locked);

            IconButton("START RUN", Ui.UiImages["start_level"], 470, 660, 340, 68, C.Green, () => A.Start(),!locked);
        }
    }

    // Coin Sets: every character with all of its coins. Build up to three sets of up to 5 coins per
    // character. Locked coins are unlocked by buying them in the shop during a run.
    public static class SetsView
    {
        const float SlotSize = 64, SlotGap = 14;
        static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            { "blade", "BLADE" }, { "seer", "SEER" }, { "trader", "TRADER" },
            { "tinkerer", "TINKERER" }, { "naturalist", "NATURALIST" }, { "conductor", "CONDUCTOR" }
        };
        static readonly Dictionary<string, int> RarityRank = new Dictionary<string, int> { { "N", 1 }, { "R", 2 }, { "SR", 3 }, { "UR", 4 } };

        static void Padlock(float cx, float cy)
        {
            Color(C.Face);
            Gfx.SetLineWidth(3);
            Gfx.ArcLine(cx, cy - 2, 6, (float)Math.PI, (float)(2 * Math.PI));
            Gfx.SetLineWidth(1);
            Gfx.Rectangle(true, cx - 9, cy - 2, 18, 14, 2);
            Color(C.Ink);
            Gfx.Circle(true, cx, cy + 5, 2);
        }

        public static void Draw()
        {
            Frame(Title("sets"), "BACK", A.BackFromSets);

            // every character
            const float tabLeft = 70, tabRight = 1220, tabGap = 8;
            float tabWidth = (tabRight - tabLeft - tabGap * (Content.CharacterOrder.Count - 1)) / Content.CharacterOrder.Count;
            for (int i = 0; i < Content.CharacterOrder.Count; i++)
            {
                string id = Content.CharacterOrder[i];
                Button(L(Labels[id]), tabLeft + i * (tabWidth + tabGap), 148, tabWidth, 44, id == Ui.SetsCharacter ? C.Gold : C.PanelLight,
                    () => A.SetsPickCharacter(id), Profile.CharacterUnlocked(Ui.Profile, id));
            }

            string characterId = Ui.SetsCharacter;
            var def = Content.Characters[characterId];
            var sets = Profile.Sets(Ui.Profile, characterId);
            var coins = A.SetDraftCoins();
            bool dirty = A.SetDirty();

            // left: the set being edited
            Box(70, 212, 430, 528, C.PanelDk);
            Outline(70, 212, 430, 528, C.Line);
            for (int i = 1; i <= Profile.SetCount; i++)
            {
                int index = i;
                Button(sets[i - 1].Name, 84 + (i - 1) * 134, 226, 126, 40, Ui.SetsIndex == i ? C.Blue : C.PanelLight, () => A.SetsPickSet(index));
            }
            Centered(L("%d / %d COINS", coins.Count, Game.StartMax), 70, 280, 430, Ui.F20, C.Gold);
            float x0 = 70 + (430 - (5 * (SlotSize + SlotGap) - SlotGap)) / 2;
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
            Centered(dirty ? "UNSAVED CHANGES" : "CLICK A COIN HERE TO REMOVE IT", 70, 496, 430, Ui.F16, dirty ? C.Orange : C.Muted);
            Button(dirty ? "SAVE SET" : "SAVED", 100, 530, 370, 48, dirty ? C.Blue : C.PanelLight, A.SaveSet, dirty);
            Button("CLEAR SET", 100, 592, 370, 44, C.Red, A.ClearSet, coins.Count > 0);
            Centered("PER SET: COMMON 3  -  UNCOMMON 2", 70, 680, 430, Ui.F16, C.Muted);
            Centered("RARE 1  -  EPIC 1", 70, 704, 430, Ui.F16, C.Muted);

            // right: all of this character's coins
            Box(520, 212, 700, 528, C.PanelDk);
            Outline(520, 212, 700, 528, C.Line);
            Centered(L("%s  -  CLICK A COIN TO ADD IT  -  LOCKED COINS COME FROM THE SHOP", Lang.Upper(Lang.CharacterName(characterId))),
                520, 226, 700, Ui.F16, C.Gold);
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
            const float step = 82;
            float gx = 520 + (700 - ((columns - 1) * step + SlotSize)) / 2;
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
        const float CellW = 170, Icon = 88;

        sealed class Entry
        {
            public string Id, Name, Description, ImageId;
            public int Rank, Index;
            public bool Available = true;
        }

        static readonly (string key, string label)[] Categories =
        {
            ("coins", "COINS"), ("items", "CHIPS"), ("relics", "RELICS"),
            ("characters", "CHARACTERS"),
        };

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
            (string key, string label)[] sorts = Ui.CollectionCategory == "coins" ? Sorts : new[] { ("name", "Name"), ("order", "Default") };
            for (int i = 0; i < sorts.Length; i++)
            {
                if (sorts[i].key != Ui.CollectionSort) continue;
                Ui.CollectionSort = sorts[(i + 1) % sorts.Length].key;
                Ui.CollectionPage = 1;
                return;
            }
            Ui.CollectionSort = sorts[0].key;
        }

        static List<Entry> VisibleEntries()
        {
            var entries = new List<Entry>();
            if (Ui.CollectionCategory == "coins")
            {
                for (int index = 0; index < Content.CoinOrder.Count; index++)
                {
                    var coin = Content.CoinOrder[index];
                    string rarity = DefinitionKeys.RarityCode(coin.Rarity);
                    if (Ui.CollectionFilter != "ALL" && rarity != Ui.CollectionFilter) continue;
                    entries.Add(new Entry { Id = coin.Id, Name = Lang.CoinName(coin.Id), ImageId = coin.Id,
                        Rank = Rank.TryGetValue(rarity, out int rank) ? rank : 9, Index = index,
                        Available = Ui.Profile.Collected.Contains(coin.Id) });
                }
            }
            else if (Ui.CollectionCategory == "items")
            {
                for (int index = 0; index < Content.ItemOrder.Count; index++)
                {
                    string id = Content.ItemOrder[index];
                    entries.Add(new Entry { Id = id, Name = Lang.ItemName(id), Description = Lang.ItemDescription(id), ImageId = id, Index = index });
                }
            }
            else if (Ui.CollectionCategory == "relics")
            {
                for (int index = 0; index < Content.RelicOrder.Count; index++)
                {
                    string id = Content.RelicOrder[index];
                    entries.Add(new Entry { Id = id, Name = Lang.RelicName(id), Description = Lang.RelicDescription(id), ImageId = id, Index = index });
                }
            }
            else if (Ui.CollectionCategory == "characters")
            {
                for (int index = 0; index < Content.CharacterOrder.Count; index++)
                {
                    string id = Content.CharacterOrder[index];
                    entries.Add(new Entry { Id = id, Name = Lang.CharacterName(id), Description = Lang.CharacterDescription(id),
                        ImageId = id, Index = index, Available = Profile.CharacterUnlocked(Ui.Profile, id) });
                }
            }
            entries.Sort((a, b) =>
            {
                if (Ui.CollectionCategory == "coins" && Ui.CollectionSort == "rarity" && a.Rank != b.Rank) return a.Rank.CompareTo(b.Rank);
                if (Ui.CollectionSort == "name")
                {
                    int byName = string.CompareOrdinal(a.Name, b.Name);
                    if (byName != 0) return byName;
                }
                return a.Index.CompareTo(b.Index);
            });
            return entries;
        }

        // A dark pill with light text (the shared Button draws dark text, which is unreadable here).
        static void Pill(string label, float x, float y, float w, float h, Rgba fill, Action action, bool selected = false)
        {
            Ui.Mouse(out float mx, out float my);
            bool hover = mx >= x && mx <= x + w && my >= y && my <= y + h;
            Box(x, y + (hover ? -2 : 0), w, h, fill);
            if (selected) Outline(x, y, w, h, C.White);
            Centered(label, x, y + (h - Ui.F20.Height) / 2f + (hover ? -2 : 0), w, Ui.F20, C.White);
            AddButton(x, y, w, h, action, label);
        }

        public static void Draw()
        {
            Frame(Title("collection"), "BACK", () => A.Go("title"));

            for (int i = 0; i < Categories.Length; i++)
            {
                var category = Categories[i];
                bool selected = Ui.CollectionCategory == category.key;
                Pill(L(category.label), 144 + i * 198, 146 + (selected ? 3 : 0), 184, 40,
                    selected ? new Rgba(.13f, .27f, .50f) : C.PanelLight,
                    () => { Ui.CollectionCategory = category.key; Ui.CollectionPage = 1; Ui.CollectionFilter = "ALL"; Ui.CollectionSort = category.key == "coins" ? "rarity" : "order"; }, selected);
            }

            // sort + rarity filters
            Text("SORT", 70, 211, Ui.F16, C.Muted);
            Pill(SortLabel(), 120, 198, 120, 40, Tabs[0].fill, CycleSort);
            if (Ui.CollectionCategory == "coins")
            {
                Color(C.White);
                Gfx.Rectangle(true, 254, 194, 3, 48);
                for (int i = 0; i < Tabs.Length; i++)
                {
                    var tab = Tabs[i];
                    float x = 274 + i * 126;
                    bool on = Ui.CollectionFilter == tab.key;
                    Pill(tab.label, x, 198 + (on ? 3 : 0), 116, 40, tab.fill, () => A.SetFilter(tab.key), on);
                }
            }

            // Five columns shared by every catalogue category.
            var entries = VisibleEntries();
            int pages = Math.Max(1, (int)Math.Ceiling(entries.Count / (double)PerPage));
            Ui.CollectionPage = Math.Min(Ui.CollectionPage, pages);
            int first = (Ui.CollectionPage - 1) * PerPage;
            float x0 = 640 - Columns * CellW / 2;
            for (int slot = 0; slot < PerPage; slot++)
            {
                if (first + slot >= entries.Count) break;
                var entry = entries[first + slot];
                float x = x0 + (slot % Columns) * CellW;
                float y = 260 + (slot / Columns) * 154;
                Img image = Ui.CollectionCategory == "items" ? Ui.ItemImages[entry.ImageId] :
                    Ui.CollectionCategory == "relics" ? Ui.RelicImages[entry.ImageId] :
                    Ui.CollectionCategory == "characters" ? Ui.CharacterImages[entry.ImageId] : Ui.CoinImages[entry.ImageId];
                if (entry.Available) Color(C.White);
                else Gfx.SetColor(0, 0, 0, .8f);
                Gfx.Draw(image, x + (CellW - Icon) / 2, y, Icon / image.Width, Icon / image.Height);
                Box(x + 11, y + 100, CellW - 22, 32, C.Card);
                Outline(x + 11, y + 100, CellW - 22, 32, C.Line);
                string cardName = entry.Available ? entry.Name : Ui.CollectionCategory == "coins" ? "Uncollected" : "Locked";
                Centered(cardName, x + 11, y + 106, CellW - 22, Ui.F20, entry.Available ? C.White : C.Muted);
                if (Ui.CollectionCategory == "coins")
                {
                    if (entry.Available) CoinHover(entry.Id, x + 37, y, Icon, 132);
                }
                else
                    TextHover(entry.Name, entry.Description, x + 37, y, Icon, 132);
            }

            // paging inside the frame
            bool canPrev = Ui.CollectionPage > 1, canNext = Ui.CollectionPage < pages;
            Button("<", 70, 380, 50, 90, C.Green, () => A.ChangeCollectionPage(-1), canPrev);
            Button(">", 1160, 380, 50, 90, C.Green, () => A.ChangeCollectionPage(1), canNext);
            Centered(Ui.CollectionPage + "/" + pages, 440, 700, 400, Ui.F32, C.White);
            string count = Ui.CollectionCategory == "coins" ? L("COLLECTED %d / %d", Ui.Profile.Collected.Count, Content.CoinOrder.Count) :
                Ui.CollectionCategory == "characters" ? L("UNLOCKED %d / %d", CountAvailable(entries), entries.Count) :
                L("%d %s", entries.Count, Categories[Array.FindIndex(Categories, category => category.key == Ui.CollectionCategory)].label);
            Centered(count, 850, 710, 360, Ui.F16, C.Muted);
        }

        static int CountAvailable(List<Entry> entries)
        {
            int count = 0;
            foreach (var entry in entries) if (entry.Available) count++;
            return count;
        }
    }

    // Options: game settings, sound levels, and the keyboard/controller reference.
    public static class OptionsView
    {
        static readonly (string key, string label, string hint)[] Rows =
        {
            ("screen_shake", "SCREEN SHAKE", "Shake the screen when a coin lands."),
            ("fast_flip", "FAST FLIP", "Half-length coin flip animation."),
            ("fullscreen", "BORDERLESS FULLSCREEN", "Use a borderless fullscreen window."),
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
            Box(280, y, 720, 78, C.PanelDk);
            Outline(280, y, 720, 78, C.Line);
        }

        // A slider is a clickable strip: pressing or dragging sets the value from the mouse x; releasing saves it.
        static void Slider(string key, string label, string hint, float y)
        {
            Panel(y);
            Text(label, 308, y + 10, Ui.F32, C.Face);
            Text(hint, 308, y + 46, Ui.F16, C.Muted);
            const float x = 600, w = 280;
            float value = (float)Ui.Profile.Options.GetVolume(key);
            Box(x, y + 30, w, 16, C.SlotDk, 8);
            Color(C.Gold);
            Gfx.Rectangle(true, x, y + 30, w * value / 100, 16, 8);
            Box(x + w * value / 100 - 8, y + 24, 16, 28, C.White, 4);
            Text(GameText.Num(value) + "%", 912, y + 24, Ui.F32, C.Gold);
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
            Button("KEYBOARD", 280, 198, 350, 40, !controller ? C.Gold : C.PanelLight, () => Ui.ControlsView = "keyboard");
            Button("CONTROLLER", 650, 198, 350, 40, controller ? C.Gold : C.PanelLight, () => Ui.ControlsView = "controller");
            var rows = controller ? ControllerControls : KeyboardControls;
            float y = 252;
            foreach (var row in rows)
            {
                if (row.keys.Length == 0)
                {
                    y += 6;
                    Text(row.label, 290, y, Ui.F20, C.Gold);
                    y += 30;
                    continue;
                }

                Text(row.label, 308, y + 6, Ui.F16, C.Face);
                float x = 700;
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
            Button("GAME", 280, 146, 230, 44, Ui.OptionsTab == "game" ? C.Gold : C.PanelLight, () => Ui.OptionsTab = "game");
            Button("SOUND", 525, 146, 230, 44, Ui.OptionsTab == "sound" ? C.Gold : C.PanelLight, () => Ui.OptionsTab = "sound");
            Button("CONTROLS", 770, 146, 230, 44, Ui.OptionsTab == "controls" ? C.Gold : C.PanelLight, () => Ui.OptionsTab = "controls");

            if (Ui.OptionsTab == "controls") DrawControls();
            else if (Ui.OptionsTab == "sound")
            {
                for (int i = 0; i < Sliders.Length; i++) Slider(Sliders[i].key, Sliders[i].label, Sliders[i].hint, 214 + i * 96);
                Button("RESTORE DEFAULTS", 440, 510, 400, 48, C.PanelLight, () =>
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
                    Text(row.label, 308, y + 10, Ui.F32, C.Face);
                    Text(row.hint, 308, y + 46, Ui.F16, C.Muted);
                    Button(on ? "ON" : "OFF", 860, y + 16, 112, 46, on ? C.Green : C.PanelLight, () => A.ToggleOption(row.key));
                }
                // language: the button shows the current language's own name
                float ly = 214 + Rows.Length * 86;
                Panel(ly);
                Text("LANGUAGE", 308, ly + 10, Ui.F32, C.Face);
                Text("Language of all texts.", 308, ly + 46, Ui.F16, C.Muted);
                Button(Lang.Names[Lang.Current], 820, ly + 16, 152, 46, C.Gold, A.CycleLanguage);
                // clear progress: opens a confirmation popup
                ly += 86;
                Panel(ly);
                Text("CLEAR PROGRESS", 308, ly + 10, Ui.F32, C.Face);
                Text("Resets unlocks, collection, sets and tokens. Options stay.", 308, ly + 46, Ui.F16, C.Muted);
                Button("CLEAR", 820, ly + 16, 152, 46, C.Red, A.ClearProgress);
                Button(RuntimeMode.Dev ? "EXIT DEVELOPER MODE" : "DEVELOPER MODE", 440, 644, 400, 48, C.Purple, A.ToggleDeveloperMode);
            }
            if (Ui.OptionsTab != "controls") Centered("F3 SHOWS DEBUG INFO IN A RUN", 0, 715, 1280, Ui.F16, C.Muted);
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
                float x = 70 + col * 580, y = 160 + row * 128;
                Box(x, y, 560, 116, C.PanelDk);
                Outline(x, y, 560, 116, C.Line);
                Text(Sections[i].title, x + 16, y + 10, Ui.F20, C.Gold);
                Gfx.SetFont(Ui.F16);
                Color(C.Face);
                Gfx.Printf(L(Sections[i].body), x + 16, y + 38, 528);
            }
            // the 8th cell of the grid: controls
            Box(650, 544, 560, 116, C.PanelDk);
            Outline(650, 544, 560, 116, C.Line);
            Text("CONTROLS", 666, 554, Ui.F20, C.Gold);
            Gfx.SetFont(Ui.F16);
            Color(C.Face);
            Gfx.Printf(L("Space = Flip / Next Coin.  Click = select a coin.  Esc = menu (your run waits).  Hover a coin for details. The run is saved at each level start and in the shop; Continue resumes it, a level in progress restarts."),
                666, 582, 528);
            if (Ui.HelpNext != null)
            {
                string next = Ui.HelpNext;
                IconButton("GOT IT", Ui.UiImages["start_level"], 470, 684, 340, 56, C.Green, () => A.Go(next));
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
            Box(0, 0, 1280, 800, C.FeltDark);
            Box(36, 36, 1208, 728, C.Screen);
            Outline(36, 36, 1208, 728, C.Gold);

            // headline, drawn at double size in the outcome colour with a shadow
            string headline = L(g.Endless ? "ENDLESS RUN OVER" : won ? "THE HOUSE FALLS" : "RUN OVER");
            Gfx.SetFont(Ui.F48);
            float width = Ui.F48.GetWidth(headline) * 2;
            Color(C.Black, .35f);
            Gfx.Print(headline, (float)Math.Floor(640 - width / 2) + 4, 90 + 4, 2, 2);
            Color(won ? C.Green : C.Red);
            Gfx.Print(headline, (float)Math.Floor(640 - width / 2), 90, 2, 2);

            // the character and their starting coin
            Box(380, 206, 230, 300, C.PanelDk);
            Outline(380, 206, 230, 300, C.Line);
            var portrait = Ui.CharacterImages[g.CharacterId];
            float scale = Math.Min(210f / portrait.Width, 240f / portrait.Height);
            Color(C.White);
            Gfx.Draw(portrait, 380 + (230 - portrait.Width * scale) / 2, 214 + (250 - portrait.Height * scale) / 2, scale, scale);
            Centered(Lang.Upper(Lang.CharacterName(g.CharacterId)), 380, 476, 230, Ui.F20, C.Gold);

            // what the run reached
            Box(650, 206, 250, 300, C.PanelDk);
            Outline(650, 206, 250, 300, C.Line);
            Centered(g.Endless ? "ENDLESS MODE" : won ? "YOU WON THE RUN" : "TRY A NEW SET", 650, 222, 250, Ui.F20, C.Face);
            Centered((g.Endless ? g.Cleared - Game.Route.Count : g.Cleared).ToString(), 650, 276, 250, Ui.F48, won ? C.Green : C.Gold);
            Centered(g.Endless ? "ENDLESS LEVELS CLEARED" : "LEVELS CLEARED OF " + Game.Route.Count, 650, 336, 250, Ui.F16, C.Muted);
            ImageAt(Ui.UiImages["gold"], 690, 386, 44);
            Text(GameText.Num(g.Player.Gold), 746, 392, Ui.F32, C.Gold);
            Text("GOLD LEFT", 690, 440, Ui.F16, C.Muted);
            Text(L("SEED %s", g.Seed), 690, 468, Ui.F16, C.Muted);
            if (g.EndlessRecord) Centered("NEW RECORD!", 380, 514, 520, Ui.F20, C.Gold);
            if (won && !g.Endless)
            {
                float y = 514;
                if (g.UnlockedCharacter != null)
                {
                    Centered(L("NEW CHARACTER UNLOCKED: %s", Lang.Upper(Lang.CharacterName(g.UnlockedCharacter))), 380, y, 520, Ui.F20, C.Gold);
                    y += 32;
                }
                if (g.UnlockedStake.HasValue) Centered(L("STAGE %d UNLOCKED", g.UnlockedStake.Value), 380, y, 520, Ui.F20, C.Gold);
            }
            if (!won && g.LostWhy != null)
            {
                Gfx.SetFont(Ui.F16);
                Color(C.Red);
                string why = L(g.LostWhy);
                string capitalized = why.Length == 0 ? why : Lang.Upper(why.Substring(0, 1)) + why.Substring(1);
                Gfx.Printf(capitalized, 380, g.EndlessRecord ? 546 : 520, 520, Align.Center);
            }

            if (won && !g.Endless)
            {
                // the boss fell: keep going through endless levels or leave for the menu
                IconButton("ENDLESS MODE", Ui.UiImages["next_coin"], 470, 584, 340, 60, C.Gold, A.ContinueEndless);
                IconButton("BACK TO MENU", Ui.UiImages["give_up"], 470, 652, 340, 56, C.Blue, A.OpenMenu);
                Button("NEW RUN", 520, 718, 240, 36, C.Green, () => A.Start());
            }
            else
            {
                IconButton("NEW RUN", Ui.UiImages["start_level"], 470, 600, 340, 64, C.Green, () => A.Start());
                Button("BACK TO MENU", 520, 686, 240, 44, C.PanelLight, A.OpenMenu);
            }
            Button("MENU", 1120, 56, 100, 34, C.PanelLight, A.OpenMenu);
        }
    }
}
