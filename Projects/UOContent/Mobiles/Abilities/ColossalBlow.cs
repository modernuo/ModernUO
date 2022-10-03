using Server.Network;

namespace Server.Mobiles;

public class ColossalBlow : StunAttack
{
    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        target.Animate(21, 6, 1, true, false, 0);
        source.PlaySound(0xEE);
        target.LocalOverheadMessage(
            MessageType.Regular,
            0x3B2,
            false,
            "You have been stunned by a colossal blow!"
        );

        base.Trigger(trigger, source, target);
    }

    protected override void Recover(Mobile defender)
    {
        base.Recover(defender);

        defender.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You recover your senses.");
    }
}
