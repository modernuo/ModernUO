using System.Runtime.CompilerServices;
using Server.Mobiles.AI.BaseAI;

namespace Server.Mobiles;

public static class BaseAIExtensions
{
    public static void DebugSayFormatted(this BaseAI ai,
        [InterpolatedStringHandlerArgument("ai")] ref DebugInterpolatedStringHandler handler, int cooldownMs = 5000)
    {
        var message = handler.Text;
        if (message.Length > 0)
        {
            ai.Mobile.PublicOverheadMessage(MessageType.Regular, 41, false, message.ToString());
            ai.NextDebugMessage = Core.TickCount + cooldownMs;
        }

        handler.Clear();
    }
}
