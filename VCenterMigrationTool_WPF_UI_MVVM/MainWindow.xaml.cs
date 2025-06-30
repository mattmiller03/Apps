using System.Windows;
using System.Windows.Controls;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI;

public partial class MainWindow : Window
{
    public ConnectionSettingsViewModel ConnectionSettingsVM { get; }
    public MigrationViewModel MigrationVM { get; }
    public ValidationViewModel ValidationVM { get; }
    public LogViewModel LogVM { get; }

    public MainWindow()
    {
        InitializeComponent();

        ConnectionSettingsVM = new ConnectionSettingsViewModel();
        MigrationVM = new MigrationViewModel();
        ValidationVM = new ValidationViewModel();
        LogVM = new LogViewModel();

        Resources["ConnectionSettingsViewModel"] = ConnectionSettingsVM;
        Resources["MigrationViewModel"] = MigrationVM;
        Resources["ValidationViewModel"] = ValidationVM;
        Resources["LogViewModel"] = LogVM;
    }

    // Optional: PasswordBox <-> ViewModel relay (since WPF does not bind Password)
    private void SourcePasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (Resources["ConnectionSettingsViewModel"] is ConnectionSettingsViewModel vm)
            vm.CurrentSettings.SourcePassword = ((PasswordBox)sender).Password;
    }
    private void DestPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (Resources["ConnectionSettingsViewModel"] is ConnectionSettingsViewModel vm)
            vm.CurrentSettings.DestinationPassword = ((PasswordBox)sender).Password;
    }
}
