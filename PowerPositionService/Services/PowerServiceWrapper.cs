using Microsoft.Extensions.Logging;
using PowerPositionService.Interfaces;
using PowerPositionService.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PowerPositionService.Services
{
    public class PowerServiceWrapper : IPowerService, IDisposable
    {
        private readonly ILogger<PowerServiceWrapper> _logger;
        private readonly dynamic _powerService;
        private bool _disposed;
        private readonly string _dllPath;

        public PowerServiceWrapper(ILogger<PowerServiceWrapper> logger, string dllPath = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dllPath = dllPath ?? Path.Combine(AppContext.BaseDirectory, "PowerService.dll");
            
            try
            {
                if (!File.Exists(_dllPath))
                {
                    throw new FileNotFoundException($"Could not find PowerService.dll at: {_dllPath}");
                }

                _logger.LogInformation("Loading PowerService from: {DllPath}", _dllPath);
                var assembly = System.Reflection.Assembly.LoadFrom(_dllPath);
                
                // Get the PowerService type
                var powerServiceType = assembly.GetType("Axpo.PowerService") ?? 
                    throw new TypeLoadException("Failed to find PowerService type in the assembly.");

                // Create an instance of the PowerService
                var instance = Activator.CreateInstance(powerServiceType);
                _powerService = instance ?? 
                    throw new InvalidOperationException("Failed to create PowerService instance");
                _logger.LogInformation("Successfully created PowerService instance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize PowerService");
                throw new TypeLoadException($"Failed to initialize PowerService: {ex.Message}", ex);
            }
        }

        public IEnumerable<PowerTrade> GetTrades(DateTime date)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PowerServiceWrapper));
            
            try
            {
                // Get the trades dynamically
                dynamic trades = _powerService.GetTrades(date);
                // Convert to an enumerable of dynamic
                var tradesEnumerable = (IEnumerable<dynamic>)trades;
                return ConvertTrades(tradesEnumerable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trades");
                throw new InvalidOperationException($"Error getting trades: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(PowerServiceWrapper));
            
            try
            {
                // Get the task dynamically
                dynamic task = _powerService.GetTradesAsync(date);
                // Await the task to get the result
                var result = await task;
                // Convert the result to an enumerable of dynamic
                var trades = (IEnumerable<dynamic>)result;
                return ConvertTrades(trades);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trades asynchronously");
                throw new InvalidOperationException($"Error getting trades asynchronously: {ex.Message}", ex);
            }
        }

        private static IEnumerable<PowerTrade> ConvertTrades(IEnumerable<dynamic> trades)
        {
            if (trades == null) yield break;
            
            foreach (var trade in trades)
            {
                var powerTrade = new PowerTrade
                {
                    Date = trade.Date,
                    Periods = new List<PowerPeriod>()
                };

                try
                {
                    // Get the periods collection from the dynamic trade object
                    var periods = trade.Periods as IEnumerable<dynamic>;
                    if (periods == null)
                    {
                        // Try to handle case where Periods is an array
                        var periodArray = trade.Periods as Array;
                        if (periodArray != null)
                        {
                            periods = periodArray.Cast<dynamic>();
                        }
                        else
                        {
                            // Try to get the value as an enumerable
                            periods = Enumerable.ToList<dynamic>(trade.Periods);
                        }
                    }

                    foreach (var period in periods)
                    {
                        powerTrade.Periods = powerTrade.Periods.Append(new PowerPeriod
                        {
                            Period = (int)period.Period,
                            Volume = (double)period.Volume
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and continue with the next trade
                    Console.WriteLine($"Error processing trade periods: {ex.Message}");
                    continue;
                }

                yield return powerTrade;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_powerService is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
