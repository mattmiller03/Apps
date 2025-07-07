// App.xaml.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using VCenterMigrationTool_WPF_UI.Infrastructure.Interfaces;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;
using VCenterMigrationTool_WPF_UI.Views;
using VCenterMigrationTool_WPF_UI.Views.Pages;

namespace VCenterMigrationTool_WPF_UI
{
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }

        public App()
        {
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((ctx, services) =>
                {
                    IConfiguration config = ctx.Configuration;

                    // CORRECT overload: Configure<T>(IConfigurationSection)
                    services.Configure<AppSettings>(config.GetSection("AppSettings"));

                    // your interfaces → implementations
                    services.AddSingleton<ILogger, Logger>();
                    services.AddSingleton<ICredentialManager, WindowsCredentialManager>();
                    services.AddSingleton<IProfileManager, JsonProfileManager>();
                    services.AddSingleton<IPowerShellScriptManager, PowerShellScriptManager>();

                    // re‐add your real PowerShellManager from your repo
                    services.AddSingleton<PowerShellManager>();
                    services.AddSingleton<ConnectionManager>();

                    // ViewModels & Windows
                    services.AddSingleton<DashBoardViewModel>();
                    services.AddTransient<SettingsViewModel>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost.StartAsync()
                      .ConfigureAwait(false);

            var mainWindow = AppHost.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (AppHost)
            {
                await AppHost.StopAsync(TimeSpan.FromSeconds(5))
                          .ConfigureAwait(false);
            }
            base.OnExit(e);

        }
        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
