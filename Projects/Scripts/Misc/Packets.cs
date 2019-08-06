using System;

namespace Server.Network
{
  public static partial class Packets
  {
    public class EquipInfoAttribute
    {
      public EquipInfoAttribute(int number, int charges = -1)
      {
        Number = number;
        Charges = charges;
      }

      public int Number { get; }

      public int Charges { get; }
    }

    public class EquipmentInfo
    {
      public EquipmentInfo(int number, Mobile crafter, bool unidentified, EquipInfoAttribute[] attributes)
      {
        Number = number;
        Crafter = crafter;
        Unidentified = unidentified;
        Attributes = attributes;
      }

      public int Number { get; }

      public Mobile Crafter { get; }

      public bool Unidentified { get; }

      public EquipInfoAttribute[] Attributes { get; }
    }

    public sealed class DisplayEquipmentInfo : Packet
    {
      public DisplayEquipmentInfo(Item item, EquipmentInfo info) : base(0xBF)
      {
        EquipInfoAttribute[] attrs = info.Attributes;

        EnsureCapacity(17 + (info.Crafter?.Name.Length ?? 0) +
                       (info.Unidentified ? 4 : 0) + attrs.Length * 6);

        m_Stream.Write((short)0x10);
        m_Stream.Write(item.Serial);

        m_Stream.Write(info.Number);

        if (info.Crafter != null)
        {
          string name = info.Crafter.Name;

          m_Stream.Write(-3);

          if (name == null)
          {
            m_Stream.Write((ushort)0);
          }
          else
          {
            int length = name.Length;
            m_Stream.Write((ushort)length);
            m_Stream.WriteAsciiFixed(name, length);
          }
        }

        if (info.Unidentified)
          m_Stream.Write(-4);

        for (int i = 0; i < attrs.Length; ++i)
        {
          m_Stream.Write(attrs[i].Number);
          m_Stream.Write((short)attrs[i].Charges);
        }

        m_Stream.Write(-1);
      }
    }
  }
}
