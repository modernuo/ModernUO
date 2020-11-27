using Server.Items;
using Server.Targeting;

namespace Server.Spells.Third
{
    public class TelekinesisSpell : MagerySpell, ISpellTargetingItem
    {
        private static readonly SpellInfo m_Info = new(
            "Telekinesis",
            "Ort Por Ylem",
            203,
            9031,
            Reagent.Bloodmoss,
            Reagent.MandrakeRoot
        );

        public TelekinesisSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Third;

        public void Target(Item item)
        {
            var t = item as ITelekinesisable;
            if (!(t != null || item is Container))
            {
                Caster.SendLocalizedMessage(501857); // This spell won't work on that!
                return;
            }

            if (CheckSequence())
            {
                if (t != null)
                {
                    SpellHelper.Turn(Caster, t);
                    t.OnTelekinesis(Caster);
                }
                else
                {
                    SpellHelper.Turn(Caster, item);

                    if (!item.IsAccessibleTo(Caster))
                    {
                        item.OnDoubleClickNotAccessible(Caster);
                    }
                    else if (!item.CheckItemUse(Caster, item))
                    {
                    }
                    else if (item.RootParent is Mobile && item.RootParent != Caster)
                    {
                        item.OnSnoop(Caster);
                    }
                    else if (item is Corpse corpse && !corpse.CheckLoot(Caster, null))
                    {
                    }
                    else if (Caster.Region.OnDoubleClick(Caster, item))
                    {
                        Effects.SendLocationParticles(
                            EffectItem.Create(item.Location, item.Map, EffectItem.DefaultDuration),
                            0x376A,
                            9,
                            32,
                            5022
                        );
                        Effects.PlaySound(item.Location, item.Map, 0x1F5);

                        item.OnItemUsed(Caster, item);
                    }
                }
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetItem(this, TargetFlags.None, Core.ML ? 10 : 12);
        }
    }
}

namespace Server
{
    public interface ITelekinesisable : IPoint3D
    {
        void OnTelekinesis(Mobile from);
    }
}
