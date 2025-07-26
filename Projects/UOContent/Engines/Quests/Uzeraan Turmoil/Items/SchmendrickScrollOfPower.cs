using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven;

[SerializationGenerator(0, false)]
public partial class SchmendrickScrollOfPower : QuestItem
{
    public SchmendrickScrollOfPower() : base(0xE34) => Hue = 0x34D;

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1049118; // a scroll with ancient markings

    public override bool CanDrop(PlayerMobile player) =>
        !(player.Quest is UzeraanTurmoilQuest qs &&
          qs.IsObjectiveInProgress(typeof(ReturnScrollOfPowerObjective)));
}
