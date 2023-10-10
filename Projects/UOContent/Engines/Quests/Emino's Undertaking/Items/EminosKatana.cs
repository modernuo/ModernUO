using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja;

[SerializationGenerator(0)]
public partial class EminosKatana : QuestItem
{
    [Constructible]
    public EminosKatana() : base(0x13FF) => Weight = 1.0;

    public override int LabelNumber => 1063214; // Daimyo Emino's Katana

    public override bool CanDrop(PlayerMobile player) => player.Quest is not EminosUndertakingQuest;
}
