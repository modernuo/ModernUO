namespace Server.Items
{
  [Flippable(0x1B09, 0x1B10)]
  public class BonePile : Item, IScissorable
  {
    [Constructible]
    public BonePile() : base(0x1B09 + Utility.Random(8))
    {
      Stackable = false;
      Weight = 10.0;
    }

    public BonePile(Serial serial) : base(serial)
    {
    }

    public bool Scissor(Mobile from, Scissors scissors)
    {
      if (Deleted || !from.CanSee(this))
        return false;

      ScissorHelper(from, new Bone(), Utility.RandomMinMax(10, 15));

      return true;
    }

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
  }
}