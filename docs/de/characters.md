# Charaktere

Ein Charakter bestimmt drei Dinge: das **Standard-Deck** (die 5 Münzen, mit denen du startest), den **Pool** (Münzen, die du von Anfang an in ein
Münzset legen darfst) und die **Sperrliste** (Münzen, die es für diesen Charakter im Shop gibt, die aber erst in Sets nutzbar sind, wenn du sie einmal gekauft hast).
Der Shop verkauft Pool- und gesperrte Münzen gleichermaßen. Definiert in `content/characters.lua`; die Porträts liegen in `assets/characters/`.

Alle Charaktere teilen dieselben Regeln, Ziele und denselben Shop. Der Unterschied ist, welche Münzen ihnen angeboten werden und in welche
Richtung das Startdeck geht.

## Die Klinge - "Verlässliche Punkte"

Porträt: ein Fuchs-Duellant mit einer Münze und einem Degen.

<!-- GEN:char-blade -->
| | |
|---|---|
| **Standard-Deck** | Normal, Schwert, Dolch |
| **Pool** | Normal, Schwert, Dolch |
| **Gesperrt** | Hammer, Blut, Vampir, Kette, Verflucht, Lunte, Fokus, Märtyrer, Schneeball, Funke, Jackpot, Rettungsring, Megafon, Topf, Heiße Hand, Auszahlung, Verdoppler, Verstärker |
<!-- /GEN:char-blade -->

**Spielstil.** Geradlinige Punkte. Der Pool der Klinge besteht aus Münzen, deren Wert nicht von Haken abhängt: Schwert (+5 bei
Kopf), Dolch (EW 3,25 bei 75 %), dann Hammer und Blut für Schübe. Energie ist die Grenze, denn Hammer und Blut kosten
Energie und die Klinge hat keine Energiemünze im Pool; Funke muss über den Shop kommen. In der Praxis wechselt die Klinge pro Level eine schwere Münze mit kostenlosen Münzen ab.

**Stärken.** Der berechenbarste Charakter. Leicht, ein Deck zu bauen, das die Level 1 und 2 mit Abstand schafft. Gut mit
Lunte (Exemplare abwerfen für Ladung) und Kette.

**Schwächen.** Wenig Wirtschaft: In der Liste der Klinge gibt es keine Goldmünze außer dem Vampir. Tausche sind schwerer zu bezahlen. Das
Boss-Ziel (3,0 pro Münze) braucht Hammer-Schübe.

**Ein erstes Set.** 2 Schwert, 2 Dolch, 1 Normal (ein Set fasst 5 Münzen); Hammer und Blut kommen im Shop dazu, sobald du Deckplätze gekauft hast.

## Die Seherin - "Risiko und wechselnde Chancen"

Porträt: eine Eulenmagierin mit einer leuchtenden Münze.

<!-- GEN:char-seer -->
| | |
|---|---|
| **Standard-Deck** | 2 x Normal, Dolch, Fokus, Funke |
| **Pool** | Normal, Dolch, Verflucht, Spieler, Funke, Fokus, Glück |
| **Gesperrt** | Horoskop, Kristallkugel, Nachahmer, Querkopf, Glückssieben, Blut, Sanduhr, Narr, Echo, Phönix, Spiegel, Domino, Zwilling, Kältewelle, Anker, Wahres Echo, Verstärker |
<!-- /GEN:char-seer -->

**Spielstil.** Zocken und manipulieren. Verflucht (30 %, +15) und Spieler (50 % auf x3) sind Ausschläge; Fokus und Glückssieben biegen die
Chancen, Querkopf lässt dich die Seite planen, Echo und Narr sind Chaos. Funke gibt Energie, damit Spieler, Blut und Narr
geworfen werden können.

**Stärken.** Die höchste Decke, denn die Kombination aus Verflucht, Fokus und dem Chip Kopf erzwingen kann Würfe mit 15 Punkten
erzeugen, und Echo kann sie verdoppeln.

**Schwächen.** Weiterhin der schwächste Charakter im Simulator (Boss-Quote etwa 37 % mit einem gebauten Set, gegenüber etwa 50 % Klinge und 57 %
Händler), wurde aber gestärkt: Sie startet jetzt mit einem Dolch und hat Horoskop, Kristallkugel und Nachahmer. Verflucht und Spieler sind nicht im Standard-Deck und müssen
aus dem Pool hinzugefügt werden.

**Ein erstes Set.** 2 Funke, 1 Fokus, 1 Spieler, 1 Verflucht.

## Der Händler - "Gold und Energie"

Porträt: ein Kaufmann mit Kassenbuch und Münze.

<!-- GEN:char-trader -->
| | |
|---|---|
| **Standard-Deck** | Normal, Gezinkt, Dolch |
| **Pool** | Normal, Kupfer, Gezinkt, Dolch, Schwert |
| **Gesperrt** | Funke, Bank, Geizhals, Hammer, Kopfgeld, Schwarm, Schwung, Flux-Kondensator, Cheerleaderin, Megafon, Orchester, Rettungsring, Jackpot, Wetter, Anker, Verdoppler, Wahres Echo |
<!-- /GEN:char-trader -->

**Spielstil.** Gold und Energie in Punkte verwandeln. Gezinkt (75 %, +4 Gold), Kupfer (Gold oder Energie) und Bank finanzieren
Tausche und Shop-Käufe; Geizhals und Flux-Kondensator machen Gold und Energie zu Punkten; Kopfgeld verwandelt Punkte zurück in Gold.
Der Hammer steht für Schübe zur Verfügung und der Händler hat die Energie, ihn zu bezahlen.

**Stärken.** Der stärkste Charakter im Simulator (Boss-Quote etwa 57 % mit einem gebauten Set): Gold ist durch den Tausch auch ein Extraleben,
und der Pool enthält schon Dolch und Schwert.

**Schwächen.** Der frühe Spielverlauf ist langsam (keine großen Treffer im Startdeck), und die Wirtschaftsmünzen sind nutzlos, wenn du arm bist.

**Ein erstes Set.** 2 Dolch, 1 Schwert, 1 Gezinkt, 1 Kupfer.

## Startdecks

Die Startdecks wurden so gewählt, dass ein einfacher, ungebauter Start ungefähr vergleichbar ist:

| Deck | EW pro 5 Würfe | Ziel Level 1 (3 Pkt.) |
|---|---|---|
| Klinge: 4 Normal, Schwert | 4,5 Punkte | Wahrscheinlich |
| Seherin: 2 Normal, Dolch, Fokus, Funke | 1,0 + 3,25 + 2 + 1,5 = 7,75 Punkte, plus 1 Energie | Wahrscheinlich |
| Händler: 3 Normal, Gezinkt, Dolch | 1,5 + 3,25 = 4,75 Punkte, plus 3 Gold | Wahrscheinlich |

Der Standardstart füllt alle 5 Startplätze; der Shop verkauft bis zu 5 weitere Plätze (je 5 Gold).

## Charaktere freischalten

Die Klinge ist von Anfang an verfügbar. **Ein gewonnener Lauf schaltet den nächsten Charakter frei:** ein Sieg mit der Klinge öffnet die Seherin, ein Sieg mit der Seherin den Händler. Gesperrte Charaktere
sind ausgegraut und können weder gestartet noch bearbeitet werden.

## Sammlung und Freischaltungen

- Eine Münze ist **gesammelt**, sobald sie zum ersten Mal in deinem Deck war. Die Sammlung zeigt gesammelte Münzen und eine schwarze Silhouette für
  die übrigen.
- Eine gesperrte Münze ist **freigeschaltet**, sobald du sie zum ersten Mal in einem Shop kaufst. Ab dann kann sie in jedes Set dieses Charakters.
- Eine Münze, die in den Listen mehrerer Charaktere steht (Dolch, Blut, Hammer, Funke), muss pro Charakter freigeschaltet werden.
