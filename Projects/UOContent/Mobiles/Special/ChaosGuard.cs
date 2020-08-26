using Server.Guilds;
using Server.Items;

namespace Server.Mobiles
{
  public class ChaosGuard : BaseShieldGuard
  {
    [Constructible]
    public ChaosGuard()
    {
    }

    public ChaosGuard(Serial serial) : base(serial)
    {
    }

    public override int Keyword => 0x22; // *chaos shield*
    public override BaseShield Shield => new ChaosShield();
    public override int SignupNumber => 1007140; // Sign up with a guild of chaos if thou art interested.
    public override GuildType Type => GuildType.Chaos;

    public override bool BardImmune => true;

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