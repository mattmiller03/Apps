using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using VCenterMigrationTool_WPF_UI.Infrastructure;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            var sc = new ServiceCollection();
            sc.AddSingleton<Logger>();
            sc.AddSingleton<PowerShellManager>();
            sc.AddSingleton<MainViewModel>();
            Services = sc.BuildServiceProvider();

            var wnd = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
            wnd.Show();
            base.OnStartup(e);
        }
    }
}
