using Microsoft.Extensions.Options;
using PowerPositionService.Interfaces;
using PowerPositionService.Models;

namespace PowerPositionService.Workers;

public class PowerPositionWorker : BackgroundService, IDisposable
{
    private readonly ILogger<PowerPositionWorker> _logger;
    private readonly IPowerService _powerService;
    private readonly IPositionCalculator _positionCalculator;
    private readonly IFileManager _fileManager;
    private readonly AppSettings _settings;
    private readonly TimeSpan _extractInterval;
    private readonly object _lock = new();
    private Timer? _timer;
    private bool _isRunning;
    private bool _disposed;

    public PowerPositionWorker(
        ILogger<PowerPositionWorker> logger,
        IPowerService powerService,
        IPositionCalculator positionCalculator,
        IFileManager fileManager,
        IOptions<AppSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _powerService = powerService ?? throw new ArgumentNullException(nameof(powerService));
        _positionCalculator = positionCalculator ?? throw new ArgumentNullException(nameof(positionCalculator));
        _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        
        if (_settings.ExtractIntervalMinutes <= 0)
        {
            throw new ArgumentException("Extract interval must be greater than zero", nameof(settings));
        }
        
        _extractInterval = TimeSpan.FromMinutes(_settings.ExtractIntervalMinutes);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Power Position Worker started with {Interval} minute interval", _settings.ExtractIntervalMinutes);
        
        // Set up timer for periodic execution
        _timer = new Timer(
            callback: _ => _ = GeneratePowerPositionReportAsync(stoppingToken),
            state: null,
            dueTime: TimeSpan.Zero,  // Start immediately
            period: _extractInterval);

        return Task.CompletedTask;
    }

    private async Task GeneratePowerPositionReportAsync(CancellationToken cancellationToken)
    {
        if (_isRunning)
        {
            _logger.LogDebug("Previous execution is still running. Skipping this interval.");
            return;
        }

        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.LogDebug("Previous execution is still running (double-checked). Skipping this interval.");
                return;
            }
            _isRunning = true;
        }

        try
        {
            _logger.LogInformation("Starting power position report generation");
            
            // Get the current date (in UTC)
            var date = DateTime.UtcNow.Date;
            
            // Get the trades from the power service
            _logger.LogDebug("Retrieving power trades for {Date:yyyy-MM-dd}", date);
            var trades = await _powerService.GetTradesAsync(date);
            _logger.LogInformation("Retrieved {Count} power trades for {Date:yyyy-MM-dd}", trades.Count(), date);
            
            // Calculate the aggregated volumes
            _logger.LogDebug("Calculating aggregated volumes");
            var aggregatedVolumes = _positionCalculator.CalculateAggregatedVolumes(trades);
            
            // Generate the output file name
            var fileName = _fileManager.GetFileName(_settings.OutputFolder);
            
            // Write to file
            _logger.LogInformation("Writing aggregated positions to {FileName}", fileName);
            await _fileManager.WriteToFileAsync(fileName, aggregatedVolumes);
            
            _logger.LogInformation("Successfully generated power position report");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating power position report");
            throw;
        }
        finally
        {
            lock (_lock)
            {
                _isRunning = false;
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Power Position Worker...");
        
        // Stop the timer first
        _timer?.Change(Timeout.Infinite, 0);
        
        // Wait for any running operation to complete
        if (_isRunning)
        {
            _logger.LogInformation("Waiting for current operation to complete...");
            var startWait = DateTime.UtcNow;
            
            while (_isRunning && (DateTime.UtcNow - startWait) < TimeSpan.FromSeconds(30))
            {
                await Task.Delay(100, cancellationToken);
            }
            
            if (_isRunning)
            {
                _logger.LogWarning("Timed out waiting for operation to complete");
            }
        }
        
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _timer?.Dispose();
                if (_powerService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _disposed = true;
        }
    }
}
