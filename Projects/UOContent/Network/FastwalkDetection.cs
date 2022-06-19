using System.Collections.Generic;
using Server.Commands.Generic;

namespace Server.Network;

public static class FastwalkDetection
{
    private static readonly HashSet<Mobile> _debugFastwalk = new();

    public static void Initialize()
    {
        TargetCommands.Register(new DebugFastwalk());
        EventSink.FastWalk += OnFastwalk;
    }

    private static void OnFastwalk(FastWalkEventArgs e)
    {
        var from = e.NetState.Mobile;
        if (from == null)
        {
            return;
        }

        if (!_debugFastwalk.Contains(from))
        {
            return;
        }

        from.PublicOverheadMessage(MessageType.Emote, from.EmoteHue, false, "Fastwalk Detected", accessLevel: AccessLevel.GameMaster);
    }

    public class DebugFastwalk : BaseCommand
    {
        public DebugFastwalk()
        {
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllMobiles;
            Commands = new[] { "DebugFastwalk" };
            ObjectTypes = ObjectTypes.Mobiles;
            ListOptimized = true;
            Usage = "DebugFastwalk <on|off>";
            Description = "Enables fastwalk debug messages";
        }

        public override void ExecuteList(CommandEventArgs e, List<object> list)
        {
            var on = e.Arguments.Length == 0 || e.GetBoolean(0);

            foreach (var o in list)
            {
                if (o is not Mobile m)
                {
                    continue;
                }

                if (on)
                {
                    _debugFastwalk.Add(m);
                }
                else
                {
                    _debugFastwalk.Remove(m);
                }
            }
        }
    }
}
