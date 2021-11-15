using Server.Items;
using Server.Targeting;

namespace Server.Spells.Second
{
    public class MagicTrapSpell : MagerySpell, ISpellTargetingItem
    {
        private static readonly SpellInfo _info = new(
            "Magic Trap",
            "In Jux",
            212,
            9001,
            Reagent.Garlic,
            Reagent.SpidersSilk,
            Reagent.SulfurousAsh
        );

        public MagicTrapSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Second;

        public void Target(Item item)
        {
            if (item is not TrappableContainer cont)
            {
                Caster.SendLocalizedMessage(502942); // You can't trap this!
            }
            else if (cont.TrapType != TrapType.None && cont.TrapType != TrapType.MagicTrap)
            {
                DoFizzle();
            }
            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, item);

                cont.TrapType = TrapType.MagicTrap;
                cont.TrapPower = Core.AOS ? Utility.RandomMinMax(10, 50) : 1;
                cont.TrapLevel = 0;

                var loc = item.GetWorldLocation();

                Effects.SendLocationParticles(
                    EffectItem.Create(new Point3D(loc.X + 1, loc.Y, loc.Z), item.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    10,
                    9502
                );
                Effects.SendLocationParticles(
                    EffectItem.Create(new Point3D(loc.X, loc.Y - 1, loc.Z), item.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    10,
                    9502
                );
                Effects.SendLocationParticles(
                    EffectItem.Create(new Point3D(loc.X - 1, loc.Y, loc.Z), item.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    10,
                    9502
                );
                Effects.SendLocationParticles(
                    EffectItem.Create(new Point3D(loc.X, loc.Y + 1, loc.Z), item.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    10,
                    9502
                );
                Effects.SendLocationParticles(
                    EffectItem.Create(new Point3D(loc.X, loc.Y, loc.Z), item.Map, EffectItem.DefaultDuration),
                    0,
                    0,
                    0,
                    5014
                );

                Effects.PlaySound(loc, item.Map, 0x1EF);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetItem(this, range: Core.ML ? 10 : 12);
        }
    }
}
