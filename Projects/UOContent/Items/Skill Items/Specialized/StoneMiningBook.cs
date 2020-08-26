using Server.Mobiles;

namespace Server.Items
{
  public class StoneMiningBook : Item
  {
    [Constructible]
    public StoneMiningBook() : base(0xFBE) => Weight = 1.0;

    public StoneMiningBook(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Mining For Quality Stone";

    public override void Serialize(IGenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(IGenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }

    public override void OnDoubleClick(Mobile from)
    {
      PlayerMobile pm = from as PlayerMobile;

      if (!IsChildOf(from.Backpack))
      {
        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
      }
      else if (pm == null || from.Skills.Mining.Base < 100.0)
      {
        from.SendMessage("Only a Grandmaster Miner can learn from this book.");
      }
      else if (pm.StoneMining)
      {
        pm.SendMessage("You have already learned this knowledge.");
      }
      else
      {
        pm.StoneMining = true;
        pm.SendMessage("You have learned to mine for stones. Target mountains when mining to find stones.");
        Delete();
      }
    }
  }
}