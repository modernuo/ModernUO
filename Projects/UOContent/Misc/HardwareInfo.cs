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

    public static void Initialize()
    {
      PacketHandlers.Register(0xD9, 0x10C, false, OnReceive);

      CommandSystem.Register("HWInfo", AccessLevel.GameMaster, HWInfo_OnCommand);
    }

    [Usage("HWInfo")]
    [Description("Displays information about a targeted player's hardware.")]
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
          HardwareInfo hwInfo = acct.HardwareInfo;

          if (hwInfo != null)
            CommandLogging.WriteLine(from, "{0} {1} viewing hardware info of {2}", from.AccessLevel,
              CommandLogging.Format(from), CommandLogging.Format(m));

          if (hwInfo != null)
            from.SendGump(new PropertiesGump(from, hwInfo));
          else
            from.SendMessage("No hardware information for that account was found.");
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

    public static void OnReceive(NetState state, PacketReader pvSrc)
    {
      pvSrc.ReadByte(); // 1: <4.0.1a, 2>=4.0.1a

      HardwareInfo info = new HardwareInfo();

      info.InstanceID = pvSrc.ReadInt32();
      info.OSMajor = pvSrc.ReadInt32();
      info.OSMinor = pvSrc.ReadInt32();
      info.OSRevision = pvSrc.ReadInt32();
      info.CpuManufacturer = pvSrc.ReadByte();
      info.CpuFamily = pvSrc.ReadInt32();
      info.CpuModel = pvSrc.ReadInt32();
      info.CpuClockSpeed = pvSrc.ReadInt32();
      info.CpuQuantity = pvSrc.ReadByte();
      info.PhysicalMemory = pvSrc.ReadInt32();
      info.ScreenWidth = pvSrc.ReadInt32();
      info.ScreenHeight = pvSrc.ReadInt32();
      info.ScreenDepth = pvSrc.ReadInt32();
      info.DXMajor = pvSrc.ReadInt16();
      info.DXMinor = pvSrc.ReadInt16();
      info.VCDescription = pvSrc.ReadUnicodeStringLESafe(64);
      info.VCVendorID = pvSrc.ReadInt32();
      info.VCDeviceID = pvSrc.ReadInt32();
      info.VCMemory = pvSrc.ReadInt32();
      info.Distribution = pvSrc.ReadByte();
      info.ClientsRunning = pvSrc.ReadByte();
      info.ClientsInstalled = pvSrc.ReadByte();
      info.PartialInstalled = pvSrc.ReadByte();
      info.Language = pvSrc.ReadUnicodeStringLESafe(4);
      info.Unknown = pvSrc.ReadStringSafe(64);

      info.TimeReceived = DateTime.UtcNow;

      if (state.Account is Account acct)
        acct.HardwareInfo = info;
    }
  }
}