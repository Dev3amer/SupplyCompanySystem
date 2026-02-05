using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SupplyCompanySystem.Domain.Entities;

namespace SupplyCompanySystem.Common.Export
{
    public static class ExportService
    {
        public static bool ExportToExcel(List<Customer> customers, string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("العملاء");

                    // رؤوس الأعمدة
                    worksheet.Cell(1, 1).Value = "رقم العميل";
                    worksheet.Cell(1, 2).Value = "اسم العميل";
                    worksheet.Cell(1, 3).Value = "رقم التليفون";
                    worksheet.Cell(1, 4).Value = "العنوان";
                    worksheet.Cell(1, 5).Value = "تاريخ الإنشاء";

                    // تنسيق رؤوس الأعمدة
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;
                    headerRow.Style.Font.FontColor = XLColor.White;
                    headerRow.Style.Fill.BackgroundColor = XLColor.FromArgb(44, 62, 80);
                    headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    headerRow.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    // إضافة البيانات
                    int row = 2;
                    foreach (var customer in customers)
                    {
                        worksheet.Cell(row, 1).Value = customer.Id;
                        worksheet.Cell(row, 2).Value = customer.Name;
                        worksheet.Cell(row, 3).Value = customer.PhoneNumber;
                        worksheet.Cell(row, 4).Value = customer.Address;
                        worksheet.Cell(row, 5).Value = customer.CreatedDate.ToString("yyyy-MM-dd");

                        var currentRow = worksheet.Row(row);
                        currentRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                        row++;
                    }

                    worksheet.Columns("A:E").AdjustToContents();
                    workbook.SaveAs(filePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تصدير Excel: {ex.Message}");
            }
        }

        public static bool ExportToCsv(List<Customer> customers, string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("رقم العميل,اسم العميل,رقم التليفون,العنوان,تاريخ الإنشاء");

                    foreach (var customer in customers)
                    {
                        var line = $"{customer.Id},{customer.Name},{customer.PhoneNumber},{customer.Address},{customer.CreatedDate:yyyy-MM-dd}";
                        writer.WriteLine(line);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تصدير CSV: {ex.Message}");
            }
        }

        public static bool ExportToPdf(List<Customer> customers, string filePath)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20);
                        page.ContentFromLeftToRight();

                        page.Content().Column(column =>
                        {
                            // العنوان
                            column.Item().Text("قائمة العملاء")
                                .FontSize(18)
                                .Bold()
                                .AlignCenter();

                            column.Item().PaddingTop(20);

                            // الجدول
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2.5f);
                                    columns.RelativeColumn(1);
                                });

                                // رؤوس الأعمدة
                                table.Header(header =>
                                {
                                    var headerStyle = new QuestPDF.Infrastructure.TextStyle();

                                    header.Cell().Border(1).Background("#2C3E50").Padding(8)
                                        .Text("العنوان").FontColor("#FFFFFF").Bold();
                                    header.Cell().Border(1).Background("#2C3E50").Padding(8)
                                        .Text("رقم التليفون").FontColor("#FFFFFF").Bold();
                                    header.Cell().Border(1).Background("#2C3E50").Padding(8)
                                        .Text("اسم العميل").FontColor("#FFFFFF").Bold();
                                    header.Cell().Border(1).Background("#2C3E50").Padding(8)
                                        .Text("رقم العميل").FontColor("#FFFFFF").Bold();
                                });

                                // البيانات
                                foreach (var customer in customers)
                                {
                                    table.Cell().Border(1).Padding(5).Text(customer.Address);
                                    table.Cell().Border(1).Padding(5).Text(customer.PhoneNumber);
                                    table.Cell().Border(1).Padding(5).Text(customer.Name);
                                    table.Cell().Border(1).Padding(5).Text(customer.Id.ToString());
                                }
                            });
                        });
                    });
                }).GeneratePdf(filePath);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تصدير PDF: {ex.Message}");
            }
        }

        public static string GetExcelFileName()
        {
            return $"العملاء_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.xlsx";
        }

        public static string GetCsvFileName()
        {
            return $"العملاء_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
        }

        public static string GetPdfFileName()
        {
            return $"العملاء_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf";
        }
    }
}