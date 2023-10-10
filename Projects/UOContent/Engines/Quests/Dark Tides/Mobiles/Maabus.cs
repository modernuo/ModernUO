using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Necro;

[SerializationGenerator(0, false)]
public partial class Maabus : BaseQuester
{
    public Maabus()
    {
    }

    public override string DefaultName => "Maabus";

    public override void InitBody()
    {
        Body = 0x94;
    }

    public override bool CanTalkTo(PlayerMobile to) => false;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
    }
}
