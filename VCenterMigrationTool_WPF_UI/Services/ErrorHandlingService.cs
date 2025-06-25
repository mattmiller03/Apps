// Services/ErrorHandlingService.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace VCenterMigrationTool.Services
{
    public interface IErrorHandlingService
    {
        Task LogErrorAsync(Exception exception, string context = "");
        void ShowErrorDialog(string message, string title = "Error");
        Task<bool> HandleCriticalErrorAsync(Exception exception, string context = "");
    }

    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly string _logFilePath;

        public ErrorHandlingService()
        {
            var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VCenterMigrationTool", "Logs");
            Directory.CreateDirectory(logDirectory);
            _logFilePath = Path.Combine(logDirectory, $"error_log_{DateTime.Now:yyyyMMdd}.txt");
        }

        public async Task LogErrorAsync(Exception exception, string context = "")
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}\n" +
                              $"Exception: {exception.GetType().Name}\n" +
                              $"Message: {exception.Message}\n" +
                              $"StackTrace: {exception.StackTrace}\n" +
                              $"{"".PadRight(80, '-')}\n";

                await File.AppendAllTextAsync(_logFilePath, logEntry);
            }
            catch
            {
                // Fail silently if logging fails
            }
        }

        public void ShowErrorDialog(string message, string title = "Error")
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        public async Task<bool> HandleCriticalErrorAsync(Exception exception, string context = "")
        {
            await LogErrorAsync(exception, context);

            var result = Application.Current.Dispatcher.Invoke(() =>
            {
                return MessageBox.Show(
                    $"A critical error occurred: {exception.Message}\n\nWould you like to continue?",
                    "Critical Error",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Error);
            });

            return result == MessageBoxResult.Yes;
        }
    }
}