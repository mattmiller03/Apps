using System;
using System.Windows;
using System.Windows.Controls;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI
{
    public partial class ConnectionSettingsWindow : Window
    {
        public ConnectionSettingsWindow()
        {
            InitializeComponent();
            // DataContext is set in MainViewModel
        }

        private void SourcePasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConnectionSettingsViewModel vm && vm.SelectedProfile != null)
                vm.SelectedProfile.SourcePassword = ((PasswordBox)sender).Password;
        }

        private void DestPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConnectionSettingsViewModel vm && vm.SelectedProfile != null)
                vm.SelectedProfile.DestinationPassword = ((PasswordBox)sender).Password;
        }
    }
}
