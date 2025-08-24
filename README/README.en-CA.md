# Game Upgrade Reminder

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

A reminder tool for tracking and managing in-game upgrade progress that requires significant time investment. Initially
created for "Boom Beach."

## Features

- 🕒 Track upgrade tasks across multiple accounts
- ⏰ A countdown synced with in-game timers (unlike calendars/alarms), eliminating manual time calculations
- 🔔 Receive system notifications when upgrades complete
- ♻️ Recurring tasks: daily/weekly/monthly/yearly/custom; optional end time (None by default); skip rules
- 🌐 27 languages supported

## System Requirements

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) or later
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later

> Not sure if it works with other versions :<

## Installation

1. Download the latest version from [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases)
2. Extract to any directory
3. Run `Game Upgrade Reminder.exe`

## Usage

### Adding Upgrade Tasks

1. Select an account from the top of the interface
2. Choose or create a task name (optional)
3. Set upgrade duration: start time, days, hours, and minutes (start time defaults to current system time if not set)
4. Click the "Add" button to create the task

### Managing Tasks

- Completed tasks will be highlighted - click "Complete" to mark them as done
- Delete tasks from the list (deletion can be undone within 3 seconds)

## FAQ

### Not receiving system notifications

- Turn off **Focus Assist**, or add `Game Upgrade Reminder.exe` to the Focus Assist Priority list. If a rule under Focus
  Assist - Automatic rules are set to "Alarms only", change it to "Priority only".
- If that doesn't help, I'm not sure what else to suggest

### Other issues

- Probably a miracle bug (aka feature)
- You can report issues, but I probably won't know how to fix them

## License

This project is licensed under the [GNU Affero General Public License v3.0](LICENSE).