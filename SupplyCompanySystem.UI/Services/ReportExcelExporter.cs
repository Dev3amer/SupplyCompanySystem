using ClosedXML.Excel;
using Microsoft.Win32;
using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.UI.ViewModels;
using System.Windows;

namespace SupplyCompanySystem.UI.Services
{
    public class ReportExcelExporter
    {
        public bool ExportSummaryToExcel(SalesSummaryReport summary, DateTime? fromDate, DateTime? toDate, ReportType reportType)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات Excel (*.xlsx)|*.xlsx",
                    FileName = $"تقرير_{GetReportTypeArabic(reportType)}_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx",
                    Title = "تصدير التقرير إلى Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add(GetReportTypeArabic(reportType));

                        // إضافة عنوان التقرير
                        AddReportHeader(worksheet, reportType, fromDate, toDate);

                        int row = 5;
                        ExportSummaryData(worksheet, ref row, summary);

                        // تنسيق الأعمدة
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }

                    ShowSuccessMessage(filePath, "Excel");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Excel", ex.Message);
                return false;
            }
        }

        public bool ExportProductsToExcel(List<ProductSalesReport> products, DateTime? fromDate, DateTime? toDate, ReportType reportType, bool isLeastProducts = false)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات Excel (*.xlsx)|*.xlsx",
                    FileName = $"تقرير_{GetReportTypeArabic(reportType)}_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx",
                    Title = "تصدير التقرير إلى Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add(GetReportTypeArabic(reportType));

                        // إضافة عنوان التقرير
                        AddReportHeader(worksheet, reportType, fromDate, toDate);

                        int row = 5;
                        ExportProductsData(worksheet, ref row, products, isLeastProducts);

                        // تنسيق الأعمدة
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }

                    ShowSuccessMessage(filePath, "Excel");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Excel", ex.Message);
                return false;
            }
        }

        public bool ExportCustomersToExcel(List<CustomerReport> customers, DateTime? fromDate, DateTime? toDate, ReportType reportType, bool isTopInvoiceCustomers = false)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات Excel (*.xlsx)|*.xlsx",
                    FileName = $"تقرير_{GetReportTypeArabic(reportType)}_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx",
                    Title = "تصدير التقرير إلى Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add(GetReportTypeArabic(reportType));

                        // إضافة عنوان التقرير
                        AddReportHeader(worksheet, reportType, fromDate, toDate);

                        int row = 5;
                        ExportCustomersData(worksheet, ref row, customers, isTopInvoiceCustomers);

                        // تنسيق الأعمدة
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }

                    ShowSuccessMessage(filePath, "Excel");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Excel", ex.Message);
                return false;
            }
        }

        public bool ExportDailySalesToExcel(List<DailySalesReport> dailySales, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات Excel (*.xlsx)|*.xlsx",
                    FileName = $"تقرير_المبيعات_اليومية_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx",
                    Title = "تصدير التقرير إلى Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("المبيعات اليومية");

                        // إضافة عنوان التقرير
                        AddReportHeader(worksheet, ReportType.DailySales, fromDate, toDate);

                        int row = 5;
                        ExportDailySalesData(worksheet, ref row, dailySales);

                        // تنسيق الأعمدة
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }

                    ShowSuccessMessage(filePath, "Excel");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Excel", ex.Message);
                return false;
            }
        }

        public bool ExportMonthlySalesToExcel(List<MonthlySalesReport> monthlySales, int year)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات Excel (*.xlsx)|*.xlsx",
                    FileName = $"تقرير_المبيعات_الشهرية_{year}_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx",
                    Title = "تصدير التقرير إلى Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("المبيعات الشهرية");

                        // إضافة عنوان التقرير
                        AddReportHeader(worksheet, ReportType.MonthlySales, null, null, year);

                        int row = 5;
                        ExportMonthlySalesData(worksheet, ref row, monthlySales);

                        // تنسيق الأعمدة
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }

                    ShowSuccessMessage(filePath, "Excel");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Excel", ex.Message);
                return false;
            }
        }

        public bool ExportInventoryToExcel(List<InventoryReport> inventory, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "ملفات Excel (*.xlsx)|*.xlsx",
                    FileName = $"تقرير_المخزون_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx",
                    Title = "تصدير التقرير إلى Excel"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string filePath = saveFileDialog.FileName;

                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("تقرير المخزون");

                        // إضافة عنوان التقرير
                        AddReportHeader(worksheet, ReportType.Inventory, fromDate, toDate);

                        int row = 5;
                        ExportInventoryData(worksheet, ref row, inventory);

                        // تنسيق الأعمدة
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(filePath);
                    }

                    ShowSuccessMessage(filePath, "Excel");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Excel", ex.Message);
                return false;
            }
        }

        #region Private Methods

        private void AddReportHeader(IXLWorksheet worksheet, ReportType reportType, DateTime? fromDate, DateTime? toDate, int? year = null)
        {
            // عنوان التقرير
            var titleCell = worksheet.Cell(1, 1);
            titleCell.Value = $"تقرير {GetReportTypeArabic(reportType)}";
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Font.FontColor = XLColor.White;
            titleCell.Style.Fill.BackgroundColor = XLColor.FromArgb(44, 62, 80);
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            int mergeColumns = GetExcelColumnsCount(reportType);
            worksheet.Range(1, 1, 1, mergeColumns).Merge();

            // معلومات التقرير
            int row = 3;
            worksheet.Cell(row, 1).Value = "نوع التقرير:";
            worksheet.Cell(row, 2).Value = GetReportTypeArabic(reportType);

            if (fromDate.HasValue && toDate.HasValue && reportType != ReportType.MonthlySales)
            {
                worksheet.Cell(++row, 1).Value = "الفترة من:";
                worksheet.Cell(row, 2).Value = fromDate.Value.ToString("yyyy/MM/dd");

                worksheet.Cell(++row, 1).Value = "الفترة إلى:";
                worksheet.Cell(row, 2).Value = toDate.Value.ToString("yyyy/MM/dd");
            }
            else if (year.HasValue)
            {
                worksheet.Cell(++row, 1).Value = "السنة:";
                worksheet.Cell(row, 2).Value = year.Value.ToString();
            }

            worksheet.Cell(++row, 1).Value = "تاريخ التصدير:";
            worksheet.Cell(row, 2).Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm");
        }

        private void ExportSummaryData(IXLWorksheet worksheet, ref int row, SalesSummaryReport summary)
        {
            if (summary == null) return;

            string[] headers = { "المؤشر", "القيمة" };

            // رأس الجدول
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 152, 219);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            row++;

            // البيانات
            var summaryData = new Dictionary<string, object>
            {
                { "عدد الفواتير", summary.TotalInvoices },
                { "عدد العملاء", summary.TotalCustomers },
                { "إجمالي المبيعات", summary.TotalSalesAmount },
                { "إجمالي الربح", summary.TotalProfitAmount },
                { "إجمالي الخصم", summary.TotalDiscountAmount },
                { "عدد المنتجات المباعة", summary.TotalProductsSold },
                { "متوسط الفاتورة", summary.AverageInvoiceAmount },
                { "المنتج الأكثر مبيعاً", $"{summary.MostSoldProductName} ({summary.MostSoldQuantity} قطعة)" },
                { "العميل الأكثر إنفاقاً", $"{summary.TopCustomerName} ({summary.TopCustomerTotal:0.00} جنيهاً)" }
            };

            foreach (var item in summaryData)
            {
                worksheet.Cell(row, 1).Value = item.Key;

                if (item.Value is int intValue)
                {
                    worksheet.Cell(row, 2).Value = intValue;
                }
                else if (item.Value is decimal decimalValue)
                {
                    worksheet.Cell(row, 2).Value = decimalValue;
                    worksheet.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
                }
                else if (item.Value is string stringValue)
                {
                    worksheet.Cell(row, 2).Value = stringValue;
                }
                else
                {
                    worksheet.Cell(row, 2).Value = item.Value?.ToString() ?? string.Empty;
                }

                row++;
            }
        }

        private void ExportProductsData(IXLWorksheet worksheet, ref int row, List<ProductSalesReport> products, bool isLeastProducts)
        {
            if (products == null || products.Count == 0) return;

            string[] headers = { "#", "اسم المنتج", "التصنيف", "كود المنتج", "سعر الوحدة", "الكمية", "الإجمالي", "النسبة %" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 152, 219);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            row++;

            int index = 1;
            foreach (var product in products)
            {
                worksheet.Cell(row, 1).Value = index++;
                worksheet.Cell(row, 2).Value = product.ProductName;
                worksheet.Cell(row, 3).Value = product.Category ?? string.Empty;
                worksheet.Cell(row, 4).Value = product.SKU ?? string.Empty;
                worksheet.Cell(row, 5).Value = product.UnitPrice;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 6).Value = product.TotalQuantity;
                worksheet.Cell(row, 7).Value = product.TotalAmount;
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 8).Value = product.PercentageOfTotal / 100m;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "0.00%";
                row++;
            }
        }

        private void ExportCustomersData(IXLWorksheet worksheet, ref int row, List<CustomerReport> customers, bool isTopInvoiceCustomers)
        {
            if (customers == null || customers.Count == 0) return;

            string[] headers = { "#", "اسم العميل", "رقم الهاتف", "عدد الفواتير", "إجمالي الإنفاق", "متوسط الفاتورة", "آخر فاتورة" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 152, 219);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            row++;

            int index = 1;
            foreach (var customer in customers)
            {
                worksheet.Cell(row, 1).Value = index++;
                worksheet.Cell(row, 2).Value = customer.CustomerName;
                worksheet.Cell(row, 3).Value = customer.PhoneNumber ?? string.Empty;
                worksheet.Cell(row, 4).Value = customer.InvoiceCount;
                worksheet.Cell(row, 5).Value = customer.TotalAmount;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 6).Value = customer.AverageInvoiceAmount;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 7).Value = customer.LastInvoiceDate?.ToString("yyyy/MM/dd") ?? "لا توجد";
                row++;
            }
        }

        private void ExportDailySalesData(IXLWorksheet worksheet, ref int row, List<DailySalesReport> dailySales)
        {
            if (dailySales == null || dailySales.Count == 0) return;

            string[] headers = { "التاريخ", "عدد الفواتير", "عدد العملاء", "إجمالي المبيعات", "إجمالي الربح", "إجمالي الخصم" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 152, 219);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            row++;

            foreach (var sale in dailySales)
            {
                worksheet.Cell(row, 1).Value = sale.Date.ToString("yyyy/MM/dd");
                worksheet.Cell(row, 2).Value = sale.InvoiceCount;
                worksheet.Cell(row, 3).Value = sale.CustomerCount;
                worksheet.Cell(row, 4).Value = sale.TotalAmount;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 5).Value = sale.TotalProfit;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 6).Value = sale.TotalDiscount;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }
        }

        private void ExportMonthlySalesData(IXLWorksheet worksheet, ref int row, List<MonthlySalesReport> monthlySales)
        {
            if (monthlySales == null || monthlySales.Count == 0) return;

            string[] headers = { "الشهر", "عدد الفواتير", "إجمالي المبيعات", "إجمالي الربح", "المتوسط", "النمو %" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 152, 219);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            row++;

            foreach (var sale in monthlySales)
            {
                worksheet.Cell(row, 1).Value = sale.MonthName;
                worksheet.Cell(row, 2).Value = sale.InvoiceCount;
                worksheet.Cell(row, 3).Value = sale.TotalAmount;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 4).Value = sale.TotalProfit;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 5).Value = sale.AverageAmount;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 6).Value = sale.GrowthPercentage / 100m;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "0.00%";
                row++;
            }
        }

        private void ExportInventoryData(IXLWorksheet worksheet, ref int row, List<InventoryReport> inventory)
        {
            if (inventory == null || inventory.Count == 0) return;

            string[] headers = { "اسم المنتج", "التصنيف", "كود المنتج", "سعر الوحدة", "المبيعات الكلية", "إجمالي المبلغ", "آخر شهر", "آخر 3 شهور", "المتوسط الشهري", "الاتجاه" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(row, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(52, 152, 219);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            row++;

            foreach (var item in inventory)
            {
                worksheet.Cell(row, 1).Value = item.ProductName;
                worksheet.Cell(row, 2).Value = item.Category ?? string.Empty;
                worksheet.Cell(row, 3).Value = item.SKU ?? string.Empty;
                worksheet.Cell(row, 4).Value = item.UnitPrice;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 5).Value = item.TotalSoldQuantity;
                worksheet.Cell(row, 6).Value = item.TotalSoldAmount;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 7).Value = item.LastMonthSoldQuantity;
                worksheet.Cell(row, 8).Value = item.LastThreeMonthsSoldQuantity;
                worksheet.Cell(row, 9).Value = item.AverageMonthlySales;
                worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 10).Value = item.SalesTrend ?? string.Empty;
                row++;
            }
        }

        private int GetExcelColumnsCount(ReportType reportType)
        {
            return reportType switch
            {
                ReportType.Summary => 2,
                ReportType.TopProducts or ReportType.LeastProducts => 8,
                ReportType.TopPayingCustomers or ReportType.TopInvoiceCustomers => 7,
                ReportType.DailySales => 6,
                ReportType.MonthlySales => 6,
                ReportType.Inventory => 10,
                _ => 2
            };
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

            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }

        private void ShowErrorMessage(string format, string errorMessage)
        {
            MessageBox.Show($"خطأ في التصدير إلى {format}: {errorMessage}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}