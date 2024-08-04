using ModernUO.Serialization;
using Server.Engines.ML_Quests.Mobiles;
using Server.Items;

namespace Server.Engines.Quests;

[SerializationGenerator( 0, false )]
public partial class Petrus : MondainQuestor
{
    [Constructible]
    public Petrus()
        : base( "Petrus", "the bee keeper" )
    {
    }

    public override void InitBody()
    {
        InitStats( 100, 100, 25 );

        Female = false;
        Race = Race.Human;

        Hue = 0x840C;
        HairItemID = 0x203C;
        HairHue = 0x3B3;
    }

    public override void InitOutfit()
    {
        SetWearable( new Backpack() );
        SetWearable( new Sandals(), 0x1BB, 1 );
        SetWearable( new ShortPants(), 0x71C, 1 );
        SetWearable( new Tunic(), 0x5EF, 1 );
    }
}
