/*
 * 游戏升级提醒 - 主窗体本地化扩展
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: MainForm的本地化相关方法扩展
 * 创建日期: 2025-08-22
 * 最后修改: 2025-08-23
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using Game_Upgrade_Reminder.Core.Services;
using System.Text.RegularExpressions;

namespace Game_Upgrade_Reminder.UI
{
    /// <summary>
    /// MainForm的本地化扩展方法
    /// </summary>
    public sealed partial class MainForm
    {
        /// <summary>
        /// 构建语言选择菜单
        /// </summary>
        private void BuildLanguageMenu()
        {
            _miLanguage.DropDownItems.Clear();

            var all = _localizationService.AvailableLanguages.ToList();
            var zhVariants = all.Where(IsChineseTag).ToList();
            var enVariants = all.Where(IsEnglishTag).ToList();
            var ptVariants = all.Where(IsPortugueseTag).ToList();
            var others = all.Where(l => !IsEnglishTag(l) && !IsChineseTag(l) && !IsPortugueseTag(l)).ToList();

            // 先添加非英语的语言
            foreach (var lang in others)
            {
                AddLanguageMenuItem(_miLanguage.DropDownItems, lang);
            }

            // 将所有英语变体聚合为一个父菜单
            if (enVariants.Count > 0)
            {
                var englishRaw = _localizationService.GetText("Language.en", "English");
                var englishLabel = FormatLanguageDisplay(englishRaw, _localizationService.CurrentLanguage);

                var parent = new ToolStripMenuItem(englishLabel)
                {
                    Tag = "en-*",
                    Checked = IsEnglishTag(_localizationService.CurrentLanguage)
                };

                void AddEnglish(string code, string label)
                {
                    if (!enVariants.Contains(code)) return;
                    var item = new ToolStripMenuItem(label)
                    {
                        Tag = code,
                        Checked = code == _localizationService.CurrentLanguage
                    };
                    item.Click += (_, _) => SetLanguage(code);
                    parent.DropDownItems.Add(item);
                }

                // 子项硬编码为英文名（因为其它语言使用者大概率不会关心英文具体子选项，大概）
                AddEnglish("en-US", "English (United States)");
                AddEnglish("en-GB", "English (United Kingdom)");
                AddEnglish("en-CA", "English (Canada)");

                _miLanguage.DropDownItems.Add(parent);
            }

            // 将所有葡萄牙语变体聚合为一个父菜单
            if (ptVariants.Count > 0)
            {
                var portugueseRaw = _localizationService.GetText("Language.pt", "Português");
                var portugueseLabel = FormatLanguageDisplay(portugueseRaw, _localizationService.CurrentLanguage);

                var parent = new ToolStripMenuItem(portugueseLabel)
                {
                    Tag = "pt-*",
                    Checked = IsPortugueseTag(_localizationService.CurrentLanguage)
                };

                void AddPortuguese(string code, string label)
                {
                    if (!ptVariants.Contains(code)) return;
                    var item = new ToolStripMenuItem(label)
                    {
                        Tag = code,
                        Checked = code == _localizationService.CurrentLanguage
                    };
                    item.Click += (_, _) => SetLanguage(code);
                    parent.DropDownItems.Add(item);
                }

                // 子项固定葡萄牙语名称
                AddPortuguese("pt-BR", "Português (Brasil)");
                AddPortuguese("pt-PT", "Português (Portugal)");

                _miLanguage.DropDownItems.Add(parent);
            }

            // 将所有中文变体聚合为一个父菜单（本地化标签）
            if (zhVariants.Count > 0)
            {
                var chineseRaw = _localizationService.GetText("Language.zh", "中文");
                var chineseLabel = FormatLanguageDisplay(chineseRaw, _localizationService.CurrentLanguage);

                var parent = new ToolStripMenuItem(chineseLabel)
                {
                    Tag = "zh-*",
                    Checked = IsChineseTag(_localizationService.CurrentLanguage)
                };

                void AddChinese(string code, string label)
                {
                    if (!zhVariants.Contains(code)) return;
                    var item = new ToolStripMenuItem(label)
                    {
                        Tag = code,
                        Checked = code == _localizationService.CurrentLanguage
                    };
                    item.Click += (_, _) => SetLanguage(code);
                    parent.DropDownItems.Add(item);
                }

                // 子项固定中文名称
                AddChinese("zh-CN", "简体中文（中国大陆）");
                AddChinese("zh-TW", "正體中文（臺灣）");
                AddChinese("zh-HK", "繁體中文（香港）");
                AddChinese("zh-MO", "繁體中文（澳門）");
                AddChinese("zh-SG", "简体中文（新加坡）");

                _miLanguage.DropDownItems.Add(parent);
            }
        }

        /// <summary>
        /// 在 RTL 环境中稳定显示语言名称，避免 BiDi 反转。
        /// LTR: 直接使用 JSON 中的文本。
        /// RTL: 期望显示为 "(本语言) 目标语言"，并使用双向控制符包裹两段文本以固定顺序。
        /// </summary>
        private static string FormatLanguageDisplay(string name, string currentLanguage)
        {
            // 当前语言是否为 RTL
            var isRtl = currentLanguage is "ar-SA";

            if (!isRtl)
            {
                return name;
            }

            // 双向控制符
            const char lre = '\u202A'; // Left-to-Right Embedding 从左到右嵌入
            const char rle = '\u202B'; // Right-to-Left Embedding 从右到左嵌入
            const char pdf = '\u202C'; // Pop Directional Formatting 弹出双向格式
            const char lrm = '\u200E'; // Left-to-Right Mark 从左到右标记

            // 将括号内作为 RTL 片段，目标语言作为 LTR 片段嵌入
            var mRtlPattern = RtlDisplayRegex().Match(name);
            if (mRtlPattern.Success)
            {
                // 直接内联分组值，避免与后续同名局部变量冲突
                return string.Concat(rle, '(', mRtlPattern.Groups[1].Value, ')', pdf, lrm, ' ', lre,
                    mRtlPattern.Groups[2].Value, pdf);
            }

            // 如果是 "目标语言 (本语言)" 形式（意外情况），转换并加控制符
            var mLtrPattern = LtrDisplayRegex().Match(name);
            if (!mLtrPattern.Success)
            {
                return name;
            }

            var nativePart = mLtrPattern.Groups[1].Value; // 目标语言
            var localPart = mLtrPattern.Groups[2].Value; // 本语言
            return string.Concat(rle, '(', localPart, ')', pdf, lrm, ' ', lre, nativePart, pdf);
        }

        /// <summary>
        /// 设置应用程序语言
        /// </summary>
        /// <param name="languageCode">语言代码</param>
        private void SetLanguage(string languageCode)
        {
            if (_localizationService.SetLanguage(languageCode))
            {
                _settings.Language = languageCode;
                SaveSettings();

                // 更新语言菜单的选中状态（递归处理子菜单）
                foreach (ToolStripMenuItem item in _miLanguage.DropDownItems)
                {
                    UpdateLanguageMenuChecked(item, languageCode);
                }

                // 更新所有UI文本
                UpdateAllTexts();
            }
        }

        private static bool IsEnglishTag(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var tag = code.Replace('_', '-').ToLowerInvariant();
            return tag.StartsWith("en-") || tag == "en";
        }

        private static bool IsChineseTag(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var tag = code.Replace('_', '-').ToLowerInvariant();
            return tag.StartsWith("zh-") || tag == "zh";
        }

        private static bool IsPortugueseTag(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            var tag = code.Replace('_', '-').ToLowerInvariant();
            return tag.StartsWith("pt-") || tag == "pt";
        }

        private void AddLanguageMenuItem(ToolStripItemCollection root, string lang)
        {
            var rawName = _localizationService.GetText($"Language.{lang}", lang);
            var langName = FormatLanguageDisplay(rawName, _localizationService.CurrentLanguage);
            var menuItem = new ToolStripMenuItem(langName)
            {
                Tag = lang,
                Checked = lang == _localizationService.CurrentLanguage
            };

            menuItem.Click += (sender, _) =>
            {
                if (sender is ToolStripMenuItem { Tag: string languageCode })
                {
                    SetLanguage(languageCode);
                }
            };

            root.Add(menuItem);
        }

        private static void UpdateLanguageMenuChecked(ToolStripMenuItem item, string languageCode)
        {
            if (item.DropDownItems.Count > 0)
            {
                bool anyChildChecked = false;
                foreach (ToolStripMenuItem child in item.DropDownItems)
                {
                    UpdateLanguageMenuChecked(child, languageCode);
                    if (child.Checked) anyChildChecked = true;
                }
                item.Checked = anyChildChecked || item.Tag?.ToString() == languageCode;
            }
            else
            {
                item.Checked = item.Tag?.ToString() == languageCode;
            }
        }

        /// <summary>
        /// 更新所有UI文本为当前语言
        /// </summary>
        private void UpdateAllTexts()
        {
            // 更新窗口标题
            Text = _localizationService.GetText("AppTitle", "游戏升级提醒");

            // 更新菜单文本
            UpdateMenuTexts();

            // 更新UI控件文本
            UpdateUiControlTexts();

            // 更新列表视图列标题
            UpdateListViewHeaders();

            // 应用 RTL 布局与列对齐（在列标题更新后执行）
            ApplyListViewRtlLayout();

            // 语言切换后刷新列表内容，以更新“持续时间”“重复”等列的本地化文本
            RefreshTable();

            // 更新状态栏
            UpdateStatusBar();

            // 更新托盘文本
            UpdateTrayTexts();

            // 重新构建语言菜单以更新语言名称
            BuildLanguageMenu();
        }

        /// <summary>
        /// 更新菜单文本
        /// </summary>
        private void UpdateMenuTexts()
        {
            _miSettings.Text = _localizationService.GetText("Menu.Settings", "设置(&S)");
            _miFont.Text = _localizationService.GetText("Menu.Font", "选择字体(&F)...");
            _miLanguage.Text = _localizationService.GetText("Menu.Language", "语言(&L)");
            _miAutoStart.Text = _localizationService.GetText("Menu.AutoStart", "开机自启(&A)");
            _miOpenConfig.Text = _localizationService.GetText("Menu.OpenConfig", "打开配置文件夹(&O)");
            _miOpenConfig.AccessibleName = _localizationService.GetText("Menu.OpenConfig.Name", "打开配置文件夹");
            _miOpenConfig.AccessibleDescription =
                _localizationService.GetText("Menu.OpenConfig.Description", "打开配置文件夹（Ctrl+O）");

            _miResetWindow.Text = _localizationService.GetText("Menu.ResetWindow", "重置窗口大小至默认(&Z)");
            _miResetWindow.AccessibleName = _localizationService.GetText("Menu.ResetWindow.Name", "重置窗口大小至默认");
            _miResetWindow.AccessibleDescription =
                _localizationService.GetText("Menu.ResetWindow.Description", "清除保存的窗口位置与大小，恢复默认布局");
            _miAutoDelete.Text = _localizationService.GetText("Menu.AutoDelete", "已完成任务自行删除(&D)");
            _miAdvanceNotify.Text = _localizationService.GetText("Menu.AdvanceNotify", "提前通知(&N)");
            _miCloseBehavior.Text = _localizationService.GetText("Menu.CloseBehavior", "关闭按钮行为(&C)");
            _miAboutTop.Text = _localizationService.GetText("Menu.About", "关于(&A)...");

            // 更新自动删除子菜单
            _miDelOff.Text = _localizationService.GetText("Menu.AutoDelete.Off", "关闭");
            _miDel30S.Text = _localizationService.GetText("Menu.AutoDelete.30s", "30秒");
            _miDel1M.Text = _localizationService.GetText("Menu.AutoDelete.1m", "1分钟");
            _miDel3M.Text = _localizationService.GetText("Menu.AutoDelete.3m", "3分钟");
            _miDel30M.Text = _localizationService.GetText("Menu.AutoDelete.30m", "30分钟");
            _miDel1H.Text = _localizationService.GetText("Menu.AutoDelete.1h", "1小时");
            _miDelCustom.Text = _localizationService.GetText("Menu.AutoDelete.Custom", "自定义...");

            // 更新提前通知子菜单
            _miAdvOff.Text = _localizationService.GetText("Menu.AdvanceNotify.Off", "关闭");
            _miAdvAlsoDue.Text = _localizationService.GetText("Menu.AdvanceNotify.AlsoDue", "同时准点通知");
            _miAdv30S.Text = _localizationService.GetText("Menu.AdvanceNotify.30s", "30秒");
            _miAdv1M.Text = _localizationService.GetText("Menu.AdvanceNotify.1m", "1分钟");
            _miAdv3M.Text = _localizationService.GetText("Menu.AdvanceNotify.3m", "3分钟");
            _miAdv30M.Text = _localizationService.GetText("Menu.AdvanceNotify.30m", "30分钟");
            _miAdv1H.Text = _localizationService.GetText("Menu.AdvanceNotify.1h", "1小时");
            _miAdvCustom.Text = _localizationService.GetText("Menu.AdvanceNotify.Custom", "自定义...");

            // 更新关闭行为子菜单
            _miCloseExit.Text = _localizationService.GetText("Menu.CloseBehavior.Exit", "退出程序");
            _miCloseMinimize.Text = _localizationService.GetText("Menu.CloseBehavior.Minimize", "最小化到托盘");
        }

        /// <summary>
        /// 更新UI控件文本
        /// </summary>
        private void UpdateUiControlTexts()
        {
            // 更新控件文本和可访问性属性
            // 左侧说明标签
            _lbAccount.Text = _localizationService.GetText("Control.Account.Name", "账号");
            _lbTask.Text = _localizationService.GetText("Control.Task.Name", "任务");
            _lbStartTime.Text = _localizationService.GetText("UI.StartTime", "开始时间");
            _lbDays.Text = _localizationService.GetText("UI.Days", "天");
            _lbHours.Text = _localizationService.GetText("UI.Hours", "小时");
            _lbMinutes.Text = _localizationService.GetText("UI.Minutes", "分钟");
            _lbFinishTime.Text = _localizationService.GetText("UI.FinishTime", "完成时间");

            _cbAccount.AccessibleName = _localizationService.GetText("Control.Account.Name", "账号");
            _cbAccount.AccessibleDescription = _localizationService.GetText("Control.Account.Description", "选择账号");

            _btnAccountMgr.Text = _localizationService.GetText("Control.AccountMgr.Text", "账号管理(&M)");
            _btnAccountMgr.AccessibleName = _localizationService.GetText("Control.AccountMgr.Name", "账号管理");
            _btnAccountMgr.AccessibleDescription =
                _localizationService.GetText("Control.AccountMgr.Description", "打开账号管理");

            _cbTask.AccessibleName = _localizationService.GetText("Control.Task.Name", "任务");
            _cbTask.AccessibleDescription = _localizationService.GetText("Control.Task.Description", "输入或选择任务");

            _btnTaskMgr.Text = _localizationService.GetText("Control.TaskMgr.Text", "任务管理(&K)");
            _btnTaskMgr.AccessibleName = _localizationService.GetText("Control.TaskMgr.Name", "任务管理");
            _btnTaskMgr.AccessibleDescription = _localizationService.GetText("Control.TaskMgr.Description", "打开任务管理");

            _btnDeleteDone.Text = _localizationService.GetText("Control.DeleteDone.Text", "删除已完成(&D)");
            _btnDeleteDone.AccessibleName = _localizationService.GetText("Control.DeleteDone.Name", "删除已完成");
            _btnDeleteDone.AccessibleDescription =
                _localizationService.GetText("Control.DeleteDone.Description", "删除已完成的任务（Ctrl+Shift+D）");

            _btnRefresh.Text = _localizationService.GetText("Control.Refresh.Text", "刷新(&R)");
            _btnRefresh.AccessibleName = _localizationService.GetText("Control.Refresh.Name", "刷新");
            _btnRefresh.AccessibleDescription = _localizationService.GetText("Control.Refresh.Description", "刷新列表（F5）");

            _btnNow.Text = _localizationService.GetText("Control.Now.Text", "当前时间(&T)");
            _btnNow.AccessibleName = _localizationService.GetText("Control.Now.Name", "当前时间");
            _btnNow.AccessibleDescription = _localizationService.GetText("Control.Now.Description", "将开始时间设置为当前时间");

            _numDays.AccessibleName = _localizationService.GetText("Control.Days.Name", "天");
            _numDays.AccessibleDescription = _localizationService.GetText("Control.Days.Description", "持续时间（天）");

            _numHours.AccessibleName = _localizationService.GetText("Control.Hours.Name", "小时");
            _numHours.AccessibleDescription = _localizationService.GetText("Control.Hours.Description", "持续时间（小时）");

            _numMinutes.AccessibleName = _localizationService.GetText("Control.Minutes.Name", "分钟");
            _numMinutes.AccessibleDescription = _localizationService.GetText("Control.Minutes.Description", "持续时间（分钟）");

            _tbFinish.AccessibleName = _localizationService.GetText("Control.Finish.Name", "完成时间");
            _tbFinish.AccessibleDescription =
                _localizationService.GetText("Control.Finish.Description", "根据开始时间与持续时间计算的完成时间");

            _btnAddSave.Text = _localizationService.GetText("Control.AddSave.Text", "添加(&N)");
            _btnAddSave.AccessibleName = _localizationService.GetText("Control.AddSave.Name", "添加");
            _btnAddSave.AccessibleDescription =
                _localizationService.GetText("Control.AddSave.Description", "添加/保存任务（Ctrl+N）");

            _btnRepeat.Text = _localizationService.GetText("Control.Repeat.Text", "重复(&P)");
            _btnRepeat.AccessibleName = _localizationService.GetText("Control.Repeat.Name", "重复设置");
            _btnRepeat.AccessibleDescription = _localizationService.GetText("Control.Repeat.Description", "配置重复提醒设置");

            _btnClear.Text = _localizationService.GetText("Control.Clear.Text", "清除(&C)");
            _btnClear.AccessibleName = _localizationService.GetText("Control.Clear.Name", "清除设置");
            _btnClear.AccessibleDescription =
                _localizationService.GetText("Control.Clear.Description", "将开始时间清空，且清空重复任务设置");
        }

        /// <summary>
        /// 更新列表视图列标题
        /// </summary>
        private void UpdateListViewHeaders()
        {
            if (_listView.Columns.Count >= 9)
            {
                _listView.Columns[0].Text = _localizationService.GetText("ListView.Column.Account", "账号");
                _listView.Columns[1].Text = _localizationService.GetText("ListView.Column.Task", "任务");
                _listView.Columns[2].Text = _localizationService.GetText("ListView.Column.StartTime", "开始时间");
                _listView.Columns[3].Text = _localizationService.GetText("ListView.Column.Duration", "持续时间");
                _listView.Columns[4].Text = _localizationService.GetText("ListView.Column.FinishTime", "完成时间");
                _listView.Columns[5].Text = _localizationService.GetText("ListView.Column.RemainingTime", "剩余时间");
                _listView.Columns[6].Text = _localizationService.GetText("ListView.Column.Repeat", "重复");
                _listView.Columns[7].Text = _localizationService.GetText("ListView.Column.Complete", "完成");
                _listView.Columns[8].Text = _localizationService.GetText("ListView.Column.Delete", "删除");
            }
        }

        /// <summary>
        /// 更新托盘相关文本
        /// </summary>
        private void UpdateTrayTexts()
        {
            // 更新托盘标题
            _tray.Text = _localizationService.GetText("Tray.Title", "升级提醒");

            // 重新构建托盘菜单以更新菜单项文本
            RebuildTrayMenu();

            // 应用 RTL 布局到托盘菜单
            ApplyTrayRtlLayout();
        }

        /// <summary>
        /// 初始化本地化设置
        /// </summary>
        private void InitializeLocalization()
        {
            // 初始化时长格式化器
            _durationFormatter = new LocalizedDurationFormatter(_localizationService);

            // 从设置中加载语言
            if (!string.IsNullOrEmpty(_settings.Language))
            {
                _localizationService.SetLanguage(_settings.Language);
            }


            // 初始化时更新所有文本
            UpdateAllTexts();
        }

        [GeneratedRegex(@"^\((.+?)\)\s+(.*)$", RegexOptions.CultureInvariant)]
        private static partial Regex RtlDisplayRegex();

        [GeneratedRegex(@"^(.*?)\s+\((.+)\)$", RegexOptions.CultureInvariant)]
        private static partial Regex LtrDisplayRegex();
    }
}