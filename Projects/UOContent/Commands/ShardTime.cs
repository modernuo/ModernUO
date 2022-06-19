using System.Globalization;

namespace Server.Commands
{
    public static class ShardTime
    {
        public static void Initialize()
        {
            CommandSystem.Register("Time", AccessLevel.Player, Time_OnCommand);
        }

        [Usage("Time")]
        [Description("Returns the server's local time.")]
        private static void Time_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage(Core.Now.ToString(CultureInfo.InvariantCulture));
        }
    }
}
