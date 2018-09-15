namespace Server.Items
{
  // TODO: Commodity?
  public class DaemonBone : BaseReagent
  {
    [Constructible]
    public DaemonBone() : this(1)
    {
    }

    [Constructible]
    public DaemonBone(int amount) : base(0xF80, amount)
    {
    }

    public DaemonBone(Serial serial) : base(serial)
    {
    }

    public override double DefaultWeight => 1.0;


    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}