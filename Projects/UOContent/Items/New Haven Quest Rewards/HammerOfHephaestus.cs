using System;
using ModernUO.Serialization;

namespace Server.Items;

[Flippable(0x13E3, 0x13E4)]
[SerializationGenerator(0)]
public partial class HammerOfHephaestus : SmithHammer
{
    public static readonly TimeSpan RechargeDelay = TimeSpan.FromMinutes(5);

    [Constructible]
    public HammerOfHephaestus()
    {
        UsesRemaining = 20;
        LootType = LootType.Blessed;

        // TODO: Blacksmith +10 bonus when equipped

        StartRechargeTimer();
    }

    public override int LabelNumber => 1077740; // Hammer of Hephaestus

    public override bool BreakOnDepletion => false;
    /* Note:
     * On EA, it also leaves the crafting gump open when it reaches 0 charges.
     * When crafting again, only then the crafting gump closes with the 1072306 system message.
     */

    public override void OnDoubleClick(Mobile from)
    {
        // TODO: These checks don't match EA, but they match BaseTool for now
        if (!IsChildOf(from.Backpack) && Parent != from)
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (UsesRemaining <= 0)
        {
            from.SendLocalizedMessage(1072306); // You must wait a moment for it to recharge.
        }
        else
        {
            base.OnDoubleClick(from);
        }
    }

    private void StartRechargeTimer()
    {
        // TODO: Needs work
        // Timer.DelayCall( RechargeDelay, RechargeDelay, new TimerCallback( Recharge ) );
    }

    public void Recharge()
    {
        // TODO: Stop timer at 20? Count downtime? Something more generic so we can use it for JacobsPickaxe too (both are IUsesRemaining)?
        if (UsesRemaining < 20)
        {
            ++UsesRemaining;
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        StartRechargeTimer();
    }
}
