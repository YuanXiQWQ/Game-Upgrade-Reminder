# 遊戲升級提醒

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

一個用於記錄與追蹤需要大量時間升級的遊戲進度的提醒工具。最初是為《海島奇兵》(Boom Beach) 所製作。

## 功能特色

- 🕒 可追蹤多個帳號的升級任務
- ⏰ 與日曆／鬧鐘不同，計時方式與遊戲同步為倒數計時，免去了每次自行計算時間的麻煩
- 🔔 升級完成時會顯示系統通知
- ♻️ 重複任務：每天／每週／每月／每年／自訂；可選結束時間（預設為無）；支援跳過規則
- 🌐 支援 27 種語言

## 系統需求

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) 或更高版本
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) 或更高版本

> 其他版本能不能行我也不確定 :<

## 安裝方法

1. 從 [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases) 頁面下載最新版本
2. 解壓縮至任意資料夾
3. 執行 `Game Upgrade Reminder.exe`

## 使用說明

### 新增升級任務

1. 在介面頂部選擇帳號
2. 選擇或建立任務名稱，可留空
3. 設定升級所需時間：開始時間、天、時、分（若未設定開始時間，則預設為目前系統時間）
4. 點擊「新增」按鈕以建立任務

### 管理任務

- 到期的任務會被特別標示，點擊「完成」可將其標記為完成
- 可於列表中刪除任務，刪除後三秒內可撤銷

## 常見問題

### 收不到系統通知

- 關閉 **專注小幫手 (Focus Assist)**，或將 `Game Upgrade Reminder.exe` 加入優先清單。若專注小幫手的自動規則設定為「僅限鬧鐘」，請改為「僅限優先通知」。
- 除此之外我也不清楚

### 其他奇怪的問題

- 應該是 bug 吧，無視就好
- 可以到 Issues 頁面回報，但我很可能不知道怎麼修

## 授權條款

本專案採用 [GNU Affero General Public License v3.0](../LICENSE) 開源授權。