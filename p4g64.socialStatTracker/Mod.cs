using p4g64.socialStatTracker.Template;
using p4g64.socialStatTracker.Configuration;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;
using p3ppc.socialStatTracker;

// Code adapted from Social Stat Tracker by AnimatedSwine
// https://github.com/AnimatedSwine37/p3ppc.socialStatTracker

namespace p4g64.socialStatTracker
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public unsafe class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;


        private IAsmHook _socialStatusLevelNameHook;
        private IReverseWrapper<AppendPointsDelegate> _appendPointsReverseWrapper;
        private short* _socialStatPoints;

        const string getSocialStatLevelNameFuncSig = "E8 ?? ?? ?? ?? 44 39 25 ?? ?? ?? ?? 48 8B C8";
        const int socialStatPointsOffset = 0x51BCD70 + 0xCA8;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;
            nint baseAddress = Process.GetCurrentProcess().MainModule!.BaseAddress;

            Utils.Initialise(_logger, _configuration);
            IntPtr targetAddress = IntPtr.Add(baseAddress, socialStatPointsOffset);

            _socialStatPoints = (short*)targetAddress.ToPointer();

            var startupScannerController = _modLoader.GetController<IStartupScanner>();
            if (startupScannerController == null || !startupScannerController.TryGetTarget(out var startupScanner))
            {
                Utils.LogError($"Unable to get controller for Reloaded SigScan Library, aborting initialisation");
                return;
            }

            startupScanner.AddMainModuleScan(getSocialStatLevelNameFuncSig, result =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to find GetSocialStatLevelName, aborting initialisation");
                    return;
                }
                Utils.LogDebug($"found GetSocialStatLevelName function at 0x{result.Offset + baseAddress:X}");

                string[] function =
                {
                    "use64",
                    "push rcx\npush rdx\npush r8\npush r9\npush r10\npush r11",
                    "sub rsp, 32",
                    $"{_hooks.Utilities.GetAbsoluteCallMnemonics(AppendPoints, out _appendPointsReverseWrapper)}",
                    "add rsp, 32",
                    "pop r11\npop r10\npop r9\npop r8\npop rdx\npop rcx"
                };
                // baseAdress + offset points to the function begin, so we add an extra offset
                _socialStatusLevelNameHook = _hooks.CreateAsmHook(function, baseAddress + result.Offset, AsmHookBehaviour.ExecuteAfter).Activate();
            });
        }

        // stat is actually stat index
        private string AppendPoints(string message, short stat, short statLevel)
        {
            // remove this logs for mod release
            _logger.WriteLineAsync($"[INFO] (Social Stat Tracker) stat index: {stat}");
            _logger.WriteLineAsync($"[INFO] (Social Stat Tracker) points: {_socialStatPoints[stat]}");
            return $"{message} p:{_socialStatPoints[stat]}";

            /*
            short points = _socialStatPoints[stat];
            short lastRequired = _pointsRequired[stat][statLevel - 1];

            if (statLevel == 6)
            {
                if (!_configuration.ShowAboveMax || points - lastRequired == 0)
                    return message;
                return $"{message} +{points - lastRequired}";
            }

            short required = _pointsRequired[stat][statLevel];

            return $"{message} {points - lastRequired}/{required - lastRequired}";
            */
        }

        [Function(new Register[] { Register.rax, Register.rdi, Register.r8 }, Register.rax, true )]
        private delegate string AppendPointsDelegate(string message, short stat, short statLevel);

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}