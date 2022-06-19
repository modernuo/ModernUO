using Server.Items;

namespace Server.Spells.Second
{
    public class RemoveTrapSpell : MagerySpell, ISpellTargetingItem
    {
        private static readonly SpellInfo _info = new(
            "Remove Trap",
            "An Jux",
            212,
            9001,
            Reagent.Bloodmoss,
            Reagent.SulfurousAsh
        );

        public RemoveTrapSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Second;

        public void Target(Item item)
        {
            if (item is not TrappableContainer cont)
            {
                Caster.SendLocalizedMessage(502373); // That doesn't appear to be trapped
            }
            else if (cont.TrapType != TrapType.None && cont.TrapType != TrapType.MagicTrap)
            {
                DoFizzle();
            }
            else if (CheckSequence())
            {
                SpellHelper.Turn(Caster, item);

                var loc = item.GetWorldLocation();

                Effects.SendLocationParticles(
                    EffectItem.Create(loc, item.Map, EffectItem.DefaultDuration),
                    0x376A,
                    9,
                    32,
                    5015
                );
                Effects.PlaySound(loc, item.Map, 0x1F0);

                cont.TrapType = TrapType.None;
                cont.TrapPower = 0;
                cont.TrapLevel = 0;
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetItem(this, range: Core.ML ? 10 : 12);
            Caster.SendLocalizedMessage(502368);
        }
    }
}
