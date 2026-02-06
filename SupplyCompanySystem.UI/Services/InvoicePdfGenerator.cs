using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SupplyCompanySystem.Domain.Entities;
using System.IO;

namespace SupplyCompanySystem.UI.Services
{
    public class InvoicePdfGenerator
    {
        public static string GenerateInvoiceFileName(Invoice invoice)
        {
            string customerName = invoice.Customer?.Name ?? "بدون_عميل";
            customerName = CleanFileName(customerName);

            string datePart = invoice.InvoiceDate.ToString("yyyy-MM-dd");
            string invoiceNumber = invoice.Id.ToString();

            return $"{customerName}_{datePart}_{invoiceNumber}";
        }

        private static string CleanFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "غير_معروف";

            char[] invalidChars = Path.GetInvalidFileNameChars();

            foreach (char invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            fileName = fileName.Replace(' ', '_');
            fileName = fileName.Replace('.', '_');
            fileName = fileName.Replace(',', '_');
            fileName = fileName.Replace(';', '_');
            fileName = fileName.Replace(':', '_');

            while (fileName.Contains("__"))
            {
                fileName = fileName.Replace("__", "_");
            }

            if (fileName.Length > 100)
            {
                fileName = fileName.Substring(0, 100);
            }

            return fileName.Trim('_');
        }

        public static string GenerateInvoicePdf(Invoice invoice, string customFileName = null)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

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
                else
                {
                    Console.WriteLine("تحذير: لم يتم العثور على خط Cairo-Regular.ttf");
                }

                if (File.Exists(cairoBoldPath))
                {
                    FontManager.RegisterFont(File.OpenRead(cairoBoldPath));
                }

                string invoicesFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "الفواتير"
                );

                if (!Directory.Exists(invoicesFolder))
                    Directory.CreateDirectory(invoicesFolder);

                string fileName = customFileName ?? GenerateInvoiceFileName(invoice);

                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".pdf";
                }

                string filePath = Path.Combine(invoicesFolder, fileName);

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
                                        col.Item().Text("بيان أسعار")
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

                                        // ✅ عرض تاريخ الفاتورة فقط (تم حذف تاريخ الإنشاء)
                                        InfoRow(
                                            "تحريراً في:",
                                            invoice.InvoiceDate.ToString("yyyy/MM/dd")
                                        );

                                        InfoRow(
                                            "المطلوب من السيد:",
                                            invoice.Customer?.Name ?? ""
                                        );

                                        InfoRow(
                                            "رقم الموبايل:",
                                            invoice.Customer?.PhoneNumber ?? ""
                                        );

                                        InfoRow(
                                            "العنوان:",
                                            invoice.Customer?.Address ?? ""
                                        );
                                    });

                                    row.RelativeColumn(1)
                                        .AlignBottom()
                                        .Column(col =>
                                        {
                                            col.Item()
                                                .Border(1)
                                                .Padding(10)
                                                .Text($"رقم بيان أسعار\n{invoice.Id}")
                                                .Bold()
                                                .AlignCenter();
                                        });
                                });

                                column.Item().PaddingVertical(15);
                                column.Item().LineHorizontal(1);

                                column.Item().PaddingTop(15).Table(table =>
                                {
                                    // ✅ تعديل الأعمدة حسب الطلب: 7 أعمدة فقط
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(0.6f);   // م
                                        columns.RelativeColumn(1.0f);   // الكود
                                        columns.RelativeColumn(2.0f);   // اسم الصنف
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
                                                .Padding(6)
                                                .Text(text)
                                                .Bold()
                                                .AlignCenter();

                                        HeaderCell("م");
                                        HeaderCell("الكود");
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
                                                .Padding(6)
                                                .Text(value)
                                                .FontSize(10);

                                            if (right)
                                                cell.AlignRight();
                                            else
                                                cell.AlignCenter();
                                        }

                                        Cell(rowNum.ToString());
                                        Cell(item.Product?.SKU ?? ""); // الكود
                                        Cell(item.Product?.Name ?? "", true); // اسم الصنف
                                        Cell(item.Product?.Unit ?? ""); // الوحدة
                                        Cell(item.Quantity.ToString()); // الكمية
                                        // ✅ سعر الوحدة بعد المكسب (UnitPrice)
                                        Cell(item.UnitPrice.ToString("0.00"));
                                        // ✅ الإجمالي للصنف
                                        Cell(item.LineTotal.ToString("0.00"));

                                        rowNum++;
                                    }
                                });

                                column.Item().PaddingTop(10).Row(row =>
                                {
                                    decimal totalQuantity = 0;
                                    foreach (var item in invoice.Items)
                                    {
                                        totalQuantity += item.Quantity;
                                    }

                                    row.RelativeColumn(1).Column(col =>
                                    {
                                        col.Item().Row(innerRow =>
                                        {
                                            innerRow.RelativeColumn(1)
                                                .Text("عدد كميات الفاتورة:")
                                                .Bold()
                                                .AlignRight();

                                            innerRow.RelativeColumn(1)
                                                .Text(totalQuantity.ToString())
                                                .Bold()
                                                .AlignLeft();
                                        });
                                    });
                                });

                                column.Item().PaddingTop(8).Row(row =>
                                {
                                    row.RelativeColumn(1).Column(col =>
                                    {
                                        col.Item().Row(innerRow =>
                                        {
                                            innerRow.RelativeColumn(1)
                                                .Text("عدد أصناف الفاتورة:")
                                                .Bold()
                                                .AlignRight();

                                            innerRow.RelativeColumn(1)
                                                .Text(invoice.Items.Count.ToString())
                                                .Bold()
                                                .AlignLeft();
                                        });
                                    });
                                });

                                column.Item().PaddingTop(20).AlignLeft().Column(col =>
                                {
                                    void SummaryRow(string title, string value, bool bold = false)
                                    {
                                        col.Item().Row(row =>
                                        {
                                            row.RelativeColumn(1)
                                                .Text(title)
                                                .Bold()
                                                .AlignRight();

                                            row.RelativeColumn(1)
                                                .Text(value)
                                                .Bold()
                                                .AlignLeft();
                                        });
                                    }

                                    // ✅ تم حذف حساب إجمالي خصم المنتجات
                                    decimal totalBeforeDiscount = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);
                                    decimal invoiceDiscountAmount = (totalBeforeDiscount * invoice.InvoiceDiscountPercentage) / 100;

                                    SummaryRow(
                                        "إجمالي الفاتورة:",
                                        totalBeforeDiscount.ToString("0.00")
                                    );

                                    SummaryRow(
                                        "خصم الفاتورة:",
                                        invoiceDiscountAmount.ToString("0.00")
                                    );

                                    col.Item().PaddingTop(8);
                                    col.Item().LineHorizontal(2);

                                    SummaryRow(
                                        "الإجمالي النهائي:",
                                        invoice.FinalAmount.ToString("0.00"),
                                        true
                                    );

                                    // ✅ تم حذف المكسب الكلي
                                    col.Item().PaddingTop(15);
                                    col.Item().Row(row =>
                                    {
                                        row.RelativeColumn(1)
                                            .Text("تفقيط الفاتورة:")
                                            .Bold()
                                            .AlignRight();

                                        row.RelativeColumn(2)
                                            .Text(ArabicNumberToWords.ConvertToArabicWords(invoice.FinalAmount))
                                            .AlignRight()
                                            .FontSize(10);
                                    });
                                });

                                if (!string.IsNullOrWhiteSpace(invoice.Notes))
                                {
                                    column.Item().PaddingTop(20);

                                    column.Item().Text("ملاحظات:")
                                        .Bold()
                                        .AlignRight();

                                    column.Item().Text(invoice.Notes)
                                        .AlignRight()
                                        .FontSize(10);
                                }

                                column.Item().PaddingTop(40).Row(row =>
                                {
                                    void SignCell(string title) =>
                                        row.RelativeColumn(1).Column(col =>
                                        {
                                            col.Item()
                                                .BorderTop(1)
                                                .PaddingTop(5)
                                                .Text(title)
                                                .AlignCenter()
                                                .FontSize(10);
                                        });

                                    SignCell("توقيع المستلم");
                                    SignCell("توقيع العميل");
                                    SignCell("توقيع المندوب");
                                });
                            });
                    });
                }).GeneratePdf(filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في إنشاء PDF: {ex.Message}");
            }
        }

        public static string GenerateInvoicePdf(Invoice invoice)
        {
            return GenerateInvoicePdf(invoice, null);
        }
    }
}