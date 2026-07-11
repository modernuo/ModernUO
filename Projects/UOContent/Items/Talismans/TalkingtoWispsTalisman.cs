using ModernUO.Serialization;

namespace Server.Items;

/// <summary>
///     Britain Library Community Collection reward. Ward Removal is a fixed active effect,
///     not a random talisman, imbuing, runic, or loot property.
/// </summary>
[SerializationGenerator(0)]
public partial class TalkingtoWispsTalisman : BaseTalisman
{
    public override int LabelNumber => 1073356; // Library Talisman - Talking to Wisps
    public override bool ForceShowName => true;

    [Constructible]
    public TalkingtoWispsTalisman() : base(0x2F5B)
    {
        Blessed = GetRandomBlessed();
        SkillBonuses.SetValues(0, SkillName.SpiritSpeak, 3.0);
        SkillBonuses.SetValues(1, SkillName.EvalInt, 5.0);
        Removal = TalismanRemoval.Ward;
        MaxChargeTime = 1200;
    }
}
