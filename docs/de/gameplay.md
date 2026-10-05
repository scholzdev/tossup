# Spielablauf

Dies ist das Regelwerk, wie es im Spiel umgesetzt ist (`src/game.lua`). Die Konstanten stehen in der `Game`-Tabelle.

## 1. Ein Lauf

- **Vier Level**, dann ist der Lauf gewonnen. Das letzte Level ist der Boss, **Das Haus**.
- Du startest mit **25 Gold**, **3 Energie**, ohne Chips und ohne Prämien, und mit einem Deck aus bis zu 5 Münzen aus dem Münzset, das du auf dem
  Spielbildschirm gewählt hast. Das Deck hat am Anfang **5 Plätze**; der Shop verkauft bis zu 5 weitere (siehe 9).
- Ein Lauf endet, wenn du das Ziel des Bosses erreichst (Sieg) oder in einem Level keine Münzen mehr hast, ohne das Ziel erreicht und ohne einen
  Tausch gemacht zu haben (Niederlage).
- Zwischen den Leveln besuchst du den Shop.

<!-- GEN:levels -->
| # | Level | Ziel pro Münze | Ziel bei 5 / 10 Münzen | Prämie |
|---|---|---|---|---|
| 1 | Auftakt | 0,7 | 4 / 7 | 25 Gold |
| 2 | Zweite Chance | 1,4 | 7 / 14 | 30 Gold |
| 3 | Hoher Einsatz | 2,5 | 13 / 25 | 35 Gold |
| 4 | Das Haus | 4,5 | 23 / 45 | keine (beendet den Lauf) |
<!-- /GEN:levels -->

Das Ziel ist `gerundet(Wert pro Münze x Anzahl der Münzen in deinem Deck)` beim Start des Levels. Wer Münzen kauft, erhöht also das nächste Ziel.

## 2. Aufbau eines Levels

1. **Start.** Die Energie wird auf 3 aufgefüllt. Das Deck wird in den Nachziehstapel gemischt. Münzen, die pro Level wachsen, werden aktualisiert.
2. **Starthand.** Fünf Münzen werden vom Stapel gezogen und schweben in der Mitte eines abgedunkelten Bildschirms. Klicke Münzen zum Markieren und drücke
   Abwerfen, um die markierten für dieses Level wegzuwerfen (kostenlos). Mindestens eine Münze muss bleiben. Die erste nicht markierte Münze wird zuerst
   gespielt. Drücke Level starten, wenn du zufrieden bist.
3. **Die Bank.** Die behaltenen Münzen bilden die Bank, eine geordnete Schlange. Das linke Feld zeigt die nächsten **drei**. Hat die Bank weniger als drei
   Münzen, wird sie vom Stapel aufgefüllt. Der Stapel wird nie neu gemischt, der ganze Vorrat schrumpft also nur
   (abgesehen von Tausch und "Nochmal"-Rückkehrern).
4. **Austeilen.** Die erste Münze der Bank ist die ausgeteilte Münze. Du kannst sie werfen oder abwerfen.
5. **Abwerfen** ist kostenlos. Nach der Starthand entfernt es nur **die Münze im Spiel** (die, die auf den Wurf wartet) für den Rest des Levels; die anderen
   Bankmünzen kann man nicht auswählen. Nicht erlaubt während eines Wurfs oder wenn keine nutzbare Münze übrig bliebe. Eine abgeworfene Münze kommt nie
   zurück, auch nicht durch einen Tausch.
6. **Werfen.** Hat die Münze Energiekosten, musst du die Energie haben. Kannst du sie nicht bezahlen, kann die Münze nicht geworfen werden (wirf sie ab).
   Die letzte nutzbare Münze wird immer geworfen und bezahlt, was an Energie da ist.
7. **Auflösen.** Die Effekte der Seite werden nacheinander angewendet, dann wird die nächste Münze ausgeteilt.

## 3. Der Ablauf eines Wurfs

Reihenfolge, wenn du Werfen drückst:

1. Die Energiekosten werden bezahlt. Die Münze verlässt sofort die Bank und die Bank wird aufgefüllt.
2. Ein Rohwurf wird mit dem Seed-Zufallsgenerator gegen die aktuelle Kopf-Chance der Münze gemacht.
3. Ereignis `coin_flip`: der eigene `on_flip`-Haken der Münze und aktivierte Chips (Kopf erzwingen / Zahl erzwingen, Beschwert) können die Seite ändern.
4. `finalize`: das Ereignis `coin_outcome` lässt Prämien die Seite ändern (Glückspfennig, Kaputte Uhr), danach gilt **die Boss-Regel**
   (jeder 5. Wurf im Haus dreht die Seite um). Das Ergebnis steht jetzt fest.
5. Die Animation dreht die Münze und landet auf der festen Seite. Ein Banner zeigt KOPF oder ZAHL und, falls etwas den Wurf verändert hat, eine Zeile
   "GEWORFEN ZAHL > KOPF (PRÄMIE)".
6. Nach einer kurzen Pause wird **aufgelöst**: die Effekte der Seite werden kopiert, Ereignis `coin_resolve` (Haken und Verdoppeln können sie ändern),
   jeder Effekt wird angewendet, Ereignis `coin_resolved`, Wachstum pro Wurf, Zielprüfung, nächste Münze austeilen.

## 4. Wahrscheinlichkeit

`Kopf-Chance = Basis + Münzbonus + Levelbonus + Hakenänderungen`, begrenzt auf 0 bis 100 %.

- **Basis:** die `probability` der Münze.
- **Münzbonus:** +10 % für jeden Chancen-Tuner, den du für diese Münze gekauft hast, dauerhaft für den Lauf.
- **Levelbonus:** der Effekt von Fokus und die Prämie Magnet erhöhen die Kopf-Chance für den Rest des Levels.
- **Haken:** Schwung, Schwarm und Sanduhr ändern die Chance je nach aktuellem Zustand (`on_odds` ist rein und wird für die Anzeige neu berechnet).

## 4b. Die Serie (Combo)

Jeder Wurf, der auf derselben Seite landet wie der davor, erhöht die **Serie**; eine andere Seite setzt sie auf 1 zurück (Kopf und Zahl zählen beide).
Punkte und Gold des Wurfs werden mit `1 + 0,25 x (Serie - 1)` multipliziert, begrenzt auf x3 (neun in Folge), dann gerundet:
x1, x1,25, x1,5, x1,75, x2 ... Der Multiplikator wird nach Haken und Boni angewendet, er stapelt sich also mit dem Megafon (eine Serie x2 auf einem
verdoppelten Wurf ist x4). Strafen werden nie multipliziert. Die Serie steht oben rechts im Spielfeld (`SERIE KOPF x4`, `x1,75`) und das Ergebnisbanner
zeigt `+7 PUNKTE (x1,75)`. Sie wird zu Beginn jedes Levels zurückgesetzt.

Sie macht **Abwerfen zu einer taktischen Entscheidung**: Läuft eine Serie x2,5, schützt das Wegwerfen einer Münze mit niedriger Chance die Serie. Sie macht auch
die Werkzeuge zum Erzwingen stark (Domino, Zwilling, Kopf erzwingen, Beschwert). Passende Münzen: Heiße Hand (Extra-Schritt), Anker (ein freier Bruch), Wetter (Punkte pro
Schritt), Auszahlung (gibt die Serie aus), Kältewelle (Zahl zahlt pro Zahl in Folge). Die Prämie Taktstock macht den Schritt 0,4 und die Grenze x4.

## 4d. Level-Modifikatoren

Ab Level 2 hat jedes Level (auch der Boss und jedes Endlos-Level) einen **Modifikator**, gewählt mit dem Seed-Zufallsgenerator und unten links im Spielfeld angezeigt.

<!-- GEN:modifiers -->
| Modifikator | Effekt |
|---|---|
| Glückstag | Alle Münzen +10 % Kopf. |
| Kälteeinbruch | Alle Münzen -10 % Kopf, dafür 40 % mehr Prämie. |
| Energieschub | Du startest mit 5 Energie. |
| Stromausfall | Du startest mit 1 Energie, dafür 40 % mehr Prämie. |
| Goldrausch | Gold-Effekte zahlen doppelt. |
| Hoher Einsatz | Ziel +25 %, Prämie +50 %. |
| Guter Rhythmus | Die Serie wächst um +0,4 je Schritt. |
| Bonus-Tausch | Ein zusätzlicher Tausch in diesem Level. |
<!-- /GEN:modifiers -->

## 4c. Endlosmodus

Fällt der Boss, ist der Lauf gewonnen (Siegbildschirm mit den üblichen Belohnungen). Der Bildschirm bietet den **Endlosmodus** an: Du gehst in den Shop und spielst
weiter die Level 5, 6, 7 ... Der Lauf endet jetzt nur noch durch das Verlieren eines Levels. Jedes Endlos-Level verlangt **0,5 Punkte mehr pro Münze** als das davor (3,5, 4,0, 4,5 ... pro Münze), zahlt
mehr (45, 50, ... Gold) und **dreht jeden 5. Wurf um** wie das Haus. Die Kopfzeile zeigt `LEVEL 5` und `ENDLOS 1`; der Lauf-vorbei-Bildschirm zählt die geschafften Endlos-Level,
und `runs.log` bekommt eine zweite Zeile (`result=ENDLESS`), wenn er endet. Hier sollen Serien- und Bonus-Maschinen richtig losgehen.

## 5. Effekte

| Effekt | Was er tut |
|---|---|
| `score` | Bringt Punkte Richtung Ziel. Überschuss über das Ziel zahlt Gold. |
| `gold` | Bringt Gold. |
| `energy` | Bringt Energie (keine Obergrenze für Gewinne; sie wird jedes Level auf 3 aufgefüllt). |
| `penalty` | Erhöht das Ziel (verbleibend und gesamt) um den Betrag. Verringert nie die Punkte. |
| `extra_draw` | Legt eine gespielte Münze zurück auf den Stapel. Höchstens 3 pro Level. |
| `probability` | +X Kopf-Chance für diese Münze, bis das Level endet. |
| `all_odds` | Alle Münzen bekommen +X Kopf-Chance für den Rest des Levels (Horoskop). |
| `peek` | Zeigt die nächsten zwei Münzen des Stapels (Chip Spähen). |
| `bank_discard` | Lässt dich eine der nächsten drei Bankmünzen kostenlos abwerfen (Zahl der Kristallkugel). |
| `extra_exchange` | Ein Tausch mehr in diesem Level (Rettungsring). |
| `amplify` | Jeder aktive Bonus hält eine Münze länger; Multiplikatoren wie beim Megafon steigen um 1, Chancen-Boni verdoppeln sich (max. +60 %). |
| `combo_bonus` | Die Serie wächst um so viele Extra-Schritte. |
| `combo_shield` | Das nächste Ergebnis, das die Serie brechen würde, wird ignoriert (einmal pro Schutz). |
| `next_mult`, `next_odds`, `next_swap`, `next_heads` | **Boni** für die nächsten N Münzen (Faktor auf Punkte und Gold, +X Kopf, die Effekte der anderen Seite benutzen, garantierter Kopf). Sie beginnen mit der Münze nach der, die sie erzeugt hat, und enden mit dem Level. |

## 6. Das Ziel erreichen

Erreicht das verbleibende Ziel 0, ist das Level **geschafft** (einmalig):

- Die Level-Prämie wird sofort ausgezahlt (25 / 30 / 35).
- Das Level bleibt offen. Je **2 Punkte über dem Ziel zahlen 1 zusätzliches Gold**.
- Wirf weiter für mehr, oder drücke **Shop öffnen** oben rechts, wenn gerade kein Wurf läuft.
- Geht der Stapel nach dem Schaffen aus, öffnet sich der Shop von selbst (außer ein Tausch ist möglich, dann entscheidest du).
- **Der Boss:** der Lauf endet im Sieg, sobald sein Ziel erreicht ist.

## 7. Wenn die Münzen ausgehen

Ist der Stapel leer und das Ziel nicht erreicht:

- **Mit genug Gold:** eine Meldung bietet drei Knöpfe an: **Mehr Münzen** (Tausch), **Neu starten** (neuer Lauf) und **Zum Menü**.
- **Ohne genug Gold oder ohne gespielte Münze zum Zurückholen:** eine **SPIEL-VORBEI**-Meldung erscheint über dem Rundenbildschirm ("Keine Münzen mehr und kein Tausch möglich. Der Lauf
  ist vorbei."); OK (oder Enter / Esc) führt zum Lauf-vorbei-Bildschirm, der den Grund nennt.

**Tausch:** Zahle Gold, um bis zu **3** der Münzen, die du in diesem Level schon gespielt hast, zurück in den Stapel zu holen (zufällig, abgeworfene Münzen ausgenommen).
Der Preis beginnt bei **10** und steigt um **5** für jeden Tausch, der in diesem Level schon gemacht wurde. Wiederhole es, solange du es dir leisten kannst, **höchstens 3 Tausche pro Level**
(das Fenster zeigt, wie viele übrig sind). Nach dem dritten verliert ein leerer Stapel bei nicht erreichtem Ziel den Lauf.

## 8. Energie

- 3 Energie zu Beginn jedes Levels. Das Maximum lässt sich nicht erhöhen.
- Verbraucht wird sie nur beim Werfen von Münzen mit Energiekosten: Hammer 2; Blut, Schneeball, Spieler, Flux-Kondensator, Märtyrer, Kopfgeld, Narr, Jackpot 1.
- Gewonnen wird sie durch Münzeffekte: Kupfer (Zahl +1), Funke (Kopf +2), Flux-Kondensator (Zahl +1).
- Eine Münze, die du nicht bezahlen kannst, kann nicht geworfen werden. Wirf sie ab, oder spare Energie.

## 9. Der Shop

Öffnet sich nach dem Ende eines Levels. Alles läuft über den Seed.

| Bereich | Einzelheiten |
|---|---|
| **Münzen** | 4 verschiedene Angebote aus *allen* Münzen deines Charakters (Startpool und gesperrte). Normal kostet 5, alles andere 15 bis 24 (siehe Münzseiten). Ein Deck fasst nur so viele Münzen, wie es Plätze hat; ist es voll, musst du einen Platz kaufen oder eine Münze entfernen. Das Kaufen einer gesperrten Münze schaltet sie dauerhaft für deine Münzsets frei. |
| **Deckplätze** | Das Deck startet mit 5 Plätzen und wächst auf bis zu 10. Jeder zusätzliche Platz kostet **5 Gold** und wird durch Klick auf den ersten dunklen Platz im Deck-Streifen gekauft (er zeigt `+5 GOLD`); die anderen bleiben gesperrt, bis der davor gekauft ist. Ein größeres Deck bedeutet ein größeres Ziel, zusätzliche Plätze sind also eine echte Entscheidung. |
| **Neu würfeln** | Erneuert nur die Münzangebote. 4 Gold, +2 für jedes Neu-Würfeln in diesem Besuch. |
| **Chips** | 2 Angebote aus den 11 Chips. Du kannst höchstens 3 halten. |
| **Prämie** | 1 Relikt, das du noch nicht besitzt, 25 Gold. |
| **Chancen-Tuner** | 10 Gold: +10 % Kopf auf der gewählten Deckmünze. Dauerhaft, begrenzt auf 100 %. |
| **Münz-Entfernung** | 8 Gold: entfernt die gewählte Deckmünze. Du fällst nie unter 1 Münze. |
| **Nächste Runde** | Verlässt den Shop und startet das nächste Level. |

Wähle eine Deckmünze durch Klick im Deck-Streifen am unteren Rand.

## 10. Münzsets (vor einem Lauf)

- Ein **Set** hat bis zu 5 Münzen (die Startplätze). Jeder Charakter hat 3 Sets. Set 1 beginnt als das Standard-Deck mit 5 Münzen, Set 2 und 3 sind leer
  (ein leeres Set fällt auf das Standard-Deck zurück).
- Höchstens **3 Exemplare** derselben Münze, außer Normal, die alle 5 Plätze füllen darf.
- Es können nur Münzen aus dem Pool des Charakters oder von dir freigeschaltete hinzugefügt werden. Freigeschaltet wird, indem man die Münze in einem
  Shop während eines Laufs kauft.
- Bearbeite Sets unter Münzsets und drücke **Set speichern**. Beim Wechsel von Set oder Charakter gehen ungespeicherte Änderungen verloren.

## 11. Steuerung

Alle Tasten und Knöpfe stehen im Spiel unter Optionen > Steuerung.

| Aktion | Tastatur | Controller |
|---|---|---|
| Fokus bewegen | Pfeiltasten | D-Pad oder Stick |
| Fokussierten Knopf drücken | Enter | A |
| Zurück, schließen, Menü | Esc | B oder Start |
| Werfen / nächste Münze | Leertaste | X |
| Münze im Spiel abwerfen | D | Y |
| Chip 1 / 2 / 3 benutzen | 1 / 2 / 3 | D-Pad, dann A |
| Shop öffnen (Ziel erreicht) | O | D-Pad, dann A |
| Untersuchen (Details des fokussierten Elements) | I, Q oder E | LB, RB, LT oder RT |
| Vorige / nächste Seite, Tab, Charakter | Q / E | LB, RB, LT, RT |
| Debug-Infos | F3 | |

Die Maus funktioniert überall: Klicke auf Knöpfe, markiere Münzen in der Starthand und fahre über Münzen für Details.

## 11a. Speichern und Fortsetzen

Der Lauf wird **automatisch gespeichert** (`run.lua` im Speicherordner) zu Beginn jedes Levels (Starthand) und im Shop nach jedem Kauf. Ein laufendes Level wird nicht gespeichert: Wer mitten
im Level beendet und später weitermacht, beginnt dieses Level neu bei der Starthand, mit denselben Münzen, demselben Gold und demselben Zufall, es läuft also genau so ab, wie es abgelaufen wäre. **Weiter** im
Hauptmenü lädt den gespeicherten Lauf auch nach dem Schließen des Spiels (der Lauf-vorbei- und der Siegbildschirm löschen den Speicherstand). Ein **neuer Lauf** bei vorhandenem Speicherstand fragt vorher nach. Beenden
fragt nur, wenn ein Level läuft. Das Tutorial wird nie gespeichert. Der Titelbildschirm zeigt die Version (`v0.2.0 (abc1234)`: Nummer und Git-Commit; `dev`, wenn aus dem Quellordner gestartet), und
Zeilen in `runs.log` und `crash.log` tragen sie ebenfalls.

## 11c. Stufen (Schwierigkeit)

Jeder Charakter hat seine eigene Stufe, 1 bis 8 (`content/stakes.lua`), gewählt auf dem Spielbildschirm (Standard: die höchste freigeschaltete). Ein Sieg auf der höchsten freigeschalteten Stufe schaltet für diesen Charakter die nächste frei; ein Profil aus der Zeit vor den Stufen startet bereits siegreiche Charaktere auf Stufe 2. Eine Stufe behält die Regeln der niedrigeren.

| Stufe | Regel |
|---|---|
| 1 | keine Änderungen |
| 2 | Ziele +15 % |
| 3 | Start mit 15 Gold |
| 4 | Das Haus (und Endlos-Level) dreht jeden 4. Wurf um |
| 5 | Shop-Preise +20 % (Münzen, Chips, Prämien) |
| 6 | nur 2 Tausche pro Level |
| 7 | auch Level 1 hat einen Modifikator |
| 8 | Ziele insgesamt +80 % |

Der Simulator zeigt die Boss-Quote (kluger Bot, gebautes Set) bei der Klinge von etwa 49 % (Stufe 1) auf 25 % (Stufe 8) fallend; die Stufen 3 bis 6 spüren Menschen mehr als die Bots, die Gold und Tausche kaum nutzen. `lua tools/sim.lua --stake N` spielt eine Stufe.

## 11b. Charaktere freischalten

Die Klinge ist von Anfang an offen. **Ein gewonnener Lauf** (das Haus besiegt) schaltet den nächsten Charakter frei: Klinge, dann Seherin, dann Händler. Gesperrte Charaktere sind auf dem Spielbildschirm ausgegraut
("GESPERRT - gewinne einen Lauf mit: Die Klinge") und können weder gestartet noch in Münzsets bearbeitet werden. Der Siegbildschirm meldet die Freischaltung. Die beste Zahl **Endlos-Level** pro Charakter
wird gespeichert und auf dem Spielbildschirm und dem Lauf-vorbei-Bildschirm angezeigt ("NEUER REKORD!"). Fortschritt löschen setzt all das zurück.

## 12. Ein Beispiel

Klinge, Standard-Deck: 4 Normal + Schwert, Level 1 (Ziel 3 bei 5 Münzen).

- Die Starthand zeigt Normal, Schwert, Normal, Normal, Normal. Du behältst alle (ein Abwerfen würde deine Würfe unter das Ziel senken).
- Wurf 1, Normal: Kopf, +1 Punkt (1/3). Wurf 2, Schwert: Zahl, nichts. Wurf 3, Normal: Kopf (2/3). Wurf 4, Normal: Zahl.
- Letzte Münze, Normal: Zahl. Der Stapel ist bei 2/3 leer. Du hast 25 Gold, also bietet eine Meldung einen Tausch für 10 Gold an; wenn du zahlst,
  kommen drei gespielte Münzen zurück. Jede hat eine gute Chance auf Kopf für den fehlenden Punkt (eine 50-%-Münze dreimal: 87,5 %),
  und das Level ist für +25 Gold geschafft.
- Der Shop öffnet sich: Schwert kostet 12, Normal 5. Ein Deck mit 10 Münzen würde das nächste Ziel auf 9 anheben und braucht 5 weitere Plätze, also kaufst du stattdessen einen Platz,
  eine Münze und einen Chancen-Tuner.
