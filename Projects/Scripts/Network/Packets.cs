using Server.Buffers;
using Server.Items;

namespace Server.Scripts.Network
{
  public static partial class Packets
  {
    public static void SendDisplayEquipmentInfo(Item item, EquipmentInfo info)
    {
      EquipInfoAttribute[] attrs = info.Attributes;

      int packetLength = 17 +
                   (info.Crafter != null ? 6 + info.Crafter.Name?.Length ?? 0 : 0) +
                   (info.Unidentified ? 4 : 0) + attrs.Length * 6;

      SpanWriter writer = new SpanWriter(stackalloc byte[packetLength]);

      writer.Write((byte)0xBF); // Extended Command Packet ID
      writer.Write((ushort)packetLength); // Dynamic Length

      writer.Write((short)0x10); // Subcommand
      writer.Write(item.Serial);
      writer.Write(info.Number);

      if (info.Crafter != null)
      {
        string name = info.Crafter.Name;

        writer.Write(-3);

        if (name == null)
          writer.Write((ushort)0);
        else
        {
          int length = name.Length;
          writer.Write((ushort)length);
          writer.WriteAsciiFixed(name, length);
        }
      }

      if (info.Unidentified)
        writer.Write(-4);

      for (int i = 0; i < attrs.Length; ++i)
      {
        writer.Write(attrs[i].Number);
        writer.Write((short)attrs[i].Charges);
      }

      writer.Write(-1);
    }
  }
}
