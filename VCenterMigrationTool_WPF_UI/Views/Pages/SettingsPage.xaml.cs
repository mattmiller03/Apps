using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI.Views.Pages
{
    public partial class ConnectionSettingsWindow : Window
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();

            // Inject the VM via DI and set DataContext
            DataContext = viewModel;

            // Hook PasswordBox so that SecureString flows into the VM
            PasswordBox.PasswordChanged += OnPasswordBoxChanged;
        }

        private void OnPasswordBoxChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm &&
                sender is PasswordBox passwordBox)
            {
                vm.SelectedProfilePassword = passwordBox.SecurePassword;
            }
        }
    }
}
