#!/usr/bin/env python3
"""Generate a local, static wiki (wiki/index.html) from the game's own content and the docs.

Reads the JSON that tools/dump_content.lua writes (coins, chips, prizes, modifiers, characters, constants, German
texts), the art in assets/Resources/, and the markdown in docs/ (rendered as guide pages and as notes on the coin pages).
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
RESOURCES = ROOT / "assets" / "Resources"
OUT = ROOT / "wiki"
DATA = json.load(open(sys.argv[1] if len(sys.argv) > 1 else "/tmp/content.json"))

RARITY = {"N": ("Common", "#9aa5ab"), "R": ("Uncommon", "#4bc292"), "SR": ("Rare", "#f3b958"), "UR": ("Epic", "#c27ae0")}
CHAR_ORDER = [c["id"] for c in DATA["characters"]]
CHARS = {c["id"]: c for c in DATA["characters"]}
COINS = {c["id"]: c for c in DATA["coins"]}
UNLOCK_AFTER = {"seer": "blade", "trader": "seer"}
LANG = "en"  # the language being generated; the German tree lives in wiki/de/
DESTR = DATA.get("de_strings", {})

# German texts of the wiki itself (the game's own texts come from locales/de.lua through DATA["de"] and DESTR)
DE = {
    "Home": "Start", "Coins": "Münzen", "Chips": "Chips", "Prizes": "Prämien", "Level modifiers": "Level-Modifikatoren", "Characters": "Charaktere",
    "Guides": "Anleitungen", "Search coins, chips, prizes...": "Münzen, Chips, Prämien suchen...", "No results": "Keine Treffer",
    "Every coin in the game. Click a coin for details. Prices are in gold; energy is paid when you flip.": "Alle Münzen im Spiel. Klicke eine Münze für Details. Preise sind in Gold; Energie wird beim Werfen bezahlt.",
    "Filter by name...": "Nach Namen filtern...", "Clear": "Zurücksetzen", "Name": "Name", "Rarity": "Seltenheit", "Heads": "Kopf", "Tails": "Zahl",
    "Price": "Preis", "Energy": "Energie", "On Heads": "Bei Kopf", "On Tails": "Bei Zahl", "Heads chance": "Kopf-Chance", "Shop price": "Shop-Preis",
    "Energy to flip": "Energie zum Werfen", "gold": "Gold", "Nothing": "Nichts", "See the description": "Siehe Beschreibung",
    "Who can use it": "Wer sie nutzen kann", "Starts in the deck of:": "Startet im Deck von:", "Usable in coin sets from the start:": "Von Anfang an in Münzsets nutzbar:",
    "Found in the shop and unlocked by buying it once:": "Im Shop zu finden, durch einmaliges Kaufen freigeschaltet:", "none": "keine",
    "Notes": "Hinweise", "Related coins": "Ähnliche Münzen", "How to use it": "Anwendung", "Tips": "Tipps",
    "Effect": "Effekt", "Modifier": "Modifikator", "English": "Englisch",
    "One-use helpers bought in the shop (two offers per visit, you can hold three). Use one in a level while a coin is in play.": "Einweg-Helfer aus dem Shop (zwei Angebote pro Besuch, du kannst drei halten). Benutze einen im Level, während eine Münze im Spiel ist.",
    "Passive relics that last the whole run: one offer per shop visit, 25 gold each.": "Passive Relikte, die den ganzen Lauf halten: ein Angebot pro Shop-Besuch, je 25 Gold.",
    "25 gold (one prize per shop visit)": "25 Gold (eine Prämie pro Shop-Besuch)",
    "From level 2 on, every level (including the boss and every endless level) has one modifier, picked with the run's seed. It is shown at the bottom left of the stage.": "Ab Level 2 hat jedes Level (auch der Boss und jedes Endlos-Level) einen Modifikator, gewählt über den Seed des Laufs. Er steht unten links im Spielfeld.",
    "Each character has their own starting deck and their own coins in the shop.": "Jeder Charakter hat sein eigenes Startdeck und eigene Münzen im Shop.",
    "Unlocked by winning a run with": "Wird durch einen gewonnenen Lauf mit", "Available from the start.": "Von Anfang an verfügbar.",
    "Starting deck": "Startdeck", "Usable in coin sets from the start": "Von Anfang an in Münzsets nutzbar", "Found in the shop (unlocked by buying)": "Im Shop zu finden (durch Kauf freigeschaltet)",
    "How the game works, in plain words.": "Wie das Spiel funktioniert, in einfachen Worten.",
    "Tossup is a roguelike about flipping coins: build a small deck, flip one coin at a time, and score points against a quota before your stack runs out. This wiki lists everything in the game.": "Tossup ist ein Roguelike über Münzwürfe: Baue ein kleines Deck, wirf eine Münze nach der anderen und erziele Punkte gegen ein Ziel, bevor dein Stapel leer ist. Dieses Wiki listet alles im Spiel auf.",
    "Modifiers": "Modifikatoren", "The run in one table": "Der Lauf in einer Tabelle", "Level": "Level", "Quota per coin": "Ziel pro Münze", "Payout (gold)": "Prämie (Gold)", "ends the run": "beendet den Lauf",
    "The quota is the per-coin value times the number of coins in your deck. After the boss,": "Das Ziel ist der Wert pro Münze mal der Anzahl der Münzen in deinem Deck. Nach dem Boss fügt der",
    "Endless Mode": "Endlosmodus", "adds levels that ask 0.5 more per coin each time.": "Level hinzu, die jedes Mal 0,5 mehr pro Münze verlangen.",
    "Key numbers": "Wichtige Zahlen", "Starting gold": "Startgold", "Deck slots": "Deckplätze", "Exchange": "Tausch", "Combo": "Serie", "Bank": "Bank",
    "Start with the": "Beginne mit der", "Browse the": "Stöbere in den", "guide": "Anleitung", "or browse the": "oder stöbere in den", "coins": "Münzen",
}


def t(text):
    return DE.get(text, text) if LANG == "de" else text


def de_fmt(template, *numbers):
    for n in numbers:
        template = template.replace("%d", str(n), 1)
    return template.replace("%%", "%")


def esc(text):
    return html.escape(str(text), quote=True)


# ---------------------------------------------------------------- game text
def pct(x):
    return f"{round(x * 100):d}%"


def effect_text(e):
    t_, a, c = e["type"], e.get("amount", 0), e.get("coins", 1)
    pc = round(a * 100) if isinstance(a, float) else a
    one = {"score": ("Score %d points", [a]), "gold": ("Gain %d gold", [a]), "energy": ("Gain %d energy", [a]), "penalty": ("Quota +%d", [a]),
           "extra_draw": ("Goes back into the pile", []), "probability": ("Gain %d%% Heads this level", [pc]),
           "next_mult": ("Next %d coins pay x%d", [c, a]),
           "next_odds": (("Next coin: +%d%% Heads", [pc]) if c == 1 else ("Next %d coins: +%d%% Heads", [c, pc])),
           "next_swap": ("Next coin uses its other side", []), "next_heads": ("Next coin lands Heads", []),
           "combo_bonus": ("Combo grows %d extra step", [a]), "combo_shield": ("The next combo break is prevented", []),
           "amplify": ("Buffs last 1 coin longer and get stronger", []), "all_odds": ("All coins +%d%% Heads this level", [pc]),
           "peek": ("Look at the next two coins", []), "bank_discard": ("Discard one of the next three coins", []), "extra_exchange": ("One more exchange this level", [])}
    if t_ not in one:
        return t_
    template, numbers = one[t_]
    text = de_fmt(DESTR.get(template, template) if LANG == "de" else template, *numbers)
    return re.sub(r"\b1 points\b", "1 point", text).replace("1 Punkte", "1 Punkt")


def effects_html(effects, side, coin=None):
    if not effects:
        if coin and coin["hooks"]:
            return '<span class="muted">See the description</span>'
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
    lines = [l for l in text.split("\n") if not l.startswith("<!-- ")]   # GEN markers of tools/gen_docs.py
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


# ---------------------------------------------------------------- guides
sys.path.insert(0, str(Path(__file__).resolve().parent))
from wiki_guide import GUIDE  # the player guide pages (text in English and German, numbers filled in from the game data)


def guide_text(page):
    """One guide page as markdown: the {NAME} markers become the game's numbers and tables."""
    text = page["body"][LANG]
    for key, value in DATA["constants"].items():
        text = text.replace("{" + key + "}", f"{value:g}" if isinstance(value, float) and LANG == "en" else str(value).replace(".", ",") if LANG == "de" else str(value))
    decimal = (lambda x, d=1: f"{x:.{d}f}".replace(".", ",")) if LANG == "de" else (lambda x, d=1: f"{x:.{d}f}")
    head = "| # | Level | Ziel pro Münze | Prämie |" if LANG == "de" else "| # | Level | Quota per coin | Payout |"
    rows = [head, "|---|---|---|---|"]
    for i, stage in enumerate(DATA["route"], 1):
        name = DESTR.get(stage["name"], stage["name"]) if LANG == "de" else stage["name"]
        pay = (f"{stage['payout']} Gold" if LANG == "de" else f"{stage['payout']} gold") if stage.get("payout") else ("beendet den Lauf" if LANG == "de" else "ends the run")
        rows.append(f"| {i} | {name} | {decimal(stage['per_coin'], 1)} | {pay} |")
    text = text.replace("{ROUTE}", "\n".join(rows))
    rows = ["| Stufe | Regel |" if LANG == "de" else "| Stage | Rule |", "|---|---|"]
    for i, stage in enumerate(DATA["stakes"], 1):
        rule = DESTR.get(stage["info"], stage["info"]) if LANG == "de" else stage["info"]
        rows.append(f"| {i} | {rule} |")
    return text.replace("{STAGES}", "\n".join(rows))


GUIDES = GUIDE


def guide_link(url):
    return url


# ---------------------------------------------------------------- images
def make_images():
    for sub, size in (("coins", 160), ("items", 128), ("relics", 128)):
        (OUT / "img" / sub).mkdir(parents=True, exist_ok=True)
        for src in (RESOURCES / sub).glob("*.png"):
            image = Image.open(src).convert("RGBA")
            image.thumbnail((size, size), Image.Resampling.LANCZOS)
            image.save(OUT / "img" / sub / src.name, optimize=True)
    (OUT / "img" / "characters").mkdir(parents=True, exist_ok=True)
    for src in (RESOURCES / "characters").glob("*.png"):
        image = Image.open(src).convert("RGBA")
        image.thumbnail((360, 360), Image.Resampling.LANCZOS)
        image.save(OUT / "img" / "characters" / src.name, optimize=True)
    shutil.copy(RESOURCES / "ui" / "logo.png", OUT / "img" / "logo.png")
    shutil.copy(RESOURCES / "ui" / "icon.png", OUT / "img" / "icon.png")
    shutil.copy(RESOURCES / "fonts" / "m6x11plus.ttf", OUT / "m6x11plus.ttf")


# ---------------------------------------------------------------- page frame
SEARCH = []  # {name, alt, type, url, icon}; one list per language


def dname(group, entry):
    """Name of a game object in the current language."""
    if LANG == "de":
        found = DATA["de"].get(group, {}).get(entry["id"])
        if found:
            return found["name"]
    return entry["name"]


def ddesc(group, entry):
    if LANG == "de":
        found = DATA["de"].get(group, {}).get(entry["id"])
        if found and found.get("description"):
            return found["description"]
    return entry.get("description", "")


def frame(title, body, depth, active, path):
    base = "../" * depth
    assets = "../" * (depth + (1 if LANG == "de" else 0))
    nav = [("index.html", t("Home"), "home"), ("coins/index.html", f"{t('Coins')} ({len(DATA['coins'])})", "coins"),
           ("chips/index.html", f"{t('Chips')} ({len(DATA['items'])})", "chips"), ("prizes/index.html", f"{t('Prizes')} ({len(DATA['relics'])})", "prizes"),
           ("modifiers/index.html", t("Level modifiers"), "modifiers"), ("characters/index.html", t("Characters"), "characters"),
           ("guide/index.html", t("Guides"), "guide")]
    side = "".join(f'<a class="{"on" if key == active else ""}" href="{base}{href}">{esc(label)}</a>' for href, label, key in nav)
    en_href = f"{assets}{path}"
    de_href = f"{assets}de/{path}"
    switch = (f'<div class="lang"><a class="{"on" if LANG == "en" else ""}" href="{en_href}">EN</a>'
              f'<a class="{"on" if LANG == "de" else ""}" href="{de_href}">DE</a></div>')
    return f"""<!doctype html>
<html lang="{LANG}" data-base="{base}" data-assets="{assets}"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{esc(title)} - Tossup Wiki</title><link rel="icon" href="{assets}img/icon.png"><link rel="stylesheet" href="{assets}style.css"></head>
<body><header><a class="brand" href="{base}index.html"><img src="{assets}img/logo.png" alt="Tossup"><span>WIKI</span></a>
<div class="search"><input id="q" type="search" placeholder="{esc(t("Search coins, chips, prizes..."))}" autocomplete="off"><div id="results"></div></div>{switch}</header>
<div class="layout"><nav>{side}</nav><main>{body}</main></div>
<footer>Tossup (v{esc(VERSION)})</footer>
<script src="{assets}search-index-{LANG}.js"></script><script src="{assets}wiki.js"></script></body></html>"""


def write_page(path, title, body, depth, active):
    """Write a page into the current language's tree. `@A/` in the body is the path to the shared assets."""
    assets = "../" * (depth + (1 if LANG == "de" else 0))
    text = frame(title, body, depth, active, path).replace("@A/", assets)
    target = (OUT / "de" if LANG == "de" else OUT) / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(text)


def write(path, content):
    target = OUT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content)


def badge(rarity):
    name, colour = RARITY.get(rarity, ("?", "#999"))
    if LANG == "de":
        name = DESTR.get(name, name)
    return f'<span class="badge" style="--c:{colour}">{name}</span>'


def icon(kind, id_, size=48):
    return f'<img class="ico" src="@A/img/{kind}/{id_}.png" width="{size}" height="{size}" alt="">'


def de_line(group, id_):
    """The other language's name and description under the title (German in the English tree, English in the German one)."""
    if LANG == "en":
        entry = DATA["de"].get(group, {}).get(id_)
        if not entry:
            return ""
        desc = f" - {esc(entry['description'])}" if entry.get("description") else ""
        return f'<p class="de"><strong>Deutsch:</strong> {esc(entry["name"])}{desc}</p>'
    return ""


def char_links(ids):
    return ", ".join(f'<a href="../characters/{i}.html">{esc(dname("characters", CHARS[i]))}</a>' for i in ids) or t("none")


def coin_availability(coin_id):
    start = [c for c in CHAR_ORDER if coin_id in CHARS[c]["deck"]]
    pool = [c for c in CHAR_ORDER if coin_id in CHARS[c]["pool"]]
    locked = [c for c in CHAR_ORDER if coin_id in CHARS[c]["locked"]]
    return start, pool, locked


def effects_html(effects, side, coin=None):
    if not effects:
        if coin and coin["hooks"]:
            return f'<span class="muted">{esc(t("See the description"))}</span>'
        return f'<span class="muted">{esc(t("Nothing"))}</span>'
    return "<br>".join(esc(effect_text(e)) for e in effects)


# ---------------------------------------------------------------- pages
def coin_pages():
    rows = []
    for coin in DATA["coins"]:
        cid = coin["id"]
        name = dname("coins", coin)
        start, pool, locked = coin_availability(cid)
        usable = sorted(set(start + pool + locked))
        SEARCH.append({"name": name, "alt": coin["name"], "type": t("Coins"), "url": f"coins/{cid}.html", "icon": f"img/coins/{cid}.png"})
        related = [c for c in DATA["coins"] if c["id"] != cid and {e["type"] for e in c["heads"] + c["tails"]} & {e["type"] for e in coin["heads"] + coin["tails"]} - {"score"}][:8]
        body = f"""<h1>{esc(name)} {badge(coin["rarity"])}</h1>
<div class="hero"><img class="big" src="@A/img/coins/{cid}.png" alt="{esc(name)}">
<table class="stats">
<tr><th>{t("Heads chance")}</th><td>{pct(coin["probability"])}</td></tr>
<tr><th>{t("Shop price")}</th><td>{price(coin)} {t("gold")}</td></tr>
<tr><th>{t("Energy to flip")}</th><td>{coin.get("energy_cost", 0)}</td></tr>
<tr><th class="heads">{t("Heads")}</th><td>{effects_html(coin["heads"], "heads", coin)}</td></tr>
<tr><th class="tails">{t("Tails")}</th><td>{effects_html(coin["tails"], "tails", coin)}</td></tr>
</table></div>
<p class="lead">{esc(ddesc("coins", coin))}</p>
{de_line("coins", cid)}
<h2>{t("Who can use it")}</h2>
<ul><li><strong>{t("Starts in the deck of:")}</strong> {char_links(start)}</li>
<li><strong>{t("Usable in coin sets from the start:")}</strong> {char_links(pool)}</li>
<li><strong>{t("Found in the shop and unlocked by buying it once:")}</strong> {char_links(locked)}</li></ul>
{('<h2>' + t("Related coins") + '</h2><div class="cards">' + "".join(f'<a class="card" href="{c["id"]}.html">{icon("coins", c["id"])}<span>{esc(dname("coins", c))}</span></a>' for c in related) + "</div>") if related else ""}"""
        write_page(f"coins/{cid}.html", name, body, 1, "coins")
        rows.append(f"""<tr data-name="{esc((name + ' ' + coin['name']).lower())}" data-rarity="{coin["rarity"]}" data-chars="{' '.join(usable)}">
<td><a href="{cid}.html">{icon("coins", cid)}</a></td><td><a href="{cid}.html">{esc(name)}</a></td><td>{badge(coin["rarity"])}</td>
<td>{pct(coin["probability"])}</td><td>{price(coin)}</td><td>{coin.get("energy_cost", 0)}</td>
<td>{effects_html(coin["heads"], "h", coin)}</td><td>{effects_html(coin["tails"], "t", coin)}</td></tr>""")
    rarity_names = {k: (DESTR.get(v[0], v[0]) if LANG == "de" else v[0]) for k, v in RARITY.items()}
    filters = "".join(f'<button data-rarity="{k}" style="--c:{v[1]}">{rarity_names[k]}</button>' for k, v in RARITY.items())
    chars = "".join(f'<button data-char="{c}">{esc(dname("characters", CHARS[c]))}</button>' for c in CHAR_ORDER)
    body = f"""<h1>{t("Coins")}</h1><p>{t("Every coin in the game. Click a coin for details. Prices are in gold; energy is paid when you flip.")}</p>
<div class="filters"><input id="coinq" type="search" placeholder="{t("Filter by name...")}"><span>{filters}</span><span>{chars}</span><button id="clear">{t("Clear")}</button></div>
<table class="list" id="coin-table"><thead><tr><th></th><th>{t("Name")}</th><th>{t("Rarity")}</th><th>{t("Heads")}</th><th>{t("Price")}</th><th>{t("Energy")}</th><th>{t("On Heads")}</th><th>{t("On Tails")}</th></tr></thead>
<tbody>{"".join(rows)}</tbody></table>"""
    write_page("coins/index.html", t("Coins"), body, 1, "coins")


def simple_pages(kind, label, entries, group, dirname_):
    cards = []
    for e in entries:
        eid = e["id"]
        name = dname(group, e)
        SEARCH.append({"name": name, "alt": e["name"], "type": label, "url": f"{dirname_}/{eid}.html", "icon": f"img/{kind}/{eid}.png"})
        facts = []
        if kind == "items": facts.append(f"<tr><th>{t('Price')}</th><td>{e['cost']} {t('gold')}</td></tr>")
        if kind == "relics": facts.append(f"<tr><th>{t('Price')}</th><td>{t('25 gold (one prize per shop visit)') if LANG == 'de' else '25 gold (one prize per shop visit)'}</td></tr>")
        body = f"""<h1>{esc(name)}</h1><div class="hero"><img class="big" src="@A/img/{kind}/{eid}.png" alt="">
<table class="stats">{"".join(facts)}<tr><th>{t("Effect")}</th><td>{esc(ddesc(group, e))}</td></tr></table></div>
{de_line(group, eid)}"""
        write_page(f"{dirname_}/{eid}.html", name, body, 1, dirname_)
        cards.append(f'<a class="card wide" href="{eid}.html">{icon(kind, eid, 64)}<span><strong>{esc(name)}</strong>'
                     f'{f" <em>{e["cost"]}g</em>" if kind == "items" else ""}<br><small>{esc(ddesc(group, e))}</small></span></a>')
    return cards


def other_pages():
    cards = simple_pages("items", t("Chips"), DATA["items"], "items", "chips")
    write_page("chips/index.html", t("Chips"), f"<h1>{t('Chips')}</h1><p>{t('One-use helpers bought in the shop (two offers per visit, you can hold three). Use one in a level while a coin is in play.')}</p><div class=\"cards\">" + "".join(cards) + "</div>", 1, "chips")
    cards = simple_pages("relics", t("Prizes"), DATA["relics"], "relics", "prizes")
    write_page("prizes/index.html", t("Prizes"), f"<h1>{t('Prizes')}</h1><p>{t('Passive relics that last the whole run: one offer per shop visit, 25 gold each.')}</p><div class=\"cards\">" + "".join(cards) + "</div>", 1, "prizes")
    # modifiers
    other_col = t("English") if LANG == "de" else "Deutsch"
    rows = ""
    for m in DATA["modifiers"]:
        other = m["name"] if LANG == "de" else DATA["de"]["modifiers"][m["id"]]["name"]
        rows += f"<tr><td><strong>{esc(dname('modifiers', m))}</strong></td><td>{esc(ddesc('modifiers', m))}</td><td class='de'>{esc(other)}</td></tr>"
        SEARCH.append({"name": dname("modifiers", m), "alt": m["name"], "type": t("Modifier"), "url": "modifiers/index.html", "icon": "img/icon.png"})
    write_page("modifiers/index.html", t("Level modifiers"), f"<h1>{t('Level modifiers')}</h1><p>{t('From level 2 on, every level (including the boss and every endless level) has one modifier, picked with the run\'s seed. It is shown at the bottom left of the stage.')}</p><table class='list'><thead><tr><th>{t('Modifier')}</th><th>{t('Effect')}</th><th>{other_col}</th></tr></thead><tbody>{rows}</tbody></table>", 1, "modifiers")
    # characters
    cards = []
    for c in DATA["characters"]:
        cid = c["id"]
        name = dname("characters", c)
        SEARCH.append({"name": name, "alt": c["name"], "type": t("Characters"), "url": f"characters/{cid}.html", "icon": f"img/characters/{cid}.png"})
        def coin_cards(ids):
            return '<div class="cards">' + "".join(f'<a class="card" href="../coins/{i}.html">{icon("coins", i)}<span>{esc(dname("coins", COINS[i]))}</span></a>' for i in ids) + "</div>"
        req = UNLOCK_AFTER.get(cid)
        if req:
            unlock = (f"{t('Unlocked by winning a run with')} {esc(dname('characters', CHARS[req]))}." if LANG == "en"
                      else f"Wird durch einen gewonnenen Lauf mit {esc(dname('characters', CHARS[req]))} freigeschaltet.")
        else:
            unlock = t("Available from the start.")
        body = f"""<h1>{esc(name)}</h1><div class="hero"><img class="portrait" src="@A/img/characters/{cid}.png" alt="">
<div><p class="lead">{esc(ddesc("characters", c))}</p><p>{unlock}</p>{de_line("characters", cid)}</div></div>
<h2>{t("Starting deck")}</h2>{coin_cards(c["deck"])}
<h2>{t("Usable in coin sets from the start")}</h2>{coin_cards(c["pool"])}
<h2>{t("Found in the shop (unlocked by buying)")}</h2>{coin_cards(c["locked"])}"""
        write_page(f"characters/{cid}.html", name, body, 1, "characters")
        cards.append(f'<a class="card wide" href="{cid}.html"><img class="ico" src="@A/img/characters/{cid}.png" width="64" height="64" style="object-fit:cover;border-radius:8px"><span><strong>{esc(name)}</strong><br><small>{esc(ddesc("characters", c))} - {unlock}</small></span></a>')
    write_page("characters/index.html", t("Characters"), f"<h1>{t('Characters')}</h1><p>{t('Each character has their own starting deck and their own coins in the shop.')}</p><div class=\"cards\">" + "".join(cards) + "</div>", 1, "characters")


def guide_pages():
    cards = []
    for page in GUIDES:
        title = page["title"][LANG]
        text = guide_text(page)
        SEARCH.append({"name": title, "alt": page["title"]["de" if LANG == "en" else "en"], "type": t("Guides"), "url": f"guide/{page['slug']}.html", "icon": "img/icon.png"})
        write_page(f"guide/{page['slug']}.html", title, md(text, guide_link), 1, "guide")
        cards.append(f'<a class="card wide" href="{page["slug"]}.html"><span><strong>{esc(title)}</strong></span></a>')
    write_page("guide/index.html", t("Guides"), f"<h1>{t('Guides')}</h1><p>{t('How the game works, in plain words.')}</p><div class=\"cards\">" + "".join(cards) + "</div>", 1, "guide")


def home():
    c = DATA["constants"]
    route = "".join(f"<tr><td>{i + 1}</td><td>{esc(t(s['name']) if LANG == 'en' else DESTR.get(s['name'], s['name']))}</td><td>{s['per_coin']}</td><td>{s.get('payout') or t('ends the run')}</td></tr>" for i, s in enumerate(DATA["route"]))
    counts = [(t("Coins"), "coins/index.html", len(DATA["coins"])), (t("Chips"), "chips/index.html", len(DATA["items"])), (t("Prizes"), "prizes/index.html", len(DATA["relics"])),
              (t("Modifiers"), "modifiers/index.html", len(DATA["modifiers"])), (t("Characters"), "characters/index.html", len(DATA["characters"]))] + [(t("Guides"), "guide/index.html", len(GUIDES))]
    tiles = "".join(f'<a class="tile" href="{u}"><strong>{n}</strong><span>{l}</span></a>' for l, u, n in counts)
    if LANG == "de":
        intro = t("Tossup is a roguelike about flipping coins: build a small deck, flip one coin at a time, and score points against a quota before your stack runs out. This wiki lists everything in the game.")
        quota = f"{t('The quota is the per-coin value times the number of coins in your deck. After the boss,')} <strong>{t('Endless Mode')}</strong> {t('adds levels that ask 0.5 more per coin each time.')}"
        numbers = (f"<tr><th>{t('Starting gold')}</th><td>{c['START_GOLD']}</td></tr><tr><th>{t('Deck slots')}</th><td>{c['START_MAX']} am Anfang, bis zu {c['DECK_MAX']} (erster {c['SLOT_COST']} Gold, jeder weitere {c['SLOT_STEP']} mehr)</td></tr>"
                   f"<tr><th>{t('Exchange')}</th><td>{c['EXCHANGE_BASE']} Gold zahlen (+{c['EXCHANGE_STEP']} jedes Mal), um {c['EXCHANGE_GAIN']} gespielte Münzen zurückzubekommen, höchstens {c['EXCHANGE_MAX']}-mal pro Level</td></tr>"
                   f"<tr><th>{t('Combo')}</th><td>+{c['COMBO_STEP']} Multiplikator je gleiches Ergebnis in Folge, bis x{c['COMBO_CAP']}</td></tr>"
                   f"<tr><th>{t('Bank')}</th><td>du siehst die nächsten {c['VISIBLE']} Münzen; die Starthand hat {c['MULLIGAN']}</td></tr>")
        outro = f"{t('Start with the')} <a href=\"guide/play.html\">{t('guide')}</a> {t('or browse the')} <a href=\"coins/index.html\">{t('coins')}</a>."
    else:
        intro = "Tossup is a roguelike about flipping coins: build a small deck, flip one coin at a time, and score points against a quota before your stack runs out. This wiki lists everything in the game."
        quota = "The quota is the per-coin value times the number of coins in your deck. After the boss, <strong>Endless Mode</strong> adds levels that ask 0.5 more per coin each time."
        numbers = (f"<tr><th>Starting gold</th><td>{c['START_GOLD']}</td></tr><tr><th>Deck slots</th><td>{c['START_MAX']} at the start, up to {c['DECK_MAX']} (first {c['SLOT_COST']} gold, each further one {c['SLOT_STEP']} more)</td></tr>"
                   f"<tr><th>Exchange</th><td>pay {c['EXCHANGE_BASE']} gold (+{c['EXCHANGE_STEP']} each time) to get {c['EXCHANGE_GAIN']} played coins back, at most {c['EXCHANGE_MAX']} times per level</td></tr>"
                   f"<tr><th>Combo</th><td>+{c['COMBO_STEP']} multiplier per same result in a row, up to x{c['COMBO_CAP']}</td></tr>"
                   f"<tr><th>Bank</th><td>you see the next {c['VISIBLE']} coins; the opening hand has {c['MULLIGAN']}</td></tr>")
        outro = 'Start with the <a href="guide/play.html">guide</a> or browse the <a href="coins/index.html">coins</a>.'
    body = f"""<h1>Tossup Wiki</h1>
<p class="lead">{intro}</p>
<div class="tiles">{tiles}</div>
<h2>{t("The run in one table")}</h2>
<table class="list"><thead><tr><th>#</th><th>{t("Level")}</th><th>{t("Quota per coin")}</th><th>{t("Payout (gold)")}</th></tr></thead><tbody>{route}</tbody></table>
<p>{quota}</p>
<h2>{t("Key numbers")}</h2>
<table class="stats">{numbers}</table>
<p>{outro}</p>"""
    write_page("index.html", "Home", body, 0, "home")


CSS = """
:root{--screen:#17454d;--panel:#0f333b;--card:#1d4f58;--line:#2f6670;--gold:#f3b958;--blue:#009dff;--red:#ff5f55;--green:#4bc292;--face:#f7efd7;--muted:#9fc2c6;--bg:#274b49}
@font-face{font-family:Pixel;src:url(m6x11plus.ttf)}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--face);font:16px/1.55 -apple-system,Segoe UI,Roboto,sans-serif}
a{color:var(--gold);text-decoration:none}a:hover{text-decoration:underline}
header{display:flex;align-items:center;gap:24px;padding:10px 24px;background:var(--panel);border-bottom:3px solid var(--gold);position:sticky;top:0;z-index:5}
.brand{display:flex;align-items:center;gap:12px;color:var(--face)}.brand img{height:34px}.brand span{font-family:Pixel;font-size:28px;color:var(--gold)}
.search{position:relative;flex:1;max-width:460px}.lang{margin-left:auto;display:flex;border:2px solid var(--line);border-radius:999px;overflow:hidden}.lang a{padding:4px 14px;color:var(--face);font:16px Pixel}.lang a.on{background:var(--gold);color:#17454d}.lang a:hover{text-decoration:none;background:var(--card)}.lang a.on:hover{background:var(--gold)}.search input{width:100%;padding:8px 12px;border-radius:8px;border:2px solid var(--line);background:var(--screen);color:var(--face);font-size:15px}
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
  var base = document.documentElement.dataset.base || '', assets = document.documentElement.dataset.assets || '';
  var q = document.getElementById('q'), box = document.getElementById('results');
  if (q) {
    q.addEventListener('input', function () {
      var t = q.value.trim().toLowerCase();
      if (!t) { box.style.display = 'none'; return; }
      var hits = SEARCH_INDEX.filter(function (e) { return (e.name + ' ' + (e.alt || '')).toLowerCase().indexOf(t) >= 0; }).slice(0, 12);
      box.innerHTML = hits.map(function (e) { return '<a href="' + base + e.url + '"><img src="' + assets + e.icon + '" alt=""><span>' + e.name + '</span><small>' + e.type + '</small></a>'; }).join('') || '<a>' + (document.documentElement.lang === 'de' ? 'Keine Treffer' : 'No results') + '</a>';
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

def build(lang):
    global LANG
    LANG = lang
    SEARCH.clear()
    coin_pages()
    other_pages()
    guide_pages()
    home()
    write(f"search-index-{lang}.js", "var SEARCH_INDEX = " + json.dumps(SEARCH, ensure_ascii=False) + ";")


if __name__ == "__main__":
    version_file = (ROOT / "src" / "version.lua").read_text()
    VERSION = re.search(r'number = "([^"]+)"', version_file).group(1)
    if OUT.exists():
        shutil.rmtree(OUT)
    OUT.mkdir()
    make_images()
    build("en")
    build("de")
    write("style.css", CSS)
    write("wiki.js", JS)
    pages = sum(1 for _ in OUT.rglob("*.html"))
    print(f"wiki: {pages} pages (English and German) in {OUT}")
