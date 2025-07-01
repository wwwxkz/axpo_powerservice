# Power Position Service

This service generates intra-day power position reports by aggregating trade volumes per hour and exporting them to CSV files at configurable intervals.

## Features

- Aggregates power trade positions per hour in local time (Europe/London timezone)
- Generates CSV reports with timestamps in the filename
- Configurable output folder and report generation interval
- Robust error handling and logging
- Runs as a background service with proper resource management

## Prerequisites

- .NET 8.0 SDK or later
- Access to the PowerService.dll (included in the netstandard2.0 folder)

## Configuration

Edit the `appsettings.json` file to configure:

```json
{
  "AppSettings": {
    "ExtractIntervalMinutes": 5,
    "OutputFolder": "C:\\PowerPositionData"
  }
  // ... other settings
}
```

- `ExtractIntervalMinutes`: How often to generate the report (in minutes)
- `OutputFolder`: Where to save the generated CSV files

## Building the Application

1. Open a terminal in the solution directory
2. Run the following command:
   ```
   dotnet build -c Release
   ```

## Running the Application

1. Navigate to the output directory:
   ```
   cd bin/Release/net8.0
   ```
2. Run the application:
   ```
   dotnet PowerPositionService.dll
   ```

### Command Line Arguments

You can override the settings from the command line:

```
dotnet PowerPositionService.dll --AppSettings:ExtractIntervalMinutes=10 --AppSettings:OutputFolder="C:\CustomOutput"
```

## Output Files

Reports are saved with the following naming convention:
```
PowerPosition_YYYYMMDD_HHMM.csv
```

Example: `PowerPosition_20250102_1530.csv`

Each CSV file contains:
```
Local Time,Volume
23:00,150.0
00:00,150.0
...
22:00,80.0
```

## Logging

Logs are written to:
- Console output
- `Logs/log-YYYYMMDD.txt` files (rolling daily)

## Error Handling

The service includes comprehensive error handling and logging to help diagnose issues:
- Failed API calls to the PowerService are logged and retried
- File system errors are caught and logged
- The service continues running even if a single report generation fails

## Dependencies

- .NET 8.0 Runtime
- Microsoft.Extensions.*
- Serilog for logging

## Notes

- The service uses the Europe/London timezone for all time calculations
- The first period (23:00-00:00) corresponds to period 1 in the PowerService
- The service creates the output directory if it doesn't exist
- The application must be run with appropriate permissions to write to the output directory
