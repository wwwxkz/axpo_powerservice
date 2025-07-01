using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PowerPositionService.Interfaces;

namespace PowerPositionService.Services;

public class FileManager : IFileManager
{
    private const string CsvHeader = "Local Time,Volume";
    private const string FileNamePrefix = "PowerPosition_";
    private const string FileNameSuffix = ".csv";
    
    private readonly ILogger<FileManager> _logger;

    public FileManager(ILogger<FileManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string GetFileName(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be null or whitespace", nameof(folderPath));

        try
        {
            _logger.LogDebug("Ensuring output directory exists: {Directory}", folderPath);
            Directory.CreateDirectory(folderPath);
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
            var fileName = $"{FileNamePrefix}{timestamp}{FileNameSuffix}";
            var fullPath = Path.Combine(folderPath, fileName);
            
            _logger.LogInformation("Generated output file path: {FilePath}", fullPath);
            return fullPath;
        }
        catch (Exception ex) when (LogAndWrapException(ex, "Error generating file name"))
        {
            // This block will never be reached because LogAndWrapException returns false
            throw;
        }
    }

    public async Task WriteToFileAsync(string filePath, Dictionary<string, double> data)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or whitespace", nameof(filePath));
        
        if (data == null)
            throw new ArgumentNullException(nameof(data), "Data dictionary cannot be null");

        _logger.LogDebug("Writing {DataCount} records to file: {FilePath}", data.Count, filePath);
        
        try
        {
            // Create a temporary file first to ensure atomic write operation
            var tempFilePath = Path.Combine(
                Path.GetDirectoryName(filePath) ?? string.Empty,
                Path.GetRandomFileName());
            
            try
            {
                // Write to temporary file
                await using (var writer = new StreamWriter(tempFilePath))
                {
                    await writer.WriteLineAsync(CsvHeader);
                    
                    // Write data rows in order of time
                    foreach (var (time, volume) in data.OrderBy(x => x.Key))
                    {
                        var formattedVolume = volume.ToString("F2", CultureInfo.InvariantCulture);
                        await writer.WriteLineAsync($"{time},{formattedVolume}");
                    }
                }
                
                // Move the temporary file to the target location (atomic operation)
                File.Move(tempFilePath, filePath, overwrite: true);
                
                _logger.LogInformation("Successfully wrote {RecordCount} records to file: {FilePath}", 
                    data.Count, filePath);
            }
            finally
            {
                // Clean up the temporary file if it still exists
                if (File.Exists(tempFilePath))
                {
                    try { File.Delete(tempFilePath); }
                    catch { /* Ignore cleanup errors */ }
                }
            }
        }
        catch (Exception ex) when (LogAndWrapException(ex, $"Error writing to file: {filePath}"))
        {
            // This block will never be reached because LogAndWrapException returns false
            throw;
        }
    }
    
    private bool LogAndWrapException(Exception ex, string message)
    {
        _logger.LogError(ex, message);
        return false; // Always return false to allow the exception to propagate
    }
}
