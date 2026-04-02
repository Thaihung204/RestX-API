using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RestX.BLL.Helpers;
using RestX.BLL.Interfaces;
using RestX.Models.Orders;
using RestX.Models.Tenants;

namespace RestX.BLL.Services
{
    public class ReceiptService : BaseService, IReceiptService
    {
        public ReceiptService(
            IRepository repo,
            IRedisService redisService,
            IEnumerable<ActiveTenant> tenant = null
        ) : base(repo, redisService, tenant)
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateReceiptAsync(Guid paymentId)
        {
            var payment = await Repo.GetOneAsync<Payment>(
                filter: p => p.Id == paymentId)
                ?? throw new KeyNotFoundException("Payment not found");

            var order = await Repo.GetOneAsync<Order>(
                filter: o => o.Id == payment.OrderId,
                includeProperties: "OrderDetails.Dish")
                ?? throw new KeyNotFoundException("Order not found");

            var tenant = CurrentTenant;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(col =>
                    {
                        col.Item().AlignCenter().Text(tenant?.BusinessName ?? tenant?.Name ?? "Restaurant")
                            .FontSize(16).Bold();

                        if (!string.IsNullOrEmpty(tenant?.BusinessAddressLine1))
                            col.Item().AlignCenter().Text(tenant.BusinessAddressLine1).FontSize(9);

                        if (!string.IsNullOrEmpty(tenant?.BusinessPrimaryPhone))
                            col.Item().AlignCenter().Text($"Tel: {tenant.BusinessPrimaryPhone}").FontSize(9);

                        col.Item().PaddingVertical(6).LineHorizontal(0.5f);

                        col.Item().AlignCenter().Text("RECEIPT").FontSize(13).Bold();
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            row.RelativeItem().Text($"Order: {order.Reference}");
                            row.RelativeItem().AlignRight().Text(payment.PaymentDate.ToString("dd/MM/yyyy HH:mm"));
                        });

                        col.Item().PaddingVertical(6).LineHorizontal(0.5f);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem(4).Text("Item").Bold();
                            row.RelativeItem(1).AlignCenter().Text("Qty").Bold();
                            row.RelativeItem(2).AlignRight().Text("Price").Bold();
                            row.RelativeItem(2).AlignRight().Text("Subtotal").Bold();
                        });

                        col.Item().PaddingBottom(4).LineHorizontal(0.5f);

                        foreach (var item in order.OrderDetails)
                        {
                            var unitPrice = item.Dish?.Price ?? 0;
                            var subtotal = unitPrice * item.Quantity;
                            col.Item().Row(row =>
                            {
                                row.RelativeItem(4).Text(item.Dish?.Name ?? "Item");
                                row.RelativeItem(1).AlignCenter().Text(item.Quantity.ToString());
                                row.RelativeItem(2).AlignRight().Text(FormatCurrency(unitPrice));
                                row.RelativeItem(2).AlignRight().Text(FormatCurrency(subtotal));
                            });
                        }

                        col.Item().PaddingVertical(6).LineHorizontal(0.5f);

                        if (order.DiscountAmount > 0)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Subtotal");
                                row.RelativeItem().AlignRight().Text(FormatCurrency(order.SubTotal));
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Discount");
                                row.RelativeItem().AlignRight().Text($"- {FormatCurrency(order.DiscountAmount)}");
                            });
                        }

                        if (order.TaxAmount > 0)
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Tax");
                                row.RelativeItem().AlignRight().Text(FormatCurrency(order.TaxAmount));
                            });

                        if (order.ServiceCharge > 0)
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Service Charge");
                                row.RelativeItem().AlignRight().Text(FormatCurrency(order.ServiceCharge));
                            });

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL").Bold().FontSize(12);
                            row.RelativeItem().AlignRight().Text(FormatCurrency(order.TotalAmount)).Bold().FontSize(12);
                        });

                        col.Item().PaddingVertical(6).LineHorizontal(0.5f);

                        col.Item().Text("Payment").Bold();
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Method");
                            row.RelativeItem().AlignRight().Text(payment.PaymentMethodId);
                        });
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Status");
                            row.RelativeItem().AlignRight().Text(payment.Status.ToString());
                        });

                        if (string.Equals(payment.PaymentMethodId, PaymentConstants.Method.Cash, StringComparison.OrdinalIgnoreCase) && payment.CashReceive > 0)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Cash Received");
                                row.RelativeItem().AlignRight().Text(FormatCurrency(payment.CashReceive));
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Change");
                                row.RelativeItem().AlignRight().Text(FormatCurrency(payment.Cashback));
                            });
                        }

                        if (!string.IsNullOrEmpty(payment.TransactionId))
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Transaction ID");
                                row.RelativeItem().AlignRight().Text(payment.TransactionId).FontSize(8);
                            });

                        col.Item().PaddingVertical(8).LineHorizontal(0.5f);

                        col.Item().AlignCenter().Text("Thank you for your visit!").Italic().FontSize(9);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static string FormatCurrency(decimal amount) =>
            string.Format("{0:N0} VND", amount);
    }
}
