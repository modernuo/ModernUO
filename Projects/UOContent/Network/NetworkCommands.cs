using Server.Commands;
using Server.Commands.Generic;
using Server.Mobiles;

namespace Server.Network
{
    public class PacketLoggingCommand : BaseCommand
    {
        public static void Initialize()
        {
            TargetCommands.Register(new PacketLoggingCommand());
        }

        public PacketLoggingCommand()
        {
            AccessLevel = AccessLevel.Developer;
            Commands = new[] { "PacketLogging" };
            ObjectTypes = ObjectTypes.Mobiles;
            Supports = CommandSupport.AllMobiles;
            Usage = "PacketLogging <on|off>";
            Description = "Enables or disables packet logging for a particular user until they disconnect.";
        }

        public override void Execute(CommandEventArgs e, object targeted)
        {
            if (e.Arguments.Length == 0)
            {
                LogFailure("Format: PacketLogging <on|off>");
                return;
            }

            var from = e.Mobile;
            var enable = Utility.ToBoolean(e.Arguments[0]);

            if (targeted is not PlayerMobile pm)
            {
                LogFailure("That is not a player.");
            }
            else if (pm.NetState == null)
            {
                LogFailure("The player is not connected.");
            }
            else if (from != pm && from.AccessLevel < pm.AccessLevel)
            {
                LogFailure("You do not have the required access level to do this.");
            }
            else
            {
                pm.NetState.PacketLogging = enable;
                var enabled = enable ? "enabled" : "disabled";
                CommandLogging.WriteLine(
                    from,
                    $"{from.AccessLevel} {CommandLogging.Format(from)} {enabled} packet logging for {pm.Account.Username} ({pm.NetState})"
                );

                AddResponse($"Packet logging has been {enabled} for {pm.Account.Username} ({pm.NetState})");
            }
        }
    }
}
