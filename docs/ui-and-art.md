# UI and art

## Principles

- **The shop is the reference.** It was the first screen to get the final look, and every other full-screen view copies it: a
  felt-green backdrop, a teal "screen" panel inset by 36 px with a gold outline, a big pixel title at the top left, a small
  button at the top right, content on a coarse grid with chunky buttons.
- **Few boxes, big targets.** Controls are large rectangular buttons with a drop shadow; panels are dark teal with a thin teal
  outline; the main object (the coin) gets the room.
- **Text is a pixel font** (`assets/fonts/m6x11plus.ttf`) in four sizes (16, 20, 32, 48) with nearest filtering. Headings are
  gold, body is cream, secondary text is muted teal-grey.
- **Information appears on hover.** Coins in lists show only their art; tooltips show details (`D.coin_hover`, `D.text_hover`).

## Canvas

The game draws on a fixed **1280 x 800** canvas that is scaled and centred in the window (and in fullscreen). Use
`ui.mouse()` and `ui.to_canvas()` to convert window to canvas coordinates, never `love.mouse.getPosition()`. A custom
pixel-art cursor (an ivory arrow; gold over anything clickable) is loaded in `app.load` and switched in
`app.update` by checking which button the mouse is over.

## Palette (`src/ui/theme.lua`)

| Token | Use |
|---|---|
| `felt`, `felt_dark` | backdrop around the screen panel |
| `screen` (#17454d) | the main teal panel |
| `panel_dk` (#0f333b) | recessed panels and cards |
| `card` (#1d4f58) | raised cards (bank slots, stage) |
| `line` (#2f6670) | thin outlines |
| `slot_dk` (#0b2a30) | empty slots, bar backgrounds |
| `marked` (#2c6a74) | marked-for-discard cards |
| `gold`, `orange`, `green`, `blue`, `red` | headings/prices, warnings, positive, Heads/energy, Tails/danger |
| `ink` | dark text on coloured buttons |
| `face`, `muted`, `white` | main, secondary and bright text |

Heads is always blue, Tails always red, gold is always gold (currency and headings), energy is blue lightning, green means
"go".

## Screens

All full-screen views start with `D.frame(title_image, back_label, back_action)` (`src/ui/draw.lua`) which draws the backdrop,
the teal panel, the gold outline, the title image and a Back/Menu button at the top right (1120, 56, 100x34).

| Screen | File | Contents |
|---|---|---|
| Title | `views/title.lua` | Logo, subtitle, a column of buttons: Continue / New Run (or Play), Coin Sets, Collection, Options, Quit (needs a second click during a run, because runs are not saved) |
| Play | `views/menu.lua` | Character card with arrows, the selected coin set (cycle with arrows), Edit Coin Sets, Start Run |
| Coin Sets | `views/sets.lua` | Character tabs, 3 set tabs, a 10-slot editor, Save Set / Clear Set, the character's coin list with locked padlocks |
| Collection | `views/collection.lua` | 5 x 3 grid per page, sort and rarity filters, black silhouettes for uncollected coins |
| How to play | `views/help.lua` | One screen with the whole loop; shown once on the first Play (profile option `seen_help`) and from the title menu |
| Options | `views/options.lua` | Screen shake, fast flip, fullscreen as ON/OFF rows, the language (English / Deutsch) and a Sound tab with three sliders (master, music, sound effects; drag or click, saved on release, the effects slider plays a sample; a Restore Defaults button puts them back to 80 / 40 / 80) and, on the Game tab, Clear Progress (a popup asks "your data will be permanently deleted" with OK / Cancel; resets unlocks, collection, coin sets and tokens, keeps options, ends a run in progress) |
| Round | `views/encounter.lua` | See below |
| Shop | `views/shop.lua` | See below |
| Run over | `views/finish.lua` | Headline, character card, levels cleared, gold, seed, reason, New Run, Back To Menu; after the boss also Endless Mode |

### The round view

```
 logo + level          POINTS 12 / 20   [bar]                MENU
                                                    coins  gold  energy
 +--COIN BANK--+   +-------------------- stage ---------------------+
 | next 3 cards|   |  [HEADS box]     (  BIG COIN  )    [TAILS box] |
 | + one line  |   |                  coin name                    |
 | PILE/OUT/   |   |                 odds bar 50%/50%              |
 | DECK        |   +-----------------------------------------------+
 +-------------+     [DISCARD]  [  FLIP  ]        [chip] [chip] [chip]
```

- **Header:** logo and level at the left, points and a progress bar in the middle, Menu at the top right, then three icon
  counters (coins left, gold, energy) with no boxes. When the quota is met, Open Shop appears next to Menu and the caption
  turns green.
- **Combo meter:** top right of the stage: `COMBO HEADS x4` and the multiplier (`x1.75`), blue for a Heads streak, red for Tails, grey below two; a green `SHIELD` line appears while an Anchor shield is up. Active buffs are listed under the bank panel.
- **Bank:** the next three coins as cards: art, name, Heads %. The bank cards are not clickable after the opening hand. The dealt coin
  has a CURRENT tab. One muted line shows the pile, discarded and deck counts.
- **Stage:** the dealt coin, large, with its Heads effect box on the left (blue) and its Tails box on the right (red). Below it
  are the coin name and a slim odds bar. After a flip a banner on the coin shows HEADS or TAILS and "+N POINTS", "NO POINTS" or
  "QUOTA +N". A small orange line shows when the roll was altered.
- **Bottom row:** Discard, Flip (becomes Next Coin, Flipping..., or "Need N energy"), and three chip slots. Hint text appears at
  the bottom left only when it matters.
- **Opening hand:** the whole screen is dimmed and the five hand cards float in the centre (a slight bob and a shadow). Marked
  cards lift with a red tag; the coin that plays first has a gold tag. Discard and Start Level stay bright.
- **Discarding:** after the opening hand only the coin in play can be discarded (Discard button); bank cards are display only. Marking exists only in the opening hand.
- **Out of coins:** a notice over the stage with Buy More Coins, Start Again and Back To Menu.

### The shop

Grid starting at x = 340 with a 150 px step: coin offers in four columns (price, art, Buy), chips under the first two columns,
the prize under the fourth. A reroll box sits at the left, the Tune-ups panel (Odds Tuner and Coin Removal for the selected deck
coin) at the right with the Next Round button under it, gold and Menu at the top right, and the deck strip across the bottom.

## Art

All art is generated by Python scripts using Pillow so it can be changed by editing a colour or an emblem and re-running.

| Script | Produces |
|---|---|
| `tools/gen_coin_icons.py` | `assets/coins/<id>.png` (512 px, mipmapped) for every coin plus a card back |
| `tools/gen_app_icon.py` | `assets/ui/icon.png`, the window icon (a gold coin with a pixel T; `conf.lua` sets it as `t.window.icon`), and on macOS `assets/ui/icon.icns` for the dock icon of a packaged app |
| `tools/gen_title_scene.py` | `assets/ui/title_scene.png`: the main menu backdrop (1280x800 pixel-art casino back room drawn at 320x200 and scaled 4x; the menu panel sits on its darkened left side) |
| `tools/gen_ui_icons.py` | `assets/items`, `assets/relics` (256 px emblems), `assets/ui` (icons: next round, reroll, gold, energy, coins left, open shop, exchange, give up, flip, discard, next coin, start level; title images: logo, shop, play, sets, collection, options; cursors) |

Characters are painted portraits in `assets/characters/`.

The title images use the same pixel font with letters cycling through gold, red, blue, green with a dark outline and a drop
shadow, upscaled with nearest filtering so they stay crisp.

## Keyboard and the Controls tab

The keyboard uses the same actions as the controller (`app.keypressed` maps keys onto `Pad.pressed`): arrows move the focus ring, **Enter** presses the focused button (the main action is focused by
default: Flip, Start Level, Next Round, Start Run, Cancel in popups), **Esc** = back / menu, **Space** = flip / next coin, **D** = discard the coin in play, **1 / 2 / 3** = use chip 1 / 2 / 3 in a level (or choose
a character on the play screen), **O** = open the shop once the quota is met, **Q / E** = previous / next page, tab or character, **F3** = debug info.
Options has a third tab, **Controls**, with a Keyboard / Controller switch (it starts on whichever device you used last) listing what every key or button does, grouped into Menus and In a level,
drawn as key caps (A green, B red, X blue, Y gold). The tables are `KEYBOARD` and `CONTROLLER` in `src/ui/views/options.lua`; update them when a binding changes.

## Inspect (details without a mouse)

Tooltips normally follow the mouse. With the keyboard or a controller, **Inspect** (`I`, `Q`, `E`, or LB / RB / LT / RT during a run) toggles a mode in which the tooltip of the focused item is shown (coin, chip or prize details in the
shop and in a level; coins in the coin-set editor and the collection) and follows the focus as you move; press again or move the mouse to stop. Every hover area drawn in a frame is
recorded in `ui.regions`; `Pad.inspect` picks the one a focused button contains or sits directly under (a shop Buy button under the coin art it sells) and sets `ui.hover_anchor`, which `ui.pointer()` gives the tooltip code.
Disabled buttons (FULL, too expensive, Flip during the animation) stay in `ui.buttons` with `disabled = true`: they can be focused and inspected but not pressed.

## Shortcut badges

While a gamepad is connected (`Pad.connected`), every button that has a controller shortcut wears a small badge on its top edge: **X** on Flip / Next coin and Start Level, **Y** on Discard, **B** on Back
buttons, Cancel and Skip, **START** on Menu. A button gets one through the optional last parameter of `D.button` / `D.icon_button` (`hotkey`, stored in `ui.buttons`); `Pad.draw` draws them in the
button colours of the pad (A green, B red, X blue, Y gold). When a shortcut changes, change both the call site and the `KEYBOARD` / `CONTROLLER` tables of the Controls tab.

## Controller

`src/ui/pad.lua`. The D-pad or left stick moves a **focus ring** between the clickable buttons of the current screen (the same list the mouse uses: `ui.buttons`); the nearest button in
the pressed direction wins; when nothing lies that way (a vertical menu has no left / right) left / right step through the buttons in reading order and up / down wrap around, and sliders change by 5% with left / right. **A** presses the focused button, **B** or **Start** = Esc (menu / close / skip tutorial), **X** = Space (flip / next coin),
**Y** = discard the coin in play, **LB / RB / LT / RT** = previous / next page, tab or character of the current menu screen (play screen: character, collection: page, options: tab, coin sets: character tab; the triggers are analog axes, handled as a press at half way). Each screen starts with its main action focused (Flip, Next Round, the first menu entry, OK in a popup).
If the focused button is unavailable for a moment (Flip during the flip animation) there is no focus and no ring, and A does nothing; it never jumps to another button. The ring appears after the first controller input and disappears when the mouse moves. Popups and tutorial steps already narrow `ui.buttons`, so the controller follows the same rules as the mouse.

The window is resizable (minimum 640 x 400); the 1280 x 800 canvas scales and centres itself.

## Tutorial

An interactive tutorial (title menu: TUTORIAL; also shown automatically the first time you press Play; Esc or SKIP leaves it) in `src/ui/tutorial.lua`. It plays over a **throwaway run**
(5 Normal coins, quota 2, the first three flips forced to Heads, nothing saved or logged: `game.tutorial`). Each of the 19 steps dims the screen except a **spotlight
rectangle** (orange outline) with a card beside it. A step either waits for a click ("click anywhere to continue") or is a **"do it" step** (`wait` predicate: press Start Level, Flip,
Next Coin, Open Shop, Next Round) where only the buttons inside the spotlight work and the tutorial moves on by itself when the game reaches the expected state.
Covered (19 steps): quota, resources, opening hand, bank, coin in play, flip, result, chips, discard, combo, quota met and payout, shop, tune-ups, deck and slots, next round, level modifiers, the exchange limit, character unlocks. A step can have an `enter` function that sets the scene (the modifier step starts level 2). To add a step, append to `STEPS`
(rectangle in canvas coordinates, text, optional `wait`) and add the German text to `locales/de.lua`. The older text page (HOW TO PLAY) stays as a reference.

## Sound

13 short effects in `assets/sfx/*.wav`, generated with plain Python by `tools/gen_sounds.py` (click, flip, land_heads, land_tails, score, penalty, combo, discard, buy, shop,
levelup, win, lose). The rules never play sound: `src/ui/sound.lua` `Sound.watch` compares the game state every frame (animation start/end, a new result, quota
met, discards, phase changes, gold spent in the shop) and plays what changed; buttons play `click`. Score and combo sounds rise in pitch with the amount. The music is a 32 s chiptune loop (Am F C G, a melody enters in the second half) in `assets/music/theme.wav`, also made by `gen_sounds.py`, streamed and looped from game start.
Volumes are profile options `volume_master`, `volume_music`, `volume_sfx` (0-100); `Sound.apply` combines them. A missing audio device just means silence.

## Popups

`ui.confirm = {title, text, ok, single}` shows a modal (dimmed screen, OK / Cancel; Esc cancels) drawn last by `D.confirm_dialog`; while it is open every other clickable is ignored.
Used by Clear Progress and by Quit during a run (runs are not saved); with `single = true` it is a notice with only OK, used for the GAME OVER message before the run-over screen.

## Language

The UI is English in the code and German through `locales/de.lua` (see [architecture.md](architecture.md)). Rules for new text:
write the English string in the view, add the same string as a key in `locales/de.lua`, and use a format key for text with numbers
(`D.L("LEVEL %d / 4", n)`), never string concatenation, so the translation can reorder words. Heads is "Kopf" and Tails is "Zahl".
The pixel font has ä ö ü Ä Ö Ü ß; `string.upper` is extended for umlauts. Screen titles are images, so each language needs its own set
(`title_play_de.png` and so on, generated by `tools/gen_ui_icons.py`). German is about 30% longer than English: check the layout.

## Keeping it consistent

- Start a new full-screen view with `D.frame(...)`.
- Keep 36 px between the window edge and the teal panel, 70 px left and right content margins.
- Text colour by meaning: gold = headings and currency, blue = Heads/energy, red = Tails/danger, green = positive/go.
- Use `D.icon_button` for primary actions, plain `D.button` for secondary ones.
- Register every clickable in `ui.buttons`; the last one registered wins when they overlap (the cursor also reads this list).
- Draw overlays last so their buttons are registered last.
- Do not put game logic in a view; call `src/ui/actions.lua`.
