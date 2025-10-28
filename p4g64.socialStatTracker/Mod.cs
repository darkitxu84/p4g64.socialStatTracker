using p4g64.socialStatTracker.Configuration;
using p4g64.socialStatTracker.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using Reloaded.Mod.Interfaces;
using static p4g64.socialStatTracker.Text;
using static p4g64.socialStatTracker.Utils;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

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

        private short* _socialStatPoints;

        private IAsmHook? _postRenderHook;
        private IReverseWrapper<PostRenderDelegate>? _postRenderReverseWrapper;

        const string postRenderSig = "48 8B 05 ?? ?? ?? ?? 45 0F 28 EA";
        const int socialStatPointsOffset = 0x51BCD70 + 0xCA8;
        private readonly static RevColour colour = new() { R = 0x11, G = 0x11, B = 0x11, A = 0xFF };

        private readonly static TextPos[] textPos =
        [
            new(335, 70), // courage
            new(260, 140), // knowledge
            new(432, 140), // diligence
            new(390, 233), // understanding
            new(290, 233) // expression
        ];

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            Initialise(_logger, _configuration, _modLoader);
            Text.Initialise(_hooks!);

            IntPtr targetAddress = IntPtr.Add(BaseAddress, socialStatPointsOffset);
            _socialStatPoints = (short*)targetAddress.ToPointer();

            SigScan(postRenderSig, "PostRender at StatusMenuStuff", address =>
            {
                string[] function =
                {
                    "use64",
                    "push rcx\npush rdx\npush r8\npush r9\npush r10\npush r11",
                    "sub rsp, 32",
                    $"{_hooks.Utilities.GetAbsoluteCallMnemonics(PostRender, out _postRenderReverseWrapper)}",
                    "add rsp, 32",
                    "pop r11\npop r10\npop r9\npop r8\npop rdx\npop rcx",
                };
                _postRenderHook = _hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });
        }

        private nint PostRender(nint rax)
        {
            for (int i = 0; i < 5; i++) // five social stats
            {
                short points = _socialStatPoints[i];
                short currentRank = 0;
                string text;

                if (points >= _pointsRequired[i][4])
                {
                    currentRank = 4;
                }
                else
                {
                    for (short j = 0; j < 5; j++) // five ranks per social stat
                    {
                        if (points < _pointsRequired[i][j])
                        {
                            currentRank = (short)(j - 1);
                            break;
                        }
                    }
                }

                if (currentRank == 4) // TODO: add to config show above max
                    text = $"+{_pointsRequired[i][4] - points}";
                else
                    text = $"{points - _pointsRequired[i][currentRank]}/{_pointsRequired[i][currentRank + 1] - _pointsRequired[i][currentRank]}";

                Text.Draw(textPos[i].x, textPos[i].y, 0, colour, 0, 2, text, Text.Positioning.Left);
            }

            return rax;
        }

        private static readonly short[][] _pointsRequired =
        {
            [0, 16, 40, 80, 140], // courage
            [0, 30, 80, 150, 240], // knowledge
            [0, 16, 40, 80, 130], // diligence
            [0, 16, 40, 80, 140], // understanding
            [0, 13, 33, 53, 85], // expression
        };

        [Function(new Register[] { Register.rax }, Register.rax, false)]
        private delegate nint PostRenderDelegate(nint rax);

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