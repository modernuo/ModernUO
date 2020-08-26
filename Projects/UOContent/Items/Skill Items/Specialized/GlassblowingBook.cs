using Server.Mobiles;

namespace Server.Items
{
  public class GlassblowingBook : Item
  {
    [Constructible]
    public GlassblowingBook() : base(0xFF4) => Weight = 1.0;

    public GlassblowingBook(Serial serial) : base(serial)
    {
    }

    public override string DefaultName => "Crafting Glass With Glassblowing";

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
      else if (pm == null || from.Skills.Alchemy.Base < 100.0)
      {
        pm.SendMessage("Only a Grandmaster Alchemist can learn from this book.");
      }
      else if (pm.Glassblowing)
      {
        pm.SendMessage("You have already learned this information.");
      }
      else
      {
        pm.Glassblowing = true;
        pm.SendMessage(
          "You have learned to make items from glass. You will need to find miners to mine find sand for you to make these items.");
        Delete();
      }
    }
  }
}