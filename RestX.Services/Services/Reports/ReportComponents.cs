using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestX.BLL.DataTranferObjects.Dashboard;
using RestX.BLL.DataTranferObjects.Reports;

namespace RestX.BLL.Services.Reports
{
    internal static class ReportComponents
    {
        public static void RenderHeader(ColumnDescriptor col, ReportData data)
        {
            col.Item().Background(ReportStyles.PrimaryDark).Padding(24).Column(inner =>
            {
                inner.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(data.TenantName)
                            .FontSize(ReportStyles.FontTitle).Bold().FontColor(Colors.White);

                        if (!string.IsNullOrEmpty(data.TenantAddress))
                            left.Item().PaddingTop(2).Text(data.TenantAddress)
                                .FontSize(ReportStyles.FontSmall).FontColor("#A8B4CC");

                        if (!string.IsNullOrEmpty(data.TenantPhone))
                            left.Item().Text($"Tel: {data.TenantPhone}")
                                .FontSize(ReportStyles.FontSmall).FontColor("#A8B4CC");
                    });

                    row.ConstantItem(180).AlignRight().Column(right =>
                    {
                        right.Item().AlignRight().Text(ReportTypeLabel(data.ReportType))
                            .FontSize(14).Bold().FontColor(ReportStyles.AccentGold);
                        right.Item().AlignRight().Text(data.PeriodLabel)
                            .FontSize(ReportStyles.FontSmall).FontColor("#A8B4CC");
                        right.Item().PaddingTop(4).AlignRight()
                            .Text($"Xuất lúc: {data.GeneratedAt:dd/MM/yyyy HH:mm}")
                            .FontSize(ReportStyles.FontSmall).FontColor("#A8B4CC");
                    });
                });
            });
        }
        public static void SectionTitle(ColumnDescriptor col, string title, string? subtitle = null)
        {
            col.Item().PaddingTop(ReportStyles.SectionSpacing).Row(row =>
            {
                row.ConstantItem(4).Background(ReportStyles.AccentGold).MinHeight(18);
                row.RelativeItem().PaddingLeft(8).Text(title)
                    .FontSize(ReportStyles.FontSectionHeader).Bold()
                    .FontColor(ReportStyles.PrimaryMid);
            });

            if (!string.IsNullOrEmpty(subtitle))
                col.Item().PaddingLeft(12).PaddingTop(2).Text(subtitle)
                    .FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted);

            col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(ReportStyles.BorderColor);
        }
        public static void RenderKpiCards(ColumnDescriptor col, DashboardSummary summary)
        {
            col.Item().PaddingTop(12).Row(row =>
            {
                KpiCard(row, "DOANH THU",
                    ReportStyles.FormatCurrency(summary.Revenue.Total),
                    summary.Revenue.ChangePercent, ReportStyles.AccentGold);
                row.ConstantItem(8);
                KpiCard(row, "ĐƠN HÀNG", summary.Orders.Total.ToString("N0"), 0, ReportStyles.PrimaryMid);
                row.ConstantItem(8);
                KpiCard(row, "ĐẶT BÀN", summary.Reservations.Total.ToString("N0"), 0, ReportStyles.PrimaryMid);
                row.ConstantItem(8);
                KpiCard(row, "KHÁCH MỚI",
                    summary.NewCustomers.Total.ToString("N0"),
                    summary.NewCustomers.ChangePercent, ReportStyles.AccentGreen);
            });
        }

        private static void KpiCard(RowDescriptor row, string label, string value, double changePercent, string accentColor)
        {
            row.RelativeItem()
                .Border(0.5f).BorderColor(ReportStyles.BorderColor)
                .Background(ReportStyles.SurfaceLight)
                .Padding(12).Column(card =>
                {
                    card.Item().Text(label)
                        .FontSize(ReportStyles.FontKpiLabel).Bold()
                        .FontColor(ReportStyles.TextMuted);

                    card.Item().PaddingTop(4).Text(value)
                        .FontSize(ReportStyles.FontKpiValue).Bold()
                        .FontColor(accentColor);

                    if (changePercent != 0)
                        card.Item().PaddingTop(2)
                            .Text(ReportStyles.FormatPercent(changePercent))
                            .FontSize(ReportStyles.FontSmall)
                            .FontColor(ReportStyles.PercentColor(changePercent));
                    else
                        card.Item().PaddingTop(2).Text("vs kỳ trước")
                            .FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted);
                });
        }
        public static void RenderRevenueBreakdown(ColumnDescriptor col, DashboardSummary summary)
        {
            var aov = summary.Orders.Completed > 0
                ? ReportStyles.FormatCurrency(summary.Revenue.Total / summary.Orders.Completed)
                : "—";

            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(3);
                    cols.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "CHỈ SỐ");
                    HeaderCell(header, "GIÁ TRỊ");
                });

                var rows = new (string label, string value)[]
                {
                    ("Tổng doanh thu (thực thu)", ReportStyles.FormatCurrency(summary.Revenue.Total)),
                    ("Đơn hoàn thành", summary.Orders.Completed.ToString("N0") + " đơn"),
                    ("Đơn huỷ", summary.Orders.Cancelled.ToString("N0") + " đơn"),
                    ("Giá trị đơn trung bình (AOV)", aov),
                    ("Đặt bàn xác nhận", summary.Reservations.Confirmed.ToString("N0")),
                    ("Đặt bàn huỷ", summary.Reservations.Cancelled.ToString("N0")),
                    ("Khách mới", summary.NewCustomers.Total.ToString("N0")),
                };

                for (int i = 0; i < rows.Length; i++)
                {
                    var bg = i % 2 == 0 ? ReportStyles.SurfaceWhite : ReportStyles.SurfaceLight;
                    BodyCell(table, rows[i].label, bg, false);
                    BodyCell(table, rows[i].value, bg, true);
                }
            });
        }
        public static void RenderRevenueTrendTable(ColumnDescriptor col, RevenueTrend trend)
        {
            if (!trend.RevenueTrends.Any()) return;

            var points = trend.RevenueTrends;
            var total = points.Sum(x => x.Value);
            var maxVal = points.Max(x => x.Value);

            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(52); 
                    cols.ConstantColumn(110);
                    cols.ConstantColumn(55);  
                    cols.ConstantColumn(65); 
                    cols.RelativeColumn(); 
                });

                table.Header(header =>
                {
                    HeaderCell(header, "KỲ");
                    HeaderCell(header, "DOANH THU");
                    HeaderCell(header, "TỶ TRỌNG");
                    HeaderCell(header, "SO KỲ TRƯỚC");
                    HeaderCell(header, "GHI CHÚ");
                });

                var minVal = points.Where(x => x.Value > 0).Select(x => x.Value).DefaultIfEmpty(0).Min();

                for (int i = 0; i < points.Count; i++)
                {
                    var point = points[i];
                    var bg = i % 2 == 0 ? ReportStyles.SurfaceWhite : ReportStyles.SurfaceLight;
                    var sharePct = total > 0 ? point.Value / total * 100 : 0m;

                    decimal? prevVal = i > 0 ? points[i - 1].Value : null;
                    double changePct = (prevVal.HasValue && prevVal.Value > 0)
                        ? (double)((point.Value - prevVal.Value) / prevVal.Value * 100) : 0;
                    bool isMax = point.Value == maxVal && maxVal > 0;
                    bool isMin = point.Value == minVal && minVal > 0 && points.Count > 1;

                    string note = isMax ? "★ Cao nhất" : isMin ? "▼ Thấp nhất" : "";
                    string noteColor = isMax ? ReportStyles.AccentGold : ReportStyles.AccentRed;

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH)
                        .Text(point.Label).FontSize(ReportStyles.FontBody).Bold()
                        .FontColor(ReportStyles.TextPrimary);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text(point.Value > 0 ? ReportStyles.FormatCurrency(point.Value) : "—")
                        .FontSize(ReportStyles.FontBody).Bold()
                        .FontColor(isMax ? ReportStyles.AccentGold : ReportStyles.TextPrimary);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text(point.Value > 0 ? $"{sharePct:N1}%" : "—")
                        .FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextMuted);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text(prevVal.HasValue && prevVal.Value > 0 ? ReportStyles.FormatPercent(changePct) : "—")
                        .FontSize(ReportStyles.FontBody)
                        .FontColor(prevVal.HasValue && prevVal.Value > 0
                            ? ReportStyles.PercentColor(changePct) : ReportStyles.TextMuted);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH)
                        .Text(note).FontSize(ReportStyles.FontSmall).Bold()
                        .FontColor(string.IsNullOrEmpty(note) ? ReportStyles.TextMuted : noteColor);
                }
            });

            if (points.Count > 1)
            {
                var avg = total / points.Count;
                col.Item().PaddingTop(5).PaddingHorizontal(4).Row(row =>
                {
                    row.RelativeItem()
                        .Text($"Tổng cộng: {ReportStyles.FormatCurrency(total)}   |   Trung bình / kỳ: {ReportStyles.FormatCurrency(avg)}")
                        .FontSize(ReportStyles.FontSmall).Italic().FontColor(ReportStyles.TextMuted);
                });
            }
        }
        public static void RenderOrderTrendTable(ColumnDescriptor col, OrderTrend trend)
        {
            if (!trend.OrderTrends.Any()) return;

            var points = trend.OrderTrends;
            var totalOrders = points.Sum(x => x.Total);
            var maxVal = points.Max(x => x.Total);

            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(52);   // KỲ
                    cols.ConstantColumn(80);   // ĐƠN HÀNG
                    cols.ConstantColumn(55);   // TỶ TRỌNG
                    cols.ConstantColumn(65);   // SO KỲ TRƯỚC
                    cols.RelativeColumn();     // GHI CHÚ
                });

                table.Header(header =>
                {
                    HeaderCell(header, "KỲ");
                    HeaderCell(header, "ĐƠN HÀNG");
                    HeaderCell(header, "TỶ TRỌNG");
                    HeaderCell(header, "SO KỲ TRƯỚC");
                    HeaderCell(header, "GHI CHÚ");
                });

                var minVal = points.Where(x => x.Total > 0).Select(x => x.Total).DefaultIfEmpty(0).Min();

                for (int i = 0; i < points.Count; i++)
                {
                    var point = points[i];
                    var bg = i % 2 == 0 ? ReportStyles.SurfaceWhite : ReportStyles.SurfaceLight;
                    var sharePct = totalOrders > 0 ? (decimal)point.Total / totalOrders * 100 : 0m;

                    int? prev = i > 0 ? points[i - 1].Total : null;
                    double changePct = (prev.HasValue && prev.Value > 0)
                        ? (double)((decimal)(point.Total - prev.Value) / prev.Value * 100) : 0;
                    bool isMax = point.Total == maxVal && maxVal > 0;
                    bool isMin = point.Total == minVal && minVal > 0 && points.Count > 1;

                    string note = isMax ? "★ Cao nhất" : isMin ? "▼ Thấp nhất" : "";
                    string noteColor = isMax ? ReportStyles.AccentGold : ReportStyles.AccentRed;

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH)
                        .Text(point.Label).FontSize(ReportStyles.FontBody).Bold()
                        .FontColor(ReportStyles.TextPrimary);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text(point.Total > 0 ? point.Total.ToString("N0") : "—")
                        .FontSize(ReportStyles.FontBody).Bold()
                        .FontColor(isMax ? ReportStyles.AccentGold : ReportStyles.TextPrimary);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text(point.Total > 0 ? $"{sharePct:N1}%" : "—")
                        .FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextMuted);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text(prev.HasValue && prev.Value > 0 ? ReportStyles.FormatPercent(changePct) : "—")
                        .FontSize(ReportStyles.FontBody)
                        .FontColor(prev.HasValue && prev.Value > 0
                            ? ReportStyles.PercentColor(changePct) : ReportStyles.TextMuted);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH)
                        .Text(note).FontSize(ReportStyles.FontSmall).Bold()
                        .FontColor(string.IsNullOrEmpty(note) ? ReportStyles.TextMuted : noteColor);
                }
            });

            if (points.Count > 1)
            {
                var avg = totalOrders > 0 ? (decimal)totalOrders / points.Count : 0m;
                col.Item().PaddingTop(5).PaddingHorizontal(4).Row(row =>
                {
                    row.RelativeItem()
                        .Text($"Tổng cộng: {totalOrders:N0} đơn   |   Trung bình / kỳ: {avg:N1} đơn")
                        .FontSize(ReportStyles.FontSmall).Italic().FontColor(ReportStyles.TextMuted);
                });
            }
        }

        public static void RenderTopDishes(ColumnDescriptor col, TopDish topDish)
        {
            if (!topDish.Dishes.Any()) return;

            var totalRevenue = topDish.Dishes.Sum(d => d.Revenue);

            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(28);
                    cols.RelativeColumn();
                    cols.ConstantColumn(55);
                    cols.ConstantColumn(120);
                    cols.ConstantColumn(55);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "#");
                    HeaderCell(header, "MÓN ĂN");
                    HeaderCell(header, "SL BÁN");
                    HeaderCell(header, "DOANH THU");
                    HeaderCell(header, "% TỔNG");
                });

                for (int i = 0; i < topDish.Dishes.Count; i++)
                {
                    var dish = topDish.Dishes[i];
                    var bg = i % 2 == 0 ? ReportStyles.SurfaceWhite : ReportStyles.SurfaceLight;
                    var pct = totalRevenue > 0 ? dish.Revenue / totalRevenue * 100 : 0;
                    var rankColor = i == 0 ? ReportStyles.AccentGold : ReportStyles.TextPrimary;

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignCenter()
                        .Text((i + 1).ToString()).FontSize(ReportStyles.FontBody).Bold().FontColor(rankColor);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH)
                        .Text(dish.Name).FontSize(ReportStyles.FontBody);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignCenter()
                        .Text(dish.Quantity.ToString("N0")).FontSize(ReportStyles.FontBody);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text(ReportStyles.FormatCurrency(dish.Revenue)).FontSize(ReportStyles.FontBody).Bold();

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text($"{pct:N1}%").FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextMuted);
                }
            });
        }

        public static void RenderCustomerStats(ColumnDescriptor col, CustomerStats stats)
        {
            col.Item().PaddingTop(10).Row(row =>
            {
                StatBox(row, "Khách mới", stats.NewCustomers.ToString("N0"), ReportStyles.AccentGreen);
                row.ConstantItem(8);
                StatBox(row, "Khách quay lại", stats.ReturningCustomers.ToString("N0"), ReportStyles.PrimaryMid);
                row.ConstantItem(8);
                StatBox(row, "Tổng đơn", stats.TotalOrders.ToString("N0"), ReportStyles.PrimaryMid);
                row.ConstantItem(8);
                StatBox(row, "DT TB / khách",
                    stats.AverageRevenuePerCustomer > 0 ? ReportStyles.FormatCurrency(stats.AverageRevenuePerCustomer) : "—",
                    ReportStyles.AccentGold);
            });

            if (!stats.TopCustomers.Any()) return;

            col.Item().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(28);
                    cols.RelativeColumn();
                    cols.ConstantColumn(70);
                    cols.ConstantColumn(60);
                    cols.ConstantColumn(120);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "#");
                    HeaderCell(header, "KHÁCH HÀNG");
                    HeaderCell(header, "HẠNG");
                    HeaderCell(header, "ĐIỂM");
                    HeaderCell(header, "CHI TIÊU");
                });

                for (int i = 0; i < stats.TopCustomers.Count; i++)
                {
                    var c = stats.TopCustomers[i];
                    var bg = i % 2 == 0 ? ReportStyles.SurfaceWhite : ReportStyles.SurfaceLight;
                    var rankColor = i == 0 ? ReportStyles.AccentGold : ReportStyles.TextPrimary;

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignCenter()
                        .Text(c.Rank.ToString()).FontSize(ReportStyles.FontBody).Bold().FontColor(rankColor);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH)
                        .Text(c.CustomerName ?? "—").FontSize(ReportStyles.FontBody);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignCenter()
                        .Text(c.MembershipLevel ?? "—").FontSize(ReportStyles.FontSmall)
                        .FontColor(MembershipColor(c.MembershipLevel));

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignCenter()
                        .Text(c.LoyaltyPoints.ToString("N0")).FontSize(ReportStyles.FontBody);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text(ReportStyles.FormatCurrency(c.TotalSpent)).FontSize(ReportStyles.FontBody).Bold();
                }
            });
        }

        public static void RenderPromotionStats(ColumnDescriptor col, PromotionStats stats)
        {
            col.Item().PaddingTop(10).Row(row =>
            {
                StatBox(row, "Tổng lần dùng", stats.TotalUsageCount.ToString("N0"), ReportStyles.PrimaryMid);
                row.ConstantItem(8);
                StatBox(row, "Tổng giảm giá", ReportStyles.FormatCurrency(stats.TotalDiscountAmount), ReportStyles.AccentRed);
            });

            if (!stats.TopPromotions.Any()) return;

            col.Item().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(80);
                    cols.RelativeColumn();
                    cols.ConstantColumn(70);
                    cols.ConstantColumn(120);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "MÃ");
                    HeaderCell(header, "TÊN KHUYẾN MÃI");
                    HeaderCell(header, "SỐ LẦN");
                    HeaderCell(header, "TỔNG GIẢM");
                });

                for (int i = 0; i < stats.TopPromotions.Count; i++)
                {
                    var p = stats.TopPromotions[i];
                    var bg = i % 2 == 0 ? ReportStyles.SurfaceWhite : ReportStyles.SurfaceLight;

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH)
                        .Text(p.PromotionCode).FontSize(ReportStyles.FontBody).Bold().FontColor(ReportStyles.PrimaryMid);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH)
                        .Text(p.PromotionName).FontSize(ReportStyles.FontBody);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignCenter()
                        .Text(p.UsageCount.ToString("N0")).FontSize(ReportStyles.FontBody);

                    table.Cell().Background(bg).PaddingVertical(ReportStyles.RowPaddingV)
                        .PaddingHorizontal(ReportStyles.CellPaddingH).AlignRight()
                        .Text(ReportStyles.FormatCurrency(p.TotalDiscount)).FontSize(ReportStyles.FontBody).Bold()
                        .FontColor(ReportStyles.AccentRed);
                }
            });
        }

        public static void RenderFooter(PageDescriptor page, ReportData data)
        {
            page.Footer()
                .BorderTop(0.5f).BorderColor(ReportStyles.BorderColor)
                .PaddingVertical(6).Row(row =>
                {
                    row.RelativeItem().Text(data.TenantName)
                        .FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted);

                    row.RelativeItem().AlignCenter().Text(data.PeriodLabel)
                        .FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted);

                    row.RelativeItem().AlignRight().Text(txt =>
                    {
                        txt.Span("Trang ").FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted);
                        txt.CurrentPageNumber().FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted);
                        txt.Span(" / ").FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted);
                        txt.TotalPages().FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted);
                    });
                });
        }


        internal static void HeaderCell(TableCellDescriptor h, string text)
        {
            h.Cell().Background(ReportStyles.PrimaryDark)
                .PaddingVertical(7).PaddingHorizontal(ReportStyles.CellPaddingH)
                .Text(text).FontSize(ReportStyles.FontSmall).Bold().FontColor(Colors.White);
        }

        private static void BodyCell(TableDescriptor table, string text, string bg, bool bold)
        {
            var cell = table.Cell().Background(bg)
                .PaddingVertical(ReportStyles.RowPaddingV)
                .PaddingHorizontal(ReportStyles.CellPaddingH);
            if (bold)
                cell.AlignRight().Text(text).FontSize(ReportStyles.FontBody).Bold();
            else
                cell.Text(text).FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextPrimary);
        }

        private static void StatBox(RowDescriptor row, string label, string value, string color)
        {
            row.RelativeItem()
                .Border(0.5f).BorderColor(ReportStyles.BorderColor)
                .Background(ReportStyles.SurfaceLight).Padding(10).Column(c =>
                {
                    c.Item().Text(label).FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted).Bold();
                    c.Item().PaddingTop(3).Text(value).FontSize(12).Bold().FontColor(color);
                });
        }

        private static string MembershipColor(string? level) => level?.ToUpper() switch
        {
            "GOLD" => "#C9A84C",
            "SILVER" => "#8C9DB5",
            _ => ReportStyles.TextMuted
        };

        private static string ReportTypeLabel(string type) => type.ToLower() switch
        {
            "weekly" => "BÁO CÁO TUẦN",
            "monthly" => "BÁO CÁO THÁNG",
            "quarterly" => "BÁO CÁO QUÝ",
            "yearly" => "BÁO CÁO NĂM",
            _ => "BÁO CÁO"
        };
    }
}
