# Spiel-Upgrade-Erinnerung

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

Ein Tool zum Aufzeichnen und Verfolgen des Spielfortschritts bei Upgrades, die viel Zeit in Anspruch nehmen. Ursprünglich für **Boom Beach** entwickelt.

## Funktionen

- 🕒 Verfolgung von Upgrade-Aufgaben über mehrere Konten
- ⏰ Anders als Kalender/Wecker wird der Countdown mit dem Spiel synchronisiert, wodurch die manuelle Zeitberechnung entfällt
- 🔔 Systembenachrichtigung, wenn das Upgrade abgeschlossen ist
- ♻️ Wiederkehrende Aufgaben: täglich / wöchentlich / monatlich / jährlich / benutzerdefiniert; optionales Enddatum (Standard: keines); unterstützt Überspringregeln
- 🌐 Unterstützt 27 Sprachen

## Systemanforderungen

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) oder neuer
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) oder neuer

> Ob andere Versionen funktionieren, weiß ich nicht :<

## Installation

1. Laden Sie die neueste Version von der [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases)-Seite herunter
2. Entpacken Sie die Dateien in einen beliebigen Ordner
3. Führen Sie `Game Upgrade Reminder.exe` aus

## Verwendung

### Upgrade-Aufgabe hinzufügen

1. Wählen Sie das Konto oben in der Benutzeroberfläche
2. Wählen Sie einen Aufgabennamen oder erstellen Sie einen neuen (kann leer bleiben)
3. Stellen Sie die benötigte Zeit ein: Startzeit, Tage, Stunden, Minuten (wenn keine Startzeit festgelegt ist, wird standardmäßig die aktuelle Systemzeit verwendet)
4. Klicken Sie auf die Schaltfläche „Hinzufügen“, um die Aufgabe zu erstellen

### Aufgaben verwalten

- Aufgaben, deren Zeit erreicht ist, werden hervorgehoben; klicken Sie auf „Abschließen“, um sie als erledigt zu markieren
- Aufgaben können aus der Liste gelöscht werden, das Löschen kann innerhalb von drei Sekunden rückgängig gemacht werden

## FAQ

### Keine Systembenachrichtigungen erhalten

- Deaktivieren Sie **Fokus-Assistent (Focus Assist)** oder fügen Sie `Game Upgrade Reminder.exe` zur Prioritätsliste hinzu. Wenn eine automatische Regel auf „Nur Alarme“ eingestellt ist, ändern Sie sie auf „Nur Priorität“.
- Ansonsten weiß ich es nicht

### Andere seltsame Probleme

- Wahrscheinlich ein Bug – einfach ignorieren
- Kann auf der Issues-Seite gemeldet werden, aber möglicherweise weiß ich nicht, wie man es behebt

## Lizenz

Dieses Projekt ist unter der [GNU Affero General Public License v3.0](../LICENSE) lizenziert.