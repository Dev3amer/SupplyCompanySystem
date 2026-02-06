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
        // ✅ دالة جديدة: إنشاء اسم الملف بناءً على الفاتورة
        public static string GenerateInvoiceFileName(Invoice invoice)
        {
            // الحصول على اسم العميل وتنظيفه
            string customerName = invoice.Customer?.Name ?? "بدون_عميل";
            customerName = CleanFileName(customerName);

            // تنسيق التاريخ: yyyy-MM-dd
            string datePart = invoice.InvoiceDate.ToString("yyyy-MM-dd");

            // رقم الفاتورة
            string invoiceNumber = invoice.Id.ToString();

            // اسم الملف النهائي: اسم العميل_التاريخ_رقم الفاتورة
            return $"{customerName}_{datePart}_{invoiceNumber}";
        }

        // ✅ دالة مساعدة: تنظيف اسم الملف من الأحرف غير المسموحة
        private static string CleanFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "غير_معروف";

            // قائمة بالأحرف غير المسموحة في أسماء الملفات
            char[] invalidChars = Path.GetInvalidFileNameChars();

            // استبدال كل حرف غير مسموح بشرطة سفلية
            foreach (char invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            // استبدال المسافات بشرطة سفلية
            fileName = fileName.Replace(' ', '_');

            // استبدال النقاط والفواصل بشرطة سفلية
            fileName = fileName.Replace('.', '_');
            fileName = fileName.Replace(',', '_');
            fileName = fileName.Replace(';', '_');
            fileName = fileName.Replace(':', '_');

            // إزالة الشرطات السفلية المتتالية
            while (fileName.Contains("__"))
            {
                fileName = fileName.Replace("__", "_");
            }

            // قص الاسم إذا كان طويلاً جداً (حد 100 حرف للحفاظ على اسم ملف معقول)
            if (fileName.Length > 100)
            {
                fileName = fileName.Substring(0, 100);
            }

            return fileName.Trim('_');
        }

        // ✅ الدالة الرئيسية - يمكن استخدامها باسم مخصص أو اسم افتراضي
        public static string GenerateInvoicePdf(Invoice invoice, string customFileName = null)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                // ================= Fonts =================
                string fontPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Assets",
                    "Fonts"
                );

                // ✅ التحقق من وجود الخطوط قبل التسجيل
                string cairoRegularPath = Path.Combine(fontPath, "Cairo-Regular.ttf");
                string cairoBoldPath = Path.Combine(fontPath, "Cairo-Bold.ttf");

                if (File.Exists(cairoRegularPath))
                {
                    FontManager.RegisterFont(File.OpenRead(cairoRegularPath));
                }
                else
                {
                    // استخدام خط افتراضي إذا لم يوجد الخط العربي
                    Console.WriteLine("تحذير: لم يتم العثور على خط Cairo-Regular.ttf");
                }

                if (File.Exists(cairoBoldPath))
                {
                    FontManager.RegisterFont(File.OpenRead(cairoBoldPath));
                }

                // ================= Folder =================
                string invoicesFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "الفواتير"
                );

                if (!Directory.Exists(invoicesFolder))
                    Directory.CreateDirectory(invoicesFolder);

                // ✅ استخدام الاسم المخصص أو إنشاء اسم بناءً على الفاتورة
                string fileName = customFileName ?? GenerateInvoiceFileName(invoice);

                // ✅ التأكد من أن الملف ينتهي بـ .pdf
                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".pdf";
                }

                string filePath = Path.Combine(invoicesFolder, fileName);

                // ================= PDF =================
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
                                // ================= HEADER =================
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

                                        // ✅ عرض نسبة مكسب وخصم الفاتورة إن وجدت
                                        if (invoice.ProfitMarginPercentage > 0)
                                        {
                                            InfoRow(
                                                "نسبة مكسب الفاتورة:",
                                                $"{invoice.ProfitMarginPercentage:0.00}%"
                                            );
                                        }

                                        if (invoice.InvoiceDiscountPercentage > 0)
                                        {
                                            InfoRow(
                                                "نسبة خصم الفاتورة:",
                                                $"{invoice.InvoiceDiscountPercentage:0.00}%"
                                            );
                                        }
                                    });

                                    row.RelativeColumn(1)
                                        .AlignBottom()
                                        .Column(col =>
                                        {
                                            col.Item()
                                                .Border(1)
                                                .Padding(10)
                                                .Text($"رقم الفاتورة\n{invoice.Id}")
                                                .Bold()
                                                .AlignCenter();
                                        });
                                });

                                column.Item().PaddingVertical(15);
                                column.Item().LineHorizontal(1);

                                // ================= ITEMS TABLE =================
                                column.Item().PaddingTop(15).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(0.6f);      // م
                                        columns.RelativeColumn(1.8f);      // اسم الصنف
                                        columns.RelativeColumn(0.8f);      // الوحدة
                                        columns.RelativeColumn(0.8f);      // الكمية
                                        columns.RelativeColumn(1);         // السعر الأصلي
                                        columns.RelativeColumn(0.8f);      // مكسب المنتج %
                                        columns.RelativeColumn(1);         // سعر بعد المكسب
                                        columns.RelativeColumn(0.8f);      // خصم المنتج %
                                        columns.RelativeColumn(1.2f);      // الإجمالي
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
                                        HeaderCell("اسم الصنف");
                                        HeaderCell("الوحدة");
                                        HeaderCell("الكمية");
                                        HeaderCell("السعر الأصلي");
                                        HeaderCell("مكسب %");
                                        HeaderCell("سعر بعد المكسب");
                                        HeaderCell("خصم %");
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
                                        Cell(item.Product?.Name ?? "", true);
                                        Cell(item.Product?.Unit ?? "");
                                        Cell(item.Quantity.ToString());
                                        Cell(item.OriginalUnitPrice.ToString("0.00"));
                                        Cell(item.ItemProfitMarginPercentage.ToString("0.00"));
                                        Cell(item.UnitPrice.ToString("0.00"));
                                        Cell(item.DiscountPercentage.ToString("0.00"));
                                        Cell(item.LineTotal.ToString("0.00"));

                                        rowNum++;
                                    }
                                });

                                // ✅ عدد الكميات وعدد الأصناف
                                column.Item().PaddingTop(10).Row(row =>
                                {
                                    // إجمالي الكميات
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

                                // ================= SUMMARY =================
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

                                    // حساب إجمالي الخصم من جميع البنود
                                    decimal totalItemsDiscount = 0;
                                    decimal totalItemsProfit = 0;
                                    foreach (var item in invoice.Items)
                                    {
                                        totalItemsDiscount += (item.Quantity * item.UnitPrice * item.DiscountPercentage) / 100;
                                        totalItemsProfit += (item.Quantity * item.OriginalUnitPrice * item.ItemProfitMarginPercentage) / 100;
                                    }

                                    // حساب خصم الفاتورة
                                    decimal totalBeforeDiscount = invoice.Items.Sum(i => i.Quantity * i.UnitPrice);
                                    decimal subtotalAfterItemsDiscount = totalBeforeDiscount - totalItemsDiscount;
                                    decimal invoiceDiscountAmount = (subtotalAfterItemsDiscount * invoice.InvoiceDiscountPercentage) / 100;

                                    SummaryRow(
                                        "إجمالي الفاتورة:",
                                        totalBeforeDiscount.ToString("0.00")
                                    );

                                    SummaryRow(
                                        "إجمالي خصم المنتجات:",
                                        totalItemsDiscount.ToString("0.00")
                                    );

                                    SummaryRow(
                                        "المجموع بعد خصم المنتجات:",
                                        subtotalAfterItemsDiscount.ToString("0.00")
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

                                    // ✅ عرض المكسب الكلي
                                    col.Item().PaddingTop(10);
                                    SummaryRow(
                                        "المكسب الكلي:",
                                        totalItemsProfit.ToString("0.00")
                                    );

                                    // التفنيط
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

                                // ================= NOTES =================
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

                                // ================= SIGNATURES =================
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

        // ✅ دالة بديلة يمكن استخدامها من ViewModel مباشرة
        public static string GenerateInvoicePdf(Invoice invoice)
        {
            // استخدام الاسم الافتراضي: اسم العميل_التاريخ_رقم الفاتورة
            return GenerateInvoicePdf(invoice, null);
        }
    }
}