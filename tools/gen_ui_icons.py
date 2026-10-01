#!/usr/bin/env python3
"""Generate flat icons for items (chips), relics (prizes) and UI pieces.

Same flat style as gen_coin_icons.py: a solid body, a dark outline, one inner ring and an ivory
emblem, drawn at 4x and reduced for smooth edges. Shapes are authored in a 128x128 space.
Output (PNG, transparent background):
  assets/items/<id>.png    256 px   rounded-square chips
  assets/relics/<id>.png   256 px   round prizes with a double ring
  assets/ui/<name>.png     256 px   next_round (the big red button), reroll, gold, energy, coins_left,
                                    open_shop, exchange, give_up, flip, discard, next_coin, start_level
  assets/ui/shop_title.png 544x256, assets/ui/logo.png 800x256,
  assets/ui/title_<name>.png  screen titles (play, sets, collection, options) in the same pixel-letter style,
  assets/ui/cursor_arrow.png, cursor_click.png  32 px mouse cursors (an arrow with a small coin)  the colourful "SHOP" title (drawn from the pixel font)

Run from the repo root:  python3 tools/gen_ui_icons.py   (needs Pillow).
To add an icon: add a colour to ITEMS / RELICS and an emblem branch in emblem().
"""

from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
SCALE = 4
SIZE = 128  # authoring space
PIXELS = SIZE * SCALE
OUT_SIZE = 256

IVORY = (247, 239, 215, 255)
DARK = (31, 36, 43, 255)

ITEMS = {
    "force_heads": "3f8fd8",
    "force_tails": "d9534f",
    "weighted": "8a7a63",
    "double_down": "8f5fb0",
    "swap": "3f9bb0",
    "peek": "4f9a6a",
    "extra_draw": "e0902a",
}
RELICS = {
    "magnet": "c0463c",
    "penny": "b9784a",
    "clock": "c4a13a",
    "metronome": "6f8fc4",
    "baton": "b0689a",
}


def rgb(value):
    return tuple(int(value[i : i + 2], 16) for i in (0, 2, 4))


def blend(a, b, amount):
    return tuple(round(x * (1 - amount) + y * amount) for x, y in zip(a, b))


def p(value):
    return round(value * SCALE)


def box(x0, y0, x1, y1):
    return p(x0), p(y0), p(x1), p(y1)


def points(coords):
    return [(p(x), p(y)) for x, y in coords]


def canvas():
    return Image.new("RGBA", (PIXELS, PIXELS), (0, 0, 0, 0))


def finish(image, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    image.resize((OUT_SIZE, OUT_SIZE), Image.Resampling.LANCZOS).save(path)


def chip_body(colour):
    """Rounded-square chip."""
    image = canvas()
    d = ImageDraw.Draw(image)
    body = rgb(colour)
    d.rounded_rectangle(box(12, 12, 116, 116), radius=p(20), fill=body + (255,),
                        outline=blend(body, (15, 20, 26), .66) + (255,), width=p(5))
    d.rounded_rectangle(box(22, 22, 106, 106), radius=p(14),
                        outline=blend(body, (13, 17, 24), .33) + (255,), width=p(3))
    return image


def prize_body(colour):
    """Round prize with a double ring, so relics read differently from coins."""
    image = canvas()
    d = ImageDraw.Draw(image)
    body = rgb(colour)
    d.ellipse(box(10, 10, 118, 118), fill=body + (255,),
              outline=blend(body, (15, 20, 26), .66) + (255,), width=p(5))
    d.ellipse(box(19, 19, 109, 109), outline=blend(body, (255, 240, 192), .45) + (255,), width=p(3))
    d.ellipse(box(26, 26, 102, 102), outline=blend(body, (13, 17, 24), .33) + (255,), width=p(2))
    return image


def emblem(d, name):
    w = p(5)
    if name == "force_heads":  # the letter H
        d.rectangle(box(40, 34, 53, 94), fill=IVORY)
        d.rectangle(box(75, 34, 88, 94), fill=IVORY)
        d.rectangle(box(53, 58, 75, 70), fill=IVORY)
    elif name == "force_tails":  # the letter T
        d.rectangle(box(36, 34, 92, 48), fill=IVORY)
        d.rectangle(box(57, 48, 71, 94), fill=IVORY)
    elif name == "weighted":  # a kettlebell
        d.polygon(points([(40, 62), (88, 62), (96, 96), (32, 96)]), fill=IVORY)
        d.arc(box(48, 30, 80, 70), 180, 360, fill=IVORY, width=p(8))
    elif name == "double_down":  # two chevrons pointing down
        for y in (40, 62):
            d.line(points([(40, y), (64, y + 22), (88, y)]), fill=IVORY, width=p(9))
    elif name == "swap":  # two opposing arrows
        d.polygon(points([(36, 48), (70, 48), (70, 38), (92, 54), (70, 70), (70, 60), (36, 60)]), fill=IVORY)
        d.polygon(points([(92, 76), (58, 76), (58, 66), (36, 82), (58, 98), (58, 88), (92, 88)]), fill=IVORY)
    elif name == "peek":  # an eye
        d.ellipse(box(26, 46, 102, 82), outline=IVORY, width=w)
        d.ellipse(box(54, 50, 74, 78), fill=IVORY)
        d.ellipse(box(60, 58, 68, 66), fill=DARK)
    elif name == "extra_draw":  # a coin coming back around
        d.arc(box(34, 34, 94, 94), 40, 320, fill=IVORY, width=p(8))
        d.polygon(points([(88, 26), (104, 52), (76, 52)]), fill=IVORY)
    elif name == "magnet":  # a horseshoe magnet, tips pointing up
        d.arc(box(36, 46, 92, 102), 0, 180, fill=IVORY, width=p(14))
        d.rectangle(box(36, 28, 50, 76), fill=IVORY)
        d.rectangle(box(78, 28, 92, 76), fill=IVORY)
        d.rectangle(box(36, 28, 50, 40), fill=DARK)
        d.rectangle(box(78, 28, 92, 40), fill=DARK)
    elif name == "penny":  # a coin with a 1
        d.ellipse(box(34, 34, 94, 94), outline=IVORY, width=w)
        d.rectangle(box(60, 48, 69, 82), fill=IVORY)
        d.polygon(points([(60, 48), (52, 56), (60, 58)]), fill=IVORY)
    elif name == "clock":  # a cracked clock face
        d.ellipse(box(32, 32, 96, 96), outline=IVORY, width=w)
        d.line(points([(64, 64), (64, 44)]), fill=IVORY, width=p(5))
        d.line(points([(64, 64), (78, 70)]), fill=IVORY, width=p(5))
        d.line(points([(80, 34), (72, 48), (82, 54), (74, 68)]), fill=DARK, width=p(3))
    elif name == "metronome":  # a pyramid with a swinging arm
        d.polygon(points([(48, 92), (80, 92), (72, 38), (56, 38)]), fill=IVORY)
        d.line(points([(64, 84), (80, 42)]), fill=DARK, width=p(4))
        d.ellipse(box(74, 40, 86, 52), fill=DARK)
    elif name == "baton":  # a conductor's baton
        d.line(points([(44, 90), (88, 38)]), fill=IVORY, width=p(7))
        d.ellipse(box(36, 84, 50, 98), fill=IVORY)
    else:
        raise ValueError(name)


def make_icon(kind, name, colour):
    image = chip_body(colour) if kind == "items" else prize_body(colour)
    layer = canvas()
    emblem(ImageDraw.Draw(layer), name)
    finish(Image.alpha_composite(image, layer), ROOT / "assets" / kind / f"{name}.png")


# ---------------------------------------------------------------- UI pieces
def next_round():
    """The big red button: a glossy-free flat dome with a right arrow."""
    image = canvas()
    d = ImageDraw.Draw(image)
    red = (217, 46, 41)
    d.ellipse(box(6, 14, 122, 124), fill=blend(red, (0, 0, 0), .45) + (255,))  # the base it sits on
    d.ellipse(box(6, 6, 122, 116), fill=red + (255,), outline=blend(red, (15, 20, 26), .66) + (255,), width=p(5))
    d.ellipse(box(18, 18, 110, 104), outline=blend(red, (255, 200, 190), .35) + (255,), width=p(3))
    d.polygon(points([(36, 52), (70, 52), (70, 38), (98, 61), (70, 84), (70, 70), (36, 70)]), fill=IVORY)
    finish(image, ROOT / "assets" / "ui" / "next_round.png")


def reroll():
    image = canvas()
    d = ImageDraw.Draw(image)
    body = (90, 110, 120)
    d.ellipse(box(10, 10, 118, 118), fill=body + (255,), outline=blend(body, (15, 20, 26), .66) + (255,), width=p(5))
    d.arc(box(32, 32, 96, 96), 200, 340, fill=IVORY, width=p(8))
    d.arc(box(32, 32, 96, 96), 20, 160, fill=IVORY, width=p(8))
    d.polygon(points([(92, 30), (98, 56), (72, 50)]), fill=IVORY)
    d.polygon(points([(36, 98), (30, 72), (56, 78)]), fill=IVORY)
    finish(image, ROOT / "assets" / "ui" / "reroll.png")


def gold():
    image = canvas()
    d = ImageDraw.Draw(image)
    body = (243, 185, 88)
    d.ellipse(box(10, 10, 118, 118), fill=body + (255,), outline=blend(body, (15, 20, 26), .66) + (255,), width=p(5))
    d.ellipse(box(24, 24, 104, 104), outline=blend(body, (13, 17, 24), .33) + (255,), width=p(4))
    d.rectangle(box(56, 40, 72, 88), fill=blend(body, (13, 17, 24), .55) + (255,))
    finish(image, ROOT / "assets" / "ui" / "gold.png")


def energy():
    image = canvas()
    d = ImageDraw.Draw(image)
    body = (0, 157, 255)
    d.ellipse(box(10, 10, 118, 118), fill=body + (255,), outline=blend(body, (15, 20, 26), .66) + (255,), width=p(5))
    d.polygon(points([(74, 20), (42, 70), (62, 70), (52, 108), (88, 54), (68, 54)]), fill=IVORY)
    finish(image, ROOT / "assets" / "ui" / "energy.png")


def coins_left():
    image = canvas()
    d = ImageDraw.Draw(image)
    body = (150, 160, 168)
    for i, y in enumerate((84, 62, 40)):
        d.ellipse(box(22, y, 106, y + 40), fill=body + (255,),
                  outline=blend(body, (15, 20, 26), .66) + (255,), width=p(4))
        d.ellipse(box(40, y + 8, 88, y + 28), outline=blend(body, (13, 17, 24), .33) + (255,), width=p(3))
    finish(image, ROOT / "assets" / "ui" / "coins_left.png")


def open_shop():
    """A shop front: striped awning over a door, on a green disc."""
    image = canvas()
    d = ImageDraw.Draw(image)
    body = (75, 194, 146)
    d.ellipse(box(10, 10, 118, 118), fill=body + (255,), outline=blend(body, (15, 20, 26), .66) + (255,), width=p(5))
    d.rectangle(box(36, 58, 92, 94), fill=IVORY)
    d.rectangle(box(56, 70, 72, 94), fill=blend(body, (13, 17, 24), .55) + (255,))
    for i in range(4):  # awning stripes
        x0 = 30 + i * 17
        d.polygon(points([(x0, 36), (x0 + 17, 36), (x0 + 18, 58), (x0 - 1, 58)]),
                  fill=((217, 46, 41, 255) if i % 2 == 0 else IVORY))
    finish(image, ROOT / "assets" / "ui" / "open_shop.png")


def exchange():
    """Two arrows around a coin: paying for coins."""
    image = canvas()
    d = ImageDraw.Draw(image)
    body = (243, 185, 88)
    d.ellipse(box(10, 10, 118, 118), fill=body + (255,), outline=blend(body, (15, 20, 26), .66) + (255,), width=p(5))
    d.ellipse(box(46, 46, 82, 82), fill=IVORY, outline=DARK, width=p(3))
    d.arc(box(26, 26, 102, 102), 205, 335, fill=DARK, width=p(7))
    d.arc(box(26, 26, 102, 102), 25, 155, fill=DARK, width=p(7))
    d.polygon(points([(100, 26), (104, 54), (78, 44)]), fill=DARK)
    d.polygon(points([(28, 102), (24, 74), (50, 84)]), fill=DARK)
    finish(image, ROOT / "assets" / "ui" / "exchange.png")


def give_up():
    """A white flag on a pole."""
    image = canvas()
    d = ImageDraw.Draw(image)
    body = (254, 95, 85)
    d.ellipse(box(10, 10, 118, 118), fill=body + (255,), outline=blend(body, (15, 20, 26), .66) + (255,), width=p(5))
    d.rectangle(box(40, 28, 48, 100), fill=IVORY)
    d.polygon(points([(48, 30), (94, 42), (48, 62)]), fill=IVORY)
    finish(image, ROOT / "assets" / "ui" / "give_up.png")


def disc(colour):
    image = canvas()
    d = ImageDraw.Draw(image)
    d.ellipse(box(10, 10, 118, 118), fill=colour + (255,), outline=blend(colour, (15, 20, 26), .66) + (255,), width=p(5))
    return image, d


def save_ui(image, name):
    finish(image, ROOT / "assets" / "ui" / f"{name}.png")


def flip():
    """A coin turning edge-on with a curved arrow."""
    image, d = disc((0, 157, 255))
    d.ellipse(box(44, 34, 84, 94), fill=IVORY, outline=DARK, width=p(3))
    d.ellipse(box(52, 44, 76, 84), outline=DARK, width=p(3))
    d.arc(box(22, 22, 106, 106), 200, 290, fill=IVORY, width=p(7))
    d.polygon(points([(70, 16), (90, 26), (70, 38)]), fill=IVORY)
    save_ui(image, "flip")


def discard():
    """A bin."""
    image, d = disc((254, 95, 85))
    d.rectangle(box(40, 40, 88, 46), fill=IVORY)
    d.rectangle(box(54, 32, 74, 42), fill=IVORY)
    d.polygon(points([(44, 50), (84, 50), (80, 98), (48, 98)]), fill=IVORY)
    for x in (56, 64, 72):
        d.line(points([(x, 58), (x, 90)]), fill=blend((254, 95, 85), (13, 17, 24), .5) + (255,), width=p(3))
    save_ui(image, "discard")


def next_coin():
    """Two chevrons to the right: on to the next coin."""
    image, d = disc((0, 157, 255))
    for x in (38, 62):
        d.line(points([(x, 36), (x + 24, 64), (x, 92)]), fill=IVORY, width=p(10))
    save_ui(image, "next_coin")


def start_level():
    """A play triangle."""
    image, d = disc((75, 194, 146))
    d.polygon(points([(48, 34), (48, 94), (98, 64)]), fill=IVORY)
    save_ui(image, "start_level")


def shop_title():
    """The SHOP title: big pixel letters, each in its own colour, with a dark outline and a drop shadow."""
    font = ImageFont.truetype(str(ROOT / "assets" / "fonts" / "m6x11plus.ttf"), 48)
    letters = [("S", (255, 162, 0)), ("H", (254, 95, 85)), ("O", (0, 157, 255)), ("P", (75, 194, 146))]
    small = Image.new("RGBA", (136, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(small)
    x = 5
    for i, (letter, colour) in enumerate(letters):
        y = 6 + (3 if i % 2 else 0)  # a slight bounce
        d.text((x + 3, y + 3), letter, font=font, fill=(0, 0, 0, 120), stroke_width=2, stroke_fill=(0, 0, 0, 120))
        d.text((x, y), letter, font=font, fill=colour + (255,), stroke_width=2, stroke_fill=DARK)
        x += 31
    big = small.resize((544, 256), Image.Resampling.NEAREST)  # keep the pixel look
    (ROOT / "assets" / "ui").mkdir(parents=True, exist_ok=True)
    big.save(ROOT / "assets" / "ui" / "shop_title.png")


def logo():
    """The TOSSUP logo, same pixel-letter treatment as the SHOP title."""
    font = ImageFont.truetype(str(ROOT / "assets" / "fonts" / "m6x11plus.ttf"), 48)
    colours = [(255, 162, 0), (254, 95, 85), (0, 157, 255), (75, 194, 146), (255, 162, 0), (254, 95, 85)]
    small = Image.new("RGBA", (200, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(small)
    x = 5
    for i, (letter, colour) in enumerate(zip("TOSSUP", colours)):
        y = 6 + (3 if i % 2 else 0)
        d.text((x + 3, y + 3), letter, font=font, fill=(0, 0, 0, 120), stroke_width=2, stroke_fill=(0, 0, 0, 120))
        d.text((x, y), letter, font=font, fill=colour + (255,), stroke_width=2, stroke_fill=DARK)
        x += 31
    small.resize((800, 256), Image.Resampling.NEAREST).save(ROOT / "assets" / "ui" / "logo.png")


TITLE_COLOURS = [(255, 162, 0), (254, 95, 85), (0, 157, 255), (75, 194, 146)]


def pixel_title(word, name):
    """Pixel-font letters cycling through the shop colours, with outline and drop shadow, size from the text."""
    font = ImageFont.truetype(str(ROOT / "assets" / "fonts" / "m6x11plus.ttf"), 48)
    advance = 31
    small = Image.new("RGBA", (advance * len(word) + 10, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(small)
    x, index = 5, 0
    for i, letter in enumerate(word):
        if letter != " ":
            colour = TITLE_COLOURS[index % len(TITLE_COLOURS)]
            y = 6 + (3 if index % 2 else 0)
            d.text((x + 3, y + 3), letter, font=font, fill=(0, 0, 0, 120), stroke_width=2, stroke_fill=(0, 0, 0, 120))
            d.text((x, y), letter, font=font, fill=colour + (255,), stroke_width=2, stroke_fill=DARK)
            index += 1
        x += advance if letter != " " else 18
    big = small.resize((small.width * 4, small.height * 4), Image.Resampling.NEAREST)
    big.save(ROOT / "assets" / "ui" / f"title_{name}.png")


def cursor(path_name, fill, coin_open):
    """A 32 px mouse cursor: an arrow with a small coin on its tail. Hotspot is the arrow tip (2, 2)."""
    s = 8  # supersample
    img = Image.new("RGBA", (32 * s, 32 * s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    arrow = [(2, 2), (2, 25), (8, 19), (12, 28), (17, 26), (13, 17), (21, 17)]
    d.polygon([(x * s, y * s) for x, y in arrow], fill=fill + (255,), outline=DARK, width=2 * s)
    cx, cy = 24, 24
    if coin_open:  # coin seen face on
        d.ellipse(((cx - 6) * s, (cy - 6) * s, (cx + 6) * s, (cy + 6) * s), fill=(243, 185, 88, 255), outline=DARK, width=2 * s)
        d.ellipse(((cx - 2) * s, (cy - 2) * s, (cx + 2) * s, (cy + 2) * s), fill=(160, 110, 30, 255))
    else:  # coin edge-on: it is being "pressed"
        d.ellipse(((cx - 2) * s, (cy - 6) * s, (cx + 2) * s, (cy + 6) * s), fill=(243, 185, 88, 255), outline=DARK, width=2 * s)
    img.resize((32, 32), Image.Resampling.LANCZOS).save(ROOT / "assets" / "ui" / path_name)


def main():
    for name, colour in ITEMS.items():
        make_icon("items", name, colour)
    for name, colour in RELICS.items():
        make_icon("relics", name, colour)
    for fn in (next_round, reroll, gold, energy, coins_left, open_shop, exchange, give_up, flip, discard,
               next_coin, start_level, shop_title, logo):
        fn()
    for word, name in (("PLAY", "play"), ("COIN SETS", "sets"), ("COLLECTION", "collection"), ("OPTIONS", "options"), ("HOW TO PLAY", "help"),
                       ("SPIELEN", "play_de"), ("MÜNZSETS", "sets_de"), ("SAMMLUNG", "collection_de"),
                       ("OPTIONEN", "options_de"), ("ANLEITUNG", "help_de")):
        pixel_title(word, name)
    cursor("cursor_arrow.png", IVORY[:3], True)
    cursor("cursor_click.png", (243, 185, 88), False)
    print(f"generated {len(ITEMS)} item icons, {len(RELICS)} relic icons and 14 UI images in {ROOT / 'assets'}")


if __name__ == "__main__":
    main()
