using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Server.Json;
using Server.Mobiles;

namespace Server.Network
{
    public static class PacketThrottles
    {
        // Delay in milliseconds
        private static readonly int[] Delays = new int[0x100];
        private static string ThrottlesConfiguration = "Configuration/throttles.json";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetDefault(int packetId, int value)
        {
            if (Delays[packetId] == 0)
            {
                Delays[packetId] = value;
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("GetThrottle", AccessLevel.Administrator, GetThrottle);
            CommandSystem.Register("SetThrottle", AccessLevel.Administrator, SetThrottle);

            var configPath = ThrottlesConfiguration;
            var path = Path.Join(Core.BaseDirectory, configPath);

            // Load UOContent.dll
            var throttles = JsonConfig.Deserialize<SortedDictionary<string, int>>(path);
            foreach (var (k, v) in throttles)
            {
                if (!int.TryParse(k, out var packetId))
                {
                    Utility.PushColor(ConsoleColor.DarkYellow);
                    Console.WriteLine("Packet Throttles: Error deserializing {0} from {1}", k, configPath);
                    Utility.PopColor();
                    continue;
                }

                Delays[packetId] = v;
            }

            // Defaults
            SetDefault(0x03, 5); // Speech
            SetDefault(0xAD, 5); // Speech
            SetDefault(0x75, 500); // Rename request

            for (int i = 0; i < 0x100; i++)
            {
                if (Delays[i] > 0)
                {
                    IncomingPackets.RegisterThrottler(i, Throttle);
                }
            }
        }

        [Usage("GetThrottle <packetID>")]
        [Description("Gets throttle for the given packet.")]
        public static void GetThrottle(CommandEventArgs e)
        {
            if (e.Length != 1)
            {
                e.Mobile.SendMessage("Invalid Command Format. Should be [GetThrottle <packetID>");
                return;
            }

            int packetID = e.GetInt32(0);

            if (packetID < 0 || packetID > 0x100)
            {
                e.Mobile.SendMessage("Invalid Command Format. PacketID must be between 0 and 0x100.");
                return;
            }


            e.Mobile.SendMessage("Packet 0x{0:X} throttle is currently {1}ms.", packetID, Delays[packetID]);
        }

        [Usage("SetThrottle <packetID> <timeInMilliseconds>")]
        [Description("Sets a throttle for the given packet.")]
        public static void SetThrottle(CommandEventArgs e)
        {
            if (e.Length != 2)
            {
                e.Mobile.SendMessage("Invalid Command Format. Should be [SetThrottle <packetID> <timeInMilliseconds>");
                return;
            }

            int packetID = e.GetInt32(0);
            int delay = e.GetInt32(1);

            if (packetID < 0 || packetID > 0x100)
            {
                e.Mobile.SendMessage("Invalid Command Format. PacketID must be between 0 and 0x100.");
                return;
            }


            if (delay > 5000)
            {
                e.Mobile.SendMessage("Invalid Command Format. Delay cannot exceed 5000 milliseconds.");
                return;
            }

            long oldDelay = Delays[packetID];

            if (oldDelay == 0 && delay > 0)
            {
                IncomingPackets.RegisterThrottler(packetID, Throttle);
            }
            else if (oldDelay > 0 && delay == 0)
            {
                IncomingPackets.RegisterThrottler(packetID, null);
            }

            Delays[packetID] = delay;
        }

        public static bool Throttle(int packetID, NetState ns, out bool drop)
        {
            if (ns.Mobile is not PlayerMobile player || player.AccessLevel >= AccessLevel.Counselor)
            {
                drop = false;
                return true;
            }

            if (Core.TickCount < ns.GetPacketDelay(packetID) + Delays[packetID])
            {
                drop = true;
                return false;
            }

            drop = false;
            return true;
        }
    }
}
