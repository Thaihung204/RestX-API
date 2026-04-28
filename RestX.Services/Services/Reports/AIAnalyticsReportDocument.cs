using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestX.BLL.DataTranferObjects.AI;

namespace RestX.BLL.Services.Reports
{
    internal class AIAnalyticsReportDocument : IDocument
    {
        private readonly AIAnalyticsResponse _data;
        private readonly string _tenantName;
        private readonly string _filterType;

        public AIAnalyticsReportDocument(AIAnalyticsResponse data, string tenantName, string filterType)
        {
            _data = data;
            _tenantName = tenantName;
            _filterType = filterType;
        }

        public DocumentMetadata GetMetadata() => new()
        {
            Title = $"Báo cáo AI — {_tenantName}",
            Author = _tenantName,
            CreationDate = DateTime.Now
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextPrimary));

                page.Header().Element(ComposeHeader);
                page.Content().PaddingTop(12).Column(col =>
                {
                    ComposeSummary(col);
                    ComposeInsights(col);
                    ComposeTopDishes(col);
                    ComposeSuggestedDishes(col);
                    ComposeCombos(col);
                    ComposeCustomers(col);
                });
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer c)
        {
            c.Background(ReportStyles.PrimaryDark).Padding(20).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text(_tenantName)
                        .FontSize(ReportStyles.FontTitle).Bold().FontColor(Colors.White);
                    left.Item().PaddingTop(2).Text("Báo cáo phân tích AI")
                        .FontSize(ReportStyles.FontSmall).FontColor("#A8B4CC");
                });
                row.ConstantItem(200).AlignRight().Column(right =>
                {
                    right.Item().AlignRight().Text(FilterLabel())
                        .FontSize(13).Bold().FontColor(ReportStyles.AccentGold);
                    right.Item().PaddingTop(4).AlignRight()
                        .Text($"Xuất lúc: {_data.GeneratedAt:dd/MM/yyyy HH:mm}")
                        .FontSize(ReportStyles.FontSmall).FontColor("#A8B4CC");
                });
            });
        }

        private void ComposeSummary(ColumnDescriptor col)
        {
            if (string.IsNullOrWhiteSpace(_data.Summary)) return;

            col.Item().PaddingTop(14)
                .Border(0.5f).BorderColor(ReportStyles.AccentGold)
                .Background("#FFFBF0").Padding(14).Column(inner =>
                {
                    inner.Item().Text("TỔNG QUAN").FontSize(10).Bold().FontColor(ReportStyles.AccentGold);
                    inner.Item().PaddingTop(6).Text(_data.Summary)
                        .FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextPrimary).Italic();
                });
        }

        private void ComposeInsights(ColumnDescriptor col)
        {
            if (!_data.Insights.Any()) return;

            SectionTitle(col, "INSIGHTS PHÂN TÍCH");

            foreach (var insight in _data.Insights)
            {
                var (bgColor, tagColor, tag) = insight.Category?.ToLower() switch
                {
                    "opportunity" => ("#F0FFF4", ReportStyles.AccentGreen, "CƠ HỘI"),
                    "risk"        => ("#FFF5F5", ReportStyles.AccentRed, "RỦI RO"),
                    _             => ("#F0F4FF", ReportStyles.PrimaryMid, "MARKETING")
                };

                col.Item().PaddingTop(8)
                    .Border(0.5f).BorderColor(ReportStyles.BorderColor)
                    .Background(bgColor).Padding(12).Column(inner =>
                    {
                        inner.Item().Row(row =>
                        {
                            row.ConstantItem(70).Background(tagColor).AlignCenter()
                                .PaddingVertical(2).Text(tag)
                                .FontSize(7).Bold().FontColor(Colors.White);
                            row.ConstantItem(8);
                            row.RelativeItem().Text(insight.Title)
                                .FontSize(10).Bold().FontColor(ReportStyles.TextPrimary);
                            if (!string.IsNullOrEmpty(insight.Impact))
                                row.ConstantItem(50).AlignRight().Text(insight.Impact.ToUpper())
                                    .FontSize(7).Bold()
                                    .FontColor(insight.Impact == "high" ? ReportStyles.AccentRed : ReportStyles.AccentGold);
                        });

                        if (!string.IsNullOrEmpty(insight.Evidence))
                            inner.Item().PaddingTop(6).Text($"Dẫn chứng: {insight.Evidence}")
                                .FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted).Italic();

                        if (!string.IsNullOrEmpty(insight.Analysis))
                            inner.Item().PaddingTop(4).Text(insight.Analysis)
                                .FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextPrimary);

                        if (!string.IsNullOrEmpty(insight.Action))
                            inner.Item().PaddingTop(4).Row(r =>
                            {
                                r.ConstantItem(6).Background(tagColor).MinHeight(1);
                                r.RelativeItem().PaddingLeft(6).Text($"Hành động: {insight.Action}")
                                    .FontSize(ReportStyles.FontSmall).Bold().FontColor(tagColor);
                            });
                    });
            }
        }

        private void ComposeTopDishes(ColumnDescriptor col)
        {
            if (!_data.Menu.TopDishes.Any()) return;

            SectionTitle(col, "MÓN BÁN TỐT NHẤT");

            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(28);
                    cols.RelativeColumn();
                    cols.RelativeColumn(2);
                    cols.RelativeColumn();
                });

                TableHeader(table, "#", "MÓN ĂN", "DẪN CHỨNG", "HÀNH ĐỘNG");

                foreach (var dish in _data.Menu.TopDishes)
                {
                    var bg = dish.Rank % 2 == 0 ? ReportStyles.SurfaceLight : ReportStyles.SurfaceWhite;
                    var rankColor = dish.Rank == 1 ? ReportStyles.AccentGold : ReportStyles.TextPrimary;

                    table.Cell().Background(bg).Padding(6).AlignCenter()
                        .Text(dish.Rank.ToString()).Bold().FontColor(rankColor).FontSize(ReportStyles.FontBody);
                    table.Cell().Background(bg).Padding(6)
                        .Text(dish.DishName).Bold().FontSize(ReportStyles.FontBody);
                    table.Cell().Background(bg).Padding(6)
                        .Text(dish.Evidence).FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted).Italic();
                    table.Cell().Background(bg).Padding(6)
                        .Text(dish.Action).FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.PrimaryMid);
                }
            });
        }

        private void ComposeSuggestedDishes(ColumnDescriptor col)
        {
            if (!_data.Menu.SuggestedDishes.Any()) return;

            SectionTitle(col, "ĐỀ XUẤT MÓN MỚI");

            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(28);
                    cols.RelativeColumn();
                    cols.RelativeColumn(2);
                    cols.RelativeColumn();
                });

                TableHeader(table, "#", "MÓN ĐỀ XUẤT", "LÝ DO / DẪN CHỨNG", "HÀNH ĐỘNG");

                foreach (var dish in _data.Menu.SuggestedDishes)
                {
                    var bg = dish.Rank % 2 == 0 ? ReportStyles.SurfaceLight : ReportStyles.SurfaceWhite;

                    table.Cell().Background(bg).Padding(6).AlignCenter()
                        .Text(dish.Rank.ToString()).Bold().FontSize(ReportStyles.FontBody).FontColor(ReportStyles.PrimaryMid);
                    table.Cell().Background(bg).Padding(6)
                        .Text(dish.DishName).Bold().FontSize(ReportStyles.FontBody);
                    table.Cell().Background(bg).Padding(6)
                        .Text(dish.Evidence).FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted).Italic();
                    table.Cell().Background(bg).Padding(6)
                        .Text(dish.Action).FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.PrimaryMid);
                }
            });
        }

        private void ComposeCombos(ColumnDescriptor col)
        {
            if (!_data.Menu.CombosToCreate.Any()) return;

            SectionTitle(col, "COMBO NÊN TẠO");

            foreach (var combo in _data.Menu.CombosToCreate)
            {
                col.Item().PaddingTop(6)
                    .Border(0.5f).BorderColor(ReportStyles.BorderColor)
                    .Background(ReportStyles.SurfaceLight).Padding(10).Column(inner =>
                    {
                        inner.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Combo #{combo.Rank}: {string.Join(" + ", combo.Dishes)}")
                                .Bold().FontSize(ReportStyles.FontBody);
                            if (combo.SuggestedPrice.HasValue)
                                row.ConstantItem(100).AlignRight()
                                    .Text(ReportStyles.FormatCurrency(combo.SuggestedPrice.Value))
                                    .Bold().FontColor(ReportStyles.AccentGold).FontSize(ReportStyles.FontBody);
                        });
                        if (!string.IsNullOrEmpty(combo.Evidence))
                            inner.Item().PaddingTop(4).Text(combo.Evidence)
                                .FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted).Italic();
                        if (!string.IsNullOrEmpty(combo.Reason))
                            inner.Item().PaddingTop(2).Text(combo.Reason)
                                .FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextPrimary);
                    });
            }
        }

        private void ComposeCustomers(ColumnDescriptor col)
        {
            var c = _data.Customers;
            if (string.IsNullOrWhiteSpace(c.Evidence) && string.IsNullOrWhiteSpace(c.Insight)) return;

            SectionTitle(col, "PHÂN TÍCH KHÁCH HÀNG");

            col.Item().PaddingTop(8)
                .Border(0.5f).BorderColor(ReportStyles.BorderColor)
                .Background(ReportStyles.SurfaceLight).Padding(12).Column(inner =>
                {
                    if (!string.IsNullOrEmpty(c.Evidence))
                        inner.Item().Text(c.Evidence)
                            .FontSize(ReportStyles.FontBody).Bold();
                    if (!string.IsNullOrEmpty(c.Insight))
                        inner.Item().PaddingTop(6).Text(c.Insight)
                            .FontSize(ReportStyles.FontBody).FontColor(ReportStyles.TextPrimary);
                    if (!string.IsNullOrEmpty(c.Action))
                        inner.Item().PaddingTop(6).Row(row =>
                        {
                            row.ConstantItem(6).Background(ReportStyles.AccentGreen).MinHeight(1);
                            row.RelativeItem().PaddingLeft(6).Text($"Hành động: {c.Action}")
                                .FontSize(ReportStyles.FontSmall).Bold().FontColor(ReportStyles.AccentGreen);
                        });
                });
        }

        private void ComposeFooter(IContainer c)
        {
            c.BorderTop(0.5f).BorderColor(ReportStyles.BorderColor)
                .PaddingVertical(6).Row(row =>
                {
                    row.RelativeItem().Text(_tenantName)
                        .FontSize(ReportStyles.FontSmall).FontColor(ReportStyles.TextMuted);
                    row.RelativeItem().AlignCenter().Text("Báo cáo phân tích AI — Powered by RestX")
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

        private static void SectionTitle(ColumnDescriptor col, string title)
        {
            col.Item().PaddingTop(ReportStyles.SectionSpacing).Row(row =>
            {
                row.ConstantItem(4).Background(ReportStyles.AccentGold).MinHeight(18);
                row.RelativeItem().PaddingLeft(8).Text(title)
                    .FontSize(ReportStyles.FontSectionHeader).Bold().FontColor(ReportStyles.PrimaryMid);
            });
            col.Item().PaddingTop(4).LineHorizontal(0.5f).LineColor(ReportStyles.BorderColor);
        }

        private static void TableHeader(TableDescriptor table, params string[] headers)
        {
            table.Header(header =>
            {
                foreach (var h in headers)
                    ReportComponents.HeaderCell(header, h);
            });
        }

        private string FilterLabel() => _filterType?.ToLower() switch
        {
            "week"  => "BÁO CÁO TUẦN",
            "month" => "BÁO CÁO THÁNG",
            "year"  => "BÁO CÁO NĂM",
            _       => "BÁO CÁO AI"
        };
    }
}
