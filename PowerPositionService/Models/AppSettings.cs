namespace PowerPositionService.Models;

public class AppSettings
{
    public int ExtractIntervalMinutes { get; set; }
    public string OutputFolder { get; set; } = string.Empty;
}
