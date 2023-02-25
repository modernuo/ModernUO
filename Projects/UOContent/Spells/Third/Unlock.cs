using Server.Items;
using Server.Multis;

namespace Server.Spells.Third
{
    public class UnlockSpell : MagerySpell, ISpellTargetingPoint3D
    {
        private static readonly SpellInfo _info = new(
            "Unlock Spell",
            "Ex Por",
            215,
            9001,
            Reagent.Bloodmoss,
            Reagent.SulfurousAsh
        );

        public UnlockSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Third;

        public void Target(IPoint3D p)
        {
            if (CheckSequence())
            {
                SpellHelper.Turn(Caster, p);
                var loc = new Point3D(p);

                Effects.SendLocationParticles(
                    EffectItem.Create(loc, Caster.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    32,
                    5024
                );

                Effects.PlaySound(loc, Caster.Map, 0x1FF);

                if (p is Mobile)
                {
                    // That did not need to be unlocked.
                    Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503101);
                }
                else if (p is not LockableContainer cont)
                {
                    Caster.SendLocalizedMessage(501666); // You can't unlock that!
                }
                else if (BaseHouse.CheckSecured(cont))
                {
                    Caster.SendLocalizedMessage(503098); // You cannot cast this on a secure item.
                }
                else if (!cont.Locked)
                {
                    // That did not need to be unlocked.
                    Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503101);
                }
                else if (cont.LockLevel == ILockpickable.CannotPick)
                {
                    Caster.SendLocalizedMessage(501666); // You can't unlock that!
                }
                else
                {
                    var level = (int)(Caster.Skills.Magery.Value * 0.8) - 4;

                    if (level >= cont.RequiredSkill &&
                        !(cont is TreasureMapChest chest && chest.Level > 2))
                    {
                        cont.Locked = false;

                        if (cont.LockLevel == ILockpickable.MagicLock)
                        {
                            cont.LockLevel = cont.RequiredSkill - 10;
                        }
                    }
                    else
                    {
                        // My spell does not seem to have an effect on that lock.
                        Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503099);
                    }
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetPoint3D(this, range: Core.ML ? 10 : 12);
        }
    }
}
