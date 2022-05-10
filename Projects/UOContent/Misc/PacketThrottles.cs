using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Mobiles;

namespace Server.Network
{
    public static class PacketThrottles
    {
        // Delay in milliseconds
        private static readonly int[] Delays = new int[0x100];
        private const string ThrottlesConfiguration = "Configuration/throttles.json";

        public static void Initialize()
        {
            CommandSystem.Register("GetThrottle", AccessLevel.Administrator, GetThrottle);
            CommandSystem.Register("SetThrottle", AccessLevel.Administrator, SetThrottle);

            var configPath = ThrottlesConfiguration;
            var path = Path.Join(Core.BaseDirectory, configPath);

            if (File.Exists(path))
            {
                var throttles = JsonConfig.Deserialize<SortedDictionary<string, int>>(path);
                foreach (var (k, v) in throttles)
                {
                    if (!Utility.ToInt32(k, out var packetId))
                    {
                        Utility.PushColor(ConsoleColor.DarkYellow);
                        Console.WriteLine("Packet Throttles: Error deserializing {0} from {1}", k, configPath);
                        Utility.PopColor();
                        continue;
                    }

                    Delays[packetId] = v;
                }
            }
            else
            {
                Delays[0x03] = 25; // Speech
                Delays[0xAD] = 25; // Speech
                Delays[0x75] = 500; // Rename request
            }

            for (int i = 0; i < 0x100; i++)
            {
                if (Delays[i] > 0)
                {
                    IncomingPackets.RegisterThrottler(i, Throttle);
                }
            }

            SaveDelays();
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

            if (packetID is < 0 or > 0x100)
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

            if (packetID is < 0 or > 0x100)
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
            SaveDelays();
        }

        private static void SaveDelays()
        {
            SortedDictionary<string, int> table = new();
            for (var i = 0; i < Delays.Length; i++)
            {
                var delay = Delays[i];

                if (delay != 0)
                {
                    table[$"0x{i:X2}"] = delay;
                }
            }

            var configPath = ThrottlesConfiguration;
            var path = Path.Join(Core.BaseDirectory, configPath);
            JsonConfig.Serialize(path, table);
        }

        public static bool Throttle(int packetID, NetState ns, out bool drop)
        {
            if (ns.Mobile is not PlayerMobile player || player.AccessLevel >= AccessLevel.Counselor)
            {
                drop = false;
                return true;
            }

            if (Core.TickCount < ns.GetPacketTime(packetID) + Delays[packetID])
            {
                drop = true;
                return false;
            }

            drop = false;
            return true;
        }
    }
}
