using ModernUO.Serialization;
using Server.Guilds;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class ChaosGuard : BaseShieldGuard
{
    [Constructible]
    public ChaosGuard()
    {
    }

    public override int Keyword => 0x22; // *chaos shield*
    public override BaseShield Shield => new ChaosShield();
    public override int SignupNumber => 1007140; // Sign up with a guild of chaos if thou art interested.
    public override GuildType Type => GuildType.Chaos;

    public override bool BardImmune => true;
}
