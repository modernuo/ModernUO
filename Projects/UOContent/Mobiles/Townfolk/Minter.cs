using ModernUO.Serialization;

namespace Server.Mobiles;

[SerializationGenerator(0, false)]
public partial class Minter : Banker
{
    [Constructible]
    public Minter() => Title = "the minter";

    public override NpcGuild NpcGuild => NpcGuild.MerchantsGuild;
}
