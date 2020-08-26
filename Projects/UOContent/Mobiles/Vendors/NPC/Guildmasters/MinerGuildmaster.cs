namespace Server.Mobiles
{
  public class MinerGuildmaster : BaseGuildmaster
  {
    [Constructible]
    public MinerGuildmaster() : base("miner")
    {
      SetSkill(SkillName.ItemID, 60.0, 83.0);
      SetSkill(SkillName.Mining, 90.0, 100.0);
    }

    public MinerGuildmaster(Serial serial) : base(serial)
    {
    }

    public override NpcGuild NpcGuild => NpcGuild.MinersGuild;

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