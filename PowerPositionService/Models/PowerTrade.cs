using System.Collections.Generic;

namespace PowerPositionService.Models;

public class PowerTrade
{
    public DateTime Date { get; set; }
    public IEnumerable<PowerPeriod> Periods { get; set; } = new List<PowerPeriod>();
}

public class PowerPeriod
{
    public int Period { get; set; }
    public double Volume { get; set; }
}
