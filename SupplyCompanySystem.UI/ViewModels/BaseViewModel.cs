using SupplyCompanySystem.UI.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SupplyCompanySystem.UI.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // Commands
        public RelayCommand ShowDashboardCommand { get; }
        public RelayCommand ShowProductsCommand { get; }
        public RelayCommand ShowCustomersCommand { get; }
        public RelayCommand ShowInvoicesCommand { get; }
        public RelayCommand ShowArchiveCommand { get; }
        public RelayCommand ShowReportsCommand { get; }
        public BaseViewModel()
        {
            ShowDashboardCommand = new RelayCommand(_ => ShowDashboard());
            ShowProductsCommand = new RelayCommand(_ => ShowProducts());
            ShowCustomersCommand = new RelayCommand(_ => ShowCustomers());
            ShowInvoicesCommand = new RelayCommand(_ => ShowInvoices());
            ShowArchiveCommand = new RelayCommand(_ => ShowArchive());
            ShowReportsCommand = new RelayCommand(_ => ShowReports());
        }

        private void ShowDashboard()
        {
            // عرض لوحة التحكم الرئيسية
        }

        private void ShowProducts()
        {
            // عرض شاشة المنتجات
        }

        private void ShowCustomers()
        {
            // عرض شاشة العملاء
        }

        private void ShowInvoices()
        {
            // عرض شاشة الفواتير
        }

        private void ShowArchive()
        {
            // عرض الأرشيف
        }

        private void ShowReports()
        {

        }

        private void ShowSettings()
        {

        }

        private void Logout()
        {

        }
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
