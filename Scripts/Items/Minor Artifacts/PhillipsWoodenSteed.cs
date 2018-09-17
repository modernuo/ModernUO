namespace Server.Items
{
  public class PhillipsWoodenSteed : MonsterStatuette
  {
    [Constructible]
    public PhillipsWoodenSteed() : base(MonsterStatuetteType.PhillipsWoodenSteed)
    {
      LootType = LootType.Regular;
    }

    public PhillipsWoodenSteed(Serial serial) : base(serial)
    {
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0);
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();
    }
  }
}