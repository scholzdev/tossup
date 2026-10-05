# Münzen

Jede Münze ist eine Datei in `content/coins/<id>.lua`. Eine Münze hat:

- eine **Kopf-Chance** (`probability`),
- eine Effektliste für **Kopf** und eine für **Zahl**,
- optional **Energiekosten** zum Werfen,
- eine **Seltenheit** (N gewöhnlich, R ungewöhnlich, SR selten, UR episch, für Sortierung und Farbe in der Sammlung),
- optional **Haken**, die die Regeln ändern (siehe [architecture.md](architecture.md#coin-hooks), englisch).

Preise stehen in der Tabelle unten und in jeder Münzüberschrift (aus dem Code erzeugt). "EW" unten ist der Erwartungswert eines Wurfs
ohne Haken, in Punkten, wenn nicht anders angegeben, bei unveränderter Kopf-Chance. Ein "Ziel +N" ist eine Strafe, die das Ziel um N verschiebt, was ungefähr dem
Verlust von N Punkten entspricht.

Abschnitte: [Boni](#boni-wirken-auf-die-nächsten-münzen) | [Serien-Münzen](#serien-münzen) | [Maschinen-Münzen](#maschinen-münzen-sie-machen-andere-münzen-größer) | [Neuere Münzen](#neuere-münzen) | [Die Grundmünze](#die-grundmünze) | [Einfache Punktebringer](#einfache-punktebringer) | [Wirtschaft und Energie](#wirtschaft-und-energie) |
[Risiko-Münzen](#risiko-münzen) | [Chancen-Münzen](#chancen-münzen) | [Wachsende Münzen](#wachsende-münzen) |
[Kombination und Synergie](#kombination-und-synergie) | [Stärke-Rangliste](#stärke-rangliste)

---

## Alle Münzen auf einen Blick

<!-- GEN:coins-table -->
| Münze | Seltenheit | Kopf | Preis | Energie | Bei Kopf | Bei Zahl |
|---|---|---|---|---|---|---|
| Normal | Gewöhnlich | 65% | 5 | 0 | Erzielt 1 Punkt | nichts |
| Kupfer | Gewöhnlich | 30% | 10 | 0 | Erhalte 2 Gold | Erhalte 1 Energie |
| Schwert | Gewöhnlich | 35% | 12 | 0 | Erzielt 3 Punkte | nichts |
| Glück | Gewöhnlich | 30% | 10 | 0 | Erzielt 2 Punkte, Kommt zurück auf den Stapel | nichts |
| Verflucht | Selten | 25% | 15 | 0 | Erzielt 15 Punkte | Ziel +2 |
| Gezinkt | Gewöhnlich | 70% | 12 | 0 | Erhalte 4 Gold | nichts |
| Dolch | Gewöhnlich | 80% | 12 | 0 | Erzielt 2 Punkte | Erzielt 1 Punkt |
| Hammer | Ungewöhnlich | 25% | 22 | 2 | Erzielt 16 Punkte | Erzielt 1 Punkt |
| Blut | Ungewöhnlich | 37% | 22 | 1 | Erzielt 10 Punkte | Ziel +6 |
| Funke | Ungewöhnlich | 70% | 15 | 0 | Erhalte 2 Energie | Erzielt 3 Punkte |
| Fokus | Ungewöhnlich | 65% | 10 | 0 | Nächste Münze: +35 % Kopf | Erzielt 4 Punkte |
| Schneeball | Selten | 30% | 24 | 1 | Erzielt 3 Punkte | Erzielt 1 Punkt |
| Spieler | Selten | 35% | 22 | 1 | Erzielt 7 Punkte | (siehe Beschreibung) |
| Schwung | Selten | 35% | 15 | 0 | Erzielt 6 Punkte | (siehe Beschreibung) |
| Echo | Episch | 60% | 15 | 0 | (siehe Beschreibung) | (siehe Beschreibung) |
| Vampir | Ungewöhnlich | 35% | 15 | 0 | Erzielt 2 Punkte | (siehe Beschreibung) |
| Geizhals | Ungewöhnlich | 40% | 15 | 0 | (siehe Beschreibung) | Erhalte 2 Gold |
| Lunte | Selten | 30% | 10 | 0 | Erzielt 1 Punkt | (siehe Beschreibung) |
| Phönix | Episch | 25% | 15 | 0 | Erzielt 3 Punkte | Ziel +2 |
| Querkopf | Selten | 65% | 15 | 0 | Erzielt 4 Punkte | Erzielt 1 Punkt |
| Kette | Ungewöhnlich | 60% | 15 | 0 | (siehe Beschreibung) | (siehe Beschreibung) |
| Bank | Ungewöhnlich | 40% | 15 | 0 | Erhalte 3 Gold | (siehe Beschreibung) |
| Glückssieben | Selten | 30% | 15 | 0 | Erzielt 3 Punkte | (siehe Beschreibung) |
| Sanduhr | Ungewöhnlich | 30% | 10 | 0 | Erzielt 4 Punkte | Kommt zurück auf den Stapel |
| Flux-Kondensator | Ungewöhnlich | 35% | 15 | 1 | (siehe Beschreibung) | Erhalte 1 Energie |
| Märtyrer | Selten | 40% | 15 | 1 | Erzielt 4 Punkte | Ziel +3 |
| Kopfgeld | Selten | 40% | 15 | 1 | Erzielt 4 Punkte | Erzielt 2 Punkte |
| Narr | Episch | 50% | 15 | 1 | (siehe Beschreibung) | (siehe Beschreibung) |
| Schwarm | Ungewöhnlich | 25% | 15 | 0 | Erzielt 4 Punkte | (siehe Beschreibung) |
| Megafon | Ungewöhnlich | 65% | 20 | 1 | Erzielt 2 Punkte, Die nächsten 2 Münzen zahlen x2 | nichts |
| Cheerleaderin | Ungewöhnlich | 70% | 15 | 0 | Erzielt 2 Punkte, Nächste 2 Münzen: +20 % Kopf | Nächste Münze: +20 % Kopf |
| Spiegel | Selten | 65% | 15 | 0 | Erzielt 1 Punkt, Nächste Münze nutzt ihre andere Seite | Erzielt 2 Punkte |
| Zwilling | Selten | 65% | 15 | 0 | Erzielt 4 Punkte | Erzielt 1 Punkt |
| Topf | Ungewöhnlich | 25% | 15 | 0 | (siehe Beschreibung) | Erzielt 1 Punkt |
| Domino | Episch | 60% | 15 | 0 | Erzielt 2 Punkte, Nächste Münze landet auf Kopf | Ziel +1 |
| Heiße Hand | Ungewöhnlich | 70% | 15 | 0 | Erzielt 2 Punkte, Serie wächst um 1 Schritt extra | nichts |
| Anker | Ungewöhnlich | 70% | 15 | 0 | Erzielt 2 Punkte, Der nächste Serienbruch wird verhindert | Erzielt 1 Punkt |
| Wetter | Selten | 35% | 15 | 1 | (siehe Beschreibung) | Ziel +2 |
| Auszahlung | Selten | 30% | 15 | 0 | Erzielt 3 Punkte | (siehe Beschreibung) |
| Kältewelle | Ungewöhnlich | 20% | 15 | 0 | Erzielt 1 Punkt | (siehe Beschreibung) |
| Verstärker | Selten | 70% | 15 | 1 | Erzielt 1 Punkt, Boni halten 1 Münze länger und werden stärker | Erzielt 1 Punkt |
| Wahres Echo | Episch | 50% | 15 | 0 | (siehe Beschreibung) | (siehe Beschreibung) |
| Verdoppler | Selten | 30% | 15 | 0 | (siehe Beschreibung) | (siehe Beschreibung) |
| Jackpot | Selten | 15% | 18 | 1 | Erzielt 25 Punkte | nichts |
| Nachahmer | Episch | 30% | 15 | 0 | (siehe Beschreibung) | Erzielt 1 Punkt |
| Orchester | Ungewöhnlich | 25% | 15 | 0 | (siehe Beschreibung) | Erhalte 1 Gold |
| Rettungsring | Ungewöhnlich | 70% | 15 | 0 | Erzielt 1 Punkt, Ein Tausch mehr in diesem Level | nichts |
| Horoskop | Ungewöhnlich | 70% | 15 | 0 | Erzielt 1 Punkt, Alle Münzen +7 % Kopf in diesem Level | Alle Münzen +3 % Kopf in diesem Level |
| Kristallkugel | Selten | 30% | 15 | 0 | Erzielt 5 Punkte | Wirf eine der nächsten drei Münzen ab |
<!-- /GEN:coins-table -->

---

## Boni: wirken auf die nächsten Münzen

Diese Münzen wirken über vier "Nächste"-Effekte auf die Münzen, die nach ihnen kommen. Ein Bonus, der beim Auflösen einer Münze entsteht, beginnt mit der
folgenden Münze und wird Münze für Münze verbraucht. Die Chancen in der Bank enthalten den Chancen-Bonus, die Zahlen auf dem Bildschirm sind also die echten.
Boni enden mit dem Level.

### Megafon (R) - 65% - 1 Energie - 20 Gold
Kopf: +2 Punkte und die **nächsten 2 Münzen zahlen doppelt** (Punkte und Gold). Passt gut vor einen Hammer, Blut oder Spieler; schlecht vor Normal-Münzen. Nutze Spähen
oder die Bank, um zu sehen, was folgt. Mehrere Megafone stapeln sich: zwei aktive Boni sind x4.

### Cheerleaderin (R) - 70%
Kopf: +2 Punkte und die nächsten 2 Münzen bekommen +20 % Kopf. Zahl: die nächste Münze bekommt +20 %. Nie ein toter Wurf. Stark vor Münzen mit niedriger Chance wie Hammer (35 %)
oder Verflucht (30 %).

### Spiegel (SR) - 65%
Kopf: +1 Punkt und die **nächste Münze benutzt die Effekte ihrer anderen Seite**. Zahl: +2 Punkte. Vor Blut oder Verflucht macht er aus der gefährlichen Zahl
die große Kopf-Auszahlung (die Effekte tauschen, der Wurf nicht). Vor dem Schwert macht er aus +5 bei Kopf ein Nichts, lies also vorher die Bank.

### Domino (UR) - 60%
Kopf: +2 Punkte und die **nächste Münze landet auf Kopf**. Zahl: Ziel +1. Garantierter Kopf auf der nächsten Münze macht aus Verflucht (+15) eine sichere Sache. Prämien und der
Boss können die Seite danach noch ändern.

### Zwilling (SR) - 65%
Landet immer **genau wie der vorherige Wurf** (Querkopf ist das Gegenteil). Kopf +4, Zahl +1. Stelle ihn hinter eine Münze, die du auf Kopf gebracht hast (Domino, Kopf erzwingen).

### Topf (R) - 25%
Kopf: Punkte gleich der Zahl der bisherigen Würfe in diesem Level, diesen eingerechnet (max. 12). Zahl +1. Ist beim ersten Wurf 1 wert und beim achten 8, hebe ihn also für das Ende
des Stapels auf: wirf späte Münzen nicht ab, um ihn zu erreichen.

---

## Serien-Münzen

Die **Serie** (siehe [gameplay.md](gameplay.md)) multipliziert die Punkte eines Wurfs um +0,25 für jedes gleiche Ergebnis in Folge. Diese Münzen füttern sie, geben sie aus oder brechen Regeln um sie herum.

### Heiße Hand (R) - 70%
Kopf: +2 Punkte und die Serie wächst um **1 Extra-Schritt**, eine Serie steigt also doppelt so schnell. Der günstigste Weg zur x3-Grenze.

### Anker (R) - 70%
Kopf: +2 Punkte und der **nächste Bruch wird ignoriert** (ein Schutz, angezeigt als SCHUTZ 1). Zahl: +1 Punkt. Der Schutz macht ein Glücksspiel wie Verflucht mitten in einer Serie sicher.

### Wetter (SR) - 35% - 1 Energie
Kopf: **3 Punkte pro Wurf der aktuellen Serie** (max. 30), und der Multiplikator gilt obendrauf: bei einer Serie x3 (neun in Folge) sind das 27 x 3 = 81. Zahl: Ziel +2. Wirf ihn tief in einer Serie, nie bei einer frischen.

### Auszahlung (SR) - 30%
Kopf: +3 Punkte, der Serien-Multiplikator **zählt doppelt** (x2 wird x4), dann endet die Serie. Der bewusste Abschluss an der Spitze einer Serie.

### Kältewelle (R) - 20%
Zahl: **2 Punkte pro Zahl in Folge** (max. 20), Kopf +1. Der Zahl-Zwilling der Kette: Er macht eine Zahl-Serie lohnend, und der Serien-Multiplikator gilt auch für ihn.

---

## Maschinen-Münzen: sie machen andere Münzen größer

### Verstärker (SR) - 70% - 1 Energie
Kopf: +1 Punkt und **jeder aktive Bonus hält 1 Münze länger und wird stärker** (Megafon x2 wird x3, Cheerleaderin +20 % wird +40 %). Zahl: +1. Spiele ihn
direkt nach einer Bonus-Münze, vor den Münzen, die du verstärken willst. Zwei Verstärker hinter einem Megafon ergeben x4.

### Wahres Echo (UR) - 50%
Keine eigenen Effekte. Es **wiederholt, was die vorherige Münze wirklich getan hat**, auf welcher Seite beide auch landeten: den gewachsenen Wert des Schneeballs, die berechneten Punkte des Wetters,
die Auszahlung des Topfs, sogar die Boni der vorherigen Münze (ein wiederholtes Megafon gibt den nächsten zwei Münzen erneut einen Bonus). Echo kopiert aufgedruckte Werte; Wahres Echo kopiert das Ergebnis.
Multiplikatoren gelten für die Kopie, mit einer Serie x2 zahlt eine wiederholte 9 also 18.

### Verdoppler (SR) - 30%
Kopf: 3 Punkte, **verdoppelt für jeden bisherigen Verdoppler-Wurf in diesem Level**: 3, 6, 12, 24 ... Der Zähler gilt für alle Exemplare, zählt auch Zahl-Würfe und wird jedes
Level zurückgesetzt. Drei Exemplare in einem Level erreichen 3 + 6 + 12; wenn Glück, Sanduhr oder Nachziehen Exemplare zurückbringen, laufen die Zahlen davon (begrenzt auf 384 pro Wurf).

---

## Neuere Münzen

### Jackpot (SR) - 15% - 1 Energie - 18 Gold
Kopf: **25 Punkte**. Ein Wurf von fünf. Mit Cheerleaderin, Horoskop, Glücksbringer oder Domino dahinter werden die Chancen real; allein ist er ein Lotterielos.

### Nachahmer (UR) - 30%
Kopf: tut, was die **Kopf-Seite einer zufälligen anderen Münze in deinem Deck** tut (über den Seed; wie aufgedruckt, das Wachstum des Schneeballs wird also nicht kopiert). Zahl: +1 Punkt. Am besten in einem Deck aus
starken Kopf-Münzen; Echo und Wahres Echo kopieren die *vorherige* Münze, der Nachahmer kopiert eine aus dem Deck.

### Orchester (R) - 25%
Kopf: **2 Punkte pro verschiedenem Münztyp in deinem Deck** (ein Deck mit fünf verschiedenen Münzen zahlt 10). Zahl: +1 Gold. Das Gegenteil des Schwarms: Es will Vielfalt.

### Rettungsring (R) - 70%
Kopf: 1 Punkt und **ein Tausch mehr in diesem Level** (die Grenze ist 3). Eine Sicherheitsmünze für Decks, denen die Münzen ausgehen.

### Horoskop (R) - 70%
Kopf: 1 Punkt und **alle Münzen +7 % Kopf für den Rest des Levels**; Zahl: alle Münzen +3 %. Es füttert denselben Topf wie Fokus und die Prämie Magnet, mehrere davon stapeln sich also.

### Kristallkugel (SR) - 30%
Kopf: 5 Punkte. Zahl: **wirf eine der nächsten drei Münzen der Bank ab** (kostenlos; Karte anklicken). Aus einer schlechten Zahl wird ein gewähltes Abwerfen, sie passt also zu Sanduhr oder einer riskanten Bank. Der Blick in den Stapel ist beim Chip Spähen geblieben.

---

## Die Grundmünze

### Normal (N) - 65% - 5 Gold
Kopf: +1 Punkt. Zahl: nichts. EW 0,5. Die einzige Münze, die alle 5 Plätze eines Sets füllen darf, und die günstigste Münze im
Shop. Ein Deck aus fünf Normal-Münzen hat ein Ziel von 3 Punkten und einen erwarteten Wert von 2,5, weshalb ein einfacher Start
Glück, einen Tausch oder einen ersten Kauf braucht. Normal-Münzen sind Füllmaterial: Sie halten das Ziel niedrig (das Ziel wächst mit der
Deckgröße), sind aber die schwächste Nutzung eines Wurfs. Entferne sie mit der Münz-Entfernung, sobald sich das Deck mit besseren Münzen füllt.

---

## Einfache Punktebringer

### Schwert (N) - 35% - 12 Gold
Kopf +5 Punkte. EW 2,5. Reine Verlässlichkeit: kein Haken, keine Kosten. Die Startmünze der Klinge. Vier Schwerter und ein paar Dolche
schaffen die frühen Ziele allein.

### Dolch (N) - 80% - 12 Gold
Kopf +4, Zahl +1. EW 3,25. Punktet auf beiden Seiten und hat die besten Chancen aller Startmünzen. Der effizienteste einfache
Punktebringer im Spiel. Passt zu jeder anderen Münze, weil er nie einen Wurf verschwendet.

### Hammer (R) - 25% - 2 Energie - 22 Gold
Kopf +16, Zahl +1. EW 6,25. Die stärkste reine Auszahlung, kostet aber zwei deiner drei Energie, du kannst pro Level also höchstens einen werfen,
ohne Energiequelle. Nutze ihn mit Kupfer, Funke oder Flux-Kondensator. Schlecht in einem Deck, das keine Energie erzeugt.

### Blut (R) - 37% - 1 Energie - 22 Gold
Kopf +11, Zahl Ziel +6. EW 4,2 netto (6,6 Punkte minus 2,4 Ziel). Stark, aber riskant: Eine Zahl kostet mehr als einen halben Kopf, du brauchst also Chancenhilfe (Fokus, Cheerleaderin) oder einen Vorsprung. Besser, wenn du vorn liegst und
fertig werden willst, schlechter als Last-Wurf-Glücksspiel. Steht in der Shop-Liste von Klinge und Seherin.

### Funke (R) - 70%
Kopf +2 Energie, Zahl +3 Punkte. EW 1,5 Punkte und 1,0 Energie. Kostenlos zu werfen und füttert teure Münzen. Startmünze der Seherin
und Freischaltung für Händler und Klinge. Immer einen Platz neben Hammer, Blut oder Schneeball wert.

---

## Wirtschaft und Energie

### Kupfer (N) - 30% - 10 Gold
Kopf +2 Gold, Zahl +1 Energie. EW 1 Gold, 0,5 Energie. Bezahlt Tausche und den Shop; Zahl ist nie ein toter Wurf. Im Pool des Händlers.

### Gezinkt (N) - 70% - 12 Gold
Kopf +4 Gold. EW 3 Gold. Die verlässliche Einkommensmünze. Gold ist auch der Weg, einen zweiten Versuch zu kaufen (Tausch), Gezinkt ist also ein
Sicherheitsnetz so sehr wie ein Shop-Beschleuniger. Startmünze des Händlers.

### Bank (R) - 40%
Kopf +3 Gold plus 1 Gold je 10 gehaltene Gold (max. +3). EW bis 3 bis 3 + Zinsen in Gold. Belohnt Horten; ab 30 Gold sind es
praktisch +6 pro Kopf.

### Geizhals (R) - 40%
Kopf: +1 Punkt je 10 Gold, die du hältst. Zahl +2 Gold. Bei 40 Gold sind das 4 Punkte pro Kopf. Der Punktebringer eines reichen Decks. Furchtbar
bei einem frischen Start (0 Punkte) und gut spät.

### Flux-Kondensator (R) - 35% - 1 Energie
Kopf: +2 Punkte je gehaltener Energie. Zahl: +1 Energie. Mit 3 Energie (2 nach den Kosten) sind das 4 Punkte; mit 6 sind es 10.
Macht übrige Energie zu Punkten. Passt zu Funke und Kupfer.

### Kopfgeld (SR) - 40% - 1 Energie
Kopf +4, Zahl +2, und jeder Punkt, den sie erzielt, zahlt 1 Gold je 2 Punkte (ein `register`-Haken auf `effect_applied`). EW 3
Punkte und 1,5 Gold. Ein Punktebringer, der zugleich Einkommen ist.

### Vampir (R) - 35%
Kopf +2 Punkte und +2 Gold. EW 1 Punkt und 1 Gold. Eine kleine, gleichmäßige Zwei-Ressourcen-Münze. Nicht stark, aber bei Kopf nie leer.

---

## Risiko-Münzen

### Verflucht (SR) - 25%
Kopf +15, Zahl Ziel +2. EW 4,5 Punkte minus 1,4 Ziel = 3,1 netto. Ein großer Ausschlag: sieben von zehn Würfen schieben die Torpfosten
zurück. Lohnt nur mit Möglichkeiten, die Chance zu erhöhen (Fokus, Magnet, Tuner) oder Kopf zu erzwingen (Chip).

### Spieler (SR) - 35% - 1 Energie - 22 Gold
Kopf +7, dann ein 50:50 über den Seed: entweder alle Effekte x3 (21) oder nichts. EW 5,25. Reine Streuung. Ausgezeichnet, wenn du hinten liegst
und einen Sprung brauchst, verschwenderisch, wenn du genau einen Wurf vom Ziel entfernt bist.

### Phönix (UR) - 25%
Kopf +3, Zahl Ziel +2. Jede Zahl speichert einen Zorn (max. 5) auf der Münze; der nächste Kopf bringt +2 Punkte je Zorn und löscht ihn.
Bei vollem Zorn bringt sie 13. Sie will erst Zahl, dann Kopf, wird also am besten spät im Stapel geworfen, wenn mehrere Phönixe
durch Zahl gegangen sind. Jedes Exemplar behält seinen eigenen Zorn für den ganzen Lauf.

### Märtyrer (SR) - 40% - 1 Energie
Zahl Ziel +3. Kopf +4 plus 1 je Zahl, die der Märtyrer in diesem Level hatte. Die Schuld wird jedes Level zurückgesetzt. Ein Langsambrenner für
Decks, in denen Zahl häufig ist.

### Glückssieben (SR) - 30%
Jeder Wurf hat eine Chance von 1 zu 7, Kopf zu erzwingen und die Punkte zu verdreifachen (3 x 3 = 9). Sonst ist sie eine gewöhnliche 40-%-Münze mit
+3. EW etwa 2,3. Ein kleiner Jackpot mit Boden.

---

## Chancen-Münzen

### Fokus (R) - 65% - 10 Gold
Kopf: diese Münze gewinnt +15 % Kopf-Chance für den Rest des Levels (ein `probability`-Effekt). Zahl +4 Punkte. EW 2 Punkte + Chancenwachstum. Gedacht, um mit Glück / Sanduhr oder Tunern
mehrmals geworfen zu werden; sonst eine schwache Münze.

### Schwung (SR) - 35%
+5 % Kopf für jeden Kopf in Folge in diesem Level (liest `encounter.streak`). Kopf +6. Nach vier Kopf in Folge ist sie eine 60-%-Münze.
Passt zu Kette und Magnet. Die Serie endet bei Zahl.

### Schwarm (R) - 25%
+10 % Kopf für jeden *anderen* Schwarm in deinem Deck (zählt das Deck, nicht die Bank). Kopf +4. Zwei Exemplare: 50 %, drei: 60 %
(maximal 3 Exemplare). Eine Set-Bau-Münze.

### Sanduhr (R) - 30% - 10 Gold
+30 % Kopf, wenn 3 oder weniger Münzen im Stapel sind. Kopf +4. Zahl schickt sie zurück auf den Stapel (Nachziehen), sie bekommt also eine
zweite Chance am Ende. Ein Schlussmann: früh schlecht, gut, wenn der Stapel fast leer ist.

### Querkopf (SR) - 65%
Landet immer gegenüber dem vorherigen Wurf (überschreibt den Wurf). Kopf +4, Zahl +1. Vorhersehbar: Du kennst ihre Seite vor dem Werfen. Plane sie nach einer Zahl, um 4 Punkte zu garantieren.

### Kette (R) - 60%
Kopf: +2 Punkte je Kopf in Folge, diesen eingerechnet. Nach drei Kopf in Folge zahlt sie 8. Braucht Schwung oder Münzen mit starker Kopf-Chance
davor. Endet bei Zahl.

---

## Wachsende Münzen

### Schneeball (SR) - 30% - 1 Energie - 24 Gold
Kopf +3, Zahl +1, und **+1 Punkt auf jeden Effekt für jedes Mal, das diese Münze in diesem Lauf geworfen wurde**, begrenzt auf +10. Das Wachstum liegt
auf der Münzinstanz und überlebt also zwischen den Leveln. EW an der Grenze: 12 Punkte. Spielt einen Wurf pro Level, es dauert also
10 Level bis zur Grenze; in einem Lauf mit 4 Leveln rechne höchstens mit +3, außer du hast Nachzieher (Glück, Sanduhr, Chip Nachziehen).

### Lunte (SR) - 30% - 10 Gold
Kopf +1. Wenn du sie *abwirfst*, lädt sie 6 auf (auf der Münze gespeichert, bleibt zwischen den Leveln). Bei Kopf gibt sie die ganze Ladung als
Extra-Punkte aus. Eine gegenintuitive Münze: Ein Exemplar zu verschwenden ist der Weg, das nächste aufzuladen. Braucht zwei oder mehr Exemplare.

---

## Kombination und Synergie

### Glück (N) - 30% - 10 Gold
Kopf: +2 Punkte, und sie kommt zurück auf den Nachziehstapel und kann erneut gespielt werden (Nachziehen). Ein "Gratis-Wurf"-Erzeuger, wenn
du starke Münzen zum Füttern hast. Begrenzt auf 3 Rückkehrer pro Level.

### Echo (UR) - 60%
Wiederholt die Effekte, die die vorherige Münze für dieselbe Seite hatte (Kopf-Effekte nach Kopf, Zahl nach Zahl). Echo nach
Hammer bei Kopf sind weitere 16 Punkte. Echo nach einer Münze mit leerer Seite tut nichts.

### Narr (UR) - 50% - 1 Energie
Ignoriert ihre eigenen Seiten: ein zufälliger Effekt aus {6 Punkte, 4 Gold, 2 Energie, 2 Punkte}. EW 3,5 Punkte-Äquivalent. Sicheres Chaos.

---

## Stärke-Rangliste

Grobe Reihenfolge nach dem, was der Simulator bei einem gebauten Set zeigt (siehe `lua tools/sim.lua --coins`):

| Stufe | Münzen |
|---|---|
| S | Hammer (mit Energie), Blut, Schneeball, Spieler, Dolch |
| A | Schwert, Flux-Kondensator, Kopfgeld, Verflucht (mit Chancenhilfe), Phönix, Kette + Schwung |
| B | Funke, Fokus, Glückssieben, Querkopf, Märtyrer, Echo |
| C | Gezinkt, Kupfer, Bank, Geizhals, Vampir, Schwarm, Sanduhr, Glück, Narr |
| Füllmaterial | Normal |

Bekannte Probleme: Die Münzpreise sind fast einheitlich, starke Münzen sind also unterbezahlt; einige Wirtschaftsmünzen (Bank, Geizhals) bringen in einem Lauf mit 4 Leveln
kaum etwas, weil sich selten Gold ansammelt.
