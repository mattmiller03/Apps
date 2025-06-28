using System;
using System.Windows;
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

            // Subscribe to DialogResult changes from ViewModel
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConnectionSettingsViewModel.DialogResult))
            {
                if (_viewModel.DialogResult.HasValue)
                {
                    DialogResult = _viewModel.DialogResult; // Set the DialogResult
                    Close();
                }
            }
        }

        // Parameterless constructor for design-time support (or testing)
        public ConnectionSettingsWindow() : this(new ConnectionSettingsViewModel(new ConnectionManager(), new DialogService()))
        {
        }

        public ConnectionSettings? SelectedSettings => _viewModel.SelectedProfile; // Read-only property
    }
}
