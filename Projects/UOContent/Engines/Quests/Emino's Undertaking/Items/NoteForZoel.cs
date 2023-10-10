using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Ninja;

[SerializationGenerator(0, false)]
public partial class NoteForZoel : QuestItem
{
    [Constructible]
    public NoteForZoel() : base(0x14EF)
    {
        Weight = 1.0;
        Hue = 0x6B9;
    }

    public override int LabelNumber => 1063186; // A Note for Zoel

    public override bool CanDrop(PlayerMobile player) => player.Quest is not EminosUndertakingQuest;
}
