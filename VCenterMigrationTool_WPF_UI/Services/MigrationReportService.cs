// Services/MigrationReportService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCenterMigrationTool.Models;

namespace VCenterMigrationTool.Services
{
    public interface IMigrationReportService
    {
        Task<string> GenerateReportAsync(List<MigrationProgress> migrations, string outputPath = null);
        Task<string> GenerateCsvReportAsync(List<MigrationProgress> migrations, string outputPath = null);
    }

    public class MigrationReportService : IMigrationReportService
    {
        public async Task<string> GenerateReportAsync(List<MigrationProgress> migrations, string outputPath = null)
        {
            if (outputPath == null)
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                outputPath = Path.Combine(documentsPath, $"VCenter_Migration_Report_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            }

            var html = GenerateHtmlReport(migrations);
            await File.WriteAllTextAsync(outputPath, html);
            return outputPath;
        }

        public async Task<string> GenerateCsvReportAsync(List<MigrationProgress> migrations, string outputPath = null)
        {
            if (outputPath == null)
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                outputPath = Path.Combine(documentsPath, $"VCenter_Migration_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }

            var csv = GenerateCsvReport(migrations);
            await File.WriteAllTextAsync(outputPath, csv);
            return outputPath;
        }

        private string GenerateHtmlReport(List<MigrationProgress> migrations)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<title>VCenter Migration Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("th { background-color: #f2f2f2; }");
            sb.AppendLine(".success { color: green; }");
            sb.AppendLine(".failed { color: red; }");
            sb.AppendLine(".pending { color: orange; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine($"<h1>VCenter Migration Report</h1>");
            sb.AppendLine($"<p>Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

            // Summary
            var completed = migrations.Count(m => m.Status == MigrationStatus.Completed);
            var failed = migrations.Count(m => m.Status == MigrationStatus.Failed);
            var pending = migrations.Count(m => m.Status == MigrationStatus.Pending);

            sb.AppendLine("<h2>Summary</h2>");
            sb.AppendLine($"<p>Total Migrations: {migrations.Count}</p>");
            sb.AppendLine($"<p class='success'>Completed: {completed}</p>");
            sb.AppendLine($"<p class='failed'>Failed: {failed}</p>");
            sb.AppendLine($"<p class='pending'>Pending: {pending}</p>");

            // Detailed table
            sb.AppendLine("<h2>Migration Details</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>VM Name</th>");
            sb.AppendLine("<th>Status</th>");
            sb.AppendLine("<th>Start Time</th>");
            sb.AppendLine("<th>End Time</th>");
            sb.AppendLine("<th>Duration</th>");
            sb.AppendLine("<th>Data Transferred</th>");
            sb.AppendLine("<th>Error Message</th>");
            sb.AppendLine("</tr>");

            foreach (var migration in migrations)
            {
                var statusClass = migration.Status switch
                {
                    MigrationStatus.Completed => "success",
                    MigrationStatus.Failed => "failed",
                    _ => "pending"
                };

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{migration.VmName}</td>");
                sb.AppendLine($"<td class='{statusClass}'>{migration.StatusText}</td>");
                sb.AppendLine($"<td>{migration.StartTime:yyyy-MM-dd HH:mm:ss}</td>");
                sb.AppendLine($"<td>{migration.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}</td>");
                sb.AppendLine($"<td>{migration.ElapsedTime:hh\\:mm\\:ss}</td>");
                sb.AppendLine($"<td>{migration.DataTransferredFormatted}</td>");
                sb.AppendLine($"<td>{migration.ErrorMessage ?? ""}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GenerateCsvReport(List<MigrationProgress> migrations)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("VM Name,Status,Start Time,End Time,Duration,Data Transferred,Error Message");

            // Data
            foreach (var migration in migrations)
            {
                sb.AppendLine($"\"{migration.VmName}\"," +
                             $"\"{migration.StatusText}\"," +
                             $"\"{migration.StartTime:yyyy-MM-dd HH:mm:ss}\"," +
                             $"\"{migration.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}\"," +
                             $"\"{migration.ElapsedTime:hh\\:mm\\:ss}\"," +
                             $"\"{migration.DataTransferredFormatted}\"," +
                             $"\"{migration.ErrorMessage?.Replace("\"", "\"\"") ?? ""}\"");
            }

            return sb.ToString();
        }
    }
}