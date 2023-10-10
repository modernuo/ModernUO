using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Samurai;

[SerializationGenerator(0)]
public partial class HaochisKatana : QuestItem
{
    [Constructible]
    public HaochisKatana() : base(0x13FF) => Weight = 1.0;

    public override int LabelNumber => 1063165; // Daimyo Haochi's Katana

    public override bool CanDrop(PlayerMobile player) => player.Quest is not HaochisTrialsQuest;
}
