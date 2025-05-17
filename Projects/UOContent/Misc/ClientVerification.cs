using System;
using System.Numerics;
using Server.Gumps;
using Server.Logging;
using Server.Mobiles;
using Server.Network;
using Server.Text;

namespace Server.Misc
{
    public static class ClientVerification
    {
        private static readonly ILogger logger = LogFactory.GetLogger(typeof(ClientVerification));

        private static bool _enable;
        private static InvalidClientResponse _invalidClientResponse;
        private static string _versionExpression;

        private static TimeSpan _ageLeniency;
        private static TimeSpan _gameTimeLeniency;
        private static string _allowedClientsMessage;

        public static ClientType AllowedClientTypes { get; private set; }

        public static bool AllowClassic => (AllowedClientTypes & ClientType.Classic) != 0;
        public static bool AllowUOTD => (AllowedClientTypes & ClientType.UOTD) != 0;
        public static bool AllowKR => (AllowedClientTypes & ClientType.KR) != 0;
        public static bool AllowSA => (AllowedClientTypes & ClientType.SA) != 0;

        public static TimeSpan KickDelay { get; private set; }

        public static void Configure()
        {
            UOClient.MinRequired = ServerConfiguration.GetSetting("clientVerification.minRequired", (ClientVersion)null);
            UOClient.MaxRequired = ServerConfiguration.GetSetting("clientVerification.maxRequired", (ClientVersion)null);

            _enable = ServerConfiguration.GetOrUpdateSetting("clientVerification.enable", true);
            _invalidClientResponse =
                ServerConfiguration.GetOrUpdateSetting("clientVerification.invalidClientResponse", InvalidClientResponse.Kick);
            _ageLeniency = ServerConfiguration.GetOrUpdateSetting("clientVerification.ageLeniency", TimeSpan.FromDays(10));
            _gameTimeLeniency = ServerConfiguration.GetOrUpdateSetting(
                "clientVerification.gameTimeLeniency",
                TimeSpan.FromHours(25)
            );
            KickDelay = ServerConfiguration.GetOrUpdateSetting("clientVerification.kickDelay", TimeSpan.FromSeconds(20.0));
            AllowedClientTypes = ServerConfiguration.GetSetting("clientVerification.allowedClientTypes", ClientType.Classic | ClientType.SA);
            _allowedClientsMessage = GetAllowedClientsString(AllowedClientTypes);
        }

        public static void Initialize()
        {
            if (UOClient.MinRequired == null && UOClient.MaxRequired == null)
            {
                UOClient.MinRequired = UOClient.ServerClientVersion;
            }

            if (UOClient.MinRequired != null || UOClient.MaxRequired != null)
            {
                logger.Information(
                    "Restricting client version to {ClientVersion}. Action to be taken: {Action}",
                    GetVersionExpression(),
                    _invalidClientResponse
                );
            }
        }

        private static string GetVersionExpression()
        {
            if (_versionExpression == null)
            {
                if (UOClient.MinRequired != null && UOClient.MaxRequired != null)
                {
                    _versionExpression = $"{UOClient.MinRequired}-{UOClient.MaxRequired}";
                }
                else if (UOClient.MinRequired != null)
                {
                    _versionExpression = $"{UOClient.MinRequired} or newer";
                }
                else
                {
                    _versionExpression = $"{UOClient.MaxRequired} or older";
                }
            }

            return _versionExpression;
        }

        private static string GetAllowedClientsString(ClientType allowedClients)
        {
            // Get total number of allowed clients
            var totalAllowedClients = BitOperations.PopCount((uint)allowedClients) - 1;
            if (totalAllowedClients == 0)
            {
                return "There are no clients supported at this time.";
            }

            using var builder = ValueStringBuilder.Create();
            builder.Append("Please connect with a ");
            uint flags = 0;
            var i = 0;
            while (flags < (uint)allowedClients)
            {
                flags = 1u << i;
                if (i > 0)
                {
                    builder.Append(i == totalAllowedClients ? " or " : ", ");
                }

                builder.Append(((ClientType)flags).TypeName());
                i++;
            }

            return builder.ToString();
        }

        public static void ClientVersionReceived(NetState state, ClientVersion version)
        {
            var sb = ValueStringBuilder.Create();

            if (!_enable || state.Mobile?.AccessLevel != AccessLevel.Player)
            {
                return;
            }

            var strictRequirement = _invalidClientResponse == InvalidClientResponse.Kick ||
                                    _invalidClientResponse == InvalidClientResponse.LenientKick &&
                                    Core.Now - state.Mobile.Created > _ageLeniency &&
                                    state.Mobile is PlayerMobile mobile &&
                                    mobile.GameTime > _gameTimeLeniency;

            bool shouldKick = false;
            bool isKRClient = version.Type == ClientType.KR;

            if (!isKRClient && UOClient.MinRequired != null && version < UOClient.MinRequired)
            {
                sb.Append($"This server doesn't support clients older than {UOClient.MinRequired}.");
                shouldKick = strictRequirement;
            }
            else if (!isKRClient && UOClient.MaxRequired != null && version > UOClient.MaxRequired)
            {
                sb.Append($"This server doesn't support clients newer than {UOClient.MaxRequired}.");
                shouldKick = strictRequirement;
            }
            else
            {
                if (!AllowClassic && version.Type == ClientType.Classic)
                {
                    sb.Append("This server does not allow classic clients to connect.");
                    shouldKick = true;
                }
                else if (!AllowUOTD && state.IsUOTDClient)
                {
                    sb.Append("This server does not allow UO:TD clients to connect.");
                    shouldKick = true;
                }
                else if (!AllowKR && version.Type == ClientType.KR)
                {
                    sb.Append("This server does not allow UO:KR clients to connect.");
                    shouldKick = true;
                }
                else if (!AllowSA && version.Type == ClientType.SA)
                {
                    sb.Append("This server does not allow UO:SA clients to connect.");
                    shouldKick = true;
                }

                if (sb.Length > 0)
                {
                    sb.Append(_allowedClientsMessage);
                }
            }

            if (sb.Length > 0)
            {
                state.Mobile.SendMessage(0x22, sb.ToString());
            }

            if (shouldKick)
            {
                state.Mobile.SendMessage(0x22, $"You will be disconnected in {KickDelay.TotalSeconds:F0} seconds.");
                Timer.StartTimer(KickDelay, () => OnKick(state));
                return;
            }

            if (sb.Length > 0)
            {
                switch (_invalidClientResponse)
                {
                    case InvalidClientResponse.Warn:
                        {
                            state.Mobile.SendMessage(
                                0x22,
                                $"This server recommends that your client version is {GetVersionExpression()}."
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

            sb.Dispose();
        }

        private static void OnKick(NetState ns)
        {
            if (ns.Running)
            {
                var version = ns.Version;
                ns.LogInfo($"Disconnecting, bad version ({version})");
                ns.Disconnect($"Invalid client version {version}.");
            }
        }

        private static void KickMessage(Mobile from)
        {
            from.SendMessage("You will be reminded of this again.");

            if (_invalidClientResponse == InvalidClientResponse.LenientKick)
            {
                from.SendMessage(
                    $"Invalid clients will be kicked after {_ageLeniency} days of character age and {_gameTimeLeniency} hours of play time"
                );
            }

            Timer.StartTimer(TimeSpan.FromMinutes(Utility.Random(5, 15)), () => SendAnnoyGump(from));
        }

        private static void SendAnnoyGump(Mobile m)
        {
            if (m.NetState != null)
            {
                m.SendGump(new AnnoyGump(m.NetState.Version, () => KickMessage(m)));
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

        private class AnnoyGump : StaticNoticeGump<AnnoyGump>
        {
            public override int Width => 480;
            public override int Height => 360;

            public override string Content { get; }

            public AnnoyGump(ClientVersion version, Action callback) : base(callback) =>
                Content = $"Your client is invalid.<br>This server recommends that your client version is {GetVersionExpression()}.<br><br>You are currently using version {version}.";

            protected override void BuildLayout(ref StaticGumpBuilder builder)
            {
                builder.SetNoDispose();
                builder.SetNoResize();
                builder.SetNoMove();

                base.BuildLayout(ref builder);
            }
        }
    }
}
