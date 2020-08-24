using Server.Accounting;
using Server.Items;
using Server.Network;

namespace Server
{
    public static class ExpansionConfiguration
    {
        public static void Configure()
        {
            Core.Expansion = ServerConfiguration.GetOrUpdateSetting("currentExpansion", Expansion.TOL);

            AccountGold.Enabled = ServerConfiguration.GetSetting("accountGold.enable", Core.TOL);
            AccountGold.ConvertOnBank = ServerConfiguration.GetSetting("accountGold.convertOnBank", true);
            AccountGold.ConvertOnTrade = ServerConfiguration.GetSetting("accountGold.convertOnTrade", false);
            VirtualCheck.UseEditGump = ServerConfiguration.GetSetting("virtualChecks.useEditGump", true);

            Mobile.InsuranceEnabled = ServerConfiguration.GetSetting("insurance.enable", Core.AOS);
            ObjectPropertyList.Enabled = ServerConfiguration.GetSetting("opl.enable", Core.AOS);
            bool visibleDamage = ServerConfiguration.GetSetting("visibleDamage", Core.AOS);
            Mobile.VisibleDamageType = visibleDamage ? VisibleDamageType.Related : VisibleDamageType.None;
            Mobile.GuildClickMessage = ServerConfiguration.GetSetting("guildClickMessage", !Core.AOS);
            Mobile.AsciiClickMessage = ServerConfiguration.GetSetting("asciiClickMessage", !Core.AOS);

            Mobile.ActionDelay = ServerConfiguration.GetSetting("actionDelay", Core.AOS ? 1000 : 500);

            if (Core.AOS)
            {
                AOS.DisableStatInfluences();

                if (ObjectPropertyList.Enabled)
                    PacketHandlers.SingleClickProps =
                        true; // single click for everything is overridden to check object property list

                Mobile.AOSStatusHandler = AOS.GetStatus;
            }
        }
    }
}
