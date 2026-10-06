#!/usr/bin/env python3
"""Generate pixel-font UI titles and cursors without touching authored sprites."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
RESOURCES = ROOT / "Assets" / "Resources"
DARK = (31, 36, 43, 255)
IVORY = (247, 239, 215, 255)


def shop_title():
    """The SHOP title: big pixel letters, each in its own colour, with a dark outline and a drop shadow."""
    font = ImageFont.truetype(str(RESOURCES / "fonts" / "m6x11plus.ttf"), 48)
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
    (RESOURCES / "ui").mkdir(parents=True, exist_ok=True)
    big.save(RESOURCES / "ui" / "shop_title.png")


def logo():
    """The TOSSUP logo, same pixel-letter treatment as the SHOP title."""
    font = ImageFont.truetype(str(RESOURCES / "fonts" / "m6x11plus.ttf"), 48)
    colours = [(255, 162, 0), (254, 95, 85), (0, 157, 255), (75, 194, 146), (255, 162, 0), (254, 95, 85)]
    small = Image.new("RGBA", (200, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(small)
    x = 5
    for i, (letter, colour) in enumerate(zip("TOSSUP", colours)):
        y = 6 + (3 if i % 2 else 0)
        d.text((x + 3, y + 3), letter, font=font, fill=(0, 0, 0, 120), stroke_width=2, stroke_fill=(0, 0, 0, 120))
        d.text((x, y), letter, font=font, fill=colour + (255,), stroke_width=2, stroke_fill=DARK)
        x += 31
    small.resize((800, 256), Image.Resampling.NEAREST).save(RESOURCES / "ui" / "logo.png")


TITLE_COLOURS = [(255, 162, 0), (254, 95, 85), (0, 157, 255), (75, 194, 146)]


def pixel_title(word, name):
    """Pixel-font letters cycling through the shop colours, with outline and drop shadow, size from the text."""
    font = ImageFont.truetype(str(RESOURCES / "fonts" / "m6x11plus.ttf"), 48)
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
    big.save(RESOURCES / "ui" / f"title_{name}.png")


def cursor(path_name, fill):
    """A crisp pixel-art arrow cursor: drawn on a 16x16 grid without smoothing, doubled to 32x32. Hotspot (2, 2)."""
    img = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    arrow = [(1, 1), (1, 13), (4, 10), (6, 14), (8, 13), (6, 9), (10, 9)]
    d.polygon(arrow, fill=fill + (255,), outline=DARK)
    img.resize((32, 32), Image.Resampling.NEAREST).save(RESOURCES / "ui" / path_name)


def main():
    for fn in (shop_title, logo):
        fn()
    for word, name in (("PLAY", "play"), ("COIN SETS", "sets"), ("COLLECTION", "collection"), ("OPTIONS", "options"), ("HOW TO PLAY", "help"),
                       ("SPIELEN", "play_de"), ("MÜNZSETS", "sets_de"), ("SAMMLUNG", "collection_de"),
                       ("OPTIONEN", "options_de"), ("ANLEITUNG", "help_de")):
        pixel_title(word, name)
    cursor("cursor_arrow.png", IVORY[:3])
    cursor("cursor_click.png", (243, 185, 88))
    print("Generated UI titles and cursors; authored sprites were preserved.")


if __name__ == "__main__":
    main()
