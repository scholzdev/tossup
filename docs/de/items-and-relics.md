# Chips und Prämien

Das Spiel hat zwei Arten von Objekten, die keine Münzen sind. **Chips** sind Verbrauchsgegenstände, die du während eines Levels benutzt. **Prämien** (Relikte) sind
passive Effekte, die den ganzen Lauf halten. Beide werden im Shop gekauft.

## Chips (Gegenstände)

- Im Shop verkauft, 2 Angebote pro Besuch aus den 11 Chips. Du kannst höchstens **3** halten (`Items.MAX`).
- Benutzt über die drei Plätze unten rechts in der Rundenansicht. Ein Chip ist nur benutzbar, solange eine Münze ausgeteilt ist und nicht geworfen wird.
- Ein benutzter Chip ist verbraucht, es sei denn, er lehnt ab (dann passiert nichts und er bleibt).
- Einmalige Effekte werden auf dem Ereignisbus *scharf gemacht* und feuern einmal beim nächsten passenden Ereignis; unbenutzte werden am Ende des Levels
  gelöscht.

<!-- GEN:chips -->
| Chip | Preis | Effekt | Hinweise |
|---|---|---|---|
| **Verdoppeln** | 14 | Nächste Münze: Punkte, Gold, Energie und Strafen x2 | Scharf gemacht auf `coin_resolve`; verdoppelt Punkte, Gold, Energie und Strafen. Am besten auf Hammer, Blut, Spieler oder Verflucht. Vorsicht: Es verdoppelt auch eine Strafe. |
| **Energydrink** | 8 | Erhalte 2 Energie | Schaltet einen Hammer- oder Blut-Wurf frei, den du nicht bezahlen konntest. |
| **Nachziehen** | 10 | Eine gespielte Münze kommt zurück auf den Stapel | Höchstens 3 Rückkehrer pro Level (zusammen mit Glück und Sanduhr). Lehnt ab, wenn keine Münze gespielt wurde oder bei der Grenze. |
| **Kopf erzwingen** | 12 | Die Münze landet auf Kopf | Scharf gemacht auf `coin_flip`. Eine Prämie oder der Boss können die Seite danach noch ändern. |
| **Zahl erzwingen** | 12 | Die Münze landet auf Zahl | Für Phönix (Zorn speichern), Märtyrer (Schuld) und die Planung mit dem Querkopf. |
| **Glücksbringer** | 10 | Die nächsten 2 Münzen +20 % Kopf | Sofort in den Chancen zu sehen. |
| **Spähen** | 6 | Sieh die nächsten zwei Münzen | Das Ergebnis steht im Hinweistext. Lehnt ab, wenn der Stapel leer ist. |
| **Sicherheitsnetz** | 9 | Der nächste Serienbruch wird verhindert | Nutze es mitten in einer langen Serie vor einer riskanten Münze. |
| **Abkürzung** | 12 | Erziele sofort 3 Punkte | Zählt wie alle Punkte für das Ziel und das Überschuss-Gold. |
| **Tausch** | 8 | Gratis abwerfen, neue Münze | Lehnt ab, wenn keine nutzbare Münze übrig bliebe. |
| **Beschwert** | 8 | +25 % Kopf, ein Wurf | Auf 100 % begrenzt. |
<!-- /GEN:chips -->

**Chips gut nutzen**

- Chips sind billiger als Münzen (6 bis 14), aber Einwegartikel. Der beste Einsatz ist der wertvollste Wurf des Levels.
- Kopf erzwingen plus Verflucht (+15) sind garantierte 15 Punkte für 12 Gold; Verdoppeln plus Hammer sind 32.
- Spähen und Tausch geben Information und Kontrolle für wenig Gold; sie sind am stärksten in Decks mit wenigen starken und vielen schwachen Münzen.

## Prämien (Relikte)

- 1 Angebot pro Shop-Besuch, 25 Gold. Du siehst nie eine, die du schon besitzt.
- Keine Grenze für die Zahl der besessenen. Sie werden beim Kauf an den Ereignisbus gebunden (`src/relics.lua`).

<!-- GEN:prizes -->
| Prämie | Effekt | Hinweise |
|---|---|---|
| **Taktstock** | Serie: +0,4 je Schritt statt 0,25, bis x4 | Gebaut für Serien-Decks (Heiße Hand, Anker, Domino, Zwilling). |
| **Kaputte Uhr** | Jeder 10. Wurf ist Kopf | Zählt nur in langen Leveln (ein Stapel mit 10 Münzen und Rückkehrern); in kurzen schwach. |
| **Magnet** | Je 3 Kopf in Folge: +5 % Kopf in diesem Level | Serienbasiert. Mit Schwung oder Kette schaukelt sich das auf. Der Levelbonus wird jedes Level zurückgesetzt. |
| **Metronom** | Jeder 4. Wurf zahlt doppelt | Passt zum Megafon: Lege den Megafon-Bonus auf den 4. Wurf. |
| **Glückspfennig** | Die erste Zahl jedes Levels wird zu Kopf | Angewendet nach dem Wurf und den Chips. Macht den ersten Wurf sicher; am besten bei einer Münze mit großem Kopf. |
<!-- /GEN:prizes -->

**Reihenfolge der Prämien.** Prämien ändern das Ergebnis in `coin_outcome`, nach Chips und Münzhaken und vor der Boss-Umkehrung. Der
Boss dreht danach weiterhin jeden fünften Wurf um, ein Wurf mit dem Glückspfennig wird beim fünften Wurf des Bosses also zu Zahl.

## Ideen, die noch nicht umgesetzt sind

- Ein Relikt, das die maximale Energie erhöht (die Shop-Funktion `buy_energy` existiert, ist aber an nichts angeschlossen).
- Ein Relikt, das Abwerfen Gold geben lässt.
- Chips, die die Seiteneffekte einer Münze statt der Chancen ändern (zum Beispiel "Kopf- und Zahl-Effekte für diesen Wurf tauschen").
