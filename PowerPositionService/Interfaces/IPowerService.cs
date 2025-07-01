using System.Collections.Generic;
using System.Threading.Tasks;
using PowerPositionService.Models;

namespace PowerPositionService.Interfaces;

public interface IPowerService
{
    IEnumerable<PowerTrade> GetTrades(DateTime date);
    Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date);
}
