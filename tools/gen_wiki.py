#!/usr/bin/env python3
"""Generate a local, static wiki (wiki/index.html) from the game's own content and the docs.

Reads the JSON that tools/dump_content.lua writes (coins, chips, prizes, modifiers, characters, constants, German
texts), the art in assets/, and the markdown in docs/ (rendered as guide pages and as notes on the coin pages).
Needs Pillow. Run through tools/build_wiki.sh, which makes the JSON first.
"""

import html
import json
import re
import shutil
import sys
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "wiki"
DATA = json.load(open(sys.argv[1] if len(sys.argv) > 1 else "/tmp/content.json"))

RARITY = {"N": ("Common", "#9aa5ab"), "R": ("Uncommon", "#4bc292"), "SR": ("Rare", "#f3b958"), "UR": ("Epic", "#c27ae0")}
CHAR_ORDER = [c["id"] for c in DATA["characters"]]
CHARS = {c["id"]: c for c in DATA["characters"]}
COINS = {c["id"]: c for c in DATA["coins"]}
UNLOCK_AFTER = {"seer": "blade", "trader": "seer"}


def esc(text):
    return html.escape(str(text), quote=True)


# ---------------------------------------------------------------- game text
def pct(x):
    return f"{round(x * 100):d}%"


def effect_text(e):
    t, a, c = e["type"], e.get("amount", 0), e.get("coins", 1)
    if t == "score": return f"Score {a} points"
    if t == "gold": return f"Gain {a} gold"
    if t == "energy": return f"Gain {a} energy"
    if t == "penalty": return f"Quota +{a}"
    if t == "extra_draw": return "Goes back into the pile"
    if t == "probability": return f"Gain {pct(a)} Heads this level"
    if t == "next_mult": return f"Next {c} coins pay x{a}"
    if t == "next_odds": return f"Next coin: +{pct(a)} Heads" if c == 1 else f"Next {c} coins: +{pct(a)} Heads"
    if t == "next_swap": return "Next coin uses its other side"
    if t == "next_heads": return "Next coin lands Heads"
    if t == "combo_bonus": return f"Combo grows {a} extra step"
    if t == "combo_shield": return "The next combo break is prevented"
    if t == "amplify": return "Buffs last 1 coin longer and get stronger"
    if t == "all_odds": return f"All coins +{pct(a)} Heads this level"
    if t == "peek": return "Look at the next two coins"
    if t == "extra_exchange": return "One more exchange this level"
    return t


def effects_html(effects, side, coin=None):
    if not effects:
        if coin and coin["hooks"]:
            return '<span class="muted">Depends on its special rule (see the description)</span>'
        return '<span class="muted">Nothing</span>'
    return "<br>".join(esc(effect_text(e)) for e in effects)


def price(coin):
    return coin.get("cost", 15)


# ---------------------------------------------------------------- markdown (small, for our own docs)
def inline(text, rewrite):
    text = esc(text)
    text = re.sub(r"`([^`]+)`", r"<code>\1</code>", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
    text = re.sub(r"(?<![\w*])\*([^*\n]+)\*(?![\w*])", r"<em>\1</em>", text)
    text = re.sub(r"\[([^\]]+)\]\(([^)]+)\)", lambda m: f'<a href="{rewrite(html.unescape(m.group(2)))}">{m.group(1)}</a>', text)
    return text


def slug(text):
    return re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")


def md(text, rewrite=lambda u: u):
    lines = text.split("\n")
    out, i = [], 0
    while i < len(lines):
        line = lines[i]
        if line.startswith("```"):
            code = []
            i += 1
            while i < len(lines) and not lines[i].startswith("```"):
                code.append(lines[i])
                i += 1
            out.append("<pre><code>" + esc("\n".join(code)) + "</code></pre>")
            i += 1
        elif re.match(r"^#{1,6} ", line):
            level = len(line) - len(line.lstrip("#"))
            title = line[level:].strip()
            out.append(f'<h{level} id="{slug(title)}">{inline(title, rewrite)}</h{level}>')
            i += 1
        elif line.strip() == "---":
            out.append("<hr>")
            i += 1
        elif line.startswith("|"):
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                rows.append([c.strip() for c in lines[i].strip().strip("|").split("|")])
                i += 1
            head, body = rows[0], [r for r in rows[2:]]
            table = "<table><thead><tr>" + "".join(f"<th>{inline(c, rewrite)}</th>" for c in head) + "</tr></thead><tbody>"
            for r in body:
                table += "<tr>" + "".join(f"<td>{inline(c, rewrite)}</td>" for c in r) + "</tr>"
            out.append(table + "</tbody></table>")
        elif re.match(r"^\s*([-*]|\d+\.) ", line):
            ordered = bool(re.match(r"^\s*\d+\. ", line))
            items = []
            while i < len(lines) and (re.match(r"^\s*([-*]|\d+\.) ", lines[i]) or (lines[i].startswith("  ") and lines[i].strip())):
                if re.match(r"^\s*([-*]|\d+\.) ", lines[i]):
                    items.append(re.sub(r"^\s*([-*]|\d+\.) ", "", lines[i]))
                else:
                    items[-1] += " " + lines[i].strip()
                i += 1
            tag = "ol" if ordered else "ul"
            out.append(f"<{tag}>" + "".join(f"<li>{inline(x, rewrite)}</li>" for x in items) + f"</{tag}>")
        elif line.strip() == "":
            i += 1
        else:
            para = []
            while i < len(lines) and lines[i].strip() and not re.match(r"^(#{1,6} |```|\||\s*([-*]|\d+\.) |---$)", lines[i]):
                para.append(lines[i].strip())
                i += 1
            out.append("<p>" + inline(" ".join(para), rewrite) + "</p>")
    return "\n".join(out)


# ---------------------------------------------------------------- docs
DOCS = ROOT / "docs"
GUIDES = []  # (slug, title, path)
for path in sorted(DOCS.glob("*.md")):
    if path.name == "README.md":
        continue
    title = re.search(r"^# (.+)$", path.read_text(), re.M)
    GUIDES.append((path.stem, title.group(1) if title else path.stem.title(), path))
GUIDES.append(("play", "Quick start (PLAY.md)", ROOT / "PLAY.md"))
GUIDES.append(("spec", "Complete reference (GAME_SPEC.md)", ROOT / "GAME_SPEC.md"))
LINK_MAP = {"../PLAY.md": "play.html", "../GAME_SPEC.md": "spec.html", "../readme.md": "#", "../impl.md": "#", "README.md": "index.html"}


def guide_link(url):
    if url in LINK_MAP:
        return LINK_MAP[url]
    m = re.match(r"^([\w-]+)\.md(#.*)?$", url)
    return f"{m.group(1)}.html{m.group(2) or ''}" if m else url


# notes for the coin pages: the "### Name (...)" sections of docs/coins.md
COIN_NOTES = {}
text = (DOCS / "coins.md").read_text()
for m in re.finditer(r"^### ([^\n(]+?) \([^\n]*\n(.*?)(?=^###|^---|^## |\Z)", text, re.M | re.S):
    COIN_NOTES[m.group(1).strip().lower()] = m.group(2).strip()
ITEM_NOTES, RELIC_NOTES = {}, {}
for line in (DOCS / "items-and-relics.md").read_text().split("\n"):
    cells = [c.strip() for c in line.strip().strip("|").split("|")]
    m = re.match(r"^\*\*(.+?)\*\*$", cells[0]) if cells else None
    if line.startswith("|") and m:
        if len(cells) == 4: ITEM_NOTES[m.group(1).lower()] = cells[3]   # chip: name | cost | effect | notes
        elif len(cells) == 3: RELIC_NOTES[m.group(1).lower()] = cells[2]  # prize: name | effect | notes

# ---------------------------------------------------------------- images
def make_images():
    for sub, size in (("coins", 160), ("items", 128), ("relics", 128)):
        (OUT / "img" / sub).mkdir(parents=True, exist_ok=True)
        for src in (ROOT / "assets" / sub).glob("*.png"):
            image = Image.open(src).convert("RGBA")
            image.thumbnail((size, size), Image.Resampling.LANCZOS)
            image.save(OUT / "img" / sub / src.name, optimize=True)
    (OUT / "img" / "characters").mkdir(parents=True, exist_ok=True)
    for src in (ROOT / "assets" / "characters").glob("*.png"):
        image = Image.open(src).convert("RGBA")
        image.thumbnail((360, 360), Image.Resampling.LANCZOS)
        image.save(OUT / "img" / "characters" / src.name, optimize=True)
    shutil.copy(ROOT / "assets" / "ui" / "logo.png", OUT / "img" / "logo.png")
    shutil.copy(ROOT / "assets" / "ui" / "icon.png", OUT / "img" / "icon.png")
    shutil.copy(ROOT / "assets" / "fonts" / "m6x11plus.ttf", OUT / "m6x11plus.ttf")


# ---------------------------------------------------------------- page frame
SEARCH = []  # {name, type, url, icon}


def frame(title, body, depth, active):
    base = "../" * depth
    nav = [("index.html", "Home", "home"), ("coins/index.html", f"Coins ({len(DATA['coins'])})", "coins"),
           ("chips/index.html", f"Chips ({len(DATA['items'])})", "chips"), ("prizes/index.html", f"Prizes ({len(DATA['relics'])})", "prizes"),
           ("modifiers/index.html", "Level modifiers", "modifiers"), ("characters/index.html", "Characters", "characters"),
           ("guide/index.html", "Guides", "guide")]
    side = "".join(f'<a class="{"on" if key == active else ""}" href="{base}{href}">{esc(label)}</a>' for href, label, key in nav)
    return f"""<!doctype html>
<html lang="en" data-base="{base}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{esc(title)} - Tossup Wiki</title><link rel="icon" href="{base}img/icon.png"><link rel="stylesheet" href="{base}style.css"></head>
<body><header><a class="brand" href="{base}index.html"><img src="{base}img/logo.png" alt="Tossup"><span>WIKI</span></a>
<div class="search"><input id="q" type="search" placeholder="Search coins, chips, prizes..." autocomplete="off"><div id="results"></div></div></header>
<div class="layout"><nav>{side}</nav><main>{body}</main></div>
<footer>Generated from the game's own data (v{esc(VERSION)}). Edit the game, run tools/build_wiki.sh, and this wiki updates.</footer>
<script src="{base}search-index.js"></script><script src="{base}wiki.js"></script></body></html>"""


def write(path, content):
    target = OUT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content)


def badge(rarity):
    name, colour = RARITY.get(rarity, ("?", "#999"))
    return f'<span class="badge" style="--c:{colour}">{name}</span>'


def icon(kind, id_, size=48):
    return f'<img class="ico" src="IMGBASE/img/{kind}/{id_}.png" width="{size}" height="{size}" alt="">'


def with_base(html_text, depth):
    return html_text.replace("IMGBASE/", "../" * depth)


def de_line(group, id_):
    entry = DATA["de"].get(group, {}).get(id_)
    if not entry:
        return ""
    desc = f" - {esc(entry['description'])}" if entry.get("description") else ""
    return f'<p class="de"><strong>Deutsch:</strong> {esc(entry["name"])}{desc}</p>'


def char_links(ids):
    return ", ".join(f'<a href="../characters/{i}.html">{esc(CHARS[i]["name"])}</a>' for i in ids) or "none"


def coin_availability(coin_id):
    start = [c for c in CHAR_ORDER if coin_id in CHARS[c]["deck"]]
    pool = [c for c in CHAR_ORDER if coin_id in CHARS[c]["pool"]]
    locked = [c for c in CHAR_ORDER if coin_id in CHARS[c]["locked"]]
    return start, pool, locked


# ---------------------------------------------------------------- pages
def coin_pages():
    rows = []
    for coin in DATA["coins"]:
        cid = coin["id"]
        start, pool, locked = coin_availability(cid)
        usable = sorted(set(start + pool + locked))
        SEARCH.append({"name": coin["name"], "type": "Coin", "url": f"coins/{cid}.html", "icon": f"img/coins/{cid}.png"})
        hooks = ", ".join(coin["hooks"]) if coin["hooks"] else ""
        notes = COIN_NOTES.get(coin["name"].lower())
        related = [c for c in DATA["coins"] if c["id"] != cid and {e["type"] for e in c["heads"] + c["tails"]} & {e["type"] for e in coin["heads"] + coin["tails"]} - {"score"}][:8]
        body = f"""<h1>{esc(coin["name"])} {badge(coin["rarity"])}</h1>
<div class="hero"><img class="big" src="../img/coins/{cid}.png" alt="{esc(coin["name"])}">
<table class="stats">
<tr><th>Heads chance</th><td>{pct(coin["probability"])}</td></tr>
<tr><th>Shop price</th><td>{price(coin)} gold</td></tr>
<tr><th>Energy to flip</th><td>{coin.get("energy_cost", 0)}</td></tr>
<tr><th class="heads">Heads</th><td>{effects_html(coin["heads"], "heads", coin)}</td></tr>
<tr><th class="tails">Tails</th><td>{effects_html(coin["tails"], "tails", coin)}</td></tr>
</table></div>
<p class="lead">{esc(coin["description"])}</p>
{f'<p class="note">Special rules: this coin has a hook ({esc(hooks)}) that changes the rules beyond its two sides.</p>' if hooks else ""}
{de_line("coins", cid)}
<h2>Who can use it</h2>
<ul><li><strong>Starts in the deck of:</strong> {char_links(start)}</li>
<li><strong>Usable in coin sets from the start:</strong> {char_links(pool)}</li>
<li><strong>Found in the shop and unlocked by buying it once:</strong> {char_links(locked)}</li></ul>
{f'<h2>Notes</h2>{md(notes, guide_link)}' if notes else ""}
{('<h2>Related coins</h2><div class="cards">' + "".join(f'<a class="card" href="{c["id"]}.html">{icon("coins", c["id"])}<span>{esc(c["name"])}</span></a>' for c in related) + "</div>") if related else ""}"""
        write(f"coins/{cid}.html", with_base(frame(coin["name"], body, 1, "coins"), 1))
        rows.append(f"""<tr data-name="{esc(coin["name"].lower())}" data-rarity="{coin["rarity"]}" data-chars="{' '.join(usable)}">
<td><a href="{cid}.html">{icon("coins", cid)}</a></td><td><a href="{cid}.html">{esc(coin["name"])}</a></td><td>{badge(coin["rarity"])}</td>
<td>{pct(coin["probability"])}</td><td>{price(coin)}</td><td>{coin.get("energy_cost", 0)}</td>
<td>{effects_html(coin["heads"], "h", coin)}</td><td>{effects_html(coin["tails"], "t", coin)}</td></tr>""")
    filters = "".join(f'<button data-rarity="{k}" style="--c:{v[1]}">{v[0]}</button>' for k, v in RARITY.items())
    chars = "".join(f'<button data-char="{c}">{esc(CHARS[c]["name"])}</button>' for c in CHAR_ORDER)
    body = f"""<h1>Coins</h1><p>Every coin in the game. Click a coin for details. Prices are in gold; energy is paid when you flip.</p>
<div class="filters"><input id="coinq" type="search" placeholder="Filter by name..."><span>{filters}</span><span>{chars}</span><button id="clear">Clear</button></div>
<table class="list" id="coin-table"><thead><tr><th></th><th>Name</th><th>Rarity</th><th>Heads</th><th>Price</th><th>Energy</th><th>On Heads</th><th>On Tails</th></tr></thead>
<tbody>{"".join(rows)}</tbody></table>"""
    write("coins/index.html", with_base(frame("Coins", body, 1, "coins"), 1))


def simple_pages(kind, label, entries, notes_map, group, dirname, notes_header="Notes"):
    cards = []
    for e in entries:
        eid = e["id"]
        SEARCH.append({"name": e["name"], "type": label, "url": f"{dirname}/{eid}.html", "icon": f"img/{kind}/{eid}.png"})
        facts = []
        if "cost" in e: facts.append(f"<tr><th>Price</th><td>{e['cost']} gold</td></tr>" if kind == "items" else "")
        if kind == "relics": facts.append("<tr><th>Price</th><td>25 gold (one prize per shop visit)</td></tr>")
        note = notes_map.get(e["name"].lower())
        body = f"""<h1>{esc(e["name"])}</h1><div class="hero"><img class="big" src="../img/{kind}/{eid}.png" alt="">
<table class="stats">{"".join(facts)}<tr><th>Effect</th><td>{esc(e["description"])}</td></tr></table></div>
{de_line(group, eid)}{f'<h2>{notes_header}</h2><p>{inline(note, guide_link)}</p>' if note else ""}"""
        write(f"{dirname}/{eid}.html", with_base(frame(e["name"], body, 1, dirname), 1))
        cards.append(f'<a class="card wide" href="{eid}.html">{icon(kind, eid, 64)}<span><strong>{esc(e["name"])}</strong>'
                     f'{f" <em>{e["cost"]}g</em>" if kind == "items" else ""}<br><small>{esc(e["description"])}</small></span></a>')
    return cards


def other_pages():
    # chips and prizes
    cards = simple_pages("items", "Chip", DATA["items"], ITEM_NOTES, "items", "chips", "How to use it")
    write("chips/index.html", with_base(frame("Chips", "<h1>Chips</h1><p>One-use helpers bought in the shop (two offers per visit, you can hold three). Use one in a level while a coin is in play.</p><div class=\"cards\">" + "".join(cards) + "</div>", 1, "chips"), 1))
    cards = simple_pages("relics", "Prize", DATA["relics"], RELIC_NOTES, "relics", "prizes", "Tips")
    write("prizes/index.html", with_base(frame("Prizes", "<h1>Prizes</h1><p>Passive relics that last the whole run: one offer per shop visit, 25 gold each.</p><div class=\"cards\">" + "".join(cards) + "</div>", 1, "prizes"), 1))
    # modifiers
    rows = "".join(f"<tr><td><strong>{esc(m['name'])}</strong></td><td>{esc(m['description'])}</td><td class='de'>{esc(DATA['de']['modifiers'][m['id']]['name'])}</td></tr>" for m in DATA["modifiers"])
    for m in DATA["modifiers"]:
        SEARCH.append({"name": m["name"], "type": "Modifier", "url": "modifiers/index.html", "icon": "img/icon.png"})
    write("modifiers/index.html", with_base(frame("Level modifiers", f"<h1>Level modifiers</h1><p>From level 2 on, every level (including the boss and every endless level) has one modifier, picked with the run's seed. It is shown at the bottom left of the stage.</p><table class='list'><thead><tr><th>Modifier</th><th>Effect</th><th>Deutsch</th></tr></thead><tbody>{rows}</tbody></table>", 1, "modifiers"), 1))
    # characters
    cards = []
    for c in DATA["characters"]:
        cid = c["id"]
        SEARCH.append({"name": c["name"], "type": "Character", "url": f"characters/{cid}.html", "icon": f"img/characters/{cid}.png"})
        def coin_cards(ids):
            return '<div class="cards">' + "".join(f'<a class="card" href="../coins/{i}.html">{icon("coins", i)}<span>{esc(COINS[i]["name"])}</span></a>' for i in ids) + "</div>"
        req = UNLOCK_AFTER.get(cid)
        unlock = f"Unlocked by winning a run with {esc(CHARS[req]['name'])}." if req else "Available from the start."
        body = f"""<h1>{esc(c["name"])}</h1><div class="hero"><img class="portrait" src="../img/characters/{cid}.png" alt="">
<div><p class="lead">{esc(c["description"])}</p><p>{unlock}</p>{de_line("characters", cid)}</div></div>
<h2>Starting deck</h2>{coin_cards(c["deck"])}
<h2>Usable in coin sets from the start</h2>{coin_cards(c["pool"])}
<h2>Found in the shop (unlocked by buying)</h2>{coin_cards(c["locked"])}"""
        write(f"characters/{cid}.html", with_base(frame(c["name"], body, 1, "characters"), 1))
        cards.append(f'<a class="card wide" href="{cid}.html"><img class="ico" src="../img/characters/{cid}.png" width="64" height="64" style="object-fit:cover;border-radius:8px"><span><strong>{esc(c["name"])}</strong><br><small>{esc(c["description"])} - {unlock}</small></span></a>')
    write("characters/index.html", with_base(frame("Characters", "<h1>Characters</h1><p>Each character has its own starting deck and its own coins in the shop.</p><div class=\"cards\">" + "".join(cards) + "</div>", 1, "characters"), 1))


def guide_pages():
    cards = []
    for slug_, title, path in GUIDES:
        SEARCH.append({"name": title, "type": "Guide", "url": f"guide/{slug_}.html", "icon": "img/icon.png"})
        write(f"guide/{slug_}.html", with_base(frame(title, md(path.read_text(), guide_link), 1, "guide"), 1))
        cards.append(f'<a class="card wide" href="{slug_}.html"><span><strong>{esc(title)}</strong></span></a>')
    write("guide/index.html", with_base(frame("Guides", "<h1>Guides</h1><p>The design and rules documents of the game, rendered here.</p><div class=\"cards\">" + "".join(cards) + "</div>", 1, "guide"), 1))


def home():
    c = DATA["constants"]
    route = "".join(f"<tr><td>{i + 1}</td><td>{esc(s['name'])}</td><td>{s['per_coin']}</td><td>{s.get('payout') or 'ends the run'}</td></tr>" for i, s in enumerate(DATA["route"]))
    counts = [("Coins", "coins/index.html", len(DATA["coins"])), ("Chips", "chips/index.html", len(DATA["items"])), ("Prizes", "prizes/index.html", len(DATA["relics"])),
              ("Modifiers", "modifiers/index.html", len(DATA["modifiers"])), ("Characters", "characters/index.html", len(DATA["characters"])), ("Guides", "guide/index.html", len(GUIDES))]
    tiles = "".join(f'<a class="tile" href="{u}"><strong>{n}</strong><span>{l}</span></a>' for l, u, n in counts)
    body = f"""<h1>Tossup Wiki</h1>
<p class="lead">Tossup is a roguelike about flipping coins: build a small deck, flip one coin at a time, and score points against a quota before your stack runs out. This wiki lists everything in the game.</p>
<div class="tiles">{tiles}</div>
<h2>The run in one table</h2>
<table class="list"><thead><tr><th>#</th><th>Level</th><th>Quota per coin</th><th>Payout (gold)</th></tr></thead><tbody>{route}</tbody></table>
<p>The quota is the per-coin value times the number of coins in your deck. After the boss, <strong>Endless Mode</strong> adds levels that ask 0.5 more per coin each time.</p>
<h2>Key numbers</h2>
<table class="stats"><tr><th>Starting gold</th><td>{c["START_GOLD"]}</td></tr><tr><th>Deck slots</th><td>{c["START_MAX"]} at the start, up to {c["DECK_MAX"]} (+{c["SLOT_COST"]} gold each in the shop)</td></tr>
<tr><th>Exchange</th><td>pay {c["EXCHANGE_BASE"]} gold (+{c["EXCHANGE_STEP"]} each time) to get {c["EXCHANGE_GAIN"]} played coins back, at most {c["EXCHANGE_MAX"]} times per level</td></tr>
<tr><th>Combo</th><td>+{c["COMBO_STEP"]} multiplier per same result in a row, up to x{c["COMBO_CAP"]}</td></tr>
<tr><th>Bank</th><td>you see the next {c["VISIBLE"]} coins; the opening hand has {c["MULLIGAN"]}</td></tr></table>
<p>Start with the <a href="guide/gameplay.html">gameplay guide</a> or browse the <a href="coins/index.html">coins</a>.</p>"""
    write("index.html", with_base(frame("Home", body, 0, "home"), 0))


CSS = """
:root{--screen:#17454d;--panel:#0f333b;--card:#1d4f58;--line:#2f6670;--gold:#f3b958;--blue:#009dff;--red:#ff5f55;--green:#4bc292;--face:#f7efd7;--muted:#9fc2c6;--bg:#274b49}
@font-face{font-family:Pixel;src:url(m6x11plus.ttf)}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--face);font:16px/1.55 -apple-system,Segoe UI,Roboto,sans-serif}
a{color:var(--gold);text-decoration:none}a:hover{text-decoration:underline}
header{display:flex;align-items:center;gap:24px;padding:10px 24px;background:var(--panel);border-bottom:3px solid var(--gold);position:sticky;top:0;z-index:5}
.brand{display:flex;align-items:center;gap:12px;color:var(--face)}.brand img{height:34px}.brand span{font-family:Pixel;font-size:28px;color:var(--gold)}
.search{position:relative;flex:1;max-width:460px}.search input{width:100%;padding:8px 12px;border-radius:8px;border:2px solid var(--line);background:var(--screen);color:var(--face);font-size:15px}
#results{position:absolute;top:44px;left:0;right:0;background:var(--panel);border:2px solid var(--gold);border-radius:8px;display:none;max-height:60vh;overflow:auto}
#results a{display:flex;align-items:center;gap:10px;padding:6px 10px;color:var(--face)}#results a:hover{background:var(--card);text-decoration:none}#results img{width:28px;height:28px;object-fit:contain}#results small{margin-left:auto;color:var(--muted)}
.layout{display:flex;gap:0;max-width:1300px;margin:0 auto}nav{width:210px;flex:none;padding:20px 12px;display:flex;flex-direction:column;gap:4px;position:sticky;top:64px;align-self:flex-start}
nav a{padding:8px 12px;border-radius:8px;color:var(--face)}nav a.on,nav a:hover{background:var(--card);text-decoration:none;color:var(--gold)}
main{flex:1;min-width:0;padding:24px 28px 60px;background:var(--screen);border-left:2px solid var(--line);border-right:2px solid var(--line);min-height:90vh}
h1,h2,h3{font-family:Pixel;color:var(--gold);letter-spacing:.5px;line-height:1.2}h1{font-size:40px;margin:.2em 0 .5em}h2{font-size:28px;margin-top:1.6em}h3{font-size:22px}
.lead{font-size:18px}.muted,.de{color:var(--muted)}.de{font-size:14px}.note{background:var(--panel);border-left:4px solid var(--gold);padding:8px 12px;border-radius:6px}
.hero{display:flex;gap:24px;align-items:flex-start;flex-wrap:wrap;margin:12px 0}.big{width:160px;height:160px;object-fit:contain;background:var(--panel);border-radius:14px;padding:10px}.portrait{width:220px;border-radius:12px;background:var(--panel)}
table{border-collapse:collapse;margin:12px 0}th,td{padding:8px 12px;border-bottom:1px solid var(--line);text-align:left;vertical-align:top}th{color:var(--muted);font-weight:600}
.stats th{width:150px;white-space:nowrap}.heads{color:var(--blue)!important}.tails{color:var(--red)!important}
table.list{width:100%}table.list thead th{background:var(--panel);position:sticky;top:60px}table.list tr:hover td{background:rgba(255,255,255,.04)}
.ico{vertical-align:middle;object-fit:contain}.badge{display:inline-block;font:14px Pixel;padding:1px 8px;border-radius:999px;border:2px solid var(--c);color:var(--c);vertical-align:middle}
.cards{display:grid;grid-template-columns:repeat(auto-fill,minmax(130px,1fr));gap:10px}.card{display:flex;flex-direction:column;align-items:center;gap:6px;padding:12px 8px;background:var(--panel);border:2px solid var(--line);border-radius:12px;color:var(--face);text-align:center}
.card:hover{border-color:var(--gold);text-decoration:none}.card.wide{flex-direction:row;text-align:left;gap:14px;grid-column:span 1;padding:12px 14px}.cards:has(.wide){grid-template-columns:repeat(auto-fill,minmax(340px,1fr))}
.tiles{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:12px;margin:18px 0}.tile{background:var(--panel);border:2px solid var(--line);border-radius:12px;padding:16px;text-align:center;color:var(--face)}.tile strong{display:block;font:40px Pixel;color:var(--gold)}.tile:hover{border-color:var(--gold);text-decoration:none}
.filters{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin:14px 0}.filters input{padding:8px 12px;border-radius:8px;border:2px solid var(--line);background:var(--panel);color:var(--face)}
.filters button{background:var(--panel);color:var(--face);border:2px solid var(--c,var(--line));border-radius:999px;padding:4px 12px;cursor:pointer;font:14px Pixel}.filters button.on{background:var(--gold);color:#17454d}.filters span{display:flex;gap:6px;flex-wrap:wrap}
pre{background:var(--panel);padding:12px;border-radius:8px;overflow:auto}code{background:var(--panel);padding:1px 6px;border-radius:4px;font-size:.92em}
hr{border:0;border-top:2px solid var(--line);margin:28px 0}footer{text-align:center;color:var(--muted);padding:20px;font-size:13px}
@media(max-width:800px){nav{display:none}main{padding:16px}header{padding:8px 12px}}
"""

JS = """
(function () {
  var base = document.documentElement.dataset.base || '';
  var q = document.getElementById('q'), box = document.getElementById('results');
  if (q) {
    q.addEventListener('input', function () {
      var t = q.value.trim().toLowerCase();
      if (!t) { box.style.display = 'none'; return; }
      var hits = SEARCH_INDEX.filter(function (e) { return e.name.toLowerCase().indexOf(t) >= 0; }).slice(0, 12);
      box.innerHTML = hits.map(function (e) { return '<a href="' + base + e.url + '"><img src="' + base + e.icon + '" alt=""><span>' + e.name + '</span><small>' + e.type + '</small></a>'; }).join('') || '<a>No results</a>';
      box.style.display = 'block';
    });
    document.addEventListener('click', function (e) { if (!box.contains(e.target) && e.target !== q) box.style.display = 'none'; });
  }
  var table = document.getElementById('coin-table');
  if (table) {
    var state = {rarity: null, char: null, text: ''};
    function apply() {
      table.querySelectorAll('tbody tr').forEach(function (r) {
        var ok = (!state.rarity || r.dataset.rarity === state.rarity) && (!state.char || r.dataset.chars.split(' ').indexOf(state.char) >= 0) && (!state.text || r.dataset.name.indexOf(state.text) >= 0);
        r.style.display = ok ? '' : 'none';
      });
    }
    document.querySelectorAll('.filters button[data-rarity]').forEach(function (b) { b.onclick = function () { var on = b.classList.toggle('on'); document.querySelectorAll('.filters button[data-rarity]').forEach(function (o) { if (o !== b) o.classList.remove('on'); }); state.rarity = on ? b.dataset.rarity : null; apply(); }; });
    document.querySelectorAll('.filters button[data-char]').forEach(function (b) { b.onclick = function () { var on = b.classList.toggle('on'); document.querySelectorAll('.filters button[data-char]').forEach(function (o) { if (o !== b) o.classList.remove('on'); }); state.char = on ? b.dataset.char : null; apply(); }; });
    document.getElementById('coinq').oninput = function (e) { state.text = e.target.value.trim().toLowerCase(); apply(); };
    document.getElementById('clear').onclick = function () { state = {rarity: null, char: null, text: ''}; document.getElementById('coinq').value = ''; document.querySelectorAll('.filters button').forEach(function (b) { b.classList.remove('on'); }); apply(); };
  }
})();
"""

if __name__ == "__main__":
    version_file = (ROOT / "src" / "version.lua").read_text()
    VERSION = re.search(r'number = "([^"]+)"', version_file).group(1)
    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir()
    make_images()
    coin_pages()
    other_pages()
    guide_pages()
    home()
    write("style.css", CSS)
    write("wiki.js", JS)
    write("search-index.js", "var SEARCH_INDEX = " + json.dumps(SEARCH) + ";")
    pages = sum(1 for _ in OUT.rglob("*.html"))
    print(f"wiki: {pages} pages in {OUT}")
