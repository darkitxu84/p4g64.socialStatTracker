using p4g64.socialStatTracker.Configuration;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;

namespace p4g64.socialStatTracker
{
    internal class Utils
    {
        private static ILogger _logger;
        private static Config _config;
        private static IStartupScanner _startupScanner;
        internal static nint BaseAddress { get; private set; }

        internal static bool Initialise(ILogger logger, Config config, IModLoader modLoader)
        {
            _logger = logger;
            _config = config;
            using var thisProcess = Process.GetCurrentProcess();
            BaseAddress = thisProcess.MainModule!.BaseAddress;

            var startupScannerController = modLoader.GetController<IStartupScanner>();
            if (startupScannerController == null || !startupScannerController.TryGetTarget(out _startupScanner))
            {
                LogError($"Unable to get controller for Reloaded SigScan Library, stuff won't work :(");
                return false;
            }

            return true;

        }
        internal static void LogDebug(string message)
        {
            if (_config.DebugEnabled)
                _logger.WriteLine($"[Social Stat Tracker] {message}");
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
        internal static void SigScan(string pattern, string name, Action<nint> action)
        {
            _startupScanner.AddMainModuleScan(pattern, result =>
            {
                if (!result.Found)
                {
                    LogError($"Unable to find {name}, stuff won't work :(");
                    return;
                }
                LogDebug($"Found {name} at 0x{result.Offset + BaseAddress:X}");

                action(result.Offset + BaseAddress);
            });
        }

        internal static unsafe nuint GetGlobalAddress(nint ptrAddress)
        {
            return (nuint)((*(int*)ptrAddress) + ptrAddress + 4);
        }

    }
}