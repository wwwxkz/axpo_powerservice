using System.Collections.Generic;
using PowerPositionService.Models;

namespace PowerPositionService.Interfaces;

public interface IPositionCalculator
{
    Dictionary<int, double> CalculateAggregatedPositions(IEnumerable<PowerTrade> powerTrades);
    Dictionary<string, double> MapToLocalTime(Dictionary<int, double> positions);
    
    /// <summary>
    /// Calculates the aggregated volumes for a collection of power trades
    /// </summary>
    /// <param name="powerTrades">The power trades to aggregate</param>
    /// <returns>A dictionary mapping local time strings to aggregated volumes</returns>
    Dictionary<string, double> CalculateAggregatedVolumes(IEnumerable<PowerTrade> powerTrades);
}
