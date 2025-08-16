/*
 * 游戏升级提醒 - 字体规范模型
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义应用程序中使用的字体规范
 * 创建日期: 2025-08-14
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU Affero 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

using System.Drawing;

namespace Game_Upgrade_Reminder.Models
{
    public class FontSpec
    {
        public string Family { get; init; } = "Microsoft YaHei UI";
        public float Size { get; init; } = 9f;
        public bool Bold { get; init; }
        public bool Italic { get; init; }

        public Font ToFont()
        {
            var style = FontStyle.Regular;
            if (Bold) style |= FontStyle.Bold;
            if (Italic) style |= FontStyle.Italic;
            return new Font(Family, Size, style);
        }

        public static FontSpec From(Font font)
        {
            return new FontSpec
            {
                Family = font.FontFamily.Name,
                Size = font.Size,
                Bold = font.Bold,
                Italic = font.Italic
            };
        }
    }
}