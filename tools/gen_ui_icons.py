#!/usr/bin/env python3
"""Generate flat icons for items (chips), relics (prizes) and UI pieces.

Same flat style as gen_coin_icons.py: a solid body, a dark outline, one inner ring and an ivory
emblem, drawn at 4x and reduced for smooth edges. Shapes are authored in a 128x128 space.
Output (PNG, transparent background):
  assets/items/<id>.png    256 px   rounded-square chips
  assets/relics/<id>.png   256 px   round prizes with a double ring
  assets/ui/<name>.png     256 px   next_round (the big red button), reroll, gold, energy, coins_left

Run from the repo root:  python3 tools/gen_ui_icons.py   (needs Pillow).
To add an icon: add a colour to ITEMS / RELICS and an emblem branch in emblem().
"""

from pathlib import Path
import math
from PIL import Image, ImageDraw

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


def main():
    for name, colour in ITEMS.items():
        make_icon("items", name, colour)
    for name, colour in RELICS.items():
        make_icon("relics", name, colour)
    for fn in (next_round, reroll, gold, energy, coins_left):
        fn()
    print(f"generated {len(ITEMS)} item icons, {len(RELICS)} relic icons and 5 UI icons in {ROOT / 'assets'}")


if __name__ == "__main__":
    main()
