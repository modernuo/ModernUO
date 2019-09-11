using Server.Buffers;
using Server.Items;

namespace Server.Network
{
  public static partial class Packets
  {
    public static void SendHairEquipUpdate(NetState ns, Mobile parent)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[15]);
      w.Write((byte)0x2E); // Packet ID

      int hue = parent.HairHue;

      if (parent.SolidHueOverride >= 0)
        hue = parent.SolidHueOverride;

      w.Write(HairInfo.FakeSerial(parent.Serial));
      w.Write((short)parent.HairItemID);
      w.Write((byte)0);
      w.Write((byte)Layer.Hair);
      w.Write(parent.Serial);
      w.Write((short)hue);

      ns.Send(w.RawSpan);
    }

    public static void SendFacialHairEquipUpdate(NetState ns, Mobile parent)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[15]);
      w.Write((byte)0x2E); // Packet ID

      int hue = parent.FacialHairHue;

      if (parent.SolidHueOverride >= 0)
        hue = parent.SolidHueOverride;

      w.Write(FacialHairInfo.FakeSerial(parent.Serial));
      w.Write((short)parent.FacialHairItemID);
      w.Write((byte)0);
      w.Write((byte)Layer.Hair);
      w.Write(parent.Serial);
      w.Write((short)hue);

      ns.Send(w.RawSpan);
    }

    public static void SendRemoveHair(NetState ns, Mobile parent)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[5]);
      w.Write((byte)0x1D); // Packet ID

      w.Write(HairInfo.FakeSerial(parent.Serial));

      ns.Send(w.RawSpan);
    }

    public static void SendRemoveFacialHair(NetState ns, Mobile parent)
    {
      if (ns == null)
        return;

      SpanWriter w = new SpanWriter(stackalloc byte[5]);
      w.Write((byte)0x1D); // Packet ID

      w.Write(FacialHairInfo.FakeSerial(parent.Serial));

      ns.Send(w.RawSpan);
    }
  }
}
