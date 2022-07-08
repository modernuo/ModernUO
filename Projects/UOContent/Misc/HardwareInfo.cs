using System;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Network;
using Server.Targeting;

namespace Server
{
    public class HardwareInfo
    {
        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuModel { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuClockSpeed { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuQuantity { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OSMajor { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OSMinor { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int OSRevision { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int InstanceID { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScreenWidth { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScreenHeight { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ScreenDepth { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PhysicalMemory { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuManufacturer { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CpuFamily { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VCVendorID { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VCDeviceID { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VCMemory { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DXMajor { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DXMinor { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string VCDescription { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Language { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Distribution { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ClientsRunning { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int ClientsInstalled { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public int PartialInstalled { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Unknown { get; private set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TimeReceived { get; private set; }

        public static unsafe void Configure()
        {
            IncomingPackets.Register(0xD9, 0x10C, false, &OnReceive);

            CommandSystem.Register("HWInfo", AccessLevel.GameMaster, HWInfo_OnCommand);
        }

        [Usage("HWInfo"), Description("Displays information about a targeted player's hardware.")]
        public static void HWInfo_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, HWInfo_OnTarget);
            e.Mobile.SendMessage("Target a player to view their hardware information.");
        }

        public static void HWInfo_OnTarget(Mobile from, object obj)
        {
            if (obj is Mobile m && m.Player)
            {
                if (m.Account is Account acct)
                {
                    var hwInfo = acct.HardwareInfo;

                    if (hwInfo != null)
                    {
                        CommandLogging.WriteLine(
                            from,
                            "{0} {1} viewing hardware info of {2}",
                            from.AccessLevel,
                            CommandLogging.Format(from),
                            CommandLogging.Format(m)
                        );
                    }

                    if (hwInfo != null)
                    {
                        from.SendGump(new PropertiesGump(from, hwInfo));
                    }
                    else
                    {
                        from.SendMessage("No hardware information for that account was found.");
                    }
                }
                else
                {
                    from.SendMessage("No account has been attached to that player.");
                }
            }
            else
            {
                from.BeginTarget(-1, false, TargetFlags.None, HWInfo_OnTarget);
                from.SendMessage("That is not a player. Try again.");
            }
        }

        public static void OnReceive(NetState state, CircularBufferReader reader, int packetLength)
        {
            reader.ReadByte(); // 1: <4.0.1a, 2>=4.0.1a

            var info = new HardwareInfo
            {
                InstanceID = reader.ReadInt32(),
                OSMajor = reader.ReadInt32(),
                OSMinor = reader.ReadInt32(),
                OSRevision = reader.ReadInt32(),
                CpuManufacturer = reader.ReadByte(),
                CpuFamily = reader.ReadInt32(),
                CpuModel = reader.ReadInt32(),
                CpuClockSpeed = reader.ReadInt32(),
                CpuQuantity = reader.ReadByte(),
                PhysicalMemory = reader.ReadInt32(),
                ScreenWidth = reader.ReadInt32(),
                ScreenHeight = reader.ReadInt32(),
                ScreenDepth = reader.ReadInt32(),
                DXMajor = reader.ReadInt16(),
                DXMinor = reader.ReadInt16(),
                VCDescription = reader.ReadLittleUniSafe(64),
                VCVendorID = reader.ReadInt32(),
                VCDeviceID = reader.ReadInt32(),
                VCMemory = reader.ReadInt32(),
                Distribution = reader.ReadByte(),
                ClientsRunning = reader.ReadByte(),
                ClientsInstalled = reader.ReadByte(),
                PartialInstalled = reader.ReadByte(),
                Language = reader.ReadLittleUniSafe(4),
                Unknown = reader.ReadAsciiSafe(64),
                TimeReceived = Core.Now
            };

            if (state.Account is Account acct)
            {
                acct.HardwareInfo = info;
            }
        }
    }
}
