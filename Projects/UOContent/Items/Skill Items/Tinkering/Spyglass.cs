using ModernUO.Serialization;
using Server.Engines.Quests.Hag;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

[Flippable(0x14F5, 0x14F6)]
[SerializationGenerator(0, false)]
public partial class Spyglass : Item
{
    [Constructible]
    public Spyglass() : base(0x14F5) => Weight = 3.0;

    public override void OnDoubleClick(Mobile from)
    {
        // You peer into the heavens, seeking the moons...
        from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1008155);

        from.NetState.SendMessageLocalizedAffix(
            from.Serial,
            from.Body,
            MessageType.Regular,
            0x3B2,
            3,
            1008146 + (int)Clock.GetMoonPhase(Map.Trammel, from.X, from.Y),
            "",
            AffixType.Prepend,
            "Trammel : "
        );

        from.NetState.SendMessageLocalizedAffix(
            from.Serial,
            from.Body,
            MessageType.Regular,
            0x3B2,
            3,
            1008146 + (int)Clock.GetMoonPhase(Map.Felucca, from.X, from.Y),
            "",
            AffixType.Prepend,
            "Felucca : "
        );

        if (from is PlayerMobile player)
        {
            var qs = player.Quest;

            if (qs is not WitchApprenticeQuest)
            {
                return;
            }

            var obj = qs.FindObjective<FindIngredientObjective>();

            if (obj?.Completed == false && obj.Ingredient == Ingredient.StarChart)
            {
                Clock.GetTime(from.Map, from.X, from.Y, out var hours, out int _);

                if (hours is < 5 or > 17)
                {
                    // You gaze up into the glittering night sky.  With great care, you compose a chart of the most prominent star patterns.
                    player.SendLocalizedMessage(1055040);

                    obj.Complete();
                }
                else
                {
                    // You gaze up into the sky, but it is not dark enough to see any stars.
                    player.SendLocalizedMessage(1055039);
                }
            }
        }
    }
}
