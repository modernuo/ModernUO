using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class GrizzledMareStatuette : BaseImprisonedMobile
{
    [Constructible]
    public GrizzledMareStatuette() : base(0x2617) => Weight = 1.0;

    public override int LabelNumber => 1074475; // Grizzled Mare Statuette
    public override BaseCreature Summon => new GrizzledMare();
}
