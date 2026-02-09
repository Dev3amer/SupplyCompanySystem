using System.Collections;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace SupplyCompanySystem.UI.Converters
{
    public class RowNumberConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // القيمة الأولى: DataGridRow
                if (values.Length > 0 && values[0] is DataGridRow row)
                {
                    // القيمة الثانية: DataGrid
                    if (values.Length > 1 && values[1] is DataGrid dataGrid)
                    {
                        // محاولة الحصول على رقم الصف من DataGrid
                        int index = dataGrid.ItemContainerGenerator.IndexFromContainer(row);

                        // إذا كان الفهرس -1، حاول الحصول منه بطريقة أخرى
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

                    // إذا لم نستطع الحصول على DataGrid، حاول من العنصر نفسه
                    if (row.DataContext != null && row.DataContext is IList list)
                    {
                        int index = list.IndexOf(row.DataContext);
                        if (index >= 0) return (index + 1).ToString();
                    }
                }

                // إذا فشل كل شيء، ارجع سلسلة فارغة
                return string.Empty;
            }
            catch
            {
                return "?";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        // ⭐ أضف هذه الدوال للتوافق مع IValueConverter القديم
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // هذا للتوافق مع الاستخدام القديم
            if (value is DataGridRow row)
            {
                var dataGrid = ItemsControl.ItemsControlFromItemContainer(row) as DataGrid;
                if (dataGrid != null)
                {
                    int index = dataGrid.ItemContainerGenerator.IndexFromContainer(row);
                    if (index >= 0) return (index + 1).ToString();
                }
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}