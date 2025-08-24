# 游戏升级提醒

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

一个用于记录和追踪需要较长时间才能完成的游戏升级进度的提醒工具。最初是为 **《Boom Beach》** 制作的。

## 功能特点

- 🕒 追踪多个账号的升级任务
- ⏰ 与日历/闹钟不同，倒计时与游戏同步，免去每次手动计算时间的麻烦
- 🔔 升级完成时显示系统通知
- ♻️ 重复任务：每天 / 每周 / 每月 / 每年 / 自定义；可选结束时间（默认无）；支持跳过规则
- 🌐 支持 27 种语言

## 系统需求

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) 或更高版本
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) 或更高版本

> 其它版本是否可用就不确定了 :<

## 安装方法

1. 从 [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases) 页面下载最新版本
2. 解压到任意文件夹
3. 运行 `Game Upgrade Reminder.exe`

## 使用说明

### 添加升级任务

1. 在界面顶部选择账号
2. 选择或创建任务名称，可留空
3. 设置升级所需时间：开始时间、天、小时、分钟（如未设置，默认使用当前系统时间）
4. 点击“添加”按钮创建任务

### 管理任务

- 到时间的任务会高亮显示，点击“完成”来标记完成
- 可以从列表中删除任务，删除操作可在三秒内撤销

## 常见问题

### 收不到系统通知

- 关闭 **专注助手 (Focus Assist)**，或将 `Game Upgrade Reminder.exe` 添加到优先列表。如果专注助手的自动规则设置为“仅限闹钟”，请改成“仅限优先通知”。
- 其他情况就不清楚了

### 其他奇怪的问题

- 大概率是 Bug，忽略就好
- 可以到 Issues 页面报告，但我可能不知道怎么修

## 许可证

本项目采用 [GNU Affero General Public License v3.0](../LICENSE) 开源许可。