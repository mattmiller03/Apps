using System.Windows;
using System.Windows.Controls;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI;

public partial class MainWindow : Window
{
    public MainWindow(ConnectionSettingsViewModel viewModel)  // Inject the ViewModel
    {
        InitializeComponent();
        DataContext = viewModel;
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
