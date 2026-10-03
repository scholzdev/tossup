#!/usr/bin/env python3
"""Generate the run Encounter emblems and tiered Augment icons.

Run from the repository root with: python3 tools/gen_encounter_augment_icons.py
Requires Pillow, like the other asset generators. Outputs transparent 512px PNGs.
Encounter icons go in assets/encounters/; Augments are generated for all three
visual tiers in assets/augments/{silver,gold,prismatic}/ until content assigns tiers.
"""

from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
SCALE = 4
SIZE = 128
OUT_SIZE = 512
PIXELS = SIZE * SCALE

INK = (8, 13, 36, 255)
NAVY = (15, 26, 91, 255)
BLUE = (38, 64, 172, 255)
GOLD = (255, 204, 54, 255)
IVORY = (255, 241, 194, 255)
COPPER = (190, 111, 65, 255)
TIERS = {
    "silver": ((171, 204, 225, 255), 1),
    "gold": ((248, 195, 62, 255), 2),
    "prismatic": ((153, 229, 241, 255), 3),
}

ENCOUNTERS = {
    "house_clock": "clock",
    "dead_heat": "dead_heat",
    "high_roller_table": "dice",
    "thin_market": "market",
}

AUGMENTS = {
    "bankers_cut": "bank",
    "all_in": "all_in",
    "hedge_fund": "shield",
    "scrap_dealer": "scrap",
}


def p(value):
    return round(value * SCALE)


def box(x0, y0, x1, y1):
    return p(x0), p(y0), p(x1), p(y1)


def points(coords):
    return [(p(x), p(y)) for x, y in coords]


def draw_emblem(draw, kind):
    line_width = p(5)

    if kind == "clock":
        draw.ellipse(box(35, 29, 93, 87), outline=GOLD, width=p(5))
        draw.line(points([(64, 36), (64, 57), (79, 67)]), fill=IVORY, width=p(6))
        draw.polygon(points([(51, 90), (77, 90), (64, 107)]), fill=GOLD)
        draw.polygon(points([(51, 25), (77, 25), (64, 10)]), fill=GOLD)
        draw.ellipse(box(59, 55, 69, 65), fill=IVORY)
    elif kind == "dead_heat":
        for x in (37, 65):
            draw.ellipse(box(x, 38, x + 31, 88), outline=GOLD, width=p(5))
            draw.ellipse(box(x + 7, 46, x + 24, 80), outline=IVORY, width=p(3))
        draw.line(points([(57, 34), (65, 24), (73, 34)]), fill=IVORY, width=p(4))
        draw.line(points([(57, 94), (65, 104), (73, 94)]), fill=GOLD, width=p(4))
    elif kind == "dice":
        draw.polygon(points([(23, 47), (49, 32), (76, 47), (49, 62)]), fill=GOLD, outline=INK)
        draw.polygon(points([(23, 47), (49, 62), (49, 93), (23, 78)]), fill=IVORY, outline=INK)
        draw.polygon(points([(49, 62), (76, 47), (76, 78), (49, 93)]), fill=(225, 161, 34, 255), outline=INK)
        draw.ellipse(box(32, 51, 38, 57), fill=INK)
        draw.ellipse(box(55, 46, 61, 52), fill=INK)
        draw.ellipse(box(39, 72, 45, 78), fill=INK)
        draw.ellipse(box(57, 72, 63, 78), fill=INK)
        draw.polygon(points([(60, 35), (82, 21), (105, 34), (82, 48)]), fill=GOLD, outline=INK)
        draw.polygon(points([(60, 35), (82, 48), (82, 75), (60, 62)]), fill=IVORY, outline=INK)
        draw.polygon(points([(82, 48), (105, 34), (105, 62), (82, 75)]), fill=(225, 161, 34, 255), outline=INK)
        draw.ellipse(box(70, 38, 76, 44), fill=INK)
        draw.ellipse(box(89, 52, 95, 58), fill=INK)
        draw.ellipse(box(70, 57, 76, 63), fill=INK)
    elif kind == "market":
        draw.rectangle(box(31, 56, 94, 95), outline=GOLD, width=p(5))
        draw.line(points([(25, 55), (35, 32), (91, 32), (101, 55)]), fill=IVORY, width=p(6))
        draw.line(points([(25, 55), (101, 55)]), fill=GOLD, width=p(5))
        draw.line(points([(42, 35), (42, 54), (57, 54), (57, 35), (72, 54), (86, 54), (86, 35)]),
                  fill=GOLD, width=p(4))
        draw.line(points([(53, 95), (53, 70), (72, 70), (72, 95)]), fill=IVORY, width=p(4))
        draw.ellipse(box(80, 72, 91, 83), fill=GOLD)
    elif kind == "bank":
        for y, width in ((75, 44), (61, 52), (47, 38)):
            x = 64 - width // 2
            draw.rounded_rectangle(box(x, y, x + width, y + 16), radius=p(5), fill=GOLD,
                                   outline=INK, width=p(2))
            draw.arc(box(x + 5, y + 2, x + width - 5, y + 13), 190, 350, fill=IVORY, width=p(2))
        draw.line(points([(87, 26), (87, 47)]), fill=IVORY, width=p(5))
        draw.line(points([(77, 36), (97, 36)]), fill=IVORY, width=p(5))
    elif kind == "all_in":
        for y, width in ((78, 46), (67, 54), (56, 40)):
            x = 28 + (58 - width) // 2
            draw.rounded_rectangle(box(x, y, x + width, y + 12), radius=p(4), fill=GOLD,
                                   outline=INK, width=p(2))
        draw.line(points([(58, 65), (85, 38), (85, 55)]), fill=IVORY, width=line_width)
        draw.line(points([(85, 38), (68, 38)]), fill=IVORY, width=line_width)
        draw.polygon(points([(83, 24), (101, 41), (83, 58)]), fill=GOLD)
    elif kind == "shield":
        draw.polygon(points([(64, 20), (98, 34), (93, 73), (64, 105), (35, 73), (30, 34)]),
                     outline=GOLD, fill=NAVY, width=p(5))
        draw.ellipse(box(48, 44, 80, 76), outline=IVORY, width=p(4))
        draw.line(points([(64, 48), (64, 70), (75, 70)]), fill=GOLD, width=p(5))
        draw.ellipse(box(60, 58, 68, 66), fill=GOLD)
    elif kind == "scrap":
        draw.polygon(points([(26, 82), (39, 62), (52, 72), (67, 49), (80, 66), (94, 53),
                             (104, 82), (96, 93), (33, 93)]), fill=GOLD, outline=INK)
        draw.line(points([(34, 96), (97, 96)]), fill=IVORY, width=p(5))
        draw.line(points([(47, 55), (37, 44), (49, 31)]), fill=IVORY, width=p(5))
        draw.line(points([(81, 45), (93, 34), (83, 25)]), fill=IVORY, width=p(5))


def make_icon(kind, accent, tier=None):
    image = Image.new("RGBA", (PIXELS, PIXELS), (0, 0, 0, 0))
    d = ImageDraw.Draw(image)

    # Blue enamel plaque with warm metal corners, echoing the game's coin art.
    d.rounded_rectangle(box(8, 8, 120, 120), radius=p(17), fill=COPPER,
                        outline=(91, 47, 47, 255), width=p(3))
    d.rounded_rectangle(box(15, 14, 113, 114), radius=p(13), fill=NAVY,
                        outline=accent, width=p(4))
    d.rounded_rectangle(box(22, 21, 106, 107), radius=p(9), fill=BLUE,
                        outline=(75, 101, 216, 255), width=p(2))

    for x in (23, 104):
        for y in (23, 103):
            d.polygon(points([(x - 8, y - 3), (x - 3, y - 8), (x + 8, y - 8),
                              (x + 8, y + 3), (x + 3, y + 8), (x - 8, y + 8)]), fill=COPPER)
            d.ellipse(box(x - 2, y - 2, x + 2, y + 2), fill=(79, 47, 49, 255))

    d.line(points([(28, 112), (100, 112)]), fill=accent, width=p(2))
    d.polygon(points([(59, 108), (64, 102), (69, 108), (64, 114)]), fill=accent)
    draw_emblem(d, kind)

    if tier:
        tier_color, pips = TIERS[tier]
        # Tier color lives on the rim; the icon silhouette itself remains consistent.
        d.rounded_rectangle(box(15, 14, 113, 114), radius=p(13), outline=tier_color, width=p(3))
        for i in range(pips):
            cx = 58 + (i - (pips - 1) / 2) * 10
            d.polygon(points([(cx, 9), (cx + 4, 14), (cx, 19), (cx - 4, 14)]), fill=tier_color)

    return image.resize((OUT_SIZE, OUT_SIZE), Image.Resampling.LANCZOS)


def main():
    for icon_id, symbol in ENCOUNTERS.items():
        path = ROOT / "assets" / "encounters" / f"{icon_id}.png"
        path.parent.mkdir(parents=True, exist_ok=True)
        make_icon(symbol, GOLD).save(path)

    for tier, (accent, _) in TIERS.items():
        for icon_id, symbol in AUGMENTS.items():
            path = ROOT / "assets" / "augments" / tier / f"{icon_id}.png"
            path.parent.mkdir(parents=True, exist_ok=True)
            make_icon(symbol, accent, tier).save(path)

    print("Generated 4 Encounter icons and 12 tiered Augment icons.")


if __name__ == "__main__":
    main()
