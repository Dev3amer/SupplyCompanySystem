using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SupplyCompanySystem.Domain.Entities;
using System.IO;

namespace SupplyCompanySystem.UI.Services
{
    public static class BulkInvoicePdfGenerator
    {
        public static void GenerateBulkInvoicesPdf(List<Invoice> invoices, string filePath, DateTime fromDate, DateTime toDate)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            RegisterFonts();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.ContentFromRightToLeft();

                    page.Content()
                        .DefaultTextStyle(x =>
                            x.FontFamily("Cairo")
                             .FontSize(11)
                        )
                        .Column(column =>
                        {
                            column.Item().Row(row =>
                            {
                                row.RelativeColumn(2).Column(col =>
                                {
                                    col.Item().Text("تقرير عروض الأسعار السابقة")
                                        .FontSize(22)
                                        .Bold()
                                        .AlignRight();

                                    col.Item().PaddingTop(12);

                                    void InfoRow(string title, string value)
                                    {
                                        col.Item().Row(r =>
                                        {
                                            r.RelativeColumn(1)
                                                .Text(title)
                                                .Bold()
                                                .AlignRight();

                                            r.RelativeColumn(2)
                                                .Text(value)
                                                .AlignRight();
                                        });
                                    }

                                    InfoRow("الفترة من:", fromDate.ToString("yyyy/MM/dd"));
                                    InfoRow("الفترة إلى:", toDate.ToString("yyyy/MM/dd"));
                                    InfoRow("عدد عروض الأسعار:", invoices.Count.ToString());
                                    InfoRow("تاريخ الطباعة:", DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
                                });
                            });

                            column.Item().PaddingVertical(15);
                            column.Item().LineHorizontal(1);

                            column.Item().PaddingTop(15).Text("ملخص عروض الأسعار")
                                .FontSize(16)
                                .Bold()
                                .AlignRight();

                            column.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1.0f);   // رقم الفاتورة
                                    columns.RelativeColumn(1.5f);   // اسم العميل
                                    columns.RelativeColumn(1.0f);   // التاريخ
                                    columns.RelativeColumn(1.0f);   // الإجمالي قبل الخصم
                                    columns.RelativeColumn(1.0f);   // قيمة الخصم
                                    columns.RelativeColumn(1.0f);   // الإجمالي النهائي
                                });

                                table.Header(header =>
                                {
                                    void HeaderCell(string text) =>
                                        header.Cell()
                                            .Border(1)
                                            .Padding(6)
                                            .Text(text)
                                            .Bold()
                                            .AlignCenter()
                                            .FontSize(10);

                                    HeaderCell("رقم مسلسل");
                                    HeaderCell("اسم العميل");
                                    HeaderCell("التاريخ");
                                    HeaderCell("الإجمالي");
                                    HeaderCell("الخصم");
                                    HeaderCell("الإجمالي النهائي");
                                });

                                foreach (var invoice in invoices)
                                {
                                    void Cell(string value, bool right = false)
                                    {
                                        var cell = table.Cell()
                                            .Border(1)
                                            .Padding(5)
                                            .Text(value)
                                            .FontSize(9);

                                        if (right)
                                            cell.AlignRight();
                                        else
                                            cell.AlignCenter();
                                    }

                                    Cell(invoice.Id.ToString());
                                    Cell(invoice.Customer?.Name ?? "", true);
                                    Cell(invoice.InvoiceDate.ToString("yyyy/MM/dd"));
                                    Cell(invoice.TotalAmount.ToString("0.00"), true);
                                    Cell(invoice.InvoiceDiscountAmount.ToString("0.00"), true);
                                    Cell(invoice.FinalAmount.ToString("0.00"), true);
                                }
                            });

                            column.Item().PaddingTop(20).Row(row =>
                            {
                                row.RelativeColumn(1).Column(col =>
                                {
                                    void SummaryRow(string title, string value)
                                    {
                                        col.Item().Row(innerRow =>
                                        {
                                            innerRow.RelativeColumn(1)
                                                .Text(title)
                                                .Bold()
                                                .AlignRight();

                                            innerRow.RelativeColumn(1)
                                                .Text(value)
                                                .Bold()
                                                .AlignLeft()
                                                .FontColor(Color.FromRGB(231, 76, 60));
                                        });
                                    }

                                    SummaryRow("إجمالي المبالغ:", invoices.Sum(i => i.TotalAmount).ToString("0.00"));
                                    SummaryRow("إجمالي الخصومات:", invoices.Sum(i => i.InvoiceDiscountAmount).ToString("0.00"));
                                    SummaryRow("الإجمالي النهائي:", invoices.Sum(i => i.FinalAmount).ToString("0.00"));
                                });
                            });

                            column.Item().PageBreak();

                            foreach (var invoice in invoices)
                            {
                                AddInvoiceToPdf(column, invoice);
                                column.Item().PageBreak();
                            }
                        });
                });
            }).GeneratePdf(filePath);
        }

        private static void AddInvoiceToPdf(ColumnDescriptor column, Invoice invoice)
        {
            column.Item().Row(row =>
            {
                row.RelativeColumn(2).Column(col =>
                {
                    col.Item().Text($"عرض أسعار رقم: {invoice.Id}")
                        .FontSize(18)
                        .Bold()
                        .AlignRight();

                    col.Item().PaddingTop(8);

                    void InfoRow(string title, string value)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeColumn(1)
                                .Text(title)
                                .Bold()
                                .AlignRight();

                            r.RelativeColumn(2)
                                .Text(value)
                                .AlignRight();
                        });
                    }

                    InfoRow("العميل:", invoice.Customer?.Name ?? "");
                    InfoRow("التاريخ:", invoice.InvoiceDate.ToString("yyyy/MM/dd"));
                    InfoRow("رقم الهاتف:", invoice.Customer?.PhoneNumber ?? "");
                    InfoRow("العنوان:", invoice.Customer?.Address ?? "");
                });
            });

            column.Item().PaddingVertical(10);
            column.Item().LineHorizontal(1);

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.6f);   // م
                    columns.RelativeColumn(1.5f);   // اسم الصنف
                    columns.RelativeColumn(0.8f);   // الوحدة
                    columns.RelativeColumn(0.8f);   // الكمية
                    columns.RelativeColumn(1.0f);   // سعر الوحدة
                    columns.RelativeColumn(1.0f);   // الإجمالي
                });

                table.Header(header =>
                {
                    void HeaderCell(string text) =>
                        header.Cell()
                            .Border(1)
                            .Padding(5)
                            .Text(text)
                            .Bold()
                            .AlignCenter()
                            .FontSize(9);

                    HeaderCell("م");
                    HeaderCell("اسم الصنف");
                    HeaderCell("الوحدة");
                    HeaderCell("الكمية");
                    HeaderCell("سعر الوحدة");
                    HeaderCell("الإجمالي");
                });

                int rowNum = 1;
                foreach (var item in invoice.Items)
                {
                    void Cell(string value, bool right = false)
                    {
                        var cell = table.Cell()
                            .Border(1)
                            .Padding(4)
                            .Text(value)
                            .FontSize(8);

                        if (right)
                            cell.AlignRight();
                        else
                            cell.AlignCenter();
                    }

                    decimal finalUnitPrice = CalculateFinalUnitPriceForPdf(item, invoice);
                    decimal finalLineTotal = CalculateFinalLineTotalForPdf(item, invoice);

                    Cell(rowNum.ToString());
                    Cell(item.Product?.Name ?? "", true);
                    Cell(item.Product?.Unit ?? "");
                    Cell(item.Quantity.ToString());
                    Cell(finalUnitPrice.ToString("0.00"), true);
                    Cell(finalLineTotal.ToString("0.00"), true);

                    rowNum++;
                }
            });

            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeColumn(1).Column(col =>
                {
                    void SummaryRow(string title, string value, bool bold = false)
                    {
                        col.Item().Row(innerRow =>
                        {
                            innerRow.RelativeColumn(1)
                                .Text(title)
                                .Bold()
                                .AlignRight();

                            innerRow.RelativeColumn(1)
                                .Text(value)
                                .Bold()
                                .AlignLeft();
                        });
                    }

                    decimal totalForCustomer = CalculateTotalForCustomer(invoice);
                    decimal invoiceDiscountForCustomer = CalculateInvoiceDiscountForCustomer(invoice, totalForCustomer);
                    decimal finalAmountForCustomer = totalForCustomer - invoiceDiscountForCustomer;

                    SummaryRow("إجمالي عرض الأسعار:", totalForCustomer.ToString("0.00"));

                    if (invoice.InvoiceDiscountPercentage > 0)
                    {
                        SummaryRow("خصم عرض الأسعار:", invoiceDiscountForCustomer.ToString("0.00"));
                    }

                    col.Item().PaddingTop(5);
                    col.Item().LineHorizontal(1);

                    SummaryRow("الإجمالي النهائي:", finalAmountForCustomer.ToString("0.00"), true);
                });
            });

            column.Item().PaddingTop(15);
            column.Item().LineHorizontal(0.5f);
            column.Item().PaddingTop(5).Text($"--- نهاية عرض أسعار رقم {invoice.Id} ---")
                .FontSize(9)
                .Italic()
                .AlignCenter();
        }

        private static decimal CalculateFinalUnitPriceForPdf(InvoiceItem item, Invoice invoice)
        {
            if (item == null || item.OriginalUnitPrice <= 0)
                return 0;

            decimal priceAfterProductProfit = item.OriginalUnitPrice +
                (item.OriginalUnitPrice * item.ItemProfitMarginPercentage / 100);

            decimal finalPrice = priceAfterProductProfit +
                (priceAfterProductProfit * invoice.ProfitMarginPercentage / 100);

            return finalPrice;
        }

        private static decimal CalculateFinalLineTotalForPdf(InvoiceItem item, Invoice invoice)
        {
            if (item == null)
                return 0;

            decimal finalUnitPrice = CalculateFinalUnitPriceForPdf(item, invoice);
            decimal totalBeforeDiscount = item.Quantity * finalUnitPrice;
            decimal discountAmount = (item.Quantity * item.OriginalUnitPrice * item.DiscountPercentage) / 100;

            return totalBeforeDiscount - discountAmount;
        }

        private static decimal CalculateTotalForCustomer(Invoice invoice)
        {
            if (invoice?.Items == null || invoice.Items.Count == 0)
                return 0;

            decimal total = 0;
            foreach (var item in invoice.Items)
            {
                total += CalculateFinalLineTotalForPdf(item, invoice);
            }

            return total;
        }

        private static decimal CalculateInvoiceDiscountForCustomer(Invoice invoice, decimal totalForCustomer)
        {
            if (invoice == null || invoice.InvoiceDiscountPercentage <= 0)
                return 0;

            return (totalForCustomer * invoice.InvoiceDiscountPercentage) / 100;
        }

        private static void RegisterFonts()
        {
            string fontPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets",
                "Fonts"
            );

            string cairoRegularPath = Path.Combine(fontPath, "Cairo-Regular.ttf");
            string cairoBoldPath = Path.Combine(fontPath, "Cairo-Bold.ttf");

            if (File.Exists(cairoRegularPath))
            {
                FontManager.RegisterFont(File.OpenRead(cairoRegularPath));
            }

            if (File.Exists(cairoBoldPath))
            {
                FontManager.RegisterFont(File.OpenRead(cairoBoldPath));
            }
        }
    }
}