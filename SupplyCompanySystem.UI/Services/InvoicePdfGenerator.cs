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

                                        // ✅ حساب السعر النهائي الذي يظهر للعميل
                                        // هذا السعر يحتوي على: السعر الأصلي + مكسب المنتج + مكسب الفاتورة
                                        decimal finalUnitPriceForCustomer = CalculateFinalUnitPriceForPdf(item, invoice);

                                        Cell(rowNum.ToString());
                                        Cell(item.Product?.SKU ?? ""); // الكود
                                        Cell(item.Product?.Name ?? "", true); // اسم الصنف
                                        Cell(item.Product?.Unit ?? ""); // الوحدة
                                        Cell(item.Quantity.ToString()); // الكمية
                                        // ✅ سعر الوحدة النهائي (بعد جميع المكاسب)
                                        Cell(finalUnitPriceForCustomer.ToString("0.00"));
                                        // ✅ الإجمالي النهائي للصنف (الكمية × السعر النهائي - الخصم)
                                        decimal finalLineTotal = CalculateFinalLineTotalForPdf(item, invoice);
                                        Cell(finalLineTotal.ToString("0.00"));

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
                                                .Text("عدد كميات عرض الأسعار:")
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
                                                .Text("عدد أصناف عرض الأسعار:")
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

                                    // ✅ حساب الإجماليات النهائية للفاتورة (كما تظهر للعميل)
                                    decimal totalForCustomer = CalculateTotalForCustomer(invoice);
                                    decimal invoiceDiscountForCustomer = CalculateInvoiceDiscountForCustomer(invoice, totalForCustomer);
                                    decimal finalAmountForCustomer = totalForCustomer - invoiceDiscountForCustomer;

                                    SummaryRow(
                                        "إجمالي عرض الأسعار:",
                                        totalForCustomer.ToString("0.00")
                                    );

                                    if (invoice.InvoiceDiscountPercentage > 0)
                                    {
                                        SummaryRow(
                                            "خصم عرض الأسعار:",
                                            invoiceDiscountForCustomer.ToString("0.00")
                                        );
                                    }

                                    col.Item().PaddingTop(8);
                                    col.Item().LineHorizontal(2);

                                    SummaryRow(
                                        "الإجمالي النهائي:",
                                        finalAmountForCustomer.ToString("0.00"),
                                        true
                                    );

                                    col.Item().PaddingTop(15);
                                    col.Item().Row(row =>
                                    {
                                        row.RelativeColumn(1)
                                            .Text("تفقيط عرض الأسعار:")
                                            .Bold()
                                            .AlignRight();

                                        row.RelativeColumn(2)
                                            .Text(ArabicNumberToWords.ConvertToArabicWords(finalAmountForCustomer))
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

                                // ✅ ❌ ❌ ❌ حذف جزء التوقيعات من PDF الذي به أسعار ❌ ❌ ❌
                                // تم استبدال جزء التوقيعات بمساحة إضافية فقط
                                column.Item().PaddingTop(20);
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

        // ✅ دالة جديدة لحساب سعر الوحدة النهائي للـ PDF
        private static decimal CalculateFinalUnitPriceForPdf(InvoiceItem item, Invoice invoice)
        {
            if (item == null || item.OriginalUnitPrice <= 0)
                return 0;

            // 1. السعر بعد مكسب المنتج
            decimal priceAfterProductProfit = item.OriginalUnitPrice +
                (item.OriginalUnitPrice * item.ItemProfitMarginPercentage / 100);

            // 2. السعر بعد مكسب الفاتورة (يطبق على السعر بعد مكسب المنتج)
            decimal finalPrice = priceAfterProductProfit +
                (priceAfterProductProfit * invoice.ProfitMarginPercentage / 100);

            return finalPrice;
        }

        // ✅ دالة جديدة لحساب الإجمالي النهائي للبند في الـ PDF
        private static decimal CalculateFinalLineTotalForPdf(InvoiceItem item, Invoice invoice)
        {
            if (item == null)
                return 0;

            // 1. حساب السعر النهائي للوحدة
            decimal finalUnitPrice = CalculateFinalUnitPriceForPdf(item, invoice);

            // 2. حساب الإجمالي قبل الخصم
            decimal totalBeforeDiscount = item.Quantity * finalUnitPrice;

            // 3. حساب الخصم على السعر الأصلي
            decimal discountAmount = (item.Quantity * item.OriginalUnitPrice * item.DiscountPercentage) / 100;

            // 4. الإجمالي النهائي
            return totalBeforeDiscount - discountAmount;
        }

        // ✅ دالة جديدة لحساب الإجمالي الكلي للفاتورة كما يظهر للعميل
        private static decimal CalculateTotalForCustomer(Invoice invoice)
        {
            if (invoice?.Items == null || invoice.Items.Count == 0)
                return 0;

            decimal total = 0;
            foreach (var item in invoice.Items)
            {
                // ✅ استخدام الدالة التي تحسب الإجمالي النهائي للبند
                total += CalculateFinalLineTotalForPdf(item, invoice);
            }

            return total;
        }

        // ✅ دالة جديدة لحساب خصم الفاتورة كما يظهر للعميل
        private static decimal CalculateInvoiceDiscountForCustomer(Invoice invoice, decimal totalForCustomer)
        {
            if (invoice == null || invoice.InvoiceDiscountPercentage <= 0)
                return 0;

            // ✅ الخصم يطبق على الإجمالي النهائي للفاتورة
            return (totalForCustomer * invoice.InvoiceDiscountPercentage) / 100;
        }

        public static string GenerateInvoicePdfWithoutPrices(Invoice invoice, string customFileName = null)
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

                if (File.Exists(cairoBoldPath))
                {
                    FontManager.RegisterFont(File.OpenRead(cairoBoldPath));
                }

                string invoicesFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "عروض_الأسعار"
                );

                if (!Directory.Exists(invoicesFolder))
                    Directory.CreateDirectory(invoicesFolder);

                string fileName = customFileName ?? GenerateInvoiceFileName(invoice) + "_بدون_أسعار";

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

                                        // ✅ عرض تاريخ الفاتورة فقط
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
                                    // ✅ جدول بدون أسعار: 6 أعمدة فقط (بدون سعر الوحدة والإجمالي)
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(0.6f);   // م
                                        columns.RelativeColumn(1.0f);   // الكود
                                        columns.RelativeColumn(2.0f);   // اسم الصنف
                                        columns.RelativeColumn(0.8f);   // الوحدة
                                        columns.RelativeColumn(0.8f);   // الكمية
                                        columns.RelativeColumn(1.0f);   // ملاحظات
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
                                        HeaderCell("ملاحظات");
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
                                        Cell(""); // ✅ خانة فارغة بدلاً من السعر

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
                                                .Text("عدد كميات عرض الأسعار:")
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
                                                .Text("عدد أصناف عرض الأسعار:")
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

                                    // ✅ إخفاء المبالغ في PDF بدون أسعار
                                    SummaryRow(
                                        "إجمالي عرض الأسعار:",
                                        "-"
                                    );

                                    if (invoice.InvoiceDiscountPercentage > 0)
                                    {
                                        SummaryRow(
                                            "خصم عرض الأسعار:",
                                            "-"
                                        );
                                    }

                                    col.Item().PaddingTop(8);
                                    col.Item().LineHorizontal(2);

                                    SummaryRow(
                                        "الإجمالي النهائي:",
                                        "-",
                                        true
                                    );

                                    col.Item().PaddingTop(15);
                                    col.Item().Row(row =>
                                    {
                                        row.RelativeColumn(1)
                                            .Text("تفقيط عرض الأسعار:")
                                            .Bold()
                                            .AlignRight();

                                        row.RelativeColumn(2)
                                            .Text("-")
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

                                // ✅ إضافة ملاحظة توضيحية أن هذا البيان بدون أسعار
                                column.Item().PaddingTop(20);

                                column.Item().Text("ملاحظة: هذا البيان مقدم للعرض فقط ولا يحتوي على أسعار")
                                    .Bold()
                                    .Italic()
                                    .FontSize(10)
                                    .FontColor(Color.FromRGB(1, 0, 0))
                                    .AlignCenter();

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
                throw new Exception($"خطأ في إنشاء PDF بدون أسعار: {ex.Message}");
            }
        }

        public static string GenerateInvoicePdf(Invoice invoice)
        {
            return GenerateInvoicePdf(invoice, null);
        }
    }
}