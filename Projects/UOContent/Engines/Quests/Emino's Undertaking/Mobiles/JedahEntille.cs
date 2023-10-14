using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja;

[SerializationGenerator(0)]
public partial class JedahEntille : BaseQuester
{
    [Constructible]
    public JedahEntille() : base("the Silent")
    {
    }

    public override string DefaultName => "Jedah Entille";

    public override int TalkNumber => -1;

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x83FE;
        Female = true;
        Body = 0x191;
    }

    public override void InitOutfit()
    {
        HairItemID = 0x203C;
        HairHue = 0x6BE;

        AddItem(new PlainDress(0x528));
        AddItem(new ThighBoots());
        AddItem(new FloppyHat());
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
    }
}
