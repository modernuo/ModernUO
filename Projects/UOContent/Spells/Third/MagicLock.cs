using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Spells.Third
{
    public class MagicLockSpell : MagerySpell, ISpellTargetingItem
    {
        private static readonly SpellInfo m_Info = new(
            "Magic Lock",
            "An Por",
            215,
            9001,
            Reagent.Garlic,
            Reagent.Bloodmoss,
            Reagent.SulfurousAsh
        );

        public MagicLockSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Third;

        public void Target(Item item)
        {
            if (!(item is LockableContainer cont))
            {
                Caster.SendLocalizedMessage(501762); // Target must be an unlocked chest.
            }
            else if (BaseHouse.CheckLockedDownOrSecured(cont))
            {
                Caster.LocalOverheadMessage(
                    MessageType.Regular,
                    0x22,
                    501761
                ); // You cannot cast this on a locked down item.
            }
            else if (cont.Locked || cont.LockLevel == 0 || cont is ParagonChest)
            {
                Caster.SendLocalizedMessage(501762); // Target must be an unlocked chest.
            }
            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, cont);

                var loc = cont.GetWorldLocation();

                Effects.SendLocationParticles(
                    EffectItem.Create(loc, cont.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    32,
                    5020
                );

                Effects.PlaySound(loc, cont.Map, 0x1FA);

                // The chest is now locked!
                Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 501763);

                cont.LockLevel = -255; // signal magic lock
                cont.Locked = true;
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetItem(this, TargetFlags.None, Core.ML ? 10 : 12);
        }
    }
}
