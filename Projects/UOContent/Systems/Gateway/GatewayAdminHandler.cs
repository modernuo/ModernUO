using Server.Logging;
using Server.Network;

namespace Server.Systems.Gateway;

/// <summary>
/// Handles admin commands pushed from the gateway web portal via SignalR.
/// All methods run on the game thread (marshaled by GatewayClient callback handlers).
/// </summary>
public static class GatewayAdminHandler
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(GatewayAdminHandler));

    public static void HandleCommand(GatewayClient.AdminCommandData command)
    {
        logger.Information("Received admin command: {Type}", command.Type);

        // Dispatch based on command type
        // For now, just log. Specific handlers will be implemented as features are built.
        switch (command.Type.ToLowerInvariant())
        {
            case "banaccount":
                // TODO: Find online player, disconnect them, mark account banned
                logger.Information("Ban account command received: {Payload}", command.Payload);
                break;
            case "kickplayer":
                // TODO: Find online player by name/serial, disconnect
                logger.Information("Kick player command received: {Payload}", command.Payload);
                break;
            case "broadcastmessage":
                // Broadcast to all online players
                foreach (var ns in NetState.Instances)
                {
                    ns.Mobile?.SendMessage(command.Payload);
                }

                logger.Information("Broadcast message sent: {Payload}", command.Payload);
                break;
            case "gmpage":
                // TODO: Route to online GM
                logger.Information("GM page received: {Payload}", command.Payload);
                break;
            default:
                logger.Warning("Unknown admin command type: {Type}", command.Type);
                break;
        }
    }
}
