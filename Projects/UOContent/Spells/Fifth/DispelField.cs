using Server.Items;
using Server.Misc;

namespace Server.Spells.Fifth
{
    public class DispelFieldSpell : MagerySpell, ITargetingSpell<Item>
    {
        private static readonly SpellInfo _info = new(
            "Dispel Field",
            "An Grav",
            206,
            9002,
            Reagent.BlackPearl,
            Reagent.SpidersSilk,
            Reagent.SulfurousAsh,
            Reagent.Garlic
        );

        public DispelFieldSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fifth;

        public int TargetRange => Core.T2A ? 15 : 18;

        public void Target(Item item)
        {
            if (!item.GetType().IsDefined(typeof(DispellableFieldAttribute), false))
            {
                Caster.SendLocalizedMessage(1005049); // That cannot be dispelled.
            }
            else if (item is Moongate { Dispellable: false })
            {
                Caster.SendLocalizedMessage(1005047); // That magic is too chaotic
            }
            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, item);

                Effects.SendLocationParticles(
                    EffectItem.Create(item.Location, item.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    20,
                    5042
                );
                Effects.PlaySound(item.GetWorldLocation(), item.Map, 0x201);

                item.Delete();
            }
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Item>(this);
        }
    }
}
