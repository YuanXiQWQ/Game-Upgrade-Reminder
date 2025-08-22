# 游戏升级提醒

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

**中文** | [English](#game-upgrade-reminder)

---

一个用于记录和追踪需要大量时间升级的游戏进度的提醒工具。最初是为《海岛奇兵》(Boom Beach) 而制作的。

## 功能特点

- 🕒 追踪多个账号的升级任务
- ⏰ 与日历/闹钟不同的是，计时方式与游戏同步为倒计时，省去了每次计算时间的麻烦
- 🔔 升级完成时显示系统通知
- ♻️ 重复任务：每天/每周/每月/每年/自定义；结束时间可选（默认无）；支持跳过规则

> 计划实现（~~但大概率会鸽~~）：
> - 多语言支持

## 系统要求

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) 或更高版本
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) 或更高版本

> 其它版本能不能行我也不知道 :<

## 安装方法

1. 从 [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases) 页面下载最新版本
2. 解压到任意目录
3. 运行 `Game Upgrade Reminder.exe`

## 使用说明

### 添加升级任务

1. 在界面顶部选择账号
2. 选择或创建任务名称，可留空
3. 设置升级所需时间：开始时间、天、小时、分钟（开始时间除非设置，否则默认为当前系统时间）
4. 点击"添加"按钮创建任务

### 管理任务

- 到时间的任务会突出显示，点击“完成”来标记完成
- 可以在列表中删除任务，三秒内可撤销删除

## 常见问题

### 收不到系统通知

- 把**专注助手**关了，或者把 `Game Upgrade Reminder.exe` 放入专注助手-优先列表。如果专注助手-自动规则中开启的规则的过滤为“仅闹钟”，将其修改为“仅优先通知”
- 除此之外就不知道了

### 其它神奇的问题

- 应该是 bug 吧，踩死就好了
- 可以到 Issues 页面报告，不过我很可能不知道怎么修

## 许可证

本项目采用 [GNU Affero General Public License v3.0](LICENSE) 开源许可。

---

# Game Upgrade Reminder

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

[中文](#游戏升级提醒) | **English**

---

A reminder tool for tracking and managing in-game upgrade progress that requires significant time investment. Initially
created for "Boom Beach."

## Features

- 🕒 Track upgrade tasks across multiple accounts
- ⏰ A countdown synced with in-game timers (unlike calendars/alarms), eliminating manual time calculations
- 🔔 Receive system notifications when upgrades complete
- ♻️ Recurring tasks: daily/weekly/monthly/yearly/custom; optional end time (None by default); skip rules

> Planned features (~~but might be delayed~~):
> - Multi-language support

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

- Turn off **Focus Assist**, or add `Game Upgrade Reminder.exe` to the Focus Assist Priority list. If a rule under Focus Assist - Automatic rules is set to "Alarms only", change it to "Priority only".
- If that doesn't help, I'm not sure what else to suggest

### Other issues

- Probably a miracle bug (aka feature)
- You can report issues, but I probably won't know how to fix them

## License

This project is licensed under the [GNU Affero General Public License v3.0](LICENSE).
