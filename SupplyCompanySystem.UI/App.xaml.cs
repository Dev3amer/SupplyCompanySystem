using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SupplyCompanySystem.Infrastructure.Data;
using SupplyCompanySystem.UI.Services;
using SupplyCompanySystem.UI.Views;
using System.Windows;

namespace SupplyCompanySystem.UI
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // ✅ منع فتح النافذة الافتراضية (التي في App.xaml)
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;

                // Load configuration from appsettings.json
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Initialize the custom ServiceProvider
                ServiceProvider.Initialize(configuration);

                // Apply database migrations
                ApplyDatabaseMigrations(configuration);

                // ✅ إنشاء النافذة الرئيسية يدويًا (مرة واحدة فقط)
                var mainWindow = ServiceProvider.GetService<MainView>();

                // ✅ تعيين النافذة الرئيسية بشكل صريح
                this.MainWindow = mainWindow;

                // ✅ فتح النافذة في ملء الشاشة
                mainWindow.WindowState = WindowState.Maximized;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء تشغيل التطبيق:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private void ApplyDatabaseMigrations(IConfiguration configuration)
        {
            try
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("لم يتم العثور على Connection String في الإعدادات");
                }

                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlServer(connectionString);

                using (var context = new AppDbContext(optionsBuilder.Options))
                {
                    // Check if database exists
                    var databaseExists = context.Database.CanConnect();

                    if (!databaseExists)
                    {
                        var result = MessageBox.Show("لم يتم العثور على قاعدة البيانات. هل تريد إنشاء قاعدة بيانات جديدة؟",
                            "تهيئة النظام", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        if (result != MessageBoxResult.Yes)
                        {
                            throw new Exception("تم إلغاء تشغيل البرنامج بسبب عدم وجود قاعدة بيانات");
                        }

                        MessageBox.Show("جارٍ إنشاء قاعدة البيانات...",
                            "الرجاء الانتظار", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    // Apply all pending migrations
                    var pendingMigrations = context.Database.GetPendingMigrations().ToList();

                    if (pendingMigrations.Any())
                    {
                        MessageBox.Show($"جارٍ تطبيق {pendingMigrations.Count} تحديث على قاعدة البيانات...",
                            "تحديث النظام", MessageBoxButton.OK, MessageBoxImage.Information);

                        context.Database.Migrate();

                        MessageBox.Show("تم تحديث قاعدة البيانات بنجاح!",
                            "اكتمل", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                    // Ensure database is created if no migrations exist
                    context.Database.EnsureCreated();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"فشل في تهيئة قاعدة البيانات:\n{ex.Message}", ex);
            }
        }
    }
}