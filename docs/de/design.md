# Design

## Der Pitch in einem Satz

Ein Roguelike, in dem das einzige Verb "wirf eine Münze" ist, und jede interessante Entscheidung fällt *vor* dem Wurf: welche
Münzen du besitzt, in welcher Reihenfolge sie kommen, was du wegwirfst und wann du aufhörst.

## Säulen

1. **Münzen sind die Karten.** Jede Münze ist ein kleines Programm: eine Kopf-Chance, ein Kopf-Effekt, ein Zahl-Effekt und
   manchmal ein Haken, der die Regeln verbiegt. Ein Deck beginnt mit 5 Plätzen und wächst durch gekaufte Plätze (je 5 Gold) auf höchstens 10. Sonst bringt nichts im Spiel Punkte.
2. **Keine Lebenspunkte.** Scheitern heißt, dass die Münzen ausgehen, nicht, dass man an Schaden stirbt. Schlechte Ergebnisse (Strafeffekte) erhöhen das
   Ziel, statt dir wehzutun, jeder Wurf ist also ein Glücksspiel um Tempo, nicht ums Überleben. So bleibt das Verlieren nachvollziehbar:
   "Mir waren nicht genug Münzen übrig, um es zu schaffen."
3. **Endlicher Stapel, kein Neumischen.** Ein Level dauert genau so lange wie die Münzen, die du hältst. Jede Münze wird einmal gespielt.
   Weil der Stapel nur schrumpft, kann der Spieler zählen: verbleibende Münzen, nötige Punkte, durchschnittliche Punkte pro Münze.
4. **Erst Information, dann Glück.** Du siehst die nächsten drei Münzen in der Bank, kannst mit einem Chip in den Stapel spähen
   und siehst die genauen Chancen jeder Münze. Der Zufall entscheidet über *Ergebnisse*, nie darüber, was du wissen darfst.
5. **Abwerfen ist kostenlos, Werfen kostet.** Die billige Entscheidung ist, eine schlechte Münze wegzuwerfen (kostet nichts, aber du verlierst einen
   Wurf). Die teure Entscheidung ist, eine starke Münze zu werfen, die Energie kostet. Diese Spannung ist das Herz des Spiels.
6. **Kleine, lesbare Zahlen.** Eine normale Münze ist etwa einen halben Punkt pro Wurf wert; eine starke 5 bis 8. Ziele
   liegen bei 3 bis 30. Man kann im Kopf rechnen.
7. **Alles über den Seed.** Ein Zufallsgenerator, ein Seed pro Lauf. Derselbe Seed und dieselben Aktionen laufen identisch ab; das
   macht Tests und den Balance-Simulator möglich.

## Die Kernschleife

```
 Charakter + Münzset wählen
          |
          v
  +---> LEVEL: Starthand (frei abwerfen) -> Bank mit 3 -> austeilen -> abwerfen oder werfen -> auflösen -> nächste Münze
  |        |  Ziel erreicht: Prämie sofort, weiterwerfen für Überschuss-Gold oder Shop öffnen
  |        |  Stapel leer, Ziel nicht erreicht: Gold zahlen und Münzen zurücktauschen, oder der Lauf endet
  |        v
  |      SHOP: Münzen / Chips / eine Prämie kaufen, Chancen tunen, eine Münze entfernen, neu würfeln
  |        |
  +--------+   (4 Level; das letzte ist der Boss, "Das Haus")
```

Ein Lauf dauert im Zieldesign 15 bis 25 Minuten. Ein Level ist kurz (5 bis 10 Würfe), weil der Stapel kurz ist.

## Was die Entscheidungen interessant macht

- **Reihenfolge und Starthand.** Du siehst 5 Münzen, wirfst für das Level alle weg, die du nicht willst, und der Rest wird die
  Bank. Eine gute, aber teure Münze wegzuwerfen, die du nicht bezahlen kannst, ist richtig; eine schwache Münze wegzuwerfen, um eine starke zu erreichen, ist
  ein echter Tausch, weil es einen Wurf entfernt.
- **Energie.** Du startest jedes Level mit 3 Energie. Starke Münzen kosten 1 oder 2 Energie zum Werfen. Energie kommt nur durch
  Münzeffekte zurück (Kupfer, Funke, Flux-Kondensator), ein Deck aus Schwergewichten braucht also eine Energiemaschine.
- **Das Ziel hängt von der Deckgröße ab.** Das Ziel pro Level ist ein Wert pro Münze mal der Anzahl der Münzen, also
  erhöht jeder Münzkauf das Ziel. Man kann nicht einfach "mehr Münzen" hinzufügen; man muss *bessere* Münzen hinzufügen. Qualität zählt
  mehr als Menge, sobald das Deck voll ist.
- **Überschuss ist optional.** Ist das Ziel erreicht, darfst du aufhören. Je zwei Überschusspunkte zahlen ein Gold, Gier lohnt sich also,
  aber jeder Extra-Wurf riskiert nichts (es gibt keine Lebenspunkte), außer den Münzen, die du für einen besseren Moment behalten würdest.
  In der Praxis geht es darum, wann man den Shop öffnet, und das ist billig, weil ein geschafftes Level nicht mehr verloren werden kann.
- **Die Serie.** Dasselbe Ergebnis in Folge multipliziert die Punkte (x1 bis x3). Eine riskante Münze abzuwerfen, um eine Serie zu schützen, ist die eine Stelle, an der "wirf sie weg" eine tiefe Entscheidung ist, und Domino, Zwilling und die Erzwingen-Chips werden zu Maschinen.
- **Tausch.** Ist der Stapel leer und das Ziel nicht erreicht, kann Gold drei deiner schon gespielten Münzen
  zu steigendem Preis zurückkaufen, höchstens dreimal pro Level. Gold ist damit auch ein zweites Leben, und ein Shop-Kauf konkurriert damit.
- **Der Boss dreht jeden fünften Wurf um.** Das bestraft Pläne, die auf ein festes Ergebnis setzen, und belohnt Münzen mit hoher Chance
  und Reserve.
- **Level-Modifikatoren.** Ab Level 2 hat jedes Level eine Besonderheit (Glückstag, Stromausfall, Goldrausch ...), die Entscheidungen verschiebt, ohne die Regeln zu ändern.

## Warum es so aussieht

- Das Thema ist eine Pixel-Spielhalle/ein Casino: petrolfarbene Filzbildschirme, ein goldener Rahmen, klobige Knöpfe mit Schatten, eine
  Pixelschrift und große mehrfarbige Pixel-Titel. Der **Shop-Bildschirm ist die Vorlage**; jede andere Vollbild-Ansicht
  übernimmt seinen Rahmen, seine Abstände und Farben.
- Die Münzbilder sind die Identität jeder Münze: eine flache Farbe und ein Zeichen (Schwert, Zielscheibe, Tropfen, Totenkopf). Der
  Spieler soll eine Münze an Silhouette und Farbe erkennen, bevor er sie liest.
- Kopf und Zahl stehen auf den Münzseiten, und die Wurfanimation zeigt die echte Seite in Bewegung: das Ergebnis steht
  fest, bevor die Animation beginnt, die Animation enthüllt es nur.

## Nach dem Boss

Der Sieg ist nicht das Ende: Der Endlosmodus fügt weiter Level mit wachsendem Ziel (+0,5 pro Münze je Level) und der Umkehrregel des Hauses hinzu, damit eine glückliche Maschine (Verdoppler, Wahres
Echo, Verstärker, Megafon, eine lange Serie) irgendwo zählt, und der Lauf endet erst, wenn ein Level verloren wird.

## Dinge, die ausprobiert und entfernt wurden

| Entfernt | Warum |
|---|---|
| Lebenspunkte und Schaden | Verlieren fühlte sich wie eine Strafe von außerhalb des Münzspiels an; ersetzt durch Zielstrafen. |
| Ziehbudget pro Level | Neumischen machte Level lang und die Münzzahl bedeutungslos; ein endlicher Stapel ist einfacher und zählbar. |
| Münzwahl nach einem Sieg | Fügte einen Bildschirm hinzu, der den Shop doppelte. Der Shop nach jedem Level ist der einzige Weg zu Münzen. |
| Neumischen des Ablagestapels | Machte das Spiel zu "spiele ewig die beste Münze". Nur noch als Test-/Simulationsschalter vorhanden. |
| Energie-Shop-Gegenstände (neu würfeln/erzwingen) | Toter Code, nur für Tests; nichts in der Oberfläche ruft sie auf. |
| Tokens als Währung zum Freischalten von Münzen | Freischalten geschieht jetzt durch den Kauf der Münze im Shop. Tokens werden noch verdient, aber nicht benutzt. |
| Auflösen-Knopf | Das Wurfergebnis wird nach einer kurzen Pause angewendet; der Spieler klickt nur auf Nächste Münze. |
| Rechtes Feld "Aktuelle Münze" | Es verdoppelte die Bühne (Name, Chancen, Ergebnis). Die Bühne zeigt jetzt die Münze, ihre zwei Effekte und das Ergebnis. |

## Offene Designfragen

- Wofür sind Tokens da (oder abschaffen)?
- Soll Überschuss-Gold eine sichtbare Grenze oder einen Verfall haben, damit "aufhören oder weitermachen" eine echte Wahl ist?
- Soll ein Tausch auch ohne Gold möglich sein (ein günstigerer Ersatz), damit ein Lauf nie ohne Entscheidung endet?
- Der Händler ist der stärkste Charakter mit einem gebauten Set, die Seherin der schwächste. Die Startdecks wurden angeglichen, die
  Sets mit Ausbau aber nicht (siehe [economy-and-balance.md](economy-and-balance.md), englisch).
