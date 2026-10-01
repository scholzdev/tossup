#!/usr/bin/env python3
"""Generate the app/window icon: a gold coin with a pixel "T" on a teal rounded square, a flip arc and a spark.

Output: assets/ui/icon.ico (Windows) and assets/ui/icon.png (512 px, transparent corners; conf.lua points t.window.icon at it) and, on macOS
(needs `iconutil`), assets/ui/icon.icns for the dock icon of a packaged app.
Run from the repo root:  python3 tools/gen_app_icon.py   (needs Pillow).
"""

import shutil
import subprocess
import tempfile
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
S = 8                      # supersampling
N = 512
TEAL, TEAL_DK = (23, 69, 77), (15, 51, 59)
GOLD, GOLD_DK, GOLD_LT = (243, 185, 88), (196, 134, 40), (255, 222, 140)
DARK, IVORY = (31, 36, 43), (247, 239, 215)
RED, BLUE, GREEN = (254, 95, 85), (0, 157, 255), (75, 194, 146)


def circle(d, cx, cy, r, **kw):
    d.ellipse(((cx - r) * S, (cy - r) * S, (cx + r) * S, (cy + r) * S), **kw)


def main():
    img = Image.new("RGBA", (N * S, N * S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # background: rounded square with a gold border, like the in-game screen frame
    d.rounded_rectangle((16 * S, 16 * S, (N - 16) * S, (N - 16) * S), radius=84 * S, fill=TEAL_DK + (255,))
    d.rounded_rectangle((28 * S, 28 * S, (N - 28) * S, (N - 28) * S), radius=74 * S, fill=TEAL + (255,),
                        outline=GOLD + (255,), width=6 * S)
    # coin: shadow, dark rim, body, inner ring
    circle(d, 262, 276, 166, fill=(0, 0, 0, 70))
    circle(d, 256, 256, 166, fill=DARK + (255,))
    circle(d, 256, 256, 154, fill=GOLD_DK + (255,))
    circle(d, 256, 250, 146, fill=GOLD + (255,))
    circle(d, 256, 250, 112, outline=GOLD_DK + (255,), width=8 * S)
    # highlight arc on the upper left
    d.arc(((256 - 136) * S, (250 - 136) * S, (256 + 136) * S, (250 + 136) * S), 200, 260, fill=GOLD_LT + (255,), width=10 * S)
    # pixel "T" from the game font, with a dark outline and offset shadow
    font = ImageFont.truetype(str(ROOT / "assets" / "fonts" / "m6x11plus.ttf"), 250 * S)
    box = d.textbbox((0, 0), "T", font=font)
    tx = 256 * S - (box[0] + box[2]) // 2
    ty = 250 * S - (box[1] + box[3]) // 2
    d.text((tx + 12 * S, ty + 12 * S), "T", font=font, fill=GOLD_DK + (255,))
    d.text((tx, ty), "T", font=font, fill=IVORY + (255,), stroke_width=5 * S, stroke_fill=DARK + (255,))
    # a flip arc and a spark around the coin: Heads blue, Tails red, a green sparkle
    d.arc((60 * S, 60 * S, 452 * S, 452 * S), 285, 340, fill=BLUE + (255,), width=14 * S)
    d.arc((60 * S, 60 * S, 452 * S, 452 * S), 105, 160, fill=RED + (255,), width=14 * S)
    for cx, cy, r in ((422, 112, 20), (96, 410, 12)):
        d.polygon([((cx) * S, (cy - r) * S), ((cx + r // 3) * S, (cy - r // 3) * S), ((cx + r) * S, cy * S),
                   ((cx + r // 3) * S, (cy + r // 3) * S), (cx * S, (cy + r) * S), ((cx - r // 3) * S, (cy + r // 3) * S),
                   ((cx - r) * S, cy * S), ((cx - r // 3) * S, (cy - r // 3) * S)], fill=GREEN + (255,))
    out = ROOT / "assets" / "ui" / "icon.png"
    img.resize((N, N), Image.Resampling.LANCZOS).save(out)
    print(f"wrote {out}")
    # Windows icon (used by tools/build_windows.sh for Tossup.exe)
    sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
    img.resize((256, 256), Image.Resampling.LANCZOS).save(ROOT / "assets" / "ui" / "icon.ico", sizes=sizes)
    print(f"wrote {ROOT / 'assets' / 'ui' / 'icon.ico'}")
    if not shutil.which("iconutil"):
        print("iconutil not found (not macOS): skipped icon.icns")
        return
    with tempfile.TemporaryDirectory() as tmp:
        iconset = Path(tmp) / "icon.iconset"
        iconset.mkdir()
        for size in (16, 32, 128, 256, 512):
            img.resize((size, size), Image.Resampling.LANCZOS).save(iconset / f"icon_{size}x{size}.png")
            img.resize((size * 2, size * 2), Image.Resampling.LANCZOS).save(iconset / f"icon_{size}x{size}@2x.png")
        icns = ROOT / "assets" / "ui" / "icon.icns"
        subprocess.run(["iconutil", "-c", "icns", str(iconset), "-o", str(icns)], check=True)
        print(f"wrote {icns}")

if __name__ == "__main__":
    main()
