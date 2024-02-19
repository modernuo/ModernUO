using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Engines.Quests.Hag;

[SerializationGenerator(0, false)]
public partial class MagicFlute : Item
{
    [Constructible]
    public MagicFlute() : base(0x1421) => Hue = 0x8AB;

    public override int LabelNumber => 1055051; // magic flute

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            SendLocalizedMessageTo(from, 1042292); // You must have the object in your backpack to use it.
            return;
        }

        from.PlaySound(0x3D);

        if (from is not PlayerMobile player)
        {
            return;
        }

        var qs = player.Quest;

        if (qs is not WitchApprenticeQuest)
        {
            return;
        }

        var obj = qs.FindObjective<FindZeefzorpulObjective>();
        if (obj?.Completed != false)
        {
            return;
        }

        if (player.Map != Map.Trammel && player.Map != Map.Felucca || !player.InRange(obj.ImpLocation, 8))
        {
            // Nothing happens. Zeefzorpul must not be hiding in this area.
            player.SendLocalizedMessage(1055053);
            return;
        }

        if (player.InRange(obj.ImpLocation, 4))
        {
            Delete();
            obj.Complete();
            return;
        }

        // The flute sparkles. Zeefzorpul must be in a good hiding place nearby.
        player.SendLocalizedMessage(1055052);
    }
}
