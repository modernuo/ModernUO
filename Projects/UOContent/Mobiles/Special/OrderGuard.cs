using ModernUO.Serialization;
using Server.Guilds;
using Server.Items;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class OrderGuard : BaseShieldGuard
{
    [Constructible]
    public OrderGuard()
    {
    }

    public override int Keyword => 0x21; // *order shield*
    public override BaseShield Shield => new OrderShield();
    public override int SignupNumber => 1007141; // Sign up with a guild of order if thou art interested.
    public override GuildType Type => GuildType.Order;

    public override bool BardImmune => true;
}
