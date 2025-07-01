using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using PowerPositionService.Interfaces;
using PowerPositionService.Models;

namespace PowerPositionService.Services;

public class PositionCalculator : IPositionCalculator
{
    private const int HoursInDay = 24;
    private const int StartHour = 23; // Trading day starts at 23:00
    
    private readonly ILogger<PositionCalculator> _logger;

    public PositionCalculator(ILogger<PositionCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Dictionary<int, double> CalculateAggregatedPositions(IEnumerable<PowerTrade> powerTrades)
    {
        if (powerTrades == null) 
            throw new ArgumentNullException(nameof(powerTrades));

        try
        {
            _logger.LogDebug("Calculating aggregated positions for {TradeCount} trades", powerTrades.Count());
            
            var result = powerTrades
                .SelectMany(t => t.Periods)
                .GroupBy(
                    p => p.Period,
                    p => p.Volume,
                    (period, volumes) => new { Period = period, Volume = Math.Round(volumes.Sum(), 2) })
                .ToDictionary(x => x.Period, x => x.Volume);
                
            _logger.LogDebug("Aggregated volumes per period: {Periods}", 
                string.Join(", ", result.Select(kvp => $"{{Period={kvp.Key}, Volume={kvp.Value}}}").ToArray()));
            
            _logger.LogInformation("Calculated aggregated positions for {PeriodCount} periods", result.Count);
            return result;
        }
        catch (Exception ex) when (LogAndWrapException(ex, "Error calculating aggregated positions"))
        {
            // This block will never be reached because LogAndWrapException returns false
            throw;
        }
    }

    public Dictionary<string, double> MapToLocalTime(Dictionary<int, double> positions)
    {
        if (positions == null) 
            throw new ArgumentNullException(nameof(positions));

        try
        {
            _logger.LogDebug("Mapping {PositionCount} positions to local time", positions.Count);
            
            var result = new Dictionary<string, double>();
            var time = new TimeSpan(StartHour, 0, 0);
            
            for (int i = 1; i <= HoursInDay; i++)
            {
                var timeStr = time.ToString("hh':'mm");
                positions.TryGetValue(i, out var volume);
                result.Add(timeStr, volume);
                
                time = time.Add(TimeSpan.FromHours(1));
                if (time.Days > 0)
                    time = TimeSpan.Zero;
            }
            
            _logger.LogDebug("Mapped positions to local time: {MappedPositions}", 
                string.Join(", ", result.Select(kvp => $"{{{kvp.Key}: {kvp.Value}}}")));
            
            _logger.LogInformation("Mapped {PositionCount} positions to local time", result.Count);
            return result;
        }
        catch (Exception ex) when (LogAndWrapException(ex, "Error mapping positions to local time"))
        {
            // This block will never be reached because LogAndWrapException returns false
            throw;
        }
    }
    
    public Dictionary<string, double> CalculateAggregatedVolumes(IEnumerable<PowerTrade> powerTrades)
    {
        if (powerTrades == null)
            throw new ArgumentNullException(nameof(powerTrades));
            
        try
        {
            _logger.LogDebug("Calculating aggregated volumes for {TradeCount} trades", powerTrades.Count());
            
            // First calculate the aggregated positions
            var aggregatedPositions = CalculateAggregatedPositions(powerTrades);
            
            // Then map them to local time
            var result = MapToLocalTime(aggregatedPositions);
            
            _logger.LogInformation("Calculated aggregated volumes for {PeriodCount} periods", result.Count);
            return result;
        }
        catch (Exception ex) when (LogAndWrapException(ex, "Error calculating aggregated volumes"))
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
