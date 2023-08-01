using System;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Text;

namespace Server.SkillHandlers
{
    public static class ForensicEvaluation
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Forensics].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new ForensicTarget();
            m.RevealingAction();

            m.SendLocalizedMessage(501000); // Show me the crime.

            return TimeSpan.FromSeconds(1.0);
        }

        public class ForensicTarget : Target
        {
            public ForensicTarget() : base(10, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Mobile)
                {
                    if (from.CheckTargetSkill(SkillName.Forensics, target, 40.0, 100.0))
                    {
                        if (target is PlayerMobile pm && pm.NpcGuild == NpcGuild.ThievesGuild)
                        {
                            from.SendLocalizedMessage(501004); // That individual is a thief!
                        }
                        else
                        {
                            from.SendLocalizedMessage(501003); // You notice nothing unusual.
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(501001); // You cannot determine anything useful.
                    }
                }
                else if (target is Corpse c)
                {
                    if (from.CheckTargetSkill(SkillName.Forensics, c, 0.0, 100.0))
                    {
                        if (c.Forensicist != null)
                        {
                            // The forensicist ~1_NAME~ has already discovered that:
                            from.SendLocalizedMessage(1042750, c.Forensicist);
                        }
                        else
                        {
                            c.Forensicist = from.Name;
                        }

                        if (((Body)c.Amount).IsHuman)
                        {
                            // This person was killed by ~1_KILLER_NAME~
                            from.SendLocalizedMessage(1042751, c.Killer == null ? "no one" : c.Killer.Name);
                        }

                        if (c.Looters.Count > 0)
                        {
                            var sb = ValueStringBuilder.Create(128);
                            int i = 0;
                            foreach (var looter in c.Looters)
                            {
                                if (i == c.Looters.Count - 1)
                                {
                                    sb.Append($", and {looter.Name}");
                                }
                                else if (i > 0)
                                {
                                    sb.Append($", {looter.Name}");
                                }
                                else
                                {
                                    sb.Append(looter.Name);
                                }

                                i++;
                            }

                            // This body has been disturbed by ~1_PLAYER_NAMES~
                            from.SendLocalizedMessage(1042752, sb.ToString());
                            sb.Dispose();
                        }
                        else
                        {
                            from.SendLocalizedMessage(501002); // The corpse has not be desecrated.
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(501001); // You cannot determine anything useful.
                    }
                }
                else if (target is ILockpickable p)
                {
                    if (p.Picker != null)
                    {
                        from.SendLocalizedMessage(1042749, p.Picker.Name); // This lock was opened by ~1_PICKER_NAME~
                    }
                    else
                    {
                        from.SendLocalizedMessage(501003); // You notice nothing unusual.
                    }
                }
            }
        }
    }
}
