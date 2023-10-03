using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class ClockworkAssembly : Item
{
    private static Type[] _requiredParts =
    {
        typeof(PowerCrystal),
        typeof(Gears),
        typeof(BronzeIngot),
        typeof(IronIngot),
    };

    private static int[] _requiredPartsClilocs =
    {
        1071945, // You need a power crystal to construct a golem.
        1071946, // You need more gears to construct a golem.
        1071947, // You need more bronze ingots to construct a golem.
        1071948, // You need more iron ingots to construct a golem.
    };

    private static int[] _requiredAmounts =
    {
        1,  // Power Crystal
        5,  // Gears
        50, // Bronze Ingot
        50, // Iron Ingot
    };

    [Constructible]
    public ClockworkAssembly() : base(0x1EA8)
    {
        Weight = 5.0;
        Hue = 1102;
    }

    public override int LabelNumber => 1073426; // Clockwork Assembly

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            // The clockwork assembly must be in your backpack to construct a golem.
            from.SendLocalizedMessage(1071944);
            return;
        }

        var tinkerSkill = from.Skills.Tinkering.Value;

        if (tinkerSkill < 60.0)
        {
            from.SendLocalizedMessage(1071943); // You must be a Journeyman or higher Tinker to construct a golem.
            return;
        }

        if (from.Followers + 4 > from.FollowersMax)
        {
            from.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
            return;
        }

        double scalar = tinkerSkill switch
        {
            >= 100.0 => 1.0,
            >= 90.0  => 0.9,
            >= 80.0  => 0.8,
            >= 70.0  => 0.7,
            _        => 0.6
        };

        var pack = from.Backpack;

        if (pack == null)
        {
            return;
        }

        var res = pack.ConsumeTotal(_requiredParts, _requiredAmounts);

        if (res >= 0)
        {
            from.SendLocalizedMessage(_requiredPartsClilocs[res]);
            return;
        }

        var g = new Golem(true, scalar);

        if (g.SetControlMaster(from))
        {
            Delete();

            g.MoveToWorld(from.Location, from.Map);
            from.PlaySound(0x241);
        }
    }
}
