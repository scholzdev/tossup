#!/usr/bin/env python3
"""Keep the static tables of the docs in step with the game's data.

The docs mix prose (written by hand) with tables that only restate data (coins, chips, prizes, modifiers, levels, characters).
This script rewrites the data parts from the game's content: blocks between `<!-- GEN:name -->` and `<!-- /GEN:name -->` markers
(it adds the markers the first time), and the "(R) - 45% - 1 energy" part of the coin headings in docs/coins.md and docs/de/coins.md.
Prose and the notes columns of the chip and prize tables are kept.

    python3 Tools/gen_docs.py           rewrite the docs
    python3 Tools/gen_docs.py --check   change nothing; exit 1 (and say what) if a doc is out of date or a coin is missing
Run through tools/build_docs.sh, which dumps the content first.
"""

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CHECK = "--check" in sys.argv
JSON_PATH = [a for a in sys.argv[1:] if not a.startswith("--")][0]
sys.argv = [sys.argv[0], JSON_PATH]
sys.path.insert(0, str(ROOT / "Tools"))
import gen_wiki as W  # noqa: E402  (its helpers: effect_text, dname, price, pct; importing it builds nothing)

DATA = W.DATA
RARITY_LETTER = {"N": "N", "R": "R", "SR": "SR", "UR": "UR"}
problems = []


def floor_half(x):
    return int(x + 0.5)


def number(x, de):
    text = f"{x:g}" if x != int(x) else f"{x:.1f}" if x in (3.0,) else f"{x:g}"
    return text.replace(".", ",") if de else text


def eff(effects, de):
    W.LANG = "de" if de else "en"
    return ", ".join(W.effect_text(e) for e in effects) or ("nichts" if de else "nothing")


def pct(x):
    return f"{round(x * 100):d}%"


# ---------------------------------------------------------------- blocks
def coins_table(de):
    W.LANG = "de" if de else "en"
    head = ("| Münze | Seltenheit | Kopf | Preis | Energie | Bei Kopf | Bei Zahl |" if de else "| Coin | Rarity | Heads | Price | Energy | On Heads | On Tails |")
    rows = [head, "|---|---|---|---|---|---|---|"]
    for coin in DATA["coins"]:
        rarity = W.DESTR.get(W.RARITY[coin["rarity"]][0], W.RARITY[coin["rarity"]][0]) if de else W.RARITY[coin["rarity"]][0]
        rows.append(f"| {W.dname('coins', coin)} | {rarity} | {pct(coin['probability'])} | {coin.get('cost', 15)} | {coin.get('energy_cost', 0)} | "
                    f"{eff(coin['heads'], de) if coin['heads'] else ('(siehe Beschreibung)' if de else '(see description)') if coin['hooks'] else eff(coin['heads'], de)} | "
                    f"{eff(coin['tails'], de) if coin['tails'] else ('(siehe Beschreibung)' if de else '(see description)') if coin['hooks'] else eff(coin['tails'], de)} |")
    return "\n".join(rows)


def levels_table(de, spec=False):
    if spec:
        rows = ["| # | Name | Quota per coin | Quota with 5 / 10 coins | Payout (gold) |", "|---|---|---|---|---|"]
    elif de:
        rows = ["| # | Level | Ziel pro Münze | Ziel bei 5 / 10 Münzen | Prämie |", "|---|---|---|---|---|"]
    else:
        rows = ["| # | Level | Quota per coin | Quota with 5 / 10 coins | Payout |", "|---|---|---|---|---|"]
    for i, s in enumerate(DATA["route"], 1):
        name = W.DESTR.get(s["name"], s["name"]) if de else s["name"]
        per = s["per_coin"]
        quota = f"{max(1, floor_half(per * 5))} / {max(1, floor_half(per * 10))}"
        if de:
            pay = f"{s['payout']} Gold" if s.get("payout") else "keine (beendet den Lauf)"
        elif spec:
            pay = str(s["payout"]) if s.get("payout") else "none"
        else:
            pay = f"{s['payout']} gold" if s.get("payout") else "none (ends the run)"
        if spec and s.get("boss"):
            name += " (boss)"
        rows.append(f"| {i} | {name} | {number(per, de) if per != 3 else ('3,0' if de else '3.0')} | {quota} | {pay} |")
    return "\n".join(rows)


def modifiers_table(de):
    rows = ["| Modifikator | Effekt |" if de else "| Modifier | Effect |", "|---|---|"]
    for m in DATA["modifiers"]:
        W.LANG = "de" if de else "en"
        rows.append(f"| {W.dname('modifiers', m)} | {W.ddesc('modifiers', m)} |")
    return "\n".join(rows)


def character_table(char_id, de):
    c = next(c for c in DATA["characters"] if c["id"] == char_id)
    W.LANG = "de" if de else "en"
    deck = {}
    for coin_id in c["deck"]:
        deck[coin_id] = deck.get(coin_id, 0) + 1
    deck_text = ", ".join((f"{n} x " if n > 1 else "") + W.dname("coins", W.COINS[i]) for i, n in deck.items())
    names = lambda ids: ", ".join(W.dname("coins", W.COINS[i]) for i in ids)
    labels = ("Standard-Deck", "Pool", "Gesperrt") if de else ("Default deck", "Pool", "Locked")
    return "\n".join(["| | |", "|---|---|", f"| **{labels[0]}** | {deck_text} |", f"| **{labels[1]}** | {names(c['pool'])} |", f"| **{labels[2]}** | {names(c['locked'])} |"])


def chips_table(de, old_notes):
    W.LANG = "de" if de else "en"
    rows = ["| Chip | Preis | Effekt | Hinweise |" if de else "| Chip | Cost | Effect | Notes |", "|---|---|---|---|"]
    for item in DATA["items"]:
        name = W.dname("items", item)
        rows.append(f"| **{name}** | {item['cost']} | {W.ddesc('items', item)} | {old_notes.get(name.lower(), '')} |")
    return "\n".join(rows)


def prizes_table(de, old_notes):
    W.LANG = "de" if de else "en"
    rows = ["| Prämie | Effekt | Hinweise |" if de else "| Prize | Effect | Notes |", "|---|---|---|"]
    for relic in DATA["relics"]:
        name = W.dname("relics", relic)
        rows.append(f"| **{name}** | {W.ddesc('relics', relic)} | {old_notes.get(name.lower(), '')} |")
    return "\n".join(rows)


def spec_coins():
    W.LANG = "en"
    rows = ["| id | Name | Rar | Heads | Cost | Energy | On Heads | On Tails | Description | Hooks |", "|---|---|---|---|---|---|---|---|---|---|"]
    for c in DATA["coins"]:
        rows.append(f"| `{c['id']}` | {c['name']} | {c['rarity']} | {pct(c['probability'])} | {c.get('cost', 15)} | {c.get('energy_cost', 0)} | "
                    f"{eff(c['heads'], False)} | {eff(c['tails'], False)} | {c['description']} | {', '.join(c['hooks']) or '-'} |")
    return "\n".join(rows)


def spec_items():
    rows = ["| id | Name | Cost | Effect |", "|---|---|---|---|"]
    for i in DATA["items"]:
        rows.append(f"| `{i['id']}` | {i['name']} | {i['cost']} | {i['description']} |")
    return "\n".join(rows)


def spec_relics():
    rows = ["| id | Name | Effect |", "|---|---|---|"]
    for r in DATA["relics"]:
        rows.append(f"| `{r['id']}` | {r['name']} | {r['description']} |")
    return "\n".join(rows)


def spec_characters():
    rows = ["| id | Name | Pool (usable from the start) | Locked (unlock by buying) |", "|---|---|---|---|"]
    for c in DATA["characters"]:
        rows.append(f"| `{c['id']}` | {c['name']} - \"{c['description']}\" | {', '.join(c['pool'])} | {', '.join(c['locked'])} |")
    return "\n".join(rows)


# ---------------------------------------------------------------- applying blocks to files
def block_re(name):
    return re.compile(rf"<!-- GEN:{name} -->\n(.*?)\n<!-- /GEN:{name} -->", re.S)


def wrap_table(text, header_prefix, name, nth=1):
    """First run: wrap the existing table (the nth one starting with header_prefix) in markers."""
    if block_re(name).search(text):
        return text
    lines = text.split("\n")
    seen = 0
    for i, line in enumerate(lines):
        if line.startswith(header_prefix):
            seen += 1
            if seen == nth:
                j = i
                while j < len(lines) and lines[j].startswith("|"):
                    j += 1
                return "\n".join(lines[:i] + [f"<!-- GEN:{name} -->"] + lines[i:j] + [f"<!-- /GEN:{name} -->"] + lines[j:])
    problems.append(f"no table starting with {header_prefix!r} to wrap as {name}")
    return text


def fill(text, name, content):
    m = block_re(name)
    if not m.search(text):
        problems.append(f"marker GEN:{name} missing")
        return text
    return m.sub(lambda _: f"<!-- GEN:{name} -->\n{content}\n<!-- /GEN:{name} -->", text)


def old_notes(text, name):
    """Notes column of the current chip / prize table, by name, so regenerating keeps them."""
    m = block_re(name).search(text)
    notes = {}
    if m:
        for line in m.group(1).split("\n")[2:]:
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            mm = re.match(r"^\*\*(.+?)\*\*$", cells[0])
            if mm and len(cells) >= 3:
                notes[mm.group(1).lower()] = cells[-1]
    return notes


def fix_headings(text, de):
    """### Name (R) - 45% - 1 energy - 20 gold: rebuilt from the data for every coin heading."""
    W.LANG = "de" if de else "en"
    by_name = {W.dname("coins", c).lower(): c for c in DATA["coins"]}
    found = set()

    def repl(m):
        coin = by_name.get(m.group(1).strip().lower())
        if not coin:
            return m.group(0)
        found.add(coin["id"])
        parts = [f"{coin['rarity']}", pct(coin["probability"])]
        out = f"### {m.group(1).strip()} ({coin['rarity']}) - {pct(coin['probability'])}"
        if coin.get("energy_cost", 0) > 0:
            out += f" - {coin['energy_cost']} " + ("Energie" if de else "energy")
        if coin.get("cost", 15) != 15:
            out += f" - {coin['cost']} " + ("Gold" if de else "gold")
        return out

    text = re.sub(r"^### ([^\n(]+?) \((?:N|R|SR|UR)\)[^\n]*$", repl, text, flags=re.M)
    for coin in DATA["coins"]:
        if coin["id"] not in found:
            problems.append(f"{'docs/de/coins.md' if de else 'docs/coins.md'}: no section for coin {coin['id']} ({W.dname('coins', coin)})")
    return text


def process(path, edit):
    if not path.exists():   # the markdown is kept locally, not in the repository
        print(f"skipped {path.relative_to(ROOT)} (not there)")
        return
    text = path.read_text()
    new = edit(text)
    if new != text:
        if CHECK:
            problems.append(f"{path.relative_to(ROOT)} is out of date (run tools/build_docs.sh)")
        else:
            path.write_text(new)
            print(f"updated {path.relative_to(ROOT)}")


def edit_coins(de):
    def edit(text):
        if not block_re("coins-table").search(text):
            marker = "<!-- GEN:coins-table -->\n\n<!-- /GEN:coins-table -->"
            heading = "## Alle Münzen auf einen Blick" if de else "## All coins at a glance"
            text = re.sub(r"^(## (?:Buffers|Boni))", f"{heading}\n\n{marker}\n\n---\n\n\\1", text, count=1, flags=re.M)
        text = fill(text, "coins-table", coins_table(de))
        return fix_headings(text, de)
    return edit


def edit_gameplay(de):
    def edit(text):
        text = wrap_table(text, "| # | Level |", "levels")
        text = wrap_table(text, "| Modifikator |" if de else "| Modifier |", "modifiers")
        return fill(fill(text, "levels", levels_table(de)), "modifiers", modifiers_table(de))
    return edit


def edit_characters(de):
    def edit(text):
        for n, cid in enumerate(["blade", "seer", "trader"], 1):
            text = wrap_table(text, "| | |", f"char-{cid}", n) if not block_re(f"char-{cid}").search(text) else text
        for cid in ["blade", "seer", "trader"]:
            text = fill(text, f"char-{cid}", character_table(cid, de))
        return text
    return edit


def edit_items(de):
    def edit(text):
        text = wrap_table(text, "| Chip |", "chips")
        text = wrap_table(text, "| Prämie |" if de else "| Prize |", "prizes")
        chips_notes, prizes_notes = old_notes(text, "chips"), old_notes(text, "prizes")
        return fill(fill(text, "chips", chips_table(de, chips_notes)), "prizes", prizes_table(de, prizes_notes))
    return edit


def edit_spec(text):
    for prefix, name in [("| # | Name |", "spec-route"), ("| id | Name | Rar", "spec-coins"), ("| id | Name | Cost | Effect", "spec-items"),
                         ("| id | Name | Effect", "spec-relics"), ("| id | Name | Pool", "spec-characters")]:
        text = wrap_table(text, prefix, name)
    text = fill(text, "spec-route", levels_table(False, spec=True))
    return fill(fill(fill(fill(text, "spec-coins", spec_coins()), "spec-items", spec_items()), "spec-relics", spec_relics()), "spec-characters", spec_characters())


if __name__ == "__main__":
    docs = ROOT / "docs"
    process(docs / "coins.md", edit_coins(False))
    process(docs / "de" / "coins.md", edit_coins(True))
    process(docs / "gameplay.md", edit_gameplay(False))
    process(docs / "de" / "gameplay.md", edit_gameplay(True))
    process(docs / "characters.md", edit_characters(False))
    process(docs / "de" / "characters.md", edit_characters(True))
    process(docs / "items-and-relics.md", edit_items(False))
    process(docs / "de" / "items-and-relics.md", edit_items(True))
    process(ROOT / "GAME_SPEC.md", edit_spec)
    if problems:
        print("\n".join(problems))
        sys.exit(1 if CHECK or problems else 0)
    print("docs are up to date" if CHECK else "docs done")
