// using System.Collections.Generic;

namespace Server.Items.Holiday
{
  [TypeAlias("Server.Items.ClownMask", "Server.Items.DaemonMask", "Server.Items.PlagueMask")]
  public class BasePaintedMask : Item
  {
    private static readonly string[] m_Staffers =
    {
      "Ryan",
      "Mark",
      "Krrios",
      "Zippy",
      "Athena",
      "Eos",
      "Xavier"
    };

    private string m_Staffer;

    public BasePaintedMask(int itemid) : this(m_Staffers.RandomElement(), itemid)
    {
    }

    public BasePaintedMask(string staffer, int itemid) : base(itemid + Utility.Random(2))
    {
      m_Staffer = staffer;

      Utility.Intern(m_Staffer);
    }

    public BasePaintedMask(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => m_Staffer != null ? $"{MaskName} hand painted by {m_Staffer}" : MaskName;

    public virtual string MaskName => "A Mask";

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(1); // version
      writer.Write(m_Staffer);
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      if (version == 1) m_Staffer = Utility.Intern(reader.ReadString());
    }
  }
}
