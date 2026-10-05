#!/usr/bin/env python3
"""Generate deterministic 128px coin portraits and one face-down coin.

Inspired by the pachinko project's gen_marble_icons.py: draw an emblem on a
colored round body, then add rim, bevel, shadow and a soft top-left highlight.
Pillow is only needed to regenerate these PNGs; the game loads the checked-in
assets directly.
"""

from pathlib import Path
import math
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / "assets" / "Resources"
# Keep generated art in the same Unity/LÖVE resource tree used by both runtimes.
OUT = RESOURCES / "coins"
SCALE = 8  # supersampling: shapes are drawn at 8x, then reduced for smooth edges
SIZE = 128  # coordinate space the shapes are authored in
OUT_SIZE = 512  # PNG resolution; the game shows coins up to ~300px
PIXELS = SIZE * SCALE

COLORS = {
    "echo": "7e8fc2",
    "vampire": "8c2f4a",
    "miser": "b59b3c",
    "fuse": "d06a2a",
    "phoenix": "e0553b",
    "contrarian": "5f6f8f",
    "chain": "8a93a0",
    "bank": "4f9a6a",
    "lucky_seven": "c7a63a",
    "hourglass": "b08a5a",
    "capacitor": "3f9bb0",
    "martyr": "a84a4a",
    "bounty": "a8823a",
    "jester": "8f5fb0",
    "flock": "6fae8a",
    "normal": "9aa5ab",
    "copper": "ba774b",
    "sword": "7799b1",
    "lucky": "57a77a",
    "cursed": "775a9b",
    "loaded": "c69a43",
    "dagger": "4f82ab",
    "hammer": "718793",
    "blood": "aa4b54",
    "spark": "44a7b4",
    "focus": "927eb3",
    "snowball": "8fb8d8",
    "gambler": "b0475f",
    "momentum": "d98a3d",
    "megaphone": "d4682e",
    "cheerleader": "d9609a",
    "mirror": "86b8c9",
    "twin": "7a6fb3",
    "pot": "a67c4b",
    "domino": "5c6b7a",
    "hot_hand": "d9552e",
    "anchor": "4a7aa8",
    "bettor": "9a4f7a",
    "cash_out": "4f9a58",
    "cold_streak": "6fa8c9",
    "amplifier": "e0a030",
    "true_echo": "6a7ec9",
    "doubler": "c9a82a",
    "jackpot": "d4a017",
    "mimic": "8a5fa0",
    "orchestra": "5a8f6f",
    "lifeline": "d9534f",
    "horoscope": "4a5fa8",
    "crystal_ball": "6f9fd0",
    "compost": "786a46",
    "square_dance": "9b6bc0",
    "good_dog": "b88960",
    "whetstone": "6d8490",
    "blood_pact": "8f394d",
    "counterfeiter": "518774",
    "doppelganger": "79549d",
    "conductor": "487e9b",
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


def emblem(image, coin_id):
    """High contrast stamps share an off-white face and a deep inset shadow."""
    d = ImageDraw.Draw(image)
    ivory = (247, 239, 215, 255)
    dark = (31, 36, 43, 255)
    width = p(5)

    if coin_id == "copper":
        for y, x in ((72, 47), (64, 59), (56, 47)):
            d.ellipse(box(x, y, x + 30, y + 13), fill=ivory, outline=dark, width=p(2))
            d.arc(box(x + 4, y + 3, x + 26, y + 10), 185, 350, fill=dark, width=p(2))
    elif coin_id == "sword":
        d.polygon(points([(67, 30), (76, 45), (68, 79), (60, 79), (58, 45)]), fill=ivory)
        d.polygon(points([(67, 34), (71, 47), (67, 74)]), fill=(163, 200, 213, 255))
        d.line(points([(48, 78), (86, 78)]), fill=ivory, width=width)
        d.line(points([(67, 80), (67, 98)]), fill=ivory, width=p(6))
        d.ellipse(box(62, 94, 72, 104), fill=ivory)
    elif coin_id == "lucky":
        for x, y in ((48, 43), (65, 43), (48, 60), (65, 60)):
            d.ellipse(box(x, y, x + 17, y + 17), fill=ivory)
        d.ellipse(box(60, 55, 70, 65), fill=ivory)
        d.line(points([(64, 65), (73, 87), (81, 94)]), fill=ivory, width=p(5))
        d.ellipse(box(58, 52, 70, 64), fill=(69, 129, 94, 255))
    elif coin_id == "cursed":
        d.ellipse(box(44, 38, 84, 76), fill=ivory)
        d.polygon(points([(49, 69), (79, 69), (76, 88), (52, 88)]), fill=ivory)
        d.ellipse(box(50, 53, 60, 63), fill=dark)
        d.ellipse(box(68, 53, 78, 63), fill=dark)
        d.polygon(points([(64, 62), (60, 69), (68, 69)]), fill=dark)
        for x in (56, 64, 72):
            d.line(points([(x, 79), (x, 88)]), fill=dark, width=p(2))
    elif coin_id == "loaded":
        d.rounded_rectangle(box(43, 42, 85, 85), radius=p(5), fill=ivory, outline=dark, width=p(2))
        for x, y in ((53, 53), (75, 53), (64, 64), (53, 75), (75, 75)):
            d.ellipse(box(x - 3, y - 3, x + 3, y + 3), fill=dark)
    elif coin_id == "dagger":
        d.polygon(points([(88, 36), (79, 63), (62, 79), (57, 72), (73, 55)]), fill=ivory)
        d.line(points([(51, 67), (65, 82)]), fill=ivory, width=p(6))
        d.line(points([(56, 78), (43, 91)]), fill=ivory, width=p(7))
        d.ellipse(box(39, 86, 48, 95), fill=ivory)
    elif coin_id == "hammer":
        d.line(points([(51, 91), (75, 47)]), fill=ivory, width=p(10))
        d.polygon(points([(55, 43), (65, 31), (88, 42), (78, 59)]), fill=ivory)
        d.polygon(points([(55, 43), (65, 31), (70, 34), (60, 47)]), fill=(181, 208, 216, 255))
        d.line(points([(49, 94), (55, 98)]), fill=dark, width=p(3))
    elif coin_id == "blood":
        d.polygon(points([(64, 27), (85, 61), (87, 74), (80, 87), (65, 93),
                          (50, 89), (41, 76), (43, 62)]), fill=ivory)
        d.ellipse(box(42, 53, 87, 94), fill=ivory)
        d.polygon(points([(55, 52), (64, 31), (70, 51)]), fill=ivory)
        d.arc(box(49, 58, 76, 85), 130, 225, fill=(248, 169, 174, 255), width=p(5))
    elif coin_id == "spark":
        d.polygon(points([(72, 28), (48, 65), (64, 65), (55, 99),
                          (85, 56), (69, 56)]), fill=ivory)
        d.line(points([(47, 44), (40, 38)]), fill=ivory, width=p(3))
        d.line(points([(90, 78), (96, 84)]), fill=ivory, width=p(3))
    elif coin_id == "focus":
        for radius, stroke in ((27, 5), (16, 5)):
            d.ellipse(box(64 - radius, 64 - radius, 64 + radius, 64 + radius),
                      outline=ivory, width=p(stroke))
        d.ellipse(box(58, 58, 70, 70), fill=ivory)
        d.line(points([(64, 24), (64, 34)]), fill=ivory, width=p(4))
        d.line(points([(64, 94), (64, 104)]), fill=ivory, width=p(4))
    elif coin_id == "snowball":
        for cx, cy, r in ((56, 76, 15), (74, 64, 13), (62, 48, 10)):
            d.ellipse(box(cx - r, cy - r, cx + r, cy + r), fill=ivory, outline=dark, width=p(2))
    elif coin_id == "gambler":
        d.polygon(points([(64, 30), (88, 64), (64, 98), (40, 64)]), fill=ivory)
        d.polygon(points([(64, 46), (76, 64), (64, 82), (52, 64)]), fill=dark)
    elif coin_id == "momentum":
        for y in (46, 66):
            d.line(points([(46, y + 14), (64, y - 4), (82, y + 14)]), fill=ivory, width=p(8))
    elif coin_id == "normal":
        d.ellipse(box(46, 46, 82, 82), outline=ivory, width=p(5))
    elif coin_id == "echo":
        for r in (11, 25):
            d.ellipse(box(64 - r, 64 - r, 64 + r, 64 + r), outline=ivory, width=p(5))
    elif coin_id == "vampire":
        d.polygon(points([(48, 44), (64, 44), (56, 92)]), fill=ivory)
        d.polygon(points([(64, 44), (80, 44), (72, 92)]), fill=ivory)
    elif coin_id == "miser":
        d.rounded_rectangle(box(44, 52, 84, 92), radius=p(12), fill=ivory)
        d.rectangle(box(54, 38, 74, 54), fill=ivory)
        d.ellipse(box(58, 66, 70, 78), fill=dark)
    elif coin_id == "fuse":
        d.line(points([(50, 92), (76, 56)]), fill=ivory, width=p(7))
        d.ellipse(box(70, 38, 90, 58), fill=ivory)
    elif coin_id == "phoenix":
        d.polygon(points([(64, 26), (80, 48), (90, 70), (78, 94), (64, 88), (50, 94), (38, 70), (48, 48)]), fill=ivory)
        d.polygon(points([(64, 58), (72, 76), (64, 88), (56, 76)]), fill=dark)
    elif coin_id == "contrarian":
        d.polygon(points([(40, 50), (70, 50), (70, 40), (90, 54), (70, 68), (70, 58), (40, 58)]), fill=ivory)
        d.polygon(points([(88, 76), (58, 76), (58, 66), (38, 80), (58, 94), (58, 84), (88, 84)]), fill=ivory)
    elif coin_id == "chain":
        for x in (38, 54, 70):
            d.ellipse(box(x, 52, x + 26, 76), outline=ivory, width=p(5))
    elif coin_id == "bank":
        d.polygon(points([(64, 30), (92, 50), (36, 50)]), fill=ivory)
        for x in (44, 60, 76):
            d.rectangle(box(x, 56, x + 8, 88), fill=ivory)
        d.rectangle(box(36, 92, 92, 100), fill=ivory)
    elif coin_id == "lucky_seven":
        d.line(points([(46, 40), (84, 40)]), fill=ivory, width=p(9))
        d.line(points([(84, 40), (58, 98)]), fill=ivory, width=p(9))
    elif coin_id == "hourglass":
        d.polygon(points([(44, 34), (84, 34), (64, 64)]), fill=ivory)
        d.polygon(points([(64, 64), (84, 94), (44, 94)]), fill=ivory)
    elif coin_id == "capacitor":
        d.rectangle(box(48, 38, 57, 90), fill=ivory)
        d.rectangle(box(71, 38, 80, 90), fill=ivory)
        d.line(points([(36, 64), (48, 64)]), fill=ivory, width=p(5))
        d.line(points([(80, 64), (92, 64)]), fill=ivory, width=p(5))
    elif coin_id == "martyr":
        d.rectangle(box(57, 32, 71, 96), fill=ivory)
        d.rectangle(box(38, 52, 90, 66), fill=ivory)
    elif coin_id == "bounty":
        star = []
        for i in range(10):
            r = 30 if i % 2 == 0 else 13
            a = -math.pi / 2 + i * math.pi / 5
            star.append((64 + r * math.cos(a), 66 + r * math.sin(a)))
        d.polygon(points(star), fill=ivory)
    elif coin_id == "jester":
        d.polygon(points([(36, 86), (44, 40), (56, 66), (64, 34), (72, 66), (84, 40), (92, 86)]), fill=ivory)
    elif coin_id == "flock":
        for x, y in ((44, 58), (64, 50), (84, 58)):
            d.ellipse(box(x - 9, y - 9, x + 9, y + 9), fill=ivory)
            d.line(points([(x, y + 9), (x, y + 30)]), fill=ivory, width=p(4))
    elif coin_id == "megaphone":  # a cone with sound arcs
        d.polygon(points([(40, 58), (70, 40), (70, 88), (40, 70)]), fill=ivory)
        d.rectangle(box(34, 58, 44, 70), fill=ivory)
        d.arc(box(66, 46, 90, 82), 300, 60, fill=ivory, width=p(4))
        d.arc(box(66, 38, 104, 90), 300, 60, fill=ivory, width=p(4))
    elif coin_id == "cheerleader":  # three chevrons pointing up: a boost
        for y in (42, 58, 74):
            d.line(points([(44, y + 14), (64, y), (84, y + 14)]), fill=ivory, width=p(6))
    elif coin_id == "mirror":  # an oval mirror with two shine strokes
        d.ellipse(box(42, 32, 86, 90), outline=ivory, width=width)
        d.line(points([(52, 54), (62, 44)]), fill=ivory, width=p(4))
        d.line(points([(52, 66), (70, 48)]), fill=ivory, width=p(4))
        d.rectangle(box(61, 90, 67, 100), fill=ivory)
    elif coin_id == "twin":  # two overlapping rings
        d.ellipse(box(36, 48, 74, 86), outline=ivory, width=width)
        d.ellipse(box(54, 48, 92, 86), outline=ivory, width=width)
    elif coin_id == "pot":  # a pot full of coins
        d.polygon(points([(40, 62), (88, 62), (82, 94), (46, 94)]), fill=ivory)
        for x in (50, 64, 78):
            d.ellipse(box(x - 8, 48, x + 8, 64), fill=ivory, outline=dark, width=p(2))
    elif coin_id == "domino":  # a domino tile
        d.rounded_rectangle(box(48, 30, 80, 98), radius=p(5), fill=ivory)
        d.line(points([(48, 64), (80, 64)]), fill=dark, width=p(3))
        for x, y in ((58, 44), (70, 54), (58, 78), (70, 88)):
            d.ellipse(box(x - 4, y - 4, x + 4, y + 4), fill=dark)
    elif coin_id == "hot_hand":  # a flame
        d.polygon(points([(64, 28), (80, 52), (86, 70), (78, 92), (64, 98), (50, 92), (42, 72), (50, 56), (56, 66), (58, 46)]), fill=ivory)
        d.polygon(points([(64, 66), (72, 80), (64, 92), (56, 80)]), fill=dark)
    elif coin_id == "anchor":  # an anchor
        d.ellipse(box(57, 30, 71, 44), outline=ivory, width=p(4))
        d.line(points([(64, 44), (64, 92)]), fill=ivory, width=p(6))
        d.line(points([(50, 56), (78, 56)]), fill=ivory, width=p(5))
        d.arc(box(40, 58, 88, 100), 20, 160, fill=ivory, width=p(6))
    elif coin_id == "bettor":  # a stack of poker chips
        for y in (84, 70, 56, 42):
            d.ellipse(box(42, y, 86, y + 18), fill=ivory, outline=dark, width=p(2))
            d.line(points([(54, y + 9), (74, y + 9)]), fill=dark, width=p(2))
    elif coin_id == "cash_out":  # a big arrow up on a base line
        d.polygon(points([(64, 30), (88, 58), (72, 58), (72, 84), (56, 84), (56, 58), (40, 58)]), fill=ivory)
        d.line(points([(42, 94), (86, 94)]), fill=ivory, width=p(6))
    elif coin_id == "cold_streak":  # a snowflake
        for ang in (0, 60, 120):
            a = math.radians(ang)
            d.line(points([(64 - 30 * math.cos(a), 64 - 30 * math.sin(a)), (64 + 30 * math.cos(a), 64 + 30 * math.sin(a))]), fill=ivory, width=p(5))
        d.ellipse(box(57, 57, 71, 71), fill=ivory)
    elif coin_id == "amplifier":  # signal bars growing
        for i, h in enumerate((18, 32, 46, 60)):
            x = 36 + i * 15
            d.rectangle(box(x, 94 - h, x + 10, 94), fill=ivory)
    elif coin_id == "true_echo":  # a source and repeating wave arcs
        d.ellipse(box(34, 56, 48, 70), fill=ivory)
        for r in (16, 30, 44):
            d.arc(box(41 - r, 63 - r, 41 + r, 63 + r), -55, 55, fill=ivory, width=p(5))
    elif coin_id == "doubler":  # a big x2
        d.line(points([(36, 48), (60, 80)]), fill=ivory, width=p(8))
        d.line(points([(60, 48), (36, 80)]), fill=ivory, width=p(8))
        d.line(points([(70, 52), (92, 52), (92, 64), (70, 76), (70, 80), (94, 80)]), fill=ivory, width=p(6), joint="curve")
    elif coin_id == "jackpot":  # a star over a big number sign: three 7s
        for x in (34, 54, 74):
            d.polygon(points([(x, 46), (x + 18, 46), (x + 8, 84), (x + 2, 84), (x + 10, 56), (x, 56)]), fill=ivory)
    elif coin_id == "mimic":  # a smiling mask
        d.ellipse(box(38, 36, 90, 92), fill=ivory)
        d.ellipse(box(48, 54, 58, 64), fill=dark)
        d.ellipse(box(70, 54, 80, 64), fill=dark)
        d.arc(box(48, 62, 80, 84), 20, 160, fill=dark, width=p(4))
    elif coin_id == "orchestra":  # a musical note
        d.ellipse(box(38, 72, 62, 90), fill=ivory)
        d.line(points([(60, 80), (60, 36)]), fill=ivory, width=p(5))
        d.polygon(points([(60, 36), (86, 46), (86, 58), (60, 48)]), fill=ivory)
    elif coin_id == "lifeline":  # a life ring
        d.ellipse(box(36, 36, 92, 92), outline=ivory, width=p(12))
        for a in (45, 135, 225, 315):
            r = math.radians(a)
            d.line(points([(64 + 22 * math.cos(r), 64 + 22 * math.sin(r)), (64 + 30 * math.cos(r), 64 + 30 * math.sin(r))]), fill=dark, width=p(5))
    elif coin_id == "horoscope":  # a crescent and a star
        d.ellipse(box(36, 36, 84, 84), fill=ivory)
        d.ellipse(box(48, 32, 96, 80), fill=(74, 95, 168, 255))
        star = []
        for i in range(10):
            r = 14 if i % 2 == 0 else 6
            a = -math.pi / 2 + i * math.pi / 5
            star.append((84 + r * math.cos(a), 78 + r * math.sin(a)))
        d.polygon(points(star), fill=ivory)
    elif coin_id == "crystal_ball":  # a ball on a stand with a shine
        d.ellipse(box(36, 34, 92, 90), outline=ivory, width=p(6))
        d.polygon(points([(46, 92), (82, 92), (90, 102), (38, 102)]), fill=ivory)
        d.arc(box(46, 44, 70, 68), 200, 280, fill=ivory, width=p(5))
    elif coin_id == "compost":  # a heap growing a little leaf
        d.ellipse(box(39, 68, 89, 94), fill=ivory)
        d.ellipse(box(45, 54, 75, 78), fill=ivory)
        d.line(points([(65, 60), (65, 38)]), fill=ivory, width=p(5))
        d.ellipse(box(65, 34, 84, 47), fill=ivory)
        d.ellipse(box(45, 39, 64, 52), fill=ivory)
    elif coin_id == "square_dance":  # nested squares, rotated like dance steps
        d.rectangle(box(42, 42, 86, 86), outline=ivory, width=p(6))
        d.line(points([(64, 28), (100, 64), (64, 100), (28, 64), (64, 28)]), fill=ivory, width=p(6), joint="curve")
        d.rectangle(box(57, 57, 71, 71), fill=ivory)
    elif coin_id == "good_dog":  # paw print
        for x, y in ((44, 48), (57, 38), (72, 38), (85, 48)):
            d.ellipse(box(x - 6, y - 8, x + 6, y + 5), fill=ivory)
        d.ellipse(box(46, 62, 82, 93), fill=ivory)
    elif coin_id == "whetstone":  # blade across a sharpening stone
        d.rounded_rectangle(box(35, 68, 94, 87), radius=p(7), fill=ivory)
        d.line(points([(50, 59), (78, 33)]), fill=ivory, width=p(7))
        d.polygon(points([(77, 33), (90, 28), (84, 42)]), fill=ivory)
    elif coin_id == "blood_pact":  # two drops joined by a line
        for x in (47, 81):
            d.polygon(points([(x, 40), (x - 12, 65), (x + 12, 65)]), fill=ivory)
            d.ellipse(box(x - 12, 54, x + 12, 80), fill=ivory)
        d.line(points([(50, 87), (78, 87)]), fill=ivory, width=p(6))
    elif coin_id == "counterfeiter":  # two overlapping stamped coins
        d.ellipse(box(33, 39, 76, 82), outline=ivory, width=p(5))
        d.ellipse(box(53, 50, 96, 93), outline=ivory, width=p(5))
        d.line(points([(68, 63), (82, 80), (68, 80), (82, 63)]), fill=ivory, width=p(4))
    elif coin_id == "doppelganger":  # mirrored faces
        d.ellipse(box(30, 41, 69, 88), outline=ivory, width=p(5))
        d.ellipse(box(59, 41, 98, 88), outline=ivory, width=p(5))
        for x in (44, 78):
            d.ellipse(box(x, 59, x + 5, 64), fill=ivory)
    elif coin_id == "conductor":  # baton and three beats
        d.line(points([(37, 91), (88, 37)]), fill=ivory, width=p(6))
        for x, y in ((45, 43), (64, 36), (85, 65)):
            d.ellipse(box(x - 5, y - 5, x + 5, y + 5), fill=ivory)
    elif coin_id == "back":
        d.arc(box(44, 37, 84, 76), 190, 350, fill=ivory, width=p(8))
        d.line(points([(83, 59), (64, 77), (64, 82)]), fill=ivory, width=p(8))
        d.ellipse(box(59, 89, 69, 99), fill=ivory)
    else:
        raise ValueError(coin_id)


def make_icon(coin_id, value):
    body_color = rgb(value)
    transparent = Image.new("RGBA", (PIXELS, PIXELS), (0, 0, 0, 0))
    image = transparent.copy()
    # Flat style: solid body, dark outline, one darker inner ring. No shadow or shine.
    d = ImageDraw.Draw(image)
    d.ellipse(box(10, 10, 118, 118), fill=body_color + (255,),
              outline=blend(body_color, (15, 20, 26), .66) + (255,), width=p(5))
    d.ellipse(box(20, 20, 108, 108), outline=blend(body_color, (13, 17, 24), .33) + (255,), width=p(3))

    symbol = transparent.copy()
    emblem(symbol, coin_id)
    image = Image.alpha_composite(image, symbol)
    image.resize((OUT_SIZE, OUT_SIZE), Image.Resampling.LANCZOS).save(OUT / f"{coin_id}.png")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for coin_id, value in COLORS.items():
        make_icon(coin_id, value)
    make_icon("back", "9a794e")
    print(f"generated {len(COLORS)} coin icons and a back in {OUT}")


if __name__ == "__main__":
    main()
