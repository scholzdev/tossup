#!/usr/bin/env python3
"""Generate the main-menu backdrop assets/ui/title_scene.png (1280x800): a pixel-art casino back room - brick wall, a neon coin
sign, a string of bulbs, velvet curtains, a felt table with coin stacks and a few coins in mid-air. It is drawn at 320x200 and
scaled up 4x without smoothing; the left side is darkened so the menu panel reads on top of it.
Run from the repo root:  python3 tools/gen_title_scene.py   (needs Pillow). The picture is made up here, not taken from anywhere.
"""

import math
import random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
W, H, SCALE = 320, 200, 4
rnd = random.Random(7)

INK = (13, 27, 33)
WALL_DK, WALL, WALL_LT = (14, 40, 48), (21, 58, 67), (30, 76, 86)
GOLD, GOLD_DK, GOLD_LT = (243, 185, 88), (176, 118, 36), (255, 228, 150)
ORANGE, PINK, GREEN, BLUE = (255, 150, 40), (255, 95, 130), (75, 194, 146), (0, 157, 255)
RED, RED_DK, RED_LT = (150, 34, 46), (92, 18, 30), (190, 58, 66)
FELT, FELT_DK, FELT_LT = (24, 104, 94), (14, 70, 66), (40, 134, 120)
WOOD, WOOD_DK, WOOD_LT = (92, 56, 36), (56, 32, 24), (126, 80, 50)
SILVER, SILVER_DK = (190, 200, 205), (110, 124, 132)

BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def dither_fill(img, box, top, bottom):
    """Vertical gradient with ordered dithering between two colours, so the wall keeps a pixel look."""
    x0, y0, x1, y1 = box
    px = img.load()
    for y in range(y0, y1):
        t = (y - y0) / max(1, y1 - y0 - 1)
        for x in range(x0, x1):
            px[x, y] = bottom if t * 16 > BAYER[y % 4][x % 4] else top


def glow(img, draw_fn, radius, strength=1.0):
    """Draw shapes on a separate layer, blur it, and add it on top: a neon halo that stays chunky at this size."""
    layer = Image.new("RGB", (W, H), (0, 0, 0))
    draw_fn(ImageDraw.Draw(layer))
    halo = layer.filter(ImageFilter.GaussianBlur(radius))
    base, add = img.load(), halo.load()
    for y in range(H):
        for x in range(W):
            r, g, b = base[x, y]
            a, c, d = add[x, y]
            base[x, y] = (min(255, r + int(a * strength)), min(255, g + int(c * strength)), min(255, b + int(d * strength)))


def coin(d, cx, cy, r, squash=1.0, body=GOLD, rim=GOLD_DK, light=GOLD_LT):
    rx, ry = max(1, int(r * squash)), r
    d.ellipse((cx - rx - 1, cy - ry - 1, cx + rx + 1, cy + ry + 1), fill=INK)
    d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=rim)
    d.ellipse((cx - rx + 1, cy - ry + 1, cx + rx - 1, cy + ry - 2), fill=body)
    if squash > .55 and r >= 5:
        d.ellipse((cx - rx + 3, cy - ry + 3, cx + rx - 3, cy + ry - 4), outline=rim)
        d.point((cx - rx + 3, cy - ry + 3), fill=light)
        d.point((cx - rx + 4, cy - ry + 3), fill=light)
    elif squash <= .55:
        d.line((cx - rx + 1, cy - ry + 2, cx - rx + 1, cy + ry - 3), fill=light)


def stack(d, x, y, n, body=GOLD, rim=GOLD_DK, light=GOLD_LT):
    for i in range(n):
        yy = y - i * 3
        d.ellipse((x - 8, yy - 3, x + 8, yy + 3), fill=INK)
        d.rectangle((x - 7, yy - 1, x + 7, yy + 2), fill=rim)
        d.ellipse((x - 7, yy - 3, x + 7, yy + 1), fill=body)
        d.point((x - 4, yy - 2), fill=light)
        d.point((x - 3, yy - 2), fill=light)


def sparkle(d, x, y, size, color):
    d.point((x, y), fill=color)
    for i in range(1, size + 1):
        for dx, dy in ((i, 0), (-i, 0), (0, i), (0, -i)):
            d.point((x + dx, y + dy), fill=color if i < size else lerp(color, INK, .5))


def main():
    img = Image.new("RGB", (W, H), INK)
    d = ImageDraw.Draw(img)

    # back wall: dithered gradient, brick courses
    dither_fill(img, (0, 0, W, 150), WALL_DK, WALL)
    for row, y in enumerate(range(6, 150, 9)):
        d.line((0, y, W, y), fill=WALL_DK)
        for x in range(-(row % 2) * 12, W, 24):
            d.line((x, y, x, y + 8), fill=WALL_DK)
            if rnd.random() < .25:
                d.rectangle((x + 2, y + 2, x + 21, y + 7), fill=lerp(WALL, WALL_LT, rnd.random()))

    # neon coin sign
    cx, cy = 218, 66
    def sign(dd):
        dd.ellipse((cx - 40, cy - 40, cx + 40, cy + 40), outline=ORANGE, width=3)
        dd.ellipse((cx - 30, cy - 30, cx + 30, cy + 30), outline=PINK, width=2)
        dd.rectangle((cx - 14, cy - 18, cx + 14, cy - 12), fill=GOLD)
        dd.rectangle((cx - 4, cy - 12, cx + 4, cy + 20), fill=GOLD)
    glow(img, sign, 7, 1.1)
    d = ImageDraw.Draw(img)
    d.ellipse((cx - 40, cy - 40, cx + 40, cy + 40), fill=INK)
    d.ellipse((cx - 38, cy - 38, cx + 38, cy + 38), fill=(22, 30, 40), outline=ORANGE, width=3)
    d.ellipse((cx - 30, cy - 30, cx + 30, cy + 30), outline=PINK, width=2)
    d.rectangle((cx - 14, cy - 18, cx + 14, cy - 12), fill=GOLD_LT)
    d.rectangle((cx - 4, cy - 12, cx + 4, cy + 20), fill=GOLD_LT)
    d.rectangle((cx - 14, cy - 12, cx + 14, cy - 12), fill=GOLD_DK)

    # string of bulbs
    pts = [(x, 12 + int(9 * math.sin((x - 90) / 230 * math.pi))) for x in range(90, 321, 2)]
    d.line(pts, fill=INK)
    bulbs = [pts[i] for i in range(4, len(pts), 8)]
    glow(img, lambda dd: [dd.ellipse((x - 2, y + 2, x + 2, y + 6), fill=(255, 190, 90)) for x, y in bulbs], 4, 1.2)
    d = ImageDraw.Draw(img)
    for x, y in bulbs:
        d.rectangle((x - 1, y, x + 1, y + 1), fill=INK)
        d.ellipse((x - 2, y + 2, x + 2, y + 6), fill=GOLD_LT)

    # velvet curtains with folds and a scalloped valance
    for side in (0, 1):
        x0, x1 = (0, 34) if side == 0 else (W - 30, W)
        for x in range(x0, x1):
            t = (x - x0) % 8 / 7
            d.line((x, 0, x, 160), fill=lerp(RED_DK, RED_LT, abs(t - .5) * 1.4 if side else abs(t - .5) * 1.1))
        d.line((x0 if side else x1 - 1, 0, x0 if side else x1 - 1, 160), fill=INK)
    for x in range(0, W, 16):
        d.pieslice((x, 0, x + 16, 14), 0, 180, fill=RED)
    d.rectangle((0, 0, W, 4), fill=RED_DK)

    # table: wooden front, felt top with a gold edge
    d.rectangle((0, 168, W, H), fill=WOOD)
    for x in range(0, W, 18):
        d.line((x, 168, x, H), fill=WOOD_DK)
    d.line((0, 168, W, 168), fill=WOOD_LT)
    d.rectangle((0, 140, W, 167), fill=FELT)
    for y in range(140, 168):
        d.line((0, y, W, y), fill=lerp(FELT_DK, FELT_LT, (y - 140) / 27))
    d.line((0, 140, W, 140), fill=GOLD_DK)
    d.line((0, 141, W, 141), fill=GOLD)
    d.line((0, 166, W, 166), fill=GOLD_DK)

    # coin stacks on the table
    for x, y, n, kind in ((132, 160, 5, 0), (150, 164, 3, 1), (170, 158, 7, 0), (252, 162, 4, 0), (270, 158, 6, 1), (290, 164, 2, 0), (208, 164, 3, 1)):
        if kind:
            stack(d, x, y, n, SILVER, SILVER_DK, (240, 245, 247))
        else:
            stack(d, x, y, n)

    # coins in the air: face-on, edge-on, tumbling
    air = [(176, 110, 8, 1.0), (240, 128, 6, .35), (140, 92, 5, .7), (276, 98, 7, .9), (200, 124, 4, .2)]
    for x, y, r, s in air:
        d.ellipse((x - r, y + r + 5, x + r, y + r + 8), fill=lerp(FELT_DK, INK, .5)) if y > 100 and False else None
        coin(d, x, y, r, s)
    for i in range(5):
        sparkle(d, 120 + rnd.randint(0, 190), 40 + rnd.randint(0, 100), rnd.choice((1, 1, 2)), rnd.choice((GOLD_LT, (255, 255, 255), (170, 230, 255))))

    # dust motes
    for _ in range(40):
        x, y = rnd.randint(0, W - 1), rnd.randint(0, 150)
        d.point((x, y), fill=lerp(WALL, GOLD_LT, .35))

    # darker on the left (menu panel) and in the corners
    px = img.load()
    for y in range(H):
        for x in range(W):
            shade = max(0.0, 1 - x / 150) * .62 + (abs(x - W / 2) / (W / 2)) ** 3 * .25 + (abs(y - H / 2) / (H / 2)) ** 3 * .18
            px[x, y] = lerp(px[x, y], INK, min(.85, shade))

    big = img.resize((W * SCALE, H * SCALE), Image.NEAREST)
    scan = ImageDraw.Draw(big, "RGBA")
    for y in range(0, H * SCALE, 4):
        scan.line((0, y, W * SCALE, y), fill=(0, 0, 0, 22))
    out = ROOT / "assets" / "ui" / "title_scene.png"
    big.save(out, optimize=True)
    print("wrote", out, out.stat().st_size // 1024, "KB")


if __name__ == "__main__":
    main()
