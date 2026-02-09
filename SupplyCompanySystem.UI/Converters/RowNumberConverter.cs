using System.Collections;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class RowNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // إذا كان value هو DataGridRow
            if (value is DataGridRow row)
            {
                // محاولة الحصول على رقم الصف من DataGrid
                if (ItemsControl.ItemsControlFromItemContainer(row) is DataGrid dataGrid)
                {
                    try
                    {
                        int index = dataGrid.ItemContainerGenerator.IndexFromContainer(row);
                        // إذا كان الفهرس -1 (غير معروف)، حاول الحصول منه بطريقة أخرى
                        if (index == -1)
                        {
                            // محاولة الحصول من ItemsSource
                            if (dataGrid.ItemsSource != null)
                            {
                                var itemsList = dataGrid.ItemsSource as IList;
                                if (itemsList != null)
                                {
                                    index = itemsList.IndexOf(row.DataContext);
                                    if (index >= 0) return (index + 1).ToString();
                                }
                            }
                            return "?";
                        }
                        return (index + 1).ToString(); // +1 لتبدأ من 1 بدلاً من 0
                    }
                    catch
                    {
                        return "?";
                    }
                }
            }

            // إذا كان value ليس DataGridRow، حاول الحصول على الفهرس من parameter
            if (parameter is DataGrid dataGridParam && value != null)
            {
                try
                {
                    if (dataGridParam.ItemsSource != null)
                    {
                        var itemsList = dataGridParam.ItemsSource as IList;
                        if (itemsList != null)
                        {
                            int index = itemsList.IndexOf(value);
                            if (index >= 0) return (index + 1).ToString();
                        }
                    }
                }
                catch
                {
                    // تجاهل الخطأ
                }
            }

            // إذا فشل كل شيء، ارجع سلسلة فارغة
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}