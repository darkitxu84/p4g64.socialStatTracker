using Reloaded.Hooks.Definitions;
using static p4g64.socialStatTracker.Utils;

namespace p4g64.socialStatTracker;
internal static unsafe class Text
{
    internal static DrawTextDelegate Draw;

    internal static void Initialise(IReloadedHooks hooks)
    {
        SigScan("E8 ?? ?? ?? ?? 45 33 FF 4C 8D 2D ?? ?? ?? ?? EB ??", "Text::DrawWapped2 Ptr", address =>
        {
            var funcAddr = GetGlobalAddress(address + 1);
            Draw = hooks.CreateWrapper<DrawTextDelegate>((long)funcAddr, out _);
        });

    }

    internal delegate void DrawTextDelegate(float xPos, float yPos, nuint param_3, RevColour colour, byte param_5, byte textSize, string text, Positioning position);

    internal enum Positioning : int
    {
        Right = 0,
        Left = 2,
        Center = 8
    }
    public struct RevColour
    {
        public byte A;
        public byte B;
        public byte G;
        public byte R;
    }

    public struct TextPos(float x, float y)
    {
        public float x = x;
        public float y = y;
    }
}