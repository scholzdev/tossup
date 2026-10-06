using System;
using System.Collections.Generic;

namespace Tossup.UI
{
    // Drawing primitives shared by every view.
    public static class D
    {
        public static string L(string text, params object[] args) => Lang.T(text, args);

        static string N(double v) => GameText.Num(v);

        public static Rgba CoinTypeColor(CoinType type) => Rgba.Hex(DefinitionKeys.CoinColorHex(type));

        public static void Color(Rgba c, float alpha = 1) => Gfx.SetColor(c.R, c.G, c.B, alpha);

        public static void Box(float x, float y, float w, float h, Rgba fill, float radius = 6)
        {
            Color(C.Black, .34f);
            Gfx.Rectangle(true, x, y + 5, w, h, radius);
            Color(fill);
            Gfx.Rectangle(true, x, y, w, h, radius);
        }

        public static void Outline(float x, float y, float w, float h, Rgba tint, float radius = 6)
        {
            Color(tint);
            Gfx.SetLineWidth(2);
            Gfx.Rectangle(false, x + 1, y + 1, w - 2, h - 2, radius);
            Gfx.SetLineWidth(1);
        }

        public static void Text(string str, float x, float y, PixFont face = null, Rgba? tint = null)
        {
            str = L(str);
            Gfx.SetFont(face ?? Ui.F16);
            Color(tint ?? C.Face);
            Gfx.Print(str, (float)Math.Floor(x), (float)Math.Floor(y));
        }

        public static void Centered(string str, float x, float y, float w, PixFont face = null, Rgba? tint = null)
        {
            face = face ?? Ui.F20;
            str = L(str);
            Text(str, x + (float)Math.Floor((w - face.GetWidth(str)) / 2f), y, face, tint);
        }

        static bool Over(float x, float y, float w, float h)
        {
            Ui.Mouse(out float mx, out float my);
            return mx >= x && mx <= x + w && my >= y && my <= y + h;
        }

        public static void AddButton(float x, float y, float w, float h, Action action, string label = null, bool disabled = false, string hotkey = null) =>
            Ui.Buttons.Add(new Button { X = x, Y = y, W = w, H = h, Action = action, Label = label, Disabled = disabled, Hotkey = hotkey });

        public static void Button(string str, float x, float y, float w, float h, Rgba tint, Action action, bool enabled = true, string hotkey = null, PixFont face = null)
        {
            face = face ?? Ui.F20;
            bool hover = enabled && Over(x, y, w, h);
            var fill = enabled ? tint : C.PanelLight;
            float lift = hover ? -3 : 0;
            Box(x, y + lift, w, h, fill);
            Outline(x, y + lift, w, h, enabled ? C.Face : C.Slot);
            Centered(str, x, y + (h - face.Height) / 2f + lift, w, face, enabled ? C.Ink : C.Muted);
            AddButton(x, y + lift, w, h, action, str, !enabled, hotkey);
        }

        // Short effect list for cards: "+5 PTS, NEXT 2 x2".
        public static string Effects(IReadOnlyList<Effect> list)
        {
            if (list.Count == 0) return L("Nothing");
            var parts = new List<string>();
            foreach (var e in list)
            {
                switch (e.Type)
                {
                    case EffectType.NextMult: parts.Add(L("NEXT %d x%d", e.Coins ?? 1, e.Amount)); continue;
                    case EffectType.NextOdds: parts.Add(L("NEXT %d +%d%%", e.Coins ?? 1, Math.Floor(e.Amount * 100 + .5))); continue;
                    case EffectType.Amplify: parts.Add(L("AMPLIFY")); continue;
                    case EffectType.ComboBonus: parts.Add(L("COMBO +%d", e.Amount)); continue;
                    case EffectType.ComboShield: parts.Add(L("COMBO SHIELD")); continue;
                    case EffectType.NextSwap: parts.Add(L("NEXT: SWAP")); continue;
                    case EffectType.NextHeads: parts.Add(L("NEXT: HEADS")); continue;
                    case EffectType.GoldLoss: parts.Add(L("-%d GOLD", e.Amount)); continue;
                    case EffectType.AllOdds: parts.Add(L("ALL +%d%%", Math.Floor(e.Amount*100+.5))); continue;
                    case EffectType.FortuneOdds: parts.Add(L("FORTUNE +%d%%", Math.Floor(e.Amount*100+.5))); continue;
                    case EffectType.TypeBuff: parts.Add(L("NEXT %d %s", e.Coins??1, (e.Kind.HasValue?e.Kind.Value.ToString().ToUpperInvariant():""))); continue;
                    case EffectType.BankDiscard: parts.Add(L("DISCARD ONE")); continue;
                    case EffectType.ExtraExchange: parts.Add(L("+%d EXCHANGE", e.Amount)); continue;
                    case EffectType.FetchBest: parts.Add(L("FETCH BEST")); continue;
                }
                double amount = e.Type == EffectType.Probability ? Math.Floor(e.Amount * 100 + .5) : e.Amount;
                string label;
                switch (e.Type)
                {
                    case EffectType.Score: label = L("PTS"); break;
                    case EffectType.Gold: label = L("GOLD"); break;
                    case EffectType.Energy: label = L("NRG"); break;
                    case EffectType.Penalty: label = L("QUOTA"); break;
                    case EffectType.ExtraDraw: label = L("REPLAY"); break;
                    case EffectType.Probability: label = L("% HEADS"); break;
                    default: label = DefinitionKeys.Key(e.Type); break;
                }
                parts.Add("+" + N(amount) + " " + label);
            }
            return string.Join(", ", parts);
        }

        // Full sentence per effect, for tooltips and the stage.
        public static string EffectDescription(IReadOnlyList<Effect> list)
        {
            if (list.Count == 0) return L("No effect");
            var parts = new List<string>();
            foreach (var effect in list)
            {
                double amount = effect.Amount;
                switch (effect.Type)
                {
                    case EffectType.Score: parts.Add(L("Score %d points", amount)); break;
                    case EffectType.Gold: parts.Add(L("Gain %d gold", amount)); break;
                    case EffectType.Energy: parts.Add(L("Gain %d energy", amount)); break;
                    case EffectType.Penalty: parts.Add(L("Quota +%d", amount)); break;
                    case EffectType.ExtraDraw: parts.Add(L("Goes back into the pile")); break;
                    case EffectType.Probability: parts.Add(L("Gain %d%% Heads this level", Math.Floor(amount * 100 + .5))); break;
                    case EffectType.NextMult: parts.Add(L("Next %d coins pay x%d", effect.Coins ?? 1, amount)); break;
                    case EffectType.NextOdds:
                        {
                            double pct = Math.Floor(amount * 100 + .5);
                            parts.Add(effect.Coins == 1 ? L("Next coin: +%d%% Heads", pct) : L("Next %d coins: +%d%% Heads", effect.Coins ?? 1, pct));
                            break;
                        }
                    case EffectType.Amplify: parts.Add(L("Buffs last 1 coin longer and get stronger")); break;
                    case EffectType.ComboBonus: parts.Add(L("Combo grows %d extra step", amount)); break;
                    case EffectType.ComboShield: parts.Add(L("The next combo break is prevented")); break;
                    case EffectType.NextSwap: parts.Add(L("Next coin uses its other side")); break;
                    case EffectType.NextHeads: parts.Add(L("Next coin lands Heads")); break;
                    case EffectType.GoldLoss: parts.Add(L("Lose up to %d gold", amount)); break;
                    case EffectType.AllOdds: parts.Add(L("All coins gain %d%% Heads this level", Math.Floor(amount*100+.5))); break;
                    case EffectType.FortuneOdds: parts.Add(L("Fortune coins gain %d%% Heads for the run", Math.Floor(amount*100+.5))); break;
                    case EffectType.TypeBuff: parts.Add(L("Buff the next %d %s coins", effect.Coins??1, DefinitionKeys.Key(effect.Kind.Value))); break;
                    case EffectType.BankDiscard: parts.Add(L("Discard one bank coin")); break;
                    case EffectType.ExtraExchange: parts.Add(L("Gain one exchange this level")); break;
                    case EffectType.FetchBest: parts.Add(L("Return the best played coin")); break;
                }
            }
            return string.Join("; ", parts);
        }

        public static string CoinOutcomeDescription(CoinDef coin, string outcome, Upgrade upgrade = null)
        {
            OutcomeSide side = outcome == Side.Heads ? OutcomeSide.Heads : outcome == Side.Tails ? OutcomeSide.Tails : OutcomeSide.Edge;
            IReadOnlyList<Effect> effects = coin.EffectsFor(side, upgrade);
            if (outcome == Side.Heads)
                return string.IsNullOrEmpty(coin.HeadsDescription) ? EffectDescription(effects) : Lang.CoinHeadsDescription(coin.Id) + UpgradeEffectDescription(coin, upgrade, side);
            if (outcome == Side.Tails)
                return string.IsNullOrEmpty(coin.TailsDescription) ? EffectDescription(effects) : Lang.CoinTailsDescription(coin.Id) + UpgradeEffectDescription(coin, upgrade, side);
            if (outcome == Side.Tie)
                return string.IsNullOrEmpty(coin.EdgeDescription) ? EffectDescription(effects) : Lang.CoinEdgeDescription(coin.Id) + UpgradeEffectDescription(coin, upgrade, side);
            return L("No effect");
        }

        static string UpgradeEffectDescription(CoinDef coin, Upgrade upgrade, OutcomeSide side)
        {
            if (upgrade == null || !coin.TryGetUpgrade(upgrade.Id, out var owned)) return "";
            var added = new List<Effect>();
            foreach (var change in owned.Changes)
                if (change.Kind == UpgradeChangeKind.AddOutcomeEffect && change.Side == side) added.Add(change.Effect);
            return added.Count == 0 ? "" : "; " + EffectDescription(added);
        }

        static string BuffDescription(BuffSpec buff)
        {
            string type = buff.Target.Type.HasValue ? L(buff.Target.Type.Value.ToString().ToUpperInvariant()) + " " : "";
            string target = buff.Target.Count == 1 ? L("the next %scoin", type) : L("the next %d %scoins", buff.Target.Count, type);
            string trigger = L(buff.Trigger.ToString().ToUpperInvariant());
            switch (buff.Effect.Type)
            {
                case EffectType.NextOdds: return L("On %s, %s gain +%d%% Heads chance", trigger, target, Math.Floor(buff.Effect.Amount * 100 + .5));
                case EffectType.NextMult: return L("On %s, %s multiply score and gold by x%d", trigger, target, buff.Effect.Amount);
                case EffectType.NextSwap: return L("On %s, %s use the other side's effects", trigger, target);
                case EffectType.NextHeads: return L("On %s, %s are forced to land Heads", trigger, target);
                case EffectType.TypeBuff: return L("On %s, %s: %s", trigger, target, TypeBuffDescription(buff.Effect.Kind.Value));
                default:
                    string effect = EffectDescription(new[] { buff.Effect });
                    return buff.AppliesOn.HasValue
                        ? L("On %s, %s gain %s when landing %s", trigger, target, effect, L(buff.AppliesOn.Value.ToString().ToUpperInvariant()))
                        : L("On %s, %s gain %s", trigger, target, effect);
            }
        }

        static CoinType? BuffTargetType(BuffSpec buff) => buff.Target.Type;

        static string TypeBuffDescription(CoinType type)
        {
            switch (type)
            {
                case CoinType.Steel: return L("Heads score +3; Tails add +2 quota");
                case CoinType.Blood: return L("Heads score +8; Tails add +4 quota; Edge gets half of both");
                case CoinType.Greed: return L("Gold gains are doubled");
                case CoinType.Chaos: return L("Resolved effects are applied twice");
                case CoinType.Rhythm: return L("Combo steps add points; a broken combo adds quota");
                default: return L("gain a type buff");
            }
        }

        static string UpgradeChangesDescription(Upgrade upgrade)
        {
            var parts = new List<string>();
            foreach (var change in upgrade.Changes)
            {
                switch (change.Kind)
                {
                    case UpgradeChangeKind.AddOutcomeEffect:
                        parts.Add(L("%s: %s", L(change.Side.ToString().ToUpperInvariant()), EffectDescription(new[] { change.Effect })));
                        break;
                    case UpgradeChangeKind.HeadsProbability:
                        parts.Add(L("+%d%% Heads chance", Math.Floor(change.Amount * 100 + .5)));
                        break;
                    case UpgradeChangeKind.AddBuff:
                        parts.Add(BuffDescription(change.Buff));
                        break;
                }
            }
            return string.Join("; ", parts);
        }

        public static void CoinHover(CoinDef def, float x, float y, float w, float h, double? probability = null, bool locked = false,
            Upgrade upgrade = null, double? tieProbability = null)
        {
            double heads = probability ?? def.Probability;
            if (!probability.HasValue) heads += def.UpgradeHeadsProbability(upgrade);
            double tie = tieProbability ?? def.TieProbability;
            var hovered = new HoveredCoin { Definition = def, Probability = Math.Max(0, Math.Min(1 - tie, heads)), TieProbability = tie, Upgrade = upgrade, Locked = locked };
            Ui.Regions.Add(new HoverRegion { X = x, Y = y, W = w, H = h, Coin = hovered });
            if (Over(x, y, w, h)) Ui.HoveredCoin = hovered;
        }

        public static void CoinImage(CoinDef coin, float x, float y, float size) => CoinImage(coin.Id, x, y, size);
        public static void CoinHover(string id, float x, float y, float w, float h, double? probability = null, bool locked = false, Upgrade upgrade = null, double? tieProbability = null) =>
            CoinHover(Content.Coins[id],x,y,w,h,probability,locked,upgrade,tieProbability);

        public static void CoinImage(string id, float x, float y, float size)
        {
            Color(C.White);
            var image = Ui.CoinImages[id];
            Gfx.Draw(image, x, y, size / image.Width, size / image.Height);
        }

        // Draw any loaded image scaled to a square size.
        public static void ImageAt(Img image, float x, float y, float size)
        {
            Color(C.White);
            Gfx.Draw(image, x, y, size / image.Width, size / image.Height);
        }

        // A button with an icon on the left and its label centred in the rest.
        public static void IconButton(string label, Img icon, float x, float y, float w, float h, Rgba tint, Action action, bool enabled = true, string hotkey = null)
        {
            bool hover = enabled && Over(x, y, w, h);
            float top = y + (hover ? -3 : 0);
            Box(x, top, w, h, enabled ? tint : C.PanelLight);
            Outline(x, top, w, h, enabled ? C.Face : C.Slot);
            float size = h - 12;
            if (enabled) Color(C.White);
            else Gfx.SetColor(1, 1, 1, .45f);
            Gfx.Draw(icon, x + 8, top + 6, size / icon.Width, size / icon.Height);
            Centered(label, x + size + 8, top + (h - Ui.F20.Height) / 2f, w - size - 8, Ui.F20, enabled ? C.Ink : C.Muted);
            AddButton(x, y, w, h, action, label, !enabled, hotkey);
        }

        // The shared full-screen look (the shop's): felt backdrop, a teal screen with a gold border, a pixel
        // title image at the top left and, when given, a button at the top right.
        public static void Frame(Img titleImage, string backLabel = null, Action backAction = null)
        {
            Box(0, 0, Ui.Width, Ui.Height, C.FeltDark);
            Box(36, 36, Ui.Width - 72, Ui.Height - 72, C.Screen);
            Outline(36, 36, Ui.Width - 72, Ui.Height - 72, C.Gold);
            if (titleImage != null)
            {
                float scale = 90f / titleImage.Height;
                Color(C.White);
                Gfx.Draw(titleImage, 70, 46, scale, scale);
            }
            if (backAction != null) Button(backLabel, Ui.Width - 160, 56, 100, 34, C.PanelLight, backAction, true, "B");
        }

        // The pixel title image for a screen, in the current language when there is one.
        public static Img Title(string name)
        {
            if (Lang.Current != "en" && Ui.UiImages.TryGetValue("title_" + name + "_" + Lang.Current, out var localized)) return localized;
            return Ui.UiImages["title_" + name];
        }

        // A modal popup (Ui.Confirm): dims the screen and replaces every
        // other clickable while it is open. Draw it last.
        public static void ConfirmDialog()
        {
            var c = Ui.Confirm;
            if (c == null) return;
            Ui.Buttons.Clear(); // nothing behind the popup can be clicked
            Color(C.Ink, .72f);
            Gfx.Rectangle(true, 0, 0, Ui.Width, Ui.Height);
            float x = (Ui.Width - 520) / 2;
            Box(x, 270, 520, 260, C.PanelDk);
            Outline(x, 270, 520, 260, C.Red);
            Centered(c.Title, x, 292, 520, Ui.F32, C.Red);
            Gfx.SetFont(Ui.F20);
            Color(C.Face);
            Gfx.Printf(L(c.Text), x + 30, 352, 460, Align.Center);
            if (c.Single)
                Button("OK", x + 130, 454, 200, 52, C.Red, () => { Ui.Confirm = null; c.Ok(); });
            else
            {
                Button("OK", x + 40, 454, 200, 52, C.Red, () => { Ui.Confirm = null; c.Ok(); });
                Button("CANCEL", x + 280, 454, 200, 52, C.PanelLight, () => Ui.Confirm = null, true, "B");
            }
        }

        // Plain title + text tooltip (items, relics). Register while drawing; drawn once per frame on top.
        public static void TextHover(string title, string body, float x, float y, float w, float h)
        {
            var tip = new HoveredText { Title = title, Body = body };
            Ui.Regions.Add(new HoverRegion { X = x, Y = y, W = w, H = h, Text = tip });
            if (Over(x, y, w, h)) Ui.HoveredText = tip;
        }

        public static void TextTooltip()
        {
            var tip = Ui.HoveredText;
            if (tip == null) return;
            Ui.Pointer(out float mx, out float my);
            float w = 330, h = 62 + Ui.F16.GetWrap(L(tip.Body), 302).Count * 18;
            float x = Math.Min(mx + 18, Ui.Width - w - 12);
            float y = Math.Max(12, Math.Min(my + 18, 788 - h));
            Box(x, y, w, h, C.Ink);
            Outline(x, y, w, h, C.Gold);
            Text(Lang.Upper(L(tip.Title)), x + 14, y + 12, Ui.F20, C.Face);
            Gfx.SetFont(Ui.F16);
            Color(C.Muted);
            Gfx.Printf(L(tip.Body), x + 14, y + 44, w - 28);
        }

        public static Rgba RarityColor(Rarity rarity) => rarity == Rarity.Uncommon ? C.Green : rarity == Rarity.Rare ? C.Blue : rarity == Rarity.Epic ? C.Purple : C.Muted;

        public static void Padlock(float cx, float cy)
        {
            Color(C.Muted); Gfx.SetLineWidth(3); Gfx.ArcLine(cx, cy - 4, 7, (float)Math.PI, 2 * (float)Math.PI);
            Gfx.Rectangle(true, cx - 9, cy - 3, 18, 14, 3); Gfx.SetLineWidth(1);
            Color(C.Ink); Gfx.Rectangle(true, cx - 1, cy + 1, 2, 6);
        }

        public static void CoinTooltip()
        {
            var hovered = Ui.HoveredCoin;
            var game = Ui.Game;
            if (hovered == null || game != null && !game.Paused && (game.Phase == Phase.Encounter || game.Phase == Phase.GameOver && !game.OverSeen)) return;
            var coin = hovered.Definition;
            Upgrade upgrade = null;
            if (hovered.Upgrade != null) coin.TryGetUpgrade(hovered.Upgrade.Id, out upgrade);
            string upgradeDescription = upgrade == null ? null : UpgradeChangesDescription(upgrade);
            IReadOnlyList<Upgrade> collectionUpgrades = Ui.Screen == "collection" && Ui.CollectionCategory == "coins" && upgrade == null
                ? coin.Upgrades : Array.Empty<Upgrade>();
            string characterAvailability = null;
            if (Ui.Screen == "collection" && Ui.CollectionCategory == "coins")
            {
                var characters = new List<string>();
                foreach (var characterId in Content.CharacterOrder)
                {
                    var character = Content.Characters[characterId];
                    bool inPool = character.Pool.Exists(candidate => candidate.Id == coin.Id);
                    bool locked = character.Locked.Exists(candidate => candidate.Id == coin.Id);
                    if (inPool || locked)
                        characters.Add(Lang.Upper(Lang.CharacterName(characterId)) + (locked && !inPool ? " (" + L("LOCKED") + ")" : ""));
                }
                if (characters.Count > 0) characterAvailability = L("CHARACTERS: %s", string.Join(" / ", characters));
            }
            Ui.Pointer(out float mx, out float my);
            float w = 470, header = coin.Types.Count > 0 ? 110 : 94, h;
            if (coin.EnergyCost > 0) header += 22;
            int characterAvailabilityLines = characterAvailability == null ? 0 : Ui.F16.GetWrap(characterAvailability, w - 114).Count;
            if (characterAvailabilityLines > 0) header += Math.Max(22, characterAvailabilityLines * 18 + 4);
            h = header;
            var rows = new List<(string Label, double Chance, string Detail, Rgba Tint, float Height)>();
            void Row(string label, double chance, string detail, Rgba tint)
            {
                float height = 38 + Ui.F16.GetWrap(detail, w - 52).Count * 18;
                rows.Add((L(label), Math.Floor(chance * 100 + .5), detail, tint, height)); h += height + 7;
            }
            if (hovered.Probability > 0) Row("HEADS", hovered.Probability, CoinOutcomeDescription(coin, Side.Heads, upgrade), C.Blue);
            if (hovered.TieProbability > 0) Row("EDGE", hovered.TieProbability, CoinOutcomeDescription(coin, Side.Tie, upgrade), C.Purple);
            double tailsChance = 1 - hovered.Probability - hovered.TieProbability;
            if (tailsChance > 0) Row("TAILS", tailsChance, CoinOutcomeDescription(coin, Side.Tails, upgrade), C.Red);
            var buffs = coin.BuffsFor(upgrade);
            var buffDescriptions = new List<string>();
            foreach (var buff in buffs) buffDescriptions.Add(BuffDescription(buff));
            string specialRule = Lang.CoinSpecialRule(hovered.Id);
            if (string.IsNullOrWhiteSpace(specialRule)) specialRule = null;
            int buffLines = 0;
            foreach (var detail in buffDescriptions) buffLines += Ui.F16.GetWrap(detail, w - 62).Count;
            if (buffDescriptions.Count > 0) h += 40 + buffLines * 18 + (buffDescriptions.Count - 1) * 3;
            if (specialRule != null) h += 40 + Ui.F16.GetWrap(specialRule, w - 52).Count * 18;
            int upgradeDescriptionLines = 0;
            foreach (var collectionUpgrade in collectionUpgrades)
                upgradeDescriptionLines += Ui.F16.GetWrap(L(collectionUpgrade.Description), w - 52).Count;
            if (collectionUpgrades.Count > 0)
                h += 40 + collectionUpgrades.Count * 18 + upgradeDescriptionLines * 18 + collectionUpgrades.Count * 3;
            if (!string.IsNullOrWhiteSpace(upgradeDescription)) h += 42 + Ui.F16.GetWrap(upgradeDescription, w - 48).Count * 18;
            if (hovered.Locked) h += 14 + Ui.F16.GetWrap(L("LOCKED  -  BUY IT IN THE SHOP TO UNLOCK"), w - 48).Count * 18;
            h += 8;
            float x = Ui.Screen == "sets" && (game == null || game.Paused) ? 24 : mx > Ui.Width / 2 ? mx - w - 18 : mx + 18;
            float y = my > 400 ? my - h - 18 : my + 18;
            x = Math.Max(12, Math.Min(x, Ui.Width - w - 12)); y = Math.Max(12, Math.Min(y, 788 - h - 12));
            Box(x, y, w, h, C.Ink); Outline(x, y, w, h, C.Gold); CoinImage(hovered.Id, x + 14, y + 13, 66);
            Text(Lang.Upper(Lang.CoinName(hovered.Id)), x + 94, y + 16, Ui.F20, C.Face);
            string rarity = coin.Rarity == Rarity.Common ? "Common" : coin.Rarity == Rarity.Uncommon ? "Uncommon" : coin.Rarity == Rarity.Rare ? "Rare" : "Epic";
            Text(Lang.Upper(L(rarity)), x + 94, y + 47, Ui.F16, RarityColor(coin.Rarity));
            if (coin.Types.Count > 0)
            {
                string typeLabel = L("TYPE:");
                Text(typeLabel, x + 94, y + 69, Ui.F16, C.Muted);
                float typeX = x + 94 + Ui.F16.GetWidth(typeLabel) + Ui.F16.GetWidth(" ");
                for (int i = 0; i < coin.Types.Count; i++)
                {
                    var kind = coin.Types[i];
                    string name = L(kind.ToString().ToUpperInvariant()) + (kind == CoinType.Fortune && game != null && game.FortuneBonus > 0 ? L(" +%d%%", Math.Floor(game.FortuneBonus * 100 + .5)) : "");
                    Text(name, typeX, y + 69, Ui.F16, CoinTypeColor(kind));
                    typeX += Ui.F16.GetWidth(name) + Ui.F16.GetWidth(" ");
                    if (i + 1 < coin.Types.Count)
                    {
                        Text("/", typeX, y + 69, Ui.F16, C.Muted);
                        typeX += Ui.F16.GetWidth("/ ");
                    }
                }
            }
            float infoY = y + (coin.Types.Count > 0 ? 91 : 69);
            if (coin.EnergyCost > 0)
            {
                Text(L("COSTS %d ENERGY", coin.EnergyCost), x + 94, infoY, Ui.F16, C.Gold);
                infoY += 22;
            }
            if (characterAvailability != null)
            {
                Gfx.SetFont(Ui.F16); Color(C.Muted);
                Gfx.Printf(characterAvailability, x + 94, infoY, w - 114);
            }
            float rowY = y + header;
            foreach (var row in rows)
            {
                Box(x + 12, rowY, w - 24, row.Height, C.Slot); Color(row.Tint); Gfx.Rectangle(true, x + 12, rowY, 4, row.Height);
                Text(row.Label, x + 26, rowY + 7, Ui.F16, row.Tint); Text(row.Chance + "%", x + w - 78, rowY + 7, Ui.F16, row.Tint);
                Gfx.SetFont(Ui.F16); Color(C.Face); Gfx.Printf(row.Detail, x + 26, rowY + 29, w - 52); rowY += row.Height + 7;
            }
            if (buffDescriptions.Count > 0)
            {
                int lines = 0;
                foreach (var detail in buffDescriptions) lines += Ui.F16.GetWrap(detail, w - 62).Count;
                float sectionHeight = 40 + lines * 18 + (buffDescriptions.Count - 1) * 3;
                Box(x + 12, rowY, w - 24, sectionHeight, C.Slot); Outline(x + 12, rowY, w - 24, sectionHeight, C.Line);
                Color(C.Purple); Gfx.Rectangle(true, x + 12, rowY, 4, sectionHeight);
                Text(L("BUFFS"), x + 26, rowY + 7, Ui.F16, C.Purple);
                float detailY = rowY + 28;
                for (int i = 0; i < buffDescriptions.Count; i++)
                {
                    string detail = buffDescriptions[i];
                    int detailLines = Ui.F16.GetWrap(detail, w - 62).Count;
                    if (BuffTargetType(buffs[i]).HasValue)
                    {
                        Color(CoinTypeColor(BuffTargetType(buffs[i]).Value));
                        Gfx.Rectangle(true, x + 26, detailY + 2, 4, Math.Min(12, detailLines * 18));
                    }
                    Gfx.SetFont(Ui.F16); Color(C.Face); Gfx.Printf(detail, x + 36, detailY, w - 62);
                    detailY += detailLines * 18 + 3;
                }
                rowY += sectionHeight;
            }
            if (specialRule != null)
            {
                float ruleHeight = 40 + Ui.F16.GetWrap(specialRule, w - 52).Count * 18;
                Box(x + 12, rowY, w - 24, ruleHeight, C.Slot); Outline(x + 12, rowY, w - 24, ruleHeight, C.Line);
                Color(C.Gold); Gfx.Rectangle(true, x + 12, rowY, 4, ruleHeight);
                Text(L("SPECIAL RULE"), x + 26, rowY + 7, Ui.F16, C.Gold);
                Gfx.SetFont(Ui.F16); Color(C.Face); Gfx.Printf(specialRule, x + 26, rowY + 28, w - 52);
                rowY += ruleHeight;
            }
            if (collectionUpgrades.Count > 0)
            {
                float sectionHeight = 40 + collectionUpgrades.Count * 18 + upgradeDescriptionLines * 18 + collectionUpgrades.Count * 3;
                Box(x + 12, rowY, w - 24, sectionHeight, C.Slot); Outline(x + 12, rowY, w - 24, sectionHeight, C.Line);
                Color(C.Gold); Gfx.Rectangle(true, x + 12, rowY, 4, sectionHeight);
                Text("UPGRADES", x + 26, rowY + 7, Ui.F16, C.Gold);
                float detailY = rowY + 28;
                foreach (var collectionUpgrade in collectionUpgrades)
                {
                    Text(Lang.Upper(L(collectionUpgrade.Name)), x + 26, detailY, Ui.F16, C.Gold);
                    detailY += 18;
                    int descriptionLines = Ui.F16.GetWrap(L(collectionUpgrade.Description), w - 52).Count;
                    Gfx.SetFont(Ui.F16); Color(C.Face); Gfx.Printf(L(collectionUpgrade.Description), x + 26, detailY, w - 52);
                    detailY += descriptionLines * 18 + 3;
                }
                rowY += sectionHeight;
            }
            if (!string.IsNullOrWhiteSpace(upgradeDescription))
            {
                Text(L("UPGRADE: %s", Lang.Upper(L(upgrade.Name))), x + 24, rowY + 4, Ui.F16, C.Gold);
                Gfx.SetFont(Ui.F16); Color(C.Face); Gfx.Printf(upgradeDescription, x + 24, rowY + 25, w - 48);
                rowY += 42 + Ui.F16.GetWrap(upgradeDescription, w - 48).Count * 18;
            }
            if (hovered.Locked) { Gfx.SetFont(Ui.F16); Color(C.Orange); Gfx.Printf(L("LOCKED  -  BUY IT IN THE SHOP TO UNLOCK"), x + 24, rowY + 8, w - 48); }
        }

        public static void CoinFace(float cx, float cy, float radius, string outcome, bool selected, string id)
        {
            float size = radius * 2.6f;
            if (selected)
            {
                Color(C.Orange, .35f);
                Gfx.Circle(true, cx, cy, radius + 4);
            }
            CoinImage(id ?? "copper", cx - size / 2, cy - size / 2, size);
            if (outcome != null)
            {
                var tint = outcome == Side.Heads ? C.Blue : C.Red;
                float bx = cx + radius * .78f, by = cy + radius * .65f;
                Color(C.Ink);
                Gfx.Circle(true, bx, by, 15);
                Color(tint);
                Gfx.Circle(true, bx, by, 12);
                Centered(outcome.Substring(0, 1), bx - 13, by - Ui.F20.Height / 2f, 26, Ui.F20, C.Ink);
            }
        }
    }
}
