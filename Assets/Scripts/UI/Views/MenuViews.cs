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
            if (Ui.Screen != UiScreen.Title || Ui.Confirm != null)
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
            Box(70, 70, 400, 590, C.Ink);
            Outline(70, 70, 400, 590, C.Gold);
            var logo = Ui.UiImages["logo"];
            Color(C.White);
            Gfx.Draw(logo, 120, 96, 300f / logo.Width, 300f / logo.Width);
            Centered("BEAT THE ENEMY", 70, 190, 400, Ui.F20, C.Muted);

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
            entries.Add(("COIN SETS", C.Gold, () => A.OpenSets(Ui.SelectedCharacter.Id)));
            entries.Add(("COLLECTION", C.Green, () => A.Go(UiScreen.Collection)));
            var small = new List<(string, Rgba, Action)>
            {
                ("TUTORIAL", C.Green, A.StartTutorial),
                ("HELP", C.Green, () => { Ui.HelpNext = null; A.Go(UiScreen.Help); }),
                ("OPTIONS", C.PanelLight, () => A.Go(UiScreen.Options)),
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
            Frame(Title("play"), "BACK", () => A.Go(UiScreen.Title));

            string characterId = Ui.SelectedCharacter.Id;
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
            var perks = Ui.SelectedCharacter.Perks;
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
            if (set.Coins.Count == 0) Centered("THIS SET IS EMPTY  -  THE DEFAULT SET IS USED", 484, 490, 736, Ui.F16, C.Orange);
            int stake=A.Stake(),top=Profile.MaxStake(Ui.Profile,characterId);
            Button("<",510,486,50,52,C.PanelLight,()=>A.CycleStake(-1),stake>1&&!locked);
            Centered("STAGE "+stake+" / "+Game.Stakes.Count,570,490,504,Ui.F20,stake==top?C.Gold:C.Face);
            Centered(Game.Stakes[stake-1].Text,570,518,504,Ui.F16,C.Muted);
            Button(">",1084,486,50,52,C.PanelLight,()=>A.CycleStake(1),stake<top&&!locked);
            Button("EDIT COIN SETS", 674, 555, 356, 52, C.Gold, () => A.OpenSets(Ui.SelectedCharacter.Id),!locked);

            IconButton("START RUN", Ui.UiImages["start_level"], 470, 660, 340, 68, C.Green, () => A.Start(),!locked);
        }
    }

    // Coin Sets: every character with all of its coins. Build up to three sets per
    // character. Locked coins are unlocked by buying them in the shop during a run.
    public static class SetsView
    {
        const float SetSlotSize = 82, CatalogIcon = 64, CatalogStep = 82;
        const int CatalogColumns = 8, CatalogRows = 4, CatalogPerPage = CatalogColumns * CatalogRows;
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
            float x0 = 70 + (430 - (3 * SetSlotSize + 2 * 16)) / 2;
            for (int i = 0; i < Game.StartMax; i++)
            {
                float x = x0 + (i % 3) * (SetSlotSize + 16);
                float y = 316 + (i / 3) * (SetSlotSize + 16);
                Box(x - 4, y - 4, SetSlotSize + 8, SetSlotSize + 8, C.SlotDk);
                Outline(x - 4, y - 4, SetSlotSize + 8, SetSlotSize + 8, C.Line);
                if (i >= coins.Count) continue;
                CoinImage(coins[i], x, y, SetSlotSize);
                CoinHover(coins[i], x, y, SetSlotSize, SetSlotSize);
                int slot = i;
                AddButton(x - 4, y - 4, SetSlotSize + 8, SetSlotSize + 8, () => A.RemoveCoinFromSet(slot));
            }
            Centered(dirty ? "UNSAVED CHANGES" : "CLICK A COIN HERE TO REMOVE IT", 70, 505, 430, Ui.F16, dirty ? C.Orange : C.Muted);
            Button(dirty ? "SAVE SET" : "SAVED", 100, 530, 370, 48, dirty ? C.Blue : C.PanelLight, A.SaveSet, dirty);
            Button("CLEAR SET", 100, 592, 370, 44, C.Red, A.ClearSet, coins.Count > 0);
            Centered(L("EACH SET COIN GOES INTO YOUR POUCH TWICE."), 70, 656, 430, Ui.F16, C.Muted);
            Centered(L("NORMAL COINS FILL THE POUCH UP TO %d.", Game.PouchSize), 70, 678, 430, Ui.F16, C.Muted);
            Centered("MAX COPIES OF EACH COIN", 70, 680, 430, Ui.F16, C.Muted);
            Centered("COMMON 3  -  UNCOMMON 2  -  RARE/EPIC 1", 70, 704, 430, Ui.F16, C.Muted);

            // right: all of this character's coins
            Box(520, 212, 700, 528, C.PanelDk);
            Outline(520, 212, 700, 528, C.Line);
            Centered(L("%s  -  CLICK A COIN TO ADD IT  -  LOCKED COINS: SHOP OR TOKENS", Lang.Upper(Lang.CharacterName(characterId))),
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
            int pages = Math.Max(1, (entries.Count + CatalogPerPage - 1) / CatalogPerPage);
            Ui.SetsCatalogPage = Math.Min(Ui.SetsCatalogPage, pages);
            int first = (Ui.SetsCatalogPage - 1) * CatalogPerPage;
            float gx = 520 + (700 - ((CatalogColumns - 1) * CatalogStep + CatalogIcon)) / 2;
            for (int slot = 0; slot < CatalogPerPage && first + slot < entries.Count; slot++)
            {
                var (id, locked, _) = entries[first + slot];
                float x = gx + (slot % CatalogColumns) * CatalogStep;
                float y = 270 + (slot / CatalogColumns) * 102;
                Box(x - 4, y - 4, CatalogIcon + 8, CatalogIcon + 28, C.SlotDk);
                Outline(x - 4, y - 4, CatalogIcon + 8, CatalogIcon + 28,
                    locked ? C.Line : RarityColor(Content.Coins[id].Rarity));
                CoinImage(id, x, y, CatalogIcon);
                if (locked)
                {
                    Color(C.SlotDk, .7f);
                    Gfx.Circle(true, x + CatalogIcon / 2, y + CatalogIcon / 2, CatalogIcon / 2);
                    Padlock(x + CatalogIcon / 2, y + CatalogIcon / 2 - 4);
                    Centered("SHOP", x, y + CatalogIcon + 2, CatalogIcon, Ui.F16, C.Muted);
                }
                else
                {
                    int n = 0;
                    foreach (var owned in coins) if (owned == id) n++;
                    bool canAdd = Profile.CanAdd(Ui.Profile, characterId, coins, id, Game.StartMax, Game.MaxCopies);
                    Centered(n > 0 ? "x" + n : canAdd || coins.Count >= Game.StartMax ? "" : "LIMIT", x, y + CatalogIcon + 2, CatalogIcon, Ui.F16,
                        canAdd ? C.Green : C.Muted);
                    AddButton(x - 4, y - 4, CatalogIcon + 8, CatalogIcon + 28, () => A.AddCoinToSet(id), disabled: !canAdd);
                }
                CoinHover(id, x - 4, y - 4, CatalogIcon + 8, CatalogIcon + 28, null, locked);
            }
            Text(L("TOKENS: %d", (int)Ui.Profile.Tokens), 1010, 698, Ui.F16, C.Gold);
            Button("<", 540, 690, 52, 34, C.Green, () => A.ChangeSetsCatalogPage(-1), Ui.SetsCatalogPage > 1);
            Centered(Ui.SetsCatalogPage + " / " + pages, 760, 694, 220, Ui.F20, C.Gold);
            Button(">", 1148, 690, 52, 34, C.Green, () => A.ChangeSetsCatalogPage(1), Ui.SetsCatalogPage < pages);
        }
    }

    public static class CollectionView
    {
        const int Columns = 5, Rows = 3, PerPage = Columns * Rows;
        const float CellW = 170, Icon = 84, CardH = 138;

        static void MasteryEmblem(float x, float y, int level)
        {
            // The gem overlaps the coin's lower edge and rests against the nameplate.
            var gem = level == 1 ? C.Blue : level == 2 ? C.Purple : C.Orange;
            Color(C.Ink);
            Gfx.Rectangle(true, x + 8, y, 10, 3);
            Gfx.Rectangle(true, x + 5, y + 3, 16, 3);
            Gfx.Rectangle(true, x + 2, y + 6, 22, 3);
            Gfx.Rectangle(true, x, y + 9, 26, 6);
            Gfx.Rectangle(true, x + 2, y + 15, 22, 3);
            Gfx.Rectangle(true, x + 5, y + 18, 16, 3);
            Gfx.Rectangle(true, x + 8, y + 21, 10, 3);
            Color(C.Gold);
            Gfx.Rectangle(true, x + 9, y + 2, 8, 3);
            Gfx.Rectangle(true, x + 6, y + 5, 14, 3);
            Gfx.Rectangle(true, x + 3, y + 8, 20, 8);
            Gfx.Rectangle(true, x + 6, y + 16, 14, 3);
            Gfx.Rectangle(true, x + 9, y + 19, 8, 3);
            Color(gem);
            Gfx.Rectangle(true, x + 10, y + 5, 6, 3);
            Gfx.Rectangle(true, x + 7, y + 8, 12, 3);
            Gfx.Rectangle(true, x + 5, y + 11, 16, 3);
            Gfx.Rectangle(true, x + 7, y + 14, 12, 3);
            Gfx.Rectangle(true, x + 10, y + 17, 6, 3);
            Color(C.Face); Gfx.Rectangle(true, x + 9, y + 8, 3, 2);
        }

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

        // Rarity filters use the same accent colours as coin details.
        static readonly (CollectionRarityFilter key, string label, Rgba fill)[] Tabs =
        {
            (CollectionRarityFilter.All, "All", C.Gold),
            (CollectionRarityFilter.Common, "Common", C.Muted),
            (CollectionRarityFilter.Uncommon, "Uncommon", C.Green),
            (CollectionRarityFilter.Rare, "Rare", C.Blue),
            (CollectionRarityFilter.Epic, "Epic", C.Purple),
        };
        static readonly (CollectionSortMode key, string label)[] Sorts =
            { (CollectionSortMode.Rarity, "Rarity"), (CollectionSortMode.Name, "Name"), (CollectionSortMode.Order, "Default") };

        static bool MatchesFilter(Rarity rarity)
        {
            switch (Ui.CollectionFilter)
            {
                case CollectionRarityFilter.All: return true;
                case CollectionRarityFilter.Common: return rarity == Rarity.Common;
                case CollectionRarityFilter.Uncommon: return rarity == Rarity.Uncommon;
                case CollectionRarityFilter.Rare: return rarity == Rarity.Rare;
                case CollectionRarityFilter.Epic: return rarity == Rarity.Epic;
                default: return false;
            }
        }

        static string SortLabel()
        {
            foreach (var s in Sorts) if (s.key == Ui.CollectionSort) return s.label;
            return "";
        }

        static void CycleSort()
        {
            (CollectionSortMode key, string label)[] sorts = Ui.CollectionCategory == "coins" ? Sorts :
                new[] { (CollectionSortMode.Name, "Name"), (CollectionSortMode.Order, "Default") };
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
                    if (!MatchesFilter(coin.Rarity)) continue;
                    entries.Add(new Entry { Id = coin.Id, Name = Lang.CoinName(coin.Id), ImageId = coin.Id,
                        Rank = (int)coin.Rarity + 1, Index = index,
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
                if (Ui.CollectionCategory == "coins" && Ui.CollectionSort == CollectionSortMode.Rarity && a.Rank != b.Rank) return a.Rank.CompareTo(b.Rank);
                if (Ui.CollectionSort == CollectionSortMode.Name)
                {
                    int byName = string.CompareOrdinal(a.Name, b.Name);
                    if (byName != 0) return byName;
                }
                return a.Index.CompareTo(b.Index);
            });
            return entries;
        }

        static void TabButton(string label, float x, float y, float w, Rgba tint, Action action, bool selected = false)
        {
            Button(label, x, y, w, 40, selected ? tint : C.PanelLight, action);
            if (selected)
            {
                Ui.Mouse(out float mx, out float my);
                bool hovered = mx >= x && mx <= x + w && my >= y && my <= y + 40;
                Outline(x, y + (hovered ? -2 : 0), w, 40, C.Face);
            }
        }

        public static void Draw()
        {
            Frame(Title("collection"), "BACK", () => A.Go(UiScreen.Title));

            for (int i = 0; i < Categories.Length; i++)
            {
                var category = Categories[i];
                bool selected = Ui.CollectionCategory == category.key;
                TabButton(L(category.label), 251 + i * 196, 146, 184, C.Gold,
                    () => { Ui.CollectionCategory = category.key; Ui.CollectionPage = 1; Ui.CollectionFilter = CollectionRarityFilter.All; Ui.CollectionSort = category.key == "coins" ? CollectionSortMode.Rarity : CollectionSortMode.Order; }, selected);
            }

            // sort + rarity filters
            Text("SORT", 220, 211, Ui.F16, C.Muted);
            TabButton(SortLabel(), 270, 198, 120, C.Gold, CycleSort);
            if (Ui.CollectionCategory == "coins")
            {
                Color(C.Gold);
                Gfx.Rectangle(true, 406, 198, 2, 40);
                for (int i = 0; i < Tabs.Length; i++)
                {
                    var tab = Tabs[i];
                    float x = 424 + i * 128;
                    bool on = Ui.CollectionFilter == tab.key;
                    TabButton(tab.label, x, 198, 118, tab.fill, () => A.SetFilter(tab.key), on);
                }
            }

            // Five columns shared by every catalogue category.
            var entries = VisibleEntries();
            int pages = Math.Max(1, (int)Math.Ceiling(entries.Count / (double)PerPage));
            Ui.CollectionPage = Math.Min(Ui.CollectionPage, pages);
            int first = (Ui.CollectionPage - 1) * PerPage;
            int visible = Math.Min(PerPage, entries.Count - first);
            int visibleRows = (visible + Columns - 1) / Columns;
            float gridY = 258 + (Ui.CollectionCategory == "coins" ? 0 : (Rows - visibleRows) * 70);
            float x0 = 640 - Columns * CellW / 2;
            for (int slot = 0; slot < PerPage; slot++)
            {
                if (first + slot >= entries.Count) break;
                var entry = entries[first + slot];
                float x = x0 + (slot % Columns) * CellW;
                float y = gridY + (slot / Columns) * 150;
                Img image = Ui.CollectionCategory == "items" ? Ui.ItemImages[entry.ImageId] :
                    Ui.CollectionCategory == "relics" ? Ui.RelicImages[entry.ImageId] :
                    Ui.CollectionCategory == "characters" ? Ui.CharacterImages[entry.ImageId] : Ui.CoinImages[entry.ImageId];
                bool coinEntry = Ui.CollectionCategory == "coins";
                var border = coinEntry ? (entry.Available ? RarityColor(Content.Coins[entry.Id].Rarity) : C.Muted) : C.Line;
                Box(x + 6, y, CellW - 12, CardH, C.PanelDk);
                Outline(x + 6, y, CellW - 12, CardH, border);
                float imageX = x + (CellW - Icon) / 2;
                if (coinEntry && !entry.Available)
                {
                    float iconY = y + 6;
                    Color(C.White, .48f);
                    Gfx.Draw(image, imageX, iconY, Icon / image.Width, Icon / image.Height);
                    float lockX = imageX + Icon - 20;
                    float lockY = iconY + 2;
                    Color(C.Ink);
                    Gfx.Rectangle(true, lockX - 3, lockY - 2, 26, 32);
                    Color(C.Gold);
                    Gfx.Rectangle(true, lockX + 4, lockY + 2, 12, 4);
                    Gfx.Rectangle(true, lockX + 2, lockY + 6, 4, 10);
                    Gfx.Rectangle(true, lockX + 14, lockY + 6, 4, 10);
                    Gfx.Rectangle(true, lockX, lockY + 14, 20, 14);
                    Color(C.Ink);
                    Gfx.Rectangle(true, lockX + 9, lockY + 19, 3, 5);
                }
                else if (coinEntry) CoinImage(entry.Id, imageX, y + 6, Icon);
                else
                {
                    if (entry.Available) Color(C.White);
                    else Gfx.SetColor(1, 1, 1, .65f);
                    Gfx.Draw(image, imageX, y + 6, Icon / image.Width, Icon / image.Height);
                }
                Box(x + 11, y + 98, CellW - 22, 32, C.Card);
                if (coinEntry && entry.Available)
                {
                    int level = Profile.MasteryLevel(Ui.Profile, Content.Coins[entry.Id]);
                    if (level > 0)
                    {
                        Gfx.Push();
                        Gfx.Translate(x + (CellW - 32.5f) / 2, y + 70);
                        Gfx.Scale(1.25f, 1.25f);
                        MasteryEmblem(0, 0, level);
                        Gfx.Pop();
                    }
                }
                string cardName = entry.Available ? entry.Name : Ui.CollectionCategory == "coins" ? "Uncollected" : "Locked";
                Centered(cardName, x + 11, y + 104, CellW - 22, Ui.F20, entry.Available ? C.Face : C.Muted);
                if (coinEntry)
                {
                    string coinId = entry.Id;
                    AddButton(x + 6, y, CellW - 12, CardH, () => CoinDetailView.Open(Content.Coins[coinId]), "DETAIL");
                }
                else
                    TextHover(entry.Name, entry.Description, x + 6, y, CellW - 12, CardH);
            }

            // paging inside the frame
            string count = Ui.CollectionCategory == "coins" ? L("COLLECTED %d / %d  -  TOKENS: %d", Ui.Profile.Collected.Count, Content.CoinOrder.Count, (int)Ui.Profile.Tokens) :
                Ui.CollectionCategory == "characters" ? L("UNLOCKED %d / %d", CountAvailable(entries), entries.Count) :
                L("%d %s", entries.Count, Categories[Array.FindIndex(Categories, category => category.key == Ui.CollectionCategory)].label);
            if (pages > 1)
            {
                Button("<", 70, 380, 50, 90, C.Green, () => A.ChangeCollectionPage(-1), Ui.CollectionPage > 1);
                Button(">", 1160, 380, 50, 90, C.Green, () => A.ChangeCollectionPage(1), Ui.CollectionPage < pages);
                Centered(Ui.CollectionPage + "/" + pages, 440, 700, 400, Ui.F32, C.Face);
                Centered(count, 850, 710, 360, Ui.F16, C.Muted);
            }
            else Centered(count, 440, 710, 400, Ui.F20, C.Muted);
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
            ("IN A FIGHT", Array.Empty<string>()),
            ("Flip the first coin of your hand", new[] { "SPACE" }),
            ("Inspect: show the details of the focused item", new[] { "I", "Q", "E" }),
            ("Use chip 1 / 2 / 3", new[] { "1", "2", "3" }),
            ("Move on after a won fight", new[] { "O" }),
            ("Debug info", new[] { "F3" }),
        };
        static readonly (string label, string[] keys)[] ControllerControls =
        {
            ("MENUS", Array.Empty<string>()),
            ("Move the focus", new[] { "D-PAD", "STICK" }),
            ("Press the focused button", new[] { "A" }),
            ("Back, close, menu", new[] { "B", "START" }),
            ("Previous / next page, tab, character", new[] { "LB", "RB", "LT", "RT" }),
            ("IN A FIGHT", Array.Empty<string>()),
            ("Flip the first coin of your hand", new[] { "X" }),
            ("Inspect: show the details of the focused item", new[] { "LB", "RB", "LT", "RT" }),
            ("Flip a coin, end the round, use a chip", new[] { "D-PAD", "A" }),
            ("Menu", new[] { "START" }),
        };

        static void Panel(float y)
        {
            Box(280, y, 720, 78, C.PanelDk);
            Outline(280, y, 720, 78, C.Line);
        }

        // A slider is a clickable strip: pressing or dragging sets and saves the value as it changes.
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
                    Sound.Play("score");
                },
                Release = () => Sound.Play("score"),
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
            Frame(Title("options"), "BACK", () => A.Go(UiScreen.Title));
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
            ("THE GOAL", "A run has eight fights, ending with The House. Every fight lasts 5 rounds against an enemy with its own fixed pouch of coins. Score more points than the enemy by the end of round 5 to win; a tie goes to the House."),
            ("YOUR POUCH", "You start with a pouch of 30 coins: your set, two of each, and Normal coins to fill it. Every fight starts with a fresh shuffle. Each coin has a Heads chance and a Heads and a Tails effect."),
            ("EACH ROUND", "You hold 5 coins. Click any of them to flip it: as many as you like, in any order. Then press END ROUND. Unflipped coins stay in your hand, the hand refills from the pouch, and when the pouch is empty the flipped coins are shuffled back in."),
            ("THE ENEMY", "The enemy's pouch is shown on the left. After every round it flips a few coins from it and scores their printed points. Some of your coins say Enemy +n: that gives the enemy points."),
            ("ENERGY", "Strong coins cost ENERGY to flip (shown as E1, E2). Your energy refills every round; Spark, Copper and Flux Capacitor give more. A coin you cannot pay for stays in your hand."),
            ("WINNING", "A win pays gold, plus 1 gold for every 2 points of winning margin. Press CONTINUE to move on. Lose or tie and the run is over."),
            ("THE SHOP", "Buy COINS (they join your pouch), CHIPS (one-use helpers, used between flips), and a PRIZE (lasts the run). Coin Removal drops a weak coin."),
        };

        public static void Draw()
        {
            Frame(Title("help"), "BACK", () => A.Go(UiScreen.Title));
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
            Gfx.Printf(L("Space = flip the first coin.  Click = flip a coin of your hand.  Esc = menu (your run waits).  Hover a coin for details. The run is saved between flips and in the shop; Continue resumes it."),
                666, 582, 528);
            if (Ui.HelpNext != null)
            {
                UiScreen next = Ui.HelpNext.Value;
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
            Frame(null);

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
            Centered(g.Endless ? "ENDLESS FIGHTS WON" : "FIGHTS WON OF " + Game.Route.Count, 650, 336, 250, Ui.F16, C.Muted);
            ImageAt(Ui.UiImages["gold"], 690, 386, 44);
            Text(GameText.Num(g.Player.Gold), 746, 392, Ui.F32, C.Gold);
            Text("GOLD LEFT", 690, 440, Ui.F16, C.Muted);
            Text(L("SEED %s", g.Seed), 690, 468, Ui.F16, C.Muted);
            if (g.TokensPaid > 0) Text(L("+%d TOKENS", g.TokensPaid.Value), 690, 490, Ui.F16, C.Gold);
            // luck report: one line right under the boxes, the rest of the text moves down by it
            float top = 514;
            if (g.RunFlips > 0)
            {
                double diff = g.RunHeads - g.RunExpectedHeads;
                string verdict = Math.Abs(diff) <= 1 ? L("ABOUT AS EXPECTED") : diff < 0 ? L("UNLUCKY: %s HEADS BELOW EXPECTED", GameText.Num(Math.Round(-diff, 1))) :
                    L("LUCKY: %s HEADS ABOVE EXPECTED", GameText.Num(Math.Round(diff, 1)));
                Centered(L("HEADS %d / %d (EXPECTED %s)", g.RunHeads, g.RunFlips, GameText.Num(Math.Round(g.RunExpectedHeads, 1))) + "  -  " + verdict,
                    140, 512, 1000, Ui.F16, Math.Abs(diff) <= 1 ? C.Muted : diff < 0 ? C.Red : C.Green);
                top = 534;
            }
            if (g.EndlessRecord) Centered("NEW RECORD!", 380, top, 520, Ui.F20, C.Gold);
            if (won && !g.Endless)
            {
                float y = top;
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
                Gfx.Printf(capitalized, 380, top + (g.EndlessRecord ? 32 : 6), 520, Align.Center);
            }

            if (won && !g.Endless)
            {
                // the boss fell: keep going through endless levels or leave for the menu (10 lower: room for two unlock lines)
                IconButton("ENDLESS MODE", Ui.UiImages["next_coin"], 470, 594, 340, 60, C.Gold, A.ContinueEndless);
                IconButton("BACK TO MENU", Ui.UiImages["give_up"], 470, 662, 340, 56, C.Blue, A.OpenMenu);
                Button("NEW RUN", 520, 728, 240, 36, C.Green, () => A.Start());
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
