using System;
using System.Diagnostics;
using System.IO;
using Server.Gumps;
using Server.Logging;
using Server.Mobiles;
using Server.Network;

namespace Server.Misc
{
    public static class ClientVerification
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(ClientVerification));

        private static bool m_DetectClientRequirement;
        private static InvalidClientResponse m_InvalidClientResponse;

        private static TimeSpan m_AgeLeniency;
        private static TimeSpan m_GameTimeLeniency;

        public static ClientVersion Required { get; set; }

        public static bool AllowRegular { get; set; } = true;

        public static bool AllowUOTD { get; set; } = true;

        public static bool AllowGod { get; set; } = true;

        public static TimeSpan KickDelay { get; set; }

        public static void Configure()
        {
            m_DetectClientRequirement = ServerConfiguration.GetOrUpdateSetting("clientVerification.enable", true);
            m_InvalidClientResponse = ServerConfiguration.GetOrUpdateSetting("clientVerification.invalidClientResponse", InvalidClientResponse.Kick);
            m_AgeLeniency = ServerConfiguration.GetOrUpdateSetting("clientVerification.ageLeniency", TimeSpan.FromDays(10));
            m_GameTimeLeniency = ServerConfiguration.GetOrUpdateSetting("clientVerification.gameTimeLeniency", TimeSpan.FromHours(25));
            KickDelay = ServerConfiguration.GetOrUpdateSetting("clientVerification.kickDelay", TimeSpan.FromSeconds(20.0));
        }

        public static void Initialize()
        {
            EventSink.ClientVersionReceived += EventSink_ClientVersionReceived;

            if (m_DetectClientRequirement)
            {
                var path = Core.FindDataFile("client.exe", false);

                if (File.Exists(path))
                {
                    var info = FileVersionInfo.GetVersionInfo(path);

                    if (info.FileMajorPart != 0 || info.FileMinorPart != 0 || info.FileBuildPart != 0 ||
                        info.FilePrivatePart != 0)
                    {
                        Required = new ClientVersion(
                            info.FileMajorPart,
                            info.FileMinorPart,
                            info.FileBuildPart,
                            info.FilePrivatePart
                        );
                    }
                }
            }

            if (Required != null)
            {
                logger.Information(
                    "Restricting client version to {0}. Action to be taken: {1}",
                    Required,
                    m_InvalidClientResponse
                );
            }
        }

        private static void EventSink_ClientVersionReceived(NetState state, ClientVersion version)
        {
            string kickMessage = null;

            if (state.Mobile?.AccessLevel != AccessLevel.Player)
            {
                return;
            }

            if (Required != null && (m_InvalidClientResponse == InvalidClientResponse.Kick ||
                                                           m_InvalidClientResponse == InvalidClientResponse.LenientKick &&
                                                           Core.Now - state.Mobile.Created > m_AgeLeniency &&
                                                           state.Mobile is PlayerMobile mobile &&
                                                           mobile.GameTime > m_GameTimeLeniency))
            {
                if (version < Required)
                {
                    kickMessage = $"This server requires your client version be {Required}.";
                }
               /*else if (version > Required)
                {
                    kickMessage = $"This server requires your client version be {Required}.";
                }*/ //----OPTIONAL TO RESTRICT NEWER CLIENTS----
            }

            else if (!AllowGod || !AllowRegular || !AllowUOTD)
            {
                if (!AllowGod && version.Type == ClientType.God)
                {
                    kickMessage = "This server does not allow god clients to connect.";
                }
                else if (!AllowRegular && version.Type == ClientType.Regular)
                {
                    kickMessage = "This server does not allow regular clients to connect.";
                }
                else if (!AllowUOTD && state.IsUOTDClient)
                {
                    kickMessage = "This server does not allow UO:TD clients to connect.";
                }

                if (!AllowGod && !AllowRegular && !AllowUOTD)
                {
                    kickMessage = "This server does not allow any clients to connect.";
                }
                else if (AllowGod && !AllowRegular && !AllowUOTD && version.Type != ClientType.God)
                {
                    kickMessage = "This server requires you to use the god client.";
                }
                else if (kickMessage != null)
                {
                    if (AllowRegular && AllowUOTD)
                    {
                        kickMessage += " You can use regular or UO:TD clients.";
                    }
                    else if (AllowRegular)
                    {
                        kickMessage += " You can use regular clients.";
                    }
                    else if (AllowUOTD)
                    {
                        kickMessage += " You can use UO:TD clients.";
                    }
                }
            }

            if (kickMessage != null)
            {
                state.Mobile.SendMessage(0x22, kickMessage);
                state.Mobile.SendMessage(0x22, "You will be disconnected in {0} seconds.", KickDelay.TotalSeconds);

                Timer.StartTimer(KickDelay, () => OnKick(state));
            }
            else if (Required != null && version < Required)
            {
                switch (m_InvalidClientResponse)
                {
                    case InvalidClientResponse.Warn:
                        {
                            state.Mobile.SendMessage(
                                0x22,
                                "Your client is out of date. Please update your client.",
                                Required
                            );
                            state.Mobile.SendMessage(
                                0x22,
                                "This server recommends that your client version be at least {0}.",
                                Required
                            );
                            break;
                        }
                    case InvalidClientResponse.LenientKick:
                    case InvalidClientResponse.Annoy:
                        {
                            SendAnnoyGump(state.Mobile);
                            break;
                        }
                }
            }
            /*else if (Required != null && version > Required)
            {
                switch (m_InvalidClientResponse)
                {
                    case InvalidClientResponse.Warn:
                        {
                            state.Mobile.SendMessage(
                                0x22,
                                "Your client is too new. Please revert your client.",
                                Required
                            );
                            state.Mobile.SendMessage(
                                0x22,
                                "This server recommends that your client version be {0}.",
                                Required
                            );
                            break;
                        }
                    case InvalidClientResponse.LenientKick:
                    case InvalidClientResponse.Annoy:
                        {
                            SendAnnoyGump(state.Mobile);
                            break;
                        }
                }
            }*/ //----OPTIONAL TO RESTRICT NEWER CLIENTS----
        }

        private static void OnKick(NetState ns)
        {
            if (ns.Connection != null)
            {
                ns.LogInfo("Disconnecting, bad version");
                ns.Disconnect($"Invalid client version {ns.Version}.");
            }
        }

        private static void KickMessage(Mobile from, bool okay)
        {
            from.SendMessage("You will be reminded of this again.");

            if (m_InvalidClientResponse == InvalidClientResponse.LenientKick)
            {
                from.SendMessage(
                    "Invalid clients will be kicked after {0} days of character age and {1} hours of play time",
                    m_AgeLeniency,
                    m_GameTimeLeniency
                );
            }

            Timer.StartTimer(TimeSpan.FromMinutes(Utility.Random(5, 15)), () => SendAnnoyGump(from));
        }

        private static void SendAnnoyGump(Mobile m)
        {
            if (m.NetState != null && m.NetState.Version < Required)
            {
                Gump g = new WarningGump(
                    1060637,
                    30720,
                    $"Your client is invalid. Please either update or revert your client.<br>This server recommends that your client version be {Required}.<br> <br>You are currently using version {m.NetState.Version}.<br> <br>To patch, run UOPatch.exe inside your Ultima Online folder.",
                    0xFFC000,
                    480,
                    360,
                    okay => KickMessage(m, okay),
                    false
                );

                g.Draggable = false;
                g.Closable = false;
                g.Resizable = false;

                m.SendGump(g);
            }
        }

        private enum InvalidClientResponse
        {
            Ignore,
            Warn,
            Annoy,
            LenientKick,
            Kick
        }
    }
}
