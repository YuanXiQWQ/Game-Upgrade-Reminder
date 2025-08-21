/*
 * 游戏升级提醒 - 字体规范模型
 * 作者: YuanXiQWQ
 * 项目地址: https://github.com/YuanXiQWQ/Game-Upgrade-Reminder
 * 描述: 定义应用程序中使用的字体规范
 * 创建日期: 2025-08-14
 * 最后修改: 2025-08-15
 *
 * 版权所有 (C) 2025 YuanXiQWQ
 * 根据 GNU 通用公共许可证 (AGPL-3.0) 授权
 * 详情请参阅: https://www.gnu.org/licenses/agpl-3.0.html
 */

namespace Game_Upgrade_Reminder.Core.Models
{
    /// <summary>
    /// 表示字体的规格，用于序列化和反序列化字体设置
    /// </summary>
    public class FontSpec
    {
        /// <summary>
        /// 获取或设置字体系列名称
        /// </summary>
        /// <value>默认为"Microsoft YaHei UI"</value>
        public string Family { get; init; } = "Microsoft YaHei UI";

        /// <summary>
        /// 获取或设置字体大小（以磅为单位）
        /// </summary>
        /// <value>默认值为9.0f</value>
        public float Size { get; init; } = 9f;

        /// <summary>
        /// 获取或设置一个值，指示字体是否为粗体
        /// </summary>
        public bool Bold { get; init; }

        /// <summary>
        /// 获取或设置一个值，指示字体是否为斜体
        /// </summary>
        public bool Italic { get; init; }

        /// <summary>
        /// 将当前字体规格转换为<see cref="Font"/>对象
        /// </summary>
        /// <returns>根据当前规格创建的<see cref="Font"/>对象</returns>
        /// <remarks>
        /// 此方法会创建一个新的<see cref="Font"/>实例。
        /// 调用者负责在不再需要时释放返回的<see cref="Font"/>对象。
        /// </remarks>
        public Font ToFont()
        {
            var style = FontStyle.Regular;
            if (Bold) style |= FontStyle.Bold;
            if (Italic) style |= FontStyle.Italic;
            return new Font(Family, Size, style);
        }

        /// <summary>
        /// 从现有的<see cref="Font"/>对象创建<see cref="FontSpec"/>实例
        /// </summary>
        /// <param name="font">源字体对象</param>
        /// <returns>包含字体规格的新<see cref="FontSpec"/>实例</returns>
        /// <exception cref="ArgumentNullException">当font参数为null时抛出</exception>
        public static FontSpec From(Font font)
        {
            ArgumentNullException.ThrowIfNull(font);

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