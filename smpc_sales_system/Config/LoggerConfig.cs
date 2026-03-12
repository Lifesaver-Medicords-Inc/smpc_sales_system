using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace smpc_sales_system.Config
{
    internal static class LoggerConfig
    {
        public static void Configure()
        {
            // Get base directory (project root when running from bin)
            string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\.."));

            // Logs folder inside the base directory
            string logDirectory = Path.Combine(projectRoot, "/Logs");

            // Ensure Logs folder exists
            Directory.CreateDirectory(logDirectory);

            // Configure logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug()
                .WriteTo.File(
                    Path.Combine(logDirectory, "app.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 5_000_000,
                    rollOnFileSizeLimit: true,
                    restrictedToMinimumLevel: LogEventLevel.Debug
                )
                .CreateLogger();
        }
    }
}
