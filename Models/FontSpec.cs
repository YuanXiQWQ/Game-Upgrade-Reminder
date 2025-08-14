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