using System.Windows;
using System.Windows.Controls;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI.Views
{
    public partial class ConnectionSettingsWindow : Window
    {
        public ConnectionSettingsWindow(ConnectionSettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void SourcePasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConnectionSettingsViewModel vm && sender is PasswordBox pb)
            {
                // Directly set the password property
                vm.CurrentSettings.SourcePassword = pb.Password;
            }
        }

        private void DestPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConnectionSettingsViewModel vm && sender is PasswordBox pb)
            {
                // Directly set the password property
                vm.CurrentSettings.DestinationPassword = pb.Password;
            }
        }
    }
}
