using Server.Engines.PartySystem;
using Server.Items;
using Server.Spells.Fourth;
using Server.Spells.Necromancy;
using Server.Targeting;
using Server.Collections;

namespace Server.Spells.Mysticism
{
    public class CleansingWindsSpell : MysticSpell, ISpellTargetingMobile
    {
        public override SpellCircle Circle => SpellCircle.Sixth;

        private static readonly SpellInfo _info = new(
            "Cleansing Winds", "In Vas Mani Hur",
            230,
            9022,
            Reagent.Garlic,
            Reagent.Ginseng,
            Reagent.MandrakeRoot,
            Reagent.DragonsBlood
        );

        public CleansingWindsSpell(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Beneficial);
        }

        public void Target(Mobile m)
        {
            if (CheckBSequence(m))
            {
                /**
                 * Soothing winds attempt to neutralize poisons, lift curses,
                 * and heal a valid target and up to 3 party members.
                 */

                Caster.PlaySound(0x64C);

                using var pool = PooledRefQueue<Mobile>.Create();
                pool.Enqueue(m);

                var casterParty = Party.Get(Caster);
                if (casterParty != null)
                {
                    foreach (Mobile mob in Caster.Map.GetMobilesInRange(m.Location, 2))
                    {
                        if (mob == m)
                        {
                            continue;
                        }

                        if (casterParty.Contains(mob) && Caster.CanBeBeneficial(mob, false))
                        {
                            pool.Enqueue(mob);
                            if (pool.Count == 4)
                            {
                                break;
                            }
                        }
                    }
                }

                var primarySkill = GetBaseSkill(Caster);
                var secondarySkill = GetDamageSkill(Caster);

                var toHeal = ((int)((primarySkill + secondarySkill) / 4.0) + Utility.RandomMinMax(-3, 3)) / pool.Count;

                while (pool.Count > 0)
                {
                    var target = pool.Dequeue();

                    Caster.DoBeneficial(target);

                    target.FixedParticles(0x3709, 1, 30, 9963, 13, 3, EffectLayer.Head);

                    var from = new Entity(Serial.Zero, new Point3D(target.X, target.Y, target.Z - 10), target.Map);
                    var to = new Entity(Serial.Zero, new Point3D(target.X, target.Y, target.Z + 50), target.Map);
                    Effects.SendMovingParticles(
                        from,
                        to,
                        0x2255,
                        1,
                        0,
                        false,
                        false,
                        13,
                        3,
                        9501,
                        1,
                        0,
                        EffectLayer.Head,
                        0x100
                    );

                    var toHealMod = toHeal;

                    if (target.Poisoned)
                    {
                        var poisonLevel = target.Poison.Level + 1;
                        var chanceToCure = 10000 + (int)((primarySkill + secondarySkill) / 2 * 75) - poisonLevel * 1750;

                        if (chanceToCure > Utility.Random(10000) && target.CurePoison(Caster))
                        {
                            toHealMod -= (int)(toHeal * poisonLevel * 0.15);
                        }
                        else
                        {
                            toHealMod = 0;
                        }
                    }

                    if (MortalStrike.IsWounded(target))
                    {
                        toHealMod = 0;
                    }

                    var curseLevel = RemoveCurses(target);

                    if (toHealMod > 0 && curseLevel > 0)
                    {
                        toHealMod -= curseLevel * 3;
                        toHealMod -= (int)(toHealMod * (curseLevel / 100.0));
                    }

                    if (toHealMod > 0)
                    {
                        SpellHelper.Heal(toHealMod, target, Caster);
                    }
                }
            }

            FinishSequence();
        }

        public static int RemoveCurses(Mobile m)
        {
            var curseLevel = 0;

            // if (SleepSpell.EndSleep(m))
            // {
            //     curseLevel += 2;
            // }

            if (EvilOmenSpell.EndEffect(m))
            {
                curseLevel += 1;
            }

            if (StrangleSpell.RemoveCurse(m))
            {
                curseLevel += 2;
            }

            if (CorpseSkinSpell.RemoveCurse(m))
            {
                curseLevel += 3;
            }

            if (CurseSpell.RemoveEffect(m))
            {
                curseLevel += 4;
            }

            if (BloodOathSpell.RemoveCurse(m))
            {
                curseLevel += 3;
            }

            if (MindRotSpell.ClearMindRotScalar(m))
            {
                curseLevel += 2;
            }

            if (SpellPlagueSpell.RemoveEffect(m))
            {
                curseLevel += 4;
            }

            var mod = m.GetStatMod("[Magic] Str Curse");
            if (mod?.Offset < 0)
            {
                m.RemoveStatMod("[Magic] Str Curse");
            }

            mod = m.GetStatMod("[Magic] Dex Curse");
            if (mod?.Offset < 0)
            {
                m.RemoveStatMod("[Magic] Dex Curse");
            }

            mod = m.GetStatMod("[Magic] Int Curse");
            if (mod?.Offset < 0)
            {
                m.RemoveStatMod("[Magic] Int Curse");
            }

            if (MortalStrike.EndWound(m))
            {
                curseLevel += 2;
            }

            BuffInfo.RemoveBuff(m, BuffIcon.Clumsy);
            BuffInfo.RemoveBuff(m, BuffIcon.FeebleMind);
            BuffInfo.RemoveBuff(m, BuffIcon.Weaken);
            BuffInfo.RemoveBuff(m, BuffIcon.Curse);
            BuffInfo.RemoveBuff(m, BuffIcon.MassCurse);
            BuffInfo.RemoveBuff(m, BuffIcon.MortalStrike);
            BuffInfo.RemoveBuff(m, BuffIcon.CorpseSkin);
            BuffInfo.RemoveBuff(m, BuffIcon.Strangle);
            BuffInfo.RemoveBuff(m, BuffIcon.EvilOmen);

            return curseLevel;
        }
    }
}
