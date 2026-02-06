using System.Windows;

namespace SupplyCompanySystem.UI.Views
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }

        public void UpdateProgress(string message, int percentage)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressText.Text = message;
                ProgressBar.Value = percentage;
                PercentageText.Text = $"{percentage}%";
            });
        }
    }
}