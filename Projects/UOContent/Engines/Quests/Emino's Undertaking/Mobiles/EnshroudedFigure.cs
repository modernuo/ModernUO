using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja;

[SerializationGenerator(0)]
public partial class EnshroudedFigure : BaseQuester
{
    [Constructible]
    public EnshroudedFigure()
    {
    }

    public override string DefaultName => "an enshrouded figure";

    public override int TalkNumber => -1;

    public override void InitBody()
    {
        InitStats(100, 100, 25);

        Hue = 0x8401;
        Female = false;
        Body = 0x190;
    }

    public override void InitOutfit()
    {
        AddItem(new DeathShroud());
        AddItem(new ThighBoots());
    }

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
    }
}
