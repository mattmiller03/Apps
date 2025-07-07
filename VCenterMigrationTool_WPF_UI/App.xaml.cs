// App.xaml.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Windows;
using VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;
using VCenterMigrationTool_WPF_UI.Views;

namespace VCenterMigrationTool_WPF_UI
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }
        public IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Configure AppSettings
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));

            // Register other services
            services.AddSingleton<ILogger, Logger>();
            services.AddSingleton<IPowerShellScriptManager, PowerShellScriptManager>();
            services.AddSingleton<ICredentialManager, WindowsCredentialManager>();
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<MainWindow>();

            // Register ViewModels and Windows
            services.AddTransient<ConnectionSettingsViewModel>();
            services.AddTransient<ConnectionSettingsWindow>();
        }
    }
}
