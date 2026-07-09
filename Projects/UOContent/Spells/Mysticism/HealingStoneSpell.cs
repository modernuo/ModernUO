using System;
using Server.Items;

namespace Server.Spells.Mysticism;

public class HealingStoneSpell : MysticSpell
{
    private static readonly SpellInfo _info = new(
        "Healing Stone",
        "Kal In Mani",
        -1,
        9002,
        Reagent.Bone,
        Reagent.Garlic,
        Reagent.Ginseng,
        Reagent.SpidersSilk
    );

    public HealingStoneSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override SpellCircle Circle => SpellCircle.First;

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(5.0);

    public override bool CheckCast()
    {
        if (!base.CheckCast())
        {
            return false;
        }

        if (Caster.Backpack == null)
        {
            Caster.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return false;
        }

        return true;
    }

    public override void OnCast()
    {
        if (CheckSequence())
        {
            Caster.Backpack.FindItemByType<HealingStone>()?.Delete();

            var totalSkill = GetBaseSkill(Caster) + GetDamageSkill(Caster);
            var lifeForce = Math.Max(1, (int)(totalSkill * 1.25));
            var maxHealing = Math.Max(1, (int)(totalSkill / 6.0));
            var stone = new HealingStone(Caster, lifeForce, maxHealing);

            Caster.AddToBackpack(stone);
            Caster.SendLocalizedMessage(1080115); // A Healing Stone appears in your backpack.
            Caster.FixedParticles(0x3779, 1, 30, 0x26B8, 0, 0, EffectLayer.Waist);
            Caster.PlaySound(0x650);
        }

        FinishSequence();
    }
}
