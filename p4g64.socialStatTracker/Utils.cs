using p4g64.socialStatTracker.Configuration;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p3ppc.socialStatTracker
{
    internal class Utils
    {
        private static ILogger _logger;
        private static Config _config;
        internal static nint BaseAddress { get; private set; }

        internal static void Initialise(ILogger logger, Config config)
        {
            _logger = logger;
            _config = config;
        }

        internal static void LogDebug(string message)
        {
            // if (_config.DebugEnabled)
                // _logger.WriteLine($"[Social Stat Tracker] {message}");
        }

        internal static void Log(string message)
        {
            _logger.WriteLine($"[Social Stat Tracker] {message}");
        }

        internal static void LogError(string message, Exception e)
        {
            _logger.WriteLine($"[Social Stat Tracker] {message}: {e.Message}", System.Drawing.Color.Red);
        }

        internal static void LogError(string message)
        {
            _logger.WriteLine($"[Social Stat Tracker] {message}", System.Drawing.Color.Red);
        }

    }
}