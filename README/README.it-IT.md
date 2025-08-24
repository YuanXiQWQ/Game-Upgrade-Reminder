# Promemoria Aggiornamento Giochi

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

Uno strumento per registrare e monitorare i progressi degli aggiornamenti di gioco che richiedono molto tempo. Inizialmente creato per **Boom Beach**.

## Caratteristiche

- 🕒 Tracciare i compiti di aggiornamento su più account
- ⏰ Diversamente da calendari/allarmi, il conto alla rovescia è sincronizzato con il gioco, eliminando la necessità di calcolare manualmente il tempo ogni volta
- 🔔 Notifica di sistema quando l’aggiornamento è completato
- ♻️ Attività ricorrenti: giornaliere / settimanali / mensili / annuali / personalizzate; tempo di fine opzionale (predefinito: nessuno); supporto per regole di salto
- 🌐 Supporta 27 lingue

## Requisiti di sistema

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) o versioni successive
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) o versioni successive

> Non sono sicuro che altre versioni funzionino :<

## Installazione

1. Scarica l’ultima versione dalla pagina [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases)
2. Estrai in una cartella a piacere
3. Avvia `Game Upgrade Reminder.exe`

## Guida all’uso

### Aggiungere un compito di aggiornamento

1. Seleziona l’account nella parte superiore dell’interfaccia
2. Scegli o crea un nome per il compito (facoltativo)
3. Imposta la durata dell’aggiornamento: ora di inizio, giorni, ore, minuti (se non impostata, l’ora di inizio predefinita sarà l’ora di sistema corrente)
4. Clicca sul pulsante "Aggiungi" per creare il compito

### Gestire i compiti

- I compiti giunti a scadenza saranno evidenziati; clicca su "Completa" per segnarli come terminati
- I compiti possono essere eliminati dall’elenco, e l’eliminazione può essere annullata entro tre secondi

## FAQ

### Non ricevo notifiche di sistema

- Disattiva **Assistente di concentrazione (Focus Assist)** oppure aggiungi `Game Upgrade Reminder.exe` all’elenco delle priorità. Se una regola automatica è impostata su "Solo allarmi", cambiala in "Solo priorità".
- Altrimenti non lo so

### Altri problemi strani

- Probabilmente si tratta di un bug, basta ignorarlo
- Puoi segnalarlo sulla pagina Issues, ma probabilmente non saprò come risolverlo

## Licenza

Questo progetto è concesso in licenza sotto la [GNU Affero General Public License v3.0](../LICENSE).