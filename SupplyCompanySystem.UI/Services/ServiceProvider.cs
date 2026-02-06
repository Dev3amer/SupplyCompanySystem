using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SupplyCompanySystem.Application.Interfaces;
using SupplyCompanySystem.Infrastructure.Data;
using SupplyCompanySystem.Infrastructure.Repositories;
using SupplyCompanySystem.UI.ViewModels;
using SupplyCompanySystem.UI.Views;
using System.Windows.Navigation;

namespace SupplyCompanySystem.UI.Services
{
    public class ServiceProvider
    {
        public static IServiceProvider Provider { get; private set; }
        private static IServiceCollection _services;

        public static void Initialize(IConfiguration configuration)
        {
            _services = new ServiceCollection();

            _services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection")));

            _services.AddScoped<ICustomerRepository, CustomerRepository>();
            _services.AddScoped<IProductRepository, ProductRepository>();
            _services.AddScoped<IInvoiceRepository, InvoiceRepository>();

            _services.AddSingleton<NavigationService>();

            _services.AddTransient<CustomerViewModel>();
            _services.AddTransient<ProductViewModel>();
            _services.AddTransient<InvoiceViewModel>();
            _services.AddTransient<InvoiceArchiveViewModel>();

            _services.AddSingleton<MainView>();

            Provider = _services.BuildServiceProvider();
        }

        public static T GetService<T>()
        {
            return Provider.GetRequiredService<T>();
        }

        public TViewModel CreateViewModel<TViewModel>() where TViewModel : class
        {
            return Provider.GetRequiredService<TViewModel>();
        }
    }
}