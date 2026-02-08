using Microsoft.Win32;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.UI.ViewModels;
using System.IO;
using System.Windows;

namespace SupplyCompanySystem.UI.Services
{
    public class ReportPdfExporter
    {
        private string _fontPath;

        public ReportPdfExporter()
        {
            _fontPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets",
                "Fonts"
            );
        }

        public bool ExportSummaryToPdf(SalesSummaryReport summary, DateTime? fromDate, DateTime? toDate, ReportType reportType)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات PDF (*.pdf)|*.pdf",
                    FileName = $"تقرير_{GetReportTypeArabic(reportType)}_{DateTime.Now:yyyy-MM-dd_HHmmss}.pdf",
                    Title = "تصدير التقرير إلى PDF"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

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
                                    // عنوان التقرير
                                    column.Item().Row(row =>
                                    {
                                        row.RelativeColumn(2).Column(col =>
                                        {
                                            col.Item().Text($"تقرير {GetReportTypeArabic(reportType)}")
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

                                            InfoRow("نوع التقرير:", GetReportTypeArabic(reportType));
                                            InfoRow("الفترة من:", fromDate?.ToString("yyyy/MM/dd") ?? "غير محدد");
                                            InfoRow("الفترة إلى:", toDate?.ToString("yyyy/MM/dd") ?? "غير محدد");
                                            InfoRow("تاريخ التصدير:", DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
                                        });
                                    });

                                    column.Item().PaddingVertical(15);
                                    column.Item().LineHorizontal(1);

                                    // محتوى التقرير
                                    AddSummaryToPdf(column, summary);
                                });
                        });
                    }).GeneratePdf(filePath);

                    ShowSuccessMessage(filePath, "PDF");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("PDF", ex.Message);
                return false;
            }
        }

        public bool ExportProductsToPdf(List<ProductSalesReport> products, DateTime? fromDate, DateTime? toDate, ReportType reportType, bool isLeastProducts = false)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات PDF (*.pdf)|*.pdf",
                    FileName = $"تقرير_{GetReportTypeArabic(reportType)}_{DateTime.Now:yyyy-MM-dd_HHmmss}.pdf",
                    Title = "تصدير التقرير إلى PDF"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

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
                                    // عنوان التقرير
                                    AddReportHeader(column, reportType, fromDate, toDate);

                                    // محتوى التقرير
                                    AddProductsToPdf(column, products, isLeastProducts);
                                });
                        });
                    }).GeneratePdf(filePath);

                    ShowSuccessMessage(filePath, "PDF");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("PDF", ex.Message);
                return false;
            }
        }

        public bool ExportCustomersToPdf(List<CustomerReport> customers, DateTime? fromDate, DateTime? toDate, ReportType reportType, bool isTopInvoiceCustomers = false)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات PDF (*.pdf)|*.pdf",
                    FileName = $"تقرير_{GetReportTypeArabic(reportType)}_{DateTime.Now:yyyy-MM-dd_HHmmss}.pdf",
                    Title = "تصدير التقرير إلى PDF"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

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
                                    // عنوان التقرير
                                    AddReportHeader(column, reportType, fromDate, toDate);

                                    // محتوى التقرير
                                    AddCustomersToPdf(column, customers, isTopInvoiceCustomers);
                                });
                        });
                    }).GeneratePdf(filePath);

                    ShowSuccessMessage(filePath, "PDF");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("PDF", ex.Message);
                return false;
            }
        }

        public bool ExportDailySalesToPdf(List<DailySalesReport> dailySales, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات PDF (*.pdf)|*.pdf",
                    FileName = $"تقرير_المبيعات_اليومية_{DateTime.Now:yyyy-MM-dd_HHmmss}.pdf",
                    Title = "تصدير التقرير إلى PDF"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

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
                                    // عنوان التقرير
                                    AddReportHeader(column, ReportType.DailySales, fromDate, toDate);

                                    // محتوى التقرير
                                    AddDailySalesToPdf(column, dailySales);
                                });
                        });
                    }).GeneratePdf(filePath);

                    ShowSuccessMessage(filePath, "PDF");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("PDF", ex.Message);
                return false;
            }
        }

        public bool ExportMonthlySalesToPdf(List<MonthlySalesReport> monthlySales, int year)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات PDF (*.pdf)|*.pdf",
                    FileName = $"تقرير_المبيعات_الشهرية_{year}_{DateTime.Now:yyyy-MM-dd_HHmmss}.pdf",
                    Title = "تصدير التقرير إلى PDF"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

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
                                    // عنوان التقرير
                                    AddReportHeader(column, ReportType.MonthlySales, null, null, year);

                                    // محتوى التقرير
                                    AddMonthlySalesToPdf(column, monthlySales);
                                });
                        });
                    }).GeneratePdf(filePath);

                    ShowSuccessMessage(filePath, "PDF");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("PDF", ex.Message);
                return false;
            }
        }

        public bool ExportInventoryToPdf(List<InventoryReport> inventory, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات PDF (*.pdf)|*.pdf",
                    FileName = $"تقرير_المخزون_{DateTime.Now:yyyy-MM-dd_HHmmss}.pdf",
                    Title = "تصدير التقرير إلى PDF"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

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
                                    // عنوان التقرير
                                    AddReportHeader(column, ReportType.Inventory, fromDate, toDate);

                                    // محتوى التقرير
                                    AddInventoryToPdf(column, inventory);
                                });
                        });
                    }).GeneratePdf(filePath);

                    ShowSuccessMessage(filePath, "PDF");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("PDF", ex.Message);
                return false;
            }
        }

        #region Private Methods

        private void AddReportHeader(ColumnDescriptor column, ReportType reportType, DateTime? fromDate, DateTime? toDate, int? year = null)
        {
            column.Item().Row(row =>
            {
                row.RelativeColumn(2).Column(col =>
                {
                    col.Item().Text($"تقرير {GetReportTypeArabic(reportType)}")
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

                    InfoRow("نوع التقرير:", GetReportTypeArabic(reportType));

                    if (fromDate.HasValue && toDate.HasValue && reportType != ReportType.MonthlySales)
                    {
                        InfoRow("الفترة من:", fromDate.Value.ToString("yyyy/MM/dd"));
                        InfoRow("الفترة إلى:", toDate.Value.ToString("yyyy/MM/dd"));
                    }
                    else if (year.HasValue)
                    {
                        InfoRow("السنة:", year.Value.ToString());
                    }

                    InfoRow("تاريخ التصدير:", DateTime.Now.ToString("yyyy/MM/dd HH:mm"));
                });
            });

            column.Item().PaddingVertical(15);
            column.Item().LineHorizontal(1);
        }

        private void AddSummaryToPdf(ColumnDescriptor column, SalesSummaryReport summary)
        {
            if (summary == null) return;

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f);
                    columns.RelativeColumn(1.0f);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(6)
                        .Text("المؤشر").Bold().AlignCenter();

                    header.Cell().Border(1).Padding(6)
                        .Text("القيمة").Bold().AlignCenter();
                });

                void AddRow(string indicator, string value)
                {
                    table.Cell().Border(1).Padding(5)
                        .Text(indicator).AlignRight();

                    table.Cell().Border(1).Padding(5)
                        .Text(value).AlignRight();
                }

                AddRow("عدد الفواتير", summary.TotalInvoices.ToString());
                AddRow("عدد العملاء", summary.TotalCustomers.ToString());
                AddRow("عدد المنتجات المباعة", summary.TotalProductsSold.ToString());
                AddRow("إجمالي المبيعات", summary.TotalSalesAmount.ToString("0.00"));
                AddRow("إجمالي الربح", summary.TotalProfitAmount.ToString("0.00"));
                AddRow("إجمالي الخصم", summary.TotalDiscountAmount.ToString("0.00"));
                AddRow("متوسط الفاتورة", summary.AverageInvoiceAmount.ToString("0.00"));
                AddRow("المنتج الأكثر مبيعاً",
                    $"{summary.MostSoldProductName} ({summary.MostSoldQuantity} قطعة)");
                AddRow("العميل الأكثر إنفاقاً",
                    $"{summary.TopCustomerName} ({summary.TopCustomerTotal:0.00} جنيهاً)");
            });
        }

        private void AddProductsToPdf(ColumnDescriptor column, List<ProductSalesReport> products, bool isLeastProducts)
        {
            if (products == null || products.Count == 0) return;

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.6f);   // #
                    columns.RelativeColumn(1.5f);   // اسم المنتج
                    columns.RelativeColumn(1.0f);   // التصنيف
                    columns.RelativeColumn(0.8f);   // كود المنتج
                    columns.RelativeColumn(1.0f);   // سعر الوحدة
                    columns.RelativeColumn(0.8f);   // الكمية
                    columns.RelativeColumn(1.0f);   // الإجمالي
                    columns.RelativeColumn(0.8f);   // النسبة %
                });

                table.Header(header =>
                {
                    string[] headers = { "#", "اسم المنتج", "التصنيف", "الكود", "سعر الوحدة", "الكمية", "الإجمالي", "النسبة %" };

                    foreach (var headerText in headers)
                    {
                        header.Cell()
                            .Border(1)
                            .Padding(5)
                            .Text(headerText)
                            .Bold()
                            .AlignCenter()
                            .FontSize(9);
                    }
                });

                int index = 1;
                foreach (var product in products)
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

                    Cell(index.ToString());
                    Cell(product.ProductName, true);
                    Cell(product.Category);
                    Cell(product.SKU ?? "");
                    Cell(product.UnitPrice.ToString("0.00"), true);
                    Cell(product.TotalQuantity.ToString());
                    Cell(product.TotalAmount.ToString("0.00"), true);
                    Cell(product.PercentageOfTotal.ToString("0.00") + "%", true);

                    index++;
                }
            });
        }

        private void AddCustomersToPdf(ColumnDescriptor column, List<CustomerReport> customers, bool isTopInvoiceCustomers)
        {
            if (customers == null || customers.Count == 0) return;

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(0.6f);   // #
                    columns.RelativeColumn(1.5f);   // اسم العميل
                    columns.RelativeColumn(1.0f);   // رقم الهاتف
                    columns.RelativeColumn(0.8f);   // عدد الفواتير
                    columns.RelativeColumn(1.0f);   // إجمالي الإنفاق
                    columns.RelativeColumn(1.0f);   // متوسط الفاتورة
                    columns.RelativeColumn(1.0f);   // آخر فاتورة
                });

                table.Header(header =>
                {
                    string[] headers = { "#", "اسم العميل", "رقم الهاتف", "عدد الفواتير", "إجمالي الإنفاق", "متوسط الفاتورة", "آخر فاتورة" };

                    foreach (var headerText in headers)
                    {
                        header.Cell()
                            .Border(1)
                            .Padding(5)
                            .Text(headerText)
                            .Bold()
                            .AlignCenter()
                            .FontSize(9);
                    }
                });

                int index = 1;
                foreach (var customer in customers)
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

                    Cell(index.ToString());
                    Cell(customer.CustomerName, true);
                    Cell(customer.PhoneNumber ?? "");
                    Cell(customer.InvoiceCount.ToString());
                    Cell(customer.TotalAmount.ToString("0.00"), true);
                    Cell(customer.AverageInvoiceAmount.ToString("0.00"), true);
                    Cell(customer.LastInvoiceDate?.ToString("yyyy/MM/dd") ?? "لا توجد");

                    index++;
                }
            });
        }

        private void AddDailySalesToPdf(ColumnDescriptor column, List<DailySalesReport> dailySales)
        {
            if (dailySales == null || dailySales.Count == 0) return;

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.2f);   // التاريخ
                    columns.RelativeColumn(0.8f);   // عدد الفواتير
                    columns.RelativeColumn(0.8f);   // عدد العملاء
                    columns.RelativeColumn(1.0f);   // إجمالي المبيعات
                    columns.RelativeColumn(1.0f);   // إجمالي الربح
                    columns.RelativeColumn(1.0f);   // إجمالي الخصم
                });

                table.Header(header =>
                {
                    string[] headers = { "التاريخ", "عدد الفواتير", "عدد العملاء", "إجمالي المبيعات", "إجمالي الربح", "إجمالي الخصم" };

                    foreach (var headerText in headers)
                    {
                        header.Cell()
                            .Border(1)
                            .Padding(5)
                            .Text(headerText)
                            .Bold()
                            .AlignCenter()
                            .FontSize(9);
                    }
                });

                foreach (var sale in dailySales)
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

                    Cell(sale.Date.ToString("yyyy/MM/dd"));
                    Cell(sale.InvoiceCount.ToString());
                    Cell(sale.CustomerCount.ToString());
                    Cell(sale.TotalAmount.ToString("0.00"), true);
                    Cell(sale.TotalProfit.ToString("0.00"), true);
                    Cell(sale.TotalDiscount.ToString("0.00"), true);
                }
            });
        }

        private void AddMonthlySalesToPdf(ColumnDescriptor column, List<MonthlySalesReport> monthlySales)
        {
            if (monthlySales == null || monthlySales.Count == 0) return;

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.0f);   // الشهر
                    columns.RelativeColumn(0.8f);   // عدد الفواتير
                    columns.RelativeColumn(1.0f);   // إجمالي المبيعات
                    columns.RelativeColumn(1.0f);   // إجمالي الربح
                    columns.RelativeColumn(0.8f);   // المتوسط
                    columns.RelativeColumn(0.8f);   // النمو %
                });

                table.Header(header =>
                {
                    string[] headers = { "الشهر", "عدد الفواتير", "إجمالي المبيعات", "إجمالي الربح", "المتوسط", "النمو %" };

                    foreach (var headerText in headers)
                    {
                        header.Cell()
                            .Border(1)
                            .Padding(5)
                            .Text(headerText)
                            .Bold()
                            .AlignCenter()
                            .FontSize(9);
                    }
                });

                foreach (var sale in monthlySales)
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

                    Cell(sale.MonthName);
                    Cell(sale.InvoiceCount.ToString());
                    Cell(sale.TotalAmount.ToString("0.00"), true);
                    Cell(sale.TotalProfit.ToString("0.00"), true);
                    Cell(sale.AverageAmount.ToString("0.00"), true);
                    Cell(sale.GrowthPercentage.ToString("+0.00;-0.00") + "%", true);
                }
            });
        }

        private void AddInventoryToPdf(ColumnDescriptor column, List<InventoryReport> inventory)
        {
            if (inventory == null || inventory.Count == 0) return;

            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1.5f);   // اسم المنتج
                    columns.RelativeColumn(1.0f);   // التصنيف
                    columns.RelativeColumn(0.8f);   // كود المنتج
                    columns.RelativeColumn(1.0f);   // سعر الوحدة
                    columns.RelativeColumn(0.8f);   // المبيعات الكلية
                    columns.RelativeColumn(1.0f);   // إجمالي المبلغ
                    columns.RelativeColumn(0.6f);   // آخر شهر
                    columns.RelativeColumn(0.8f);   // آخر 3 شهور
                    columns.RelativeColumn(0.8f);   // المتوسط الشهري
                    columns.RelativeColumn(0.6f);   // الاتجاه
                });

                table.Header(header =>
                {
                    string[] headers = { "اسم المنتج", "التصنيف", "الكود", "سعر الوحدة", "المبيعات الكلية", "إجمالي المبلغ", "آخر شهر", "آخر 3 شهور", "المتوسط", "الاتجاه" };

                    foreach (var headerText in headers)
                    {
                        header.Cell()
                            .Border(1)
                            .Padding(5)
                            .Text(headerText)
                            .Bold()
                            .AlignCenter()
                            .FontSize(9);
                    }
                });

                foreach (var item in inventory)
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

                    Cell(item.ProductName, true);
                    Cell(item.Category);
                    Cell(item.SKU ?? "");
                    Cell(item.UnitPrice.ToString("0.00"), true);
                    Cell(item.TotalSoldQuantity.ToString());
                    Cell(item.TotalSoldAmount.ToString("0.00"), true);
                    Cell(item.LastMonthSoldQuantity.ToString());
                    Cell(item.LastThreeMonthsSoldQuantity.ToString());
                    Cell(item.AverageMonthlySales.ToString("0.00"), true);
                    Cell(item.SalesTrend);
                }
            });
        }

        private void RegisterFonts()
        {
            string cairoRegularPath = Path.Combine(_fontPath, "Cairo-Regular.ttf");
            string cairoBoldPath = Path.Combine(_fontPath, "Cairo-Bold.ttf");

            if (File.Exists(cairoRegularPath))
            {
                FontManager.RegisterFont(File.OpenRead(cairoRegularPath));
            }

            if (File.Exists(cairoBoldPath))
            {
                FontManager.RegisterFont(File.OpenRead(cairoBoldPath));
            }
        }

        private string GetReportTypeArabic(ReportType reportType)
        {
            return reportType switch
            {
                ReportType.Summary => "ملخص المبيعات",
                ReportType.TopProducts => "أكثر المنتجات مبيعاً",
                ReportType.LeastProducts => "أقل المنتجات مبيعاً",
                ReportType.TopPayingCustomers => "أكثر العملاء إنفاقاً",
                ReportType.TopInvoiceCustomers => "أكثر العملاء طلباً للعروض",
                ReportType.DailySales => "المبيعات اليومية",
                ReportType.MonthlySales => "المبيعات الشهرية",
                ReportType.Inventory => "تقرير المخزون",
                _ => "تقرير"
            };
        }

        private void ShowSuccessMessage(string filePath, string format)
        {
            MessageBox.Show(
                $"تم تصدير التقرير بنجاح إلى ملف {format}\n" +
                $"المسار: {filePath}",
                "تصدير ناجح",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }

        private void ShowErrorMessage(string format, string errorMessage)
        {
            MessageBox.Show($"خطأ في التصدير إلى {format}: {errorMessage}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}