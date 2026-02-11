using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.IO;
using System.Linq;
using CsvHelper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DonutMS.Services;

public interface IExportService
{
    Task<string> ExportToCsvAsync(IEnumerable records, string filePath);
    Task<string> ExportToJsonAsync(IEnumerable records, string filePath);
    Task<string> ExportToExcelAsync(IEnumerable records, string filePath);
    Task<string> ExportToPdfAsync(IEnumerable records, string filePath, string title);
    Task<string> GenerateTemplateAsync(Type recordType, string filePath);
}

public class ExportService : IExportService
{
    private readonly ILogger<ExportService> _logger;

    public ExportService(ILogger<ExportService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExportToCsvAsync(IEnumerable records, string filePath)
    {
        EnsureDirectory(filePath);

        var recordList = records.Cast<object>().ToList();
        if (!recordList.Any())
        {
            await File.WriteAllTextAsync(filePath, string.Empty);
            return filePath;
        }

        var recordType = recordList.First().GetType();
        var properties = GetReadableProperties(recordType);

        await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        foreach (var prop in properties)
        {
            csv.WriteField(prop.Name);
        }
        await csv.NextRecordAsync();

        foreach (var record in recordList)
        {
            foreach (var prop in properties)
            {
                var value = prop.GetValue(record);
                csv.WriteField(value);
            }
            await csv.NextRecordAsync();
        }

        _logger.LogInformation("CSV export completed: {Path}", filePath);
        return filePath;
    }

    public async Task<string> ExportToExcelAsync(IEnumerable records, string filePath)
    {
        // Excel-compatible CSV export (can be opened directly in Excel).
        return await ExportToCsvAsync(records, filePath);
    }

    public async Task<string> ExportToJsonAsync(IEnumerable records, string filePath)
    {
        EnsureDirectory(filePath);

        var list = records.Cast<object>().ToList();
        var json = JsonConvert.SerializeObject(list, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);

        _logger.LogInformation("JSON export completed: {Path}", filePath);
        return filePath;
    }

    public async Task<string> ExportToPdfAsync(IEnumerable records, string filePath, string title)
    {
        EnsureDirectory(filePath);

        var lines = BuildTextLines(records, title);
        var pdfBytes = BuildSimplePdf(lines);
        await File.WriteAllBytesAsync(filePath, pdfBytes);

        _logger.LogInformation("PDF export completed: {Path}", filePath);
        return filePath;
    }

    public async Task<string> GenerateTemplateAsync(Type recordType, string filePath)
    {
        EnsureDirectory(filePath);

        var properties = GetReadableProperties(recordType);
        var headers = string.Join(",", properties.Select(p => EscapeCsv(p.Name)));
        await File.WriteAllTextAsync(filePath, headers + Environment.NewLine, Encoding.UTF8);

        _logger.LogInformation("Template generated: {Path}", filePath);
        return filePath;
    }

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static PropertyInfo[] GetReadableProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private static List<string> BuildTextLines(IEnumerable records, string title)
    {
        var lines = new List<string>
        {
            title,
            $"Generated: {DateTime.Now:dd MMM yyyy HH:mm}"
        };

        var list = records.Cast<object>().ToList();
        if (!list.Any())
        {
            lines.Add("No data available.");
            return lines;
        }

        var recordType = list.First().GetType();
        var properties = GetReadableProperties(recordType);
        lines.Add(string.Join(" | ", properties.Select(p => p.Name)));

        foreach (var record in list.Take(200))
        {
            var values = properties
                .Select(p => p.GetValue(record)?.ToString() ?? string.Empty);
            lines.Add(string.Join(" | ", values));
        }

        if (list.Count > 200)
        {
            lines.Add($"... ({list.Count - 200} more rows)");
        }

        return lines;
    }

    private static byte[] BuildSimplePdf(IEnumerable<string> lines)
    {
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, 1024, true);

        writer.Write("%PDF-1.4\n");
        var offsets = new List<long>();

        void WriteObject(int id, string content)
        {
            offsets.Add(ms.Position);
            writer.Write($"{id} 0 obj\n{content}\nendobj\n");
        }

        var textContent = BuildPdfTextContent(lines);
        var textBytes = Encoding.ASCII.GetBytes(textContent);

        WriteObject(1, "<< /Type /Catalog /Pages 2 0 R >>");
        WriteObject(2, "<< /Type /Pages /Count 1 /Kids [3 0 R] >>");
        WriteObject(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> >>");
        WriteObject(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        offsets.Add(ms.Position);
        writer.Write($"5 0 obj\n<< /Length {textBytes.Length} >>\nstream\n");
        writer.Flush();
        ms.Write(textBytes, 0, textBytes.Length);
        writer.Write("\nendstream\nendobj\n");
        writer.Flush();

        var xrefStart = ms.Position;
        writer.Write($"xref\n0 {offsets.Count + 1}\n");
        writer.Write("0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            writer.Write($"{offset:D10} 00000 n \n");
        }
        writer.Write($"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF");
        writer.Flush();

        return ms.ToArray();
    }

    private static string BuildPdfTextContent(IEnumerable<string> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BT");
        builder.AppendLine("/F1 10 Tf");
        builder.AppendLine("50 780 Td");

        foreach (var line in lines)
        {
            var safe = line.Replace("(", "\\(").Replace(")", "\\)");
            builder.AppendLine($"({safe}) Tj");
            builder.AppendLine("0 -14 Td");
        }

        builder.AppendLine("ET");
        return builder.ToString();
    }
}
