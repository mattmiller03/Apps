using System;
using System.Windows;
using System.Windows.Controls;
using VCenterMigrationTool_WPF_UI.Models;
using VCenterMigrationTool_WPF_UI.Utilities;
using VCenterMigrationTool_WPF_UI.ViewModels;

namespace VCenterMigrationTool_WPF_UI
{
    public partial class ConnectionSettingsWindow : Window
    {
        private readonly ConnectionSettingsViewModel _viewModel;

        public ConnectionSettingsWindow(ConnectionSettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void SourcePasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedProfile != null)
                _viewModel.SelectedProfile.SourcePassword = ((PasswordBox)sender).Password;
        }

        private void DestPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedProfile != null)
                _viewModel.SelectedProfile.DestinationPassword = ((PasswordBox)sender).Password;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConnectionSettingsViewModel.DialogResult))
            {
                if (_viewModel.DialogResult.HasValue)
                {
                    DialogResult = _viewModel.DialogResult;
                    Close();
                }
            }
        }

        public ConnectionSettings? SelectedSettings => _viewModel.SelectedProfile;

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
        }
    }
}
