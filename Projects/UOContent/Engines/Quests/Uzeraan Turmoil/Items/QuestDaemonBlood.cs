using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class QuestDaemonBlood : QuestItem
{
    [Constructible]
    public QuestDaemonBlood() : base(0xF7D)
    {
    }

    public override double DefaultWeight => 1.0;

    public override bool CanDrop(PlayerMobile player) => player.Quest is not UzeraanTurmoilQuest;
}
