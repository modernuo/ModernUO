using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Hag;

[SerializationGenerator(0, false)]
public partial class Zeefzorpul : BaseQuester
{
    public Zeefzorpul()
    {
    }

    public override string DefaultName => "Zeefzorpul";

    public override void InitBody()
    {
        Body = 0x4A;
    }

    public override bool CanTalkTo(PlayerMobile to) => false;

    public override void OnTalk(PlayerMobile player, bool contextMenu)
    {
    }

    [AfterDeserialization(false)]
    private void AfterDeserialization()
    {
        Delete();
    }
}
