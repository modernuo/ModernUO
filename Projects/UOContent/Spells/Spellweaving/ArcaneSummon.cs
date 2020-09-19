using System;
using Server.Mobiles;
using Server.Utilities;

namespace Server.Spells.Spellweaving
{
    public abstract class ArcaneSummon<T> : ArcanistSpell where T : BaseCreature
    {
        public ArcaneSummon(Mobile caster, Item scroll, SpellInfo info)
            : base(caster, scroll, info)
        {
        }

        public abstract int Sound { get; }

        public override bool CheckCast()
        {
            if (!base.CheckCast())
            {
                return false;
            }

            if (Caster.Followers + 1 > Caster.FollowersMax)
            {
                Caster.SendLocalizedMessage(1074270); // You have too many followers to summon another one.
                return false;
            }

            return true;
        }

        public override void OnCast()
        {
            if (CheckSequence())
            {
                var duration = TimeSpan.FromMinutes(Caster.Skills.Spellweaving.Value / 24 + FocusLevel * 2);
                var summons = Math.Min(1 + FocusLevel, Caster.FollowersMax - Caster.Followers);

                for (var i = 0; i < summons; i++)
                {
                    BaseCreature bc;

                    try
                    {
                        bc = typeof(T).CreateInstance<BaseCreature>();
                    }
                    catch
                    {
                        break;
                    }

                    SpellHelper.Summon(bc, Caster, Sound, duration, false, false);
                }

                FinishSequence();
            }
        }
    }
}
