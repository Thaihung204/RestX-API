using QuestPDF.Infrastructure;

namespace RestX.BLL.Services.Reports
{
    internal static class ReportStyles
    {
        // Brand palette
        public static readonly string PrimaryDark = "#1A2B4A";    // deep navy — header bg
        public static readonly string PrimaryMid = "#2B4C8C";     // medium blue — section titles
        public static readonly string AccentGold = "#C9A84C";     // gold — KPI highlight
        public static readonly string AccentGreen = "#2E7D52";    // green — positive change
        public static readonly string AccentRed = "#C0392B";      // red — negative change
        public static readonly string SurfaceLight = "#F4F6FA";   // light grey — alternating row
        public static readonly string SurfaceWhite = "#FFFFFF";
        public static readonly string BorderColor = "#D0D7E6";
        public static readonly string TextPrimary = "#1A2B4A";
        public static readonly string TextMuted = "#6B7A99";

        // Font sizes
        public const float FontTitle = 22f;
        public const float FontSectionHeader = 13f;
        public const float FontBody = 9.5f;
        public const float FontSmall = 8f;
        public const float FontKpiValue = 18f;
        public const float FontKpiLabel = 8f;

        // Spacing
        public const float RowPaddingV = 5f;
        public const float SectionSpacing = 16f;
        public const float CellPaddingH = 8f;

        public static string FormatCurrency(decimal amount) =>
            string.Format("{0:N0} ₫", amount);

        public static string FormatPercent(double value)
        {
            var sign = value >= 0 ? "▲" : "▼";
            return $"{sign} {Math.Abs(value):N1}%";
        }

        public static string PercentColor(double value) =>
            value >= 0 ? AccentGreen : AccentRed;

        public static string TrendBar(decimal value, decimal max, int maxBars = 12)
        {
            if (max == 0) return string.Empty;
            var filled = (int)Math.Round((double)value / (double)max * maxBars);
            filled = Math.Clamp(filled, 0, maxBars);
            return new string('█', filled) + new string('░', maxBars - filled);
        }
    }
}
