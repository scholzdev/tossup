"""The player guide pages of the wiki, in English and German. Plain text with a few markers that gen_wiki.py fills from the game
data, so the numbers never go stale: {START_GOLD} and the other constants of Game, {ROUTE} (the level table), {STAGES} (the stage table).
Written for people who play the game, not for people who work on it."""

GUIDE = [
    {
        "slug": "play",
        "title": {"en": "How to play", "de": "So wird gespielt"},
        "body": {
            "en": """# How to play

You flip coins and score points. Each level has a quota; reach it before your coins run out, visit the shop, buy better coins, and go on. Beat the last level, The House, to win the run.

## A level

At the start of a level you are dealt an opening hand of {MULLIGAN} coins from your deck. Throw away any you do not want for this level (that is free), then press Start Level. The coins you keep form your stack, and the stack is all you get: every coin is played once, nothing is reshuffled. A bigger deck means more flips, but also a higher quota.

The left panel shows the next {VISIBLE} coins. The first one is the coin in play. You can flip it or discard it for the rest of the level. Throwing a weak coin away is free, but it also costs you that flip, so it is a real choice. The last usable coin cannot be discarded.

When you flip, the coin lands on Heads or Tails. Each side has an effect: points, gold, energy, a bonus for the coins after it, or a penalty. The odds of Heads are written on every coin and can change during the level.

## Levels and quota

The quota is the level's value per coin times the number of coins in your deck when the level starts, so every coin you buy raises the next target.

{ROUTE}

You start the run with {START_GOLD} gold and with {START_MAX} coin slots. You get 3 energy at the start of every level.

## Energy

Strong coins cost energy to flip (shown as E1 or E2 on the coin). Energy only comes back from certain coins, so a deck full of heavy hitters needs something that makes it. A coin you cannot pay for cannot be flipped, so discard it or flip something else. The last usable coin is always flipped.

## Combo

Flipping the same result several times in a row builds a multiplier on the points and gold of each flip: +{COMBO_STEP} for every extra flip, up to x{COMBO_CAP}. A different result starts again. Throwing away a risky coin to protect a streak is often right.

## After the quota

Once the quota is met you get the level payout at once and the level stays open. Every 2 points beyond the quota pay 1 more gold, so you can keep flipping for gold or press Open Shop whenever you like. The House ends the run the moment its quota is met, as a win.

## Out of coins

If your stack is empty and the quota is not met, you can Exchange: pay gold ({EXCHANGE_BASE} the first time, {EXCHANGE_STEP} more each time after that) to get {EXCHANGE_GAIN} of the coins you already played back into the stack. You can do this at most {EXCHANGE_MAX} times per level. If you cannot pay, or have used them all, the level is lost and the run is over.

## The shop

Between levels you visit the shop: four coins, two chips and one prize, plus a few services for the coin you have selected in your deck.

- Coins go into your deck. Buying a coin you have not unlocked yet unlocks it for good, so it can be in your starting sets from then on.
- Chips are one-use items. You can hold 3. Use them from the bar during a level while a coin is dealt.
- Prizes are passive and last the whole run.
- Reroll shows new coins (4 gold, 2 more every time). An extra deck slot costs 5 gold, up to {DECK_MAX} slots. Removing a coin and tuning its odds are there too.

A full deck cannot buy coins until you buy a slot or remove one.

## Level modifiers

From the second level on, every level has one modifier that changes the rules a little, shown at the bottom left of the stage. See the list of level modifiers.

## Endless mode

After you beat The House you can keep going. Every further level asks for 0.5 more points per coin than the one before, and every 5th flip is turned around like in The House. The run ends when you lose a level, and your best result is saved per character.

## Coin sets

Before a run you pick a character and one of its three coin sets. A set holds {START_MAX} coins, at most {MAX_COPIES} of the same coin (the plain Normal coin may fill the whole set). Edit sets under Coin Sets in the main menu. A set that is empty uses the character's default deck.
""",
            "de": """# So wird gespielt

Du wirfst Münzen und sammelst Punkte. Jedes Level hat ein Ziel; erreiche es, bevor deine Münzen ausgehen, geh in den Shop, kauf bessere Münzen und mach weiter. Besiege das letzte Level, Das Haus, und du gewinnst den Lauf.

## Ein Level

Zu Beginn eines Levels bekommst du eine Starthand mit {MULLIGAN} Münzen aus deinem Deck. Wirf alle weg, die du für dieses Level nicht willst (das kostet nichts), dann drücke Level starten. Die Münzen, die du behältst, bilden deinen Stapel, und mehr bekommst du nicht: Jede Münze wird einmal gespielt, nichts wird neu gemischt. Ein größeres Deck bedeutet mehr Würfe, aber auch ein höheres Ziel.

Links siehst du die nächsten {VISIBLE} Münzen. Die erste ist die Münze im Spiel. Du kannst sie werfen oder für den Rest des Levels abwerfen. Eine schwache Münze wegzuwerfen ist kostenlos, nimmt dir aber diesen Wurf, es ist also eine echte Entscheidung. Die letzte nutzbare Münze kann nicht abgeworfen werden.

Beim Werfen landet die Münze auf Kopf oder Zahl. Jede Seite hat einen Effekt: Punkte, Gold, Energie, einen Bonus für die folgenden Münzen oder eine Strafe. Die Kopf-Chance steht auf jeder Münze und kann sich im Level ändern.

## Level und Ziel

Das Ziel ist der Wert des Levels pro Münze mal der Zahl der Münzen in deinem Deck zu Beginn des Levels. Jede Münze, die du kaufst, erhöht also das nächste Ziel.

{ROUTE}

Du startest den Lauf mit {START_GOLD} Gold und {START_MAX} Münzplätzen. Zu Beginn jedes Levels bekommst du 3 Energie.

## Energie

Starke Münzen kosten Energie zum Werfen (auf der Münze als E1 oder E2 angezeigt). Energie kommt nur von bestimmten Münzen zurück, ein Deck voller Schwergewichte braucht also etwas, das sie erzeugt. Eine Münze, die du nicht bezahlen kannst, kann nicht geworfen werden; wirf sie ab oder wirf etwas anderes. Die letzte nutzbare Münze wird immer geworfen.

## Serie

Dasselbe Ergebnis mehrmals hintereinander baut einen Multiplikator auf die Punkte und das Gold jedes Wurfs auf: +{COMBO_STEP} für jeden weiteren Wurf, bis x{COMBO_CAP}. Ein anderes Ergebnis fängt neu an. Eine riskante Münze abzuwerfen, um eine Serie zu schützen, ist oft richtig.

## Nach dem Ziel

Sobald das Ziel erreicht ist, bekommst du die Prämie des Levels sofort, und das Level bleibt offen. Je 2 Punkte über dem Ziel bringen 1 weiteres Gold, du kannst also weiterwerfen oder jederzeit Shop öffnen drücken. Das Haus beendet den Lauf in dem Moment, in dem sein Ziel erreicht ist, als Sieg.

## Keine Münzen mehr

Ist dein Stapel leer und das Ziel nicht erreicht, kannst du tauschen: Zahle Gold ({EXCHANGE_BASE} beim ersten Mal, danach jeweils {EXCHANGE_STEP} mehr), um {EXCHANGE_GAIN} der schon gespielten Münzen zurück in den Stapel zu holen. Das geht höchstens {EXCHANGE_MAX}-mal pro Level. Kannst du nicht zahlen oder hast alle benutzt, ist das Level verloren und der Lauf vorbei.

## Der Shop

Zwischen den Leveln gehst du in den Shop: vier Münzen, zwei Chips und eine Prämie, dazu ein paar Dienste für die Münze, die du in deinem Deck ausgewählt hast.

- Münzen kommen in dein Deck. Kaufst du eine noch nicht freigeschaltete Münze, ist sie dauerhaft freigeschaltet und kann danach in deine Startsets.
- Chips sind Einwegartikel. Du kannst 3 halten. Benutze sie über die Leiste, solange eine Münze ausgeteilt ist.
- Prämien wirken passiv und halten den ganzen Lauf.
- Neu würfeln zeigt neue Münzen (4 Gold, jedes Mal 2 mehr). Ein zusätzlicher Deckplatz kostet 5 Gold, bis zu {DECK_MAX} Plätze. Eine Münze entfernen und ihre Chance verbessern gibt es auch.

Ein volles Deck kann keine Münzen kaufen, bis du einen Platz kaufst oder eine Münze entfernst.

## Level-Modifikatoren

Ab dem zweiten Level hat jedes Level einen Modifikator, der die Regeln ein wenig ändert, unten links im Spielfeld angezeigt. Siehe die Liste der Level-Modifikatoren.

## Endlosmodus

Nach dem Sieg über Das Haus kannst du weitermachen. Jedes weitere Level verlangt 0,5 Punkte mehr pro Münze als das davor, und jeder 5. Wurf wird wie bei Das Haus umgedreht. Der Lauf endet, wenn du ein Level verlierst, und dein bestes Ergebnis wird pro Charakter gespeichert.

## Münzsets

Vor einem Lauf wählst du einen Charakter und eines seiner drei Münzsets. Ein Set fasst {START_MAX} Münzen, höchstens {MAX_COPIES} derselben Münze (die einfache Normal-Münze darf das ganze Set füllen). Bearbeite Sets im Hauptmenü unter Münzsets. Ein leeres Set nutzt das Standard-Deck des Charakters.
""",
        },
    },
    {
        "slug": "progress",
        "title": {"en": "Characters, stages and saving", "de": "Charaktere, Stufen und Speichern"},
        "body": {
            "en": """# Characters, stages and saving

## Characters

The Blade is open from the start. Winning a run with the Blade unlocks the Seer, and winning with the Seer unlocks the Trader. Each character has its own starting deck, its own coins to start with, and a list of coins that only appear in its shop. See the pages of the characters.

A coin counts as collected the first time it is in your deck (the Collection screen shows it). A coin from a character's locked list becomes usable in sets once you have bought it in a shop; that is per character.

## Stages

Every character has its own difficulty stage, 1 to 8. You choose it on the play screen; the highest unlocked stage is selected by default. Winning a run on the highest stage you have unlocked unlocks the next one for that character. A stage includes the rules of the ones below it.

{STAGES}

## Saving

The game saves your run by itself at the start of every level and in the shop, and Continue on the main menu picks it up again, even after you quit. A level in progress starts again from its opening hand. Coins, sets, options and records are saved separately. Options has a Clear Progress button that deletes everything.
""",
            "de": """# Charaktere, Stufen und Speichern

## Charaktere

Die Klinge ist von Anfang an offen. Ein Sieg mit der Klinge schaltet die Seherin frei, ein Sieg mit der Seherin den Händler. Jeder Charakter hat ein eigenes Startdeck, eigene Münzen zum Start und eine Liste von Münzen, die nur in seinem Shop auftauchen. Siehe die Seiten der Charaktere.

Eine Münze gilt als gesammelt, sobald sie zum ersten Mal in deinem Deck war (die Sammlung zeigt sie). Eine Münze aus der Sperrliste eines Charakters ist in Sets nutzbar, sobald du sie in einem Shop gekauft hast; das gilt pro Charakter.

## Stufen

Jeder Charakter hat seine eigene Schwierigkeitsstufe, 1 bis 8. Du wählst sie auf dem Spielbildschirm; standardmäßig ist die höchste freigeschaltete gewählt. Ein Sieg auf der höchsten freigeschalteten Stufe schaltet für diesen Charakter die nächste frei. Eine Stufe enthält die Regeln der niedrigeren.

{STAGES}

## Speichern

Das Spiel speichert deinen Lauf von selbst zu Beginn jedes Levels und im Shop, und Weiter im Hauptmenü setzt ihn fort, auch nach dem Beenden. Ein laufendes Level beginnt wieder bei der Starthand. Münzen, Sets, Optionen und Rekorde werden getrennt gespeichert. In den Optionen löscht der Knopf Fortschritt löschen alles.
""",
        },
    },
    {
        "slug": "controls",
        "title": {"en": "Controls", "de": "Steuerung"},
        "body": {
            "en": """# Controls

The full list is in the game under Options > Controls. The short version:

## Keyboard and mouse

| Action | Key |
|---|---|
| Move the focus | Arrow keys |
| Press the focused button | Enter |
| Flip / next coin | Space |
| Discard the coin in play | D |
| Use chip 1, 2, 3 | 1, 2, 3 |
| Open the shop | O |
| Previous / next page or tab | Q / E |
| Menu | Esc |

Everything can also be clicked. Hovering a coin, chip or prize shows its details.

## Controller

| Action | Button |
|---|---|
| Move the focus | D-pad or left stick |
| Press | A |
| Menu | B or Start |
| Flip / next coin | X |
| Discard | Y |
| Pages, tabs, characters | LB, RB, LT, RT |

While a controller is connected, the buttons show the key that triggers them. The window can be resized and the game scales to fit.
""",
            "de": """# Steuerung

Die vollständige Liste steht im Spiel unter Optionen > Steuerung. Kurz:

## Tastatur und Maus

| Aktion | Taste |
|---|---|
| Fokus bewegen | Pfeiltasten |
| Fokussierten Knopf drücken | Enter |
| Werfen / nächste Münze | Leertaste |
| Münze im Spiel abwerfen | D |
| Chip 1, 2, 3 benutzen | 1, 2, 3 |
| Shop öffnen | O |
| Vorige / nächste Seite oder Tab | Q / E |
| Menü | Esc |

Alles lässt sich auch anklicken. Über einer Münze, einem Chip oder einer Prämie zeigt das Spiel die Details.

## Controller

| Aktion | Knopf |
|---|---|
| Fokus bewegen | D-Pad oder linker Stick |
| Drücken | A |
| Menü | B oder Start |
| Werfen / nächste Münze | X |
| Abwerfen | Y |
| Seiten, Tabs, Charaktere | LB, RB, LT, RT |

Solange ein Controller verbunden ist, zeigen die Knöpfe die Taste, die sie auslöst. Die Fenstergröße lässt sich ändern, das Spiel passt sich an.
""",
        },
    },
]
