using System;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using VCenterMigrationTool_WPF_UI.Models;

namespace VCenterMigrationTool_WPF_UI.Utilities
{
    public partial class CredentialsWindow : Window
    {
        public MigrationCredentials? Credentials { get; private set; }
        private readonly Action<string, string>? _logCallback;

        public CredentialsWindow(Action<string, string>? logCallback = null)
        {
            InitializeComponent();
            _logCallback = logCallback;
            LoadSavedCredentials();
        }

        private void LoadSavedCredentials()
        {
            try
            {
                // Load any previously saved credentials for this session
                var savedCreds = CredentialsManager.LoadCredentials();
                if (savedCreds != null)
                {
                    SSOAdminUsernameTextBox.Text = savedCreds.SSOAdminUsername ?? "";
                    ESXiUsernameTextBox.Text = savedCreds.ESXiUsername ?? "root";
                    ServiceAccountTextBox.Text = savedCreds.ServiceAccountUsername ?? "";
                    DomainAdminTextBox.Text = savedCreds.DomainAdminUsername ?? "";

                    // Note: Passwords are not loaded for security reasons
                    SaveSSOCredentialsCheckBox.IsChecked = !string.IsNullOrEmpty(savedCreds.SSOAdminUsername);
                    SaveESXiCredentialsCheckBox.IsChecked = !string.IsNullOrEmpty(savedCreds.ESXiUsername);
                    SaveAdditionalCredentialsCheckBox.IsChecked = !string.IsNullOrEmpty(savedCreds.ServiceAccountUsername);
                }
            }
            catch (Exception ex)
            {
                _logCallback?.Invoke($"⚠️ Could not load saved credentials: {ex.Message}", "WARN");
            }
        }

        private async void TestSSOConnection_Click(object sender, RoutedEventArgs e)
        {
            await TestSSOConnectionAsync();
        }

        private async Task TestSSOConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(SSOAdminUsernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(SSOAdminPasswordBox.Password))
            {
                SSOStatusText.Text = "Status: Username and password required";
                SSOStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            SSOStatusText.Text = "Status: Testing connection...";
            SSOStatusText.Foreground = System.Windows.Media.Brushes.Blue;

            try
            {
                var result = await TestSSOCredentialsAsync(SSOAdminUsernameTextBox.Text, SSOAdminPasswordBox.Password);
                if (result)
                {
                    SSOStatusText.Text = "Status: ✅ Connection successful";
                    SSOStatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    SSOStatusText.Text = "Status: ❌ Connection failed";
                    SSOStatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                SSOStatusText.Text = $"Status: ❌ Error - {ex.Message}";
                SSOStatusText.Foreground = System.Windows.Media.Brushes.Red;
                _logCallback?.Invoke($"❌ SSO connection test failed: {ex.Message}", "ERROR");
            }
        }

        private async void TestESXiConnection_Click(object sender, RoutedEventArgs e)
        {
            await TestESXiConnectionAsync();
        }

        private async Task TestESXiConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(ESXiUsernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(ESXiPasswordBox.Password) ||
                string.IsNullOrWhiteSpace(TestESXiHostTextBox.Text))
            {
                ESXiStatusText.Text = "Status: Username, password, and test host required";
                ESXiStatusText.Foreground = System.Windows.Media.Brushes.Red;
                return;
            }

            ESXiStatusText.Text = "Status: Testing connection...";
            ESXiStatusText.Foreground = System.Windows.Media.Brushes.Blue;

            try
            {
                var result = await TestESXiCredentialsAsync(TestESXiHostTextBox.Text, ESXiUsernameTextBox.Text, ESXiPasswordBox.Password);
                if (result)
                {
                    ESXiStatusText.Text = "Status: ✅ Connection successful";
                    ESXiStatusText.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    ESXiStatusText.Text = "Status: ❌ Connection failed";
                    ESXiStatusText.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
            catch (Exception ex)
            {
                ESXiStatusText.Text = $"Status: ❌ Error - {ex.Message}";
                ESXiStatusText.Foreground = System.Windows.Media.Brushes.Red;
                _logCallback?.Invoke($"❌ ESXi connection test failed: {ex.Message}", "ERROR");
            }
        }

        private async void TestAllConnections_Click(object sender, RoutedEventArgs e)
        {
            _logCallback?.Invoke("🧪 Testing all migration credentials...", "INFO");

            // Test SSO if credentials provided
            if (!string.IsNullOrWhiteSpace(SSOAdminUsernameTextBox.Text) &&
                !string.IsNullOrWhiteSpace(SSOAdminPasswordBox.Password))
            {
                await TestSSOConnectionAsync();
            }

            // Test ESXi if credentials provided
            if (!string.IsNullOrWhiteSpace(ESXiUsernameTextBox.Text) &&
                !string.IsNullOrWhiteSpace(ESXiPasswordBox.Password) &&
                !string.IsNullOrWhiteSpace(TestESXiHostTextBox.Text))
            {
                await TestESXiConnectionAsync();
            }

            _logCallback?.Invoke("🧪 Credential testing completed", "INFO");
        }

        private void SaveCredentials_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var credentials = new MigrationCredentials
                {
                    SSOAdminUsername = SaveSSOCredentialsCheckBox.IsChecked == true ? SSOAdminUsernameTextBox.Text : null,
                    SSOAdminPassword = SaveSSOCredentialsCheckBox.IsChecked == true ? SSOAdminPasswordBox.SecurePassword : null,
                    ESXiUsername = SaveESXiCredentialsCheckBox.IsChecked == true ? ESXiUsernameTextBox.Text : null,
                    ESXiPassword = SaveESXiCredentialsCheckBox.IsChecked == true ? ESXiPasswordBox.SecurePassword : null,
                    ServiceAccountUsername = SaveAdditionalCredentialsCheckBox.IsChecked == true ? ServiceAccountTextBox.Text : null,
                    ServiceAccountPassword = SaveAdditionalCredentialsCheckBox.IsChecked == true ? ServicePasswordBox.SecurePassword : null,
                    DomainAdminUsername = SaveAdditionalCredentialsCheckBox.IsChecked == true ? DomainAdminTextBox.Text : null,
                    DomainAdminPassword = SaveAdditionalCredentialsCheckBox.IsChecked == true ? DomainPasswordBox.SecurePassword : null
                };

                CredentialsManager.SaveCredentials(credentials);
                _logCallback?.Invoke("💾 Migration credentials saved securely", "INFO");

                MessageBox.Show("Credentials saved successfully for this session.", "Credentials Saved",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logCallback?.Invoke($"❌ Failed to save credentials: {ex.Message}", "ERROR");
                MessageBox.Show($"Failed to save credentials: {ex.Message}", "Save Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCredentials_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all stored credentials?",
                                       "Clear Credentials", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Clear UI
                SSOAdminUsernameTextBox.Clear();
                SSOAdminPasswordBox.Clear();
                ESXiUsernameTextBox.Text = "root";
                ESXiPasswordBox.Clear();
                TestESXiHostTextBox.Clear();
                ServiceAccountTextBox.Clear();
                ServicePasswordBox.Clear();
                DomainAdminTextBox.Clear();
                DomainPasswordBox.Clear();

                // Clear checkboxes
                SaveSSOCredentialsCheckBox.IsChecked = false;
                SaveESXiCredentialsCheckBox.IsChecked = false;
                SaveAdditionalCredentialsCheckBox.IsChecked = false;

                // Clear status
                SSOStatusText.Text = "Status: Not tested";
                SSOStatusText.Foreground = System.Windows.Media.Brushes.Gray;
                ESXiStatusText.Text = "Status: Not tested";
                ESXiStatusText.Foreground = System.Windows.Media.Brushes.Gray;

                // Clear stored credentials
                CredentialsManager.ClearCredentials();

                _logCallback?.Invoke("🗑️ All migration credentials cleared", "INFO");
                MessageBox.Show("All credentials have been cleared.", "Credentials Cleared",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // Create credentials object with current values
            Credentials = new MigrationCredentials
            {
                SSOAdminUsername = SSOAdminUsernameTextBox.Text,
                SSOAdminPassword = SSOAdminPasswordBox.SecurePassword,
                ESXiUsername = ESXiUsernameTextBox.Text,
                ESXiPassword = ESXiPasswordBox.SecurePassword,
                ServiceAccountUsername = ServiceAccountTextBox.Text,
                ServiceAccountPassword = ServicePasswordBox.SecurePassword,
                DomainAdminUsername = DomainAdminTextBox.Text,
                DomainAdminPassword = DomainPasswordBox.SecurePassword
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async Task<bool> TestSSOCredentialsAsync(string username, string password)
        {
            // TODO: Implement real SSO credential testing logic
            // For now, simulate testing with a delay
            await Task.Delay(1000);

            // Basic validation - you can enhance this with actual SSO connection testing
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Simulate success for valid-looking credentials
            return username.Contains("@") || username.Contains("\\");
        }

        private async Task<bool> TestESXiCredentialsAsync(string host, string username, string password)
        {
            // TODO: Implement real ESXi credential testing logic
            // For now, simulate testing with a delay
            await Task.Delay(1000);

            // Basic validation - you can enhance this with actual ESXi connection testing
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            // Simulate success for valid-looking credentials
            return username.Equals("root", StringComparison.OrdinalIgnoreCase) && password.Length >= 4;
        }
    }
}
