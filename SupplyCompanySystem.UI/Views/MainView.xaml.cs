using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SupplyCompanySystem.UI.Views
{
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
            InitializeClock();
        }

        private void InitializeClock()
        {
            // تحديث الساعة كل ثانية
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                ClockTextBlock.Text = DateTime.Now.ToString("hh:mm:ss tt");
            };
            timer.Start();

            // تحديث أول مرة
            ClockTextBlock.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is string tag)
            {
                switch (tag)
                {
                    case "NewInvoice":
                        ShowNewInvoicePage();
                        break;
                    case "Customers":
                        ShowCustomersPage();
                        break;
                    case "Products":
                        ShowProductsPage();
                        break;
                    case "Reports":
                        ShowReportsPage();
                        break;
                    case "Settings":
                        ShowSettingsPage();
                        break;
                    case "Archive":
                        ShowArchivePage();
                        break;
                }
            }
        }

        private void ShowNewInvoicePage()
        {
            ContentArea.Content = new Views.InvoicesView();
        }

        private void ShowCustomersPage()
        {
            ContentArea.Content = new Views.CustomersView();
        }

        private void ShowProductsPage()
        {
            ContentArea.Content = new Views.ProductsView();
        }

        private void ShowArchivePage()
        {
            ContentArea.Content = new Views.InvoiceArchiveView();
        }

        private void ShowReportsPage()
        {
            ContentArea.Content = new TextBlock
            {
                Text = "صفحة التقارير",
                FontSize = 20,
                Foreground = System.Windows.Media.Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private void ShowSettingsPage()
        {
            ContentArea.Content = new TextBlock
            {
                Text = "صفحة الإعدادات",
                FontSize = 20,
                Foreground = System.Windows.Media.Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("تم تسجيل الخروج بنجاح", "تسجيل الخروج", MessageBoxButton.OK, MessageBoxImage.Information);
            System.Windows.Application.Current.Shutdown();
        }
    }
}