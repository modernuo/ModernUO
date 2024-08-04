using ModernUO.Serialization;
using Server.Engines.ML_Quests.Mobiles;
using Server.Items;

namespace Server.Engines.Quests;

[SerializationGenerator( 0 )]
public partial class Verity : MondainQuestor
{
    [Constructible]
    public Verity()
        : base( "Verity", "the librarian" )
    {
    }

    public override void InitBody()
    {
        InitStats( 100, 100, 25 );

        Female = true;
        Race = Race.Human;

        Hue = 0x83EF;
        HairItemID = 0x2047;
        HairHue = 0x3B3;
    }

    public override void InitOutfit()
    {
        SetWearable( new Backpack() );
        SetWearable( new Shoes(), 0x754, 1 );
        SetWearable( new Shirt(), 0x653, 1 );
        SetWearable( new Cap(), 0x901, 1 );
        SetWearable( new Kilt(), 0x901, 1 );
    }
}
