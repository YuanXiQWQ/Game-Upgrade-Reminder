# 遊戲升級提醒

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

一個用於記錄和追蹤需要大量時間升級的遊戲進度的提醒工具。最初是為《海島奇兵》(Boom Beach) 而製作的。

## 功能特點

- 🕒 追蹤多個賬號的升級任務
- ⏰ 與日曆/鬧鐘不同的是，計時方式與遊戲同步為倒計時，省去了每次計算時間的麻煩
- 🔔 升級完成時顯示系統通知
- ♻️ 重複任務：每天/每週/每月/每年/自定義；結束時間可選（默認無）；支持跳過規則
- 🌐 27 種語言支持

## 系統要求

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) 或更高版本
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) 或更高版本

> 其他版本能不能行我也不知道 :<

## 安裝方法

1. 從 [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases) 頁面下載最新版本
2. 解壓到任意目錄
3. 運行 `Game Upgrade Reminder.exe`

## 使用說明

### 添加升級任務

1. 在界面頂部選擇賬號
2. 選擇或創建任務名稱，可留空
3. 設置升級所需時間：開始時間、天、小時、分鐘（開始時間除非設置，否則默認為當前系統時間）
4. 點擊"添加"按鈕創建任務

### 管理任務

- 到時間的任務會突出顯示，點擊“完成”來標記完成
- 可以在列表中刪除任務，三秒內可撤銷刪除

## 常見問題

### 收不到系統通知

- 把**專注助手**關了，或者把 `Game Upgrade Reminder.exe` 放入專注助手-優先列表。如果專注助手-自動規則中開啟的規則的過濾為“僅鬧鐘”，將其修改為“僅優先通知”
- 除此之外就不知道了

### 其他神奇的問題

- 應該是 bug 吧，踩死就好了
- 可以到 Issues 頁面報告，不過我很可能不知道怎麼修

## 許可證

本項目採用 [GNU Affero General Public License v3.0](LICENSE) 開源許可。