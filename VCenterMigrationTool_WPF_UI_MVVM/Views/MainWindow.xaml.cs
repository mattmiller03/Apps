using System.Windows;
using System.Windows.Controls;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI.Views
{
    public partial class MainWindow : Window
    {
        // Parameterless constructor for XAML
        public MainWindow()
        {
            InitializeComponent();
        }

        // Constructor with ViewModel for programmatic creation
        public MainWindow(MainViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void SourcePasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ConnectionSettingsViewModel.CurrentSettings.SourcePassword =
                    ((PasswordBox)sender).Password;
            }
        }

        private void DestPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ConnectionSettingsViewModel.CurrentSettings.DestinationPassword =
                    ((PasswordBox)sender).Password;
            }
        }

    }
}
