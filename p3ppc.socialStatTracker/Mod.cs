using p3ppc.socialStatTracker.Configuration;
using p3ppc.socialStatTracker.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p3ppc.socialStatTracker
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
        private short* _socialStatPoints;
        private IReverseWrapper<AppendPointsDelegate> _appendPointsReverseWrapper;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            Utils.Initialise(_logger, _configuration);

            var startupScannerController = _modLoader.GetController<IStartupScanner>();
            if (startupScannerController == null || !startupScannerController.TryGetTarget(out var startupScanner))
            {
                Utils.LogError($"Unable to get controller for Reloaded SigScan Library, aborting initialisation");
                return;
            }

            startupScanner.AddMainModuleScan("48 8D 0D ?? ?? ?? ?? 0F B7 14 ??", result =>
            {
                if (!result.Found)
                {
                    Utils.LogError($"Unable to find SocialStatPoints, aborting initialisation");
                    return;
                }
                Utils.LogDebug($"Found SocialStatPoints pointer at 0x{result.Offset + Utils.BaseAddress:X}");

                _socialStatPoints = (short*)Utils.GetGlobalAddress(result.Offset + Utils.BaseAddress + 3);
                Utils.LogDebug($"Found SocialStatPoints at 0x{(nuint)_socialStatPoints:X}");
            });

            startupScanner.AddMainModuleScan("E8 ?? ?? ?? ?? 48 63 0D ?? ?? ?? ?? 33 FF", result =>
            {
                if(!result.Found)
                {
                    Utils.LogError($"Unable to find StatusMenu function to hook, aborting initialisation");
                    return;
                }
                Utils.LogDebug($"Found StatusMenu function at 0x{result.Offset + Utils.BaseAddress:X}");

                string[] function =
                {
                    "use64",
                    "push rcx\npush rdx\npush r8\npush r9\npush r10\npush r11",
                    "sub rsp, 32",
                    $"{_hooks.Utilities.GetAbsoluteCallMnemonics(AppendPoints, out _appendPointsReverseWrapper)}",
                    "add rsp, 32",
                    "pop r11\npop r10\npop r9\npop r8\npop rdx\npop rcx"
                };

                _socialStatusLevelNameHook = _hooks.CreateAsmHook(function, Utils.BaseAddress + result.Offset, AsmHookBehaviour.ExecuteAfter).Activate();
            });
        }

        private string AppendPoints(string message, short stat, short statLevel)
        {
            if(_socialStatPoints == null)
                return message;

            short points = _socialStatPoints[stat];
            short lastRequired = _pointsRequired[stat][statLevel - 1];
            
            if(statLevel == 6)
            {
                if (!_configuration.ShowAboveMax || points - lastRequired == 0)
                    return message;
                return $"{message} +{points-lastRequired}";
            }

            short required = _pointsRequired[stat][statLevel];

            return $"{message} {points-lastRequired}/{required-lastRequired}";      
        }
        
        private readonly short[][] _pointsRequired = { 
            new short[]{ 0, 20, 55, 100, 155, 230 }, 
            new short[]{ 0, 15, 30, 45, 70, 100 },
            new short[]{ 0, 15, 30, 45, 65, 80 }
        };

        [Function( new Register[] { Register.rax, Register.r12, Register.rbx }, Register.rax, true)]
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