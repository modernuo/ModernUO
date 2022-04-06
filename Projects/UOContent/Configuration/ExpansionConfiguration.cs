using Server.Network;

namespace Server
{
    public static class ExpansionConfiguration
    {
        public static void Configure()
        {
            Mobile.InsuranceEnabled = ServerConfiguration.GetSetting("insurance.enable", Core.AOS);
            Tooltip.Enabled = ServerConfiguration.GetSetting("tooltip.enable", Core.AOS);
            var visibleDamage = ServerConfiguration.GetSetting("visibleDamage", Core.AOS);
            Mobile.VisibleDamageType = visibleDamage ? VisibleDamageType.Related : VisibleDamageType.None;
            Mobile.GuildClickMessage = ServerConfiguration.GetSetting("guildClickMessage", !Core.AOS);
            Mobile.AsciiClickMessage = ServerConfiguration.GetSetting("asciiClickMessage", !Core.AOS);

            Mobile.ActionDelay = ServerConfiguration.GetSetting("actionDelay", Core.AOS ? 1000 : 500);

            if (Core.AOS)
            {
                AOS.DisableStatInfluences();

                Mobile.AOSStatusHandler = AOS.GetStatus;
            }

            if (Tooltip.Enabled)
            {
                // single click for everything is overridden to check object property list
                IncomingEntityPackets.SingleClickProps = true;
            }
        }
    }
}
