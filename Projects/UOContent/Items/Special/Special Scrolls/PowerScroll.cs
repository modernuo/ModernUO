using System.Collections.Generic;

namespace Server.Items
{
    public class PowerScroll : SpecialScroll
    {
        private static readonly SkillName[] m_Skills =
        {
            SkillName.Blacksmith,
            SkillName.Tailoring,
            SkillName.Swords,
            SkillName.Fencing,
            SkillName.Macing,
            SkillName.Archery,
            SkillName.Wrestling,
            SkillName.Parry,
            SkillName.Tactics,
            SkillName.Anatomy,
            SkillName.Healing,
            SkillName.Magery,
            SkillName.Meditation,
            SkillName.EvalInt,
            SkillName.MagicResist,
            SkillName.AnimalTaming,
            SkillName.AnimalLore,
            SkillName.Veterinary,
            SkillName.Musicianship,
            SkillName.Provocation,
            SkillName.Discordance,
            SkillName.Peacemaking
        };

        private static readonly SkillName[] m_AOSSkills =
        {
            SkillName.Chivalry,
            SkillName.Focus,
            SkillName.Necromancy,
            SkillName.Stealing,
            SkillName.Stealth,
            SkillName.SpiritSpeak
        };

        private static readonly SkillName[] m_SESkills =
        {
            SkillName.Ninjitsu,
            SkillName.Bushido
        };

        private static readonly SkillName[] m_MLSkills =
        {
            SkillName.Spellweaving
        };

        /*
        private static SkillName[] m_SASkills = new SkillName[]
          {
            SkillName.Throwing,
            SkillName.Mysticism,
            SkillName.Imbuing
          };

        private static SkillName[] m_HSSkills = new SkillName[]
          {
            SkillName.Fishing
          };
        */

        private static readonly List<SkillName> _Skills = new();

        [Constructible]
        public PowerScroll(SkillName skill = SkillName.Alchemy, double value = 0.0) : base(skill, value)
        {
            Hue = 0x481;

            if (Value == 105.0 || skill is SkillName.Blacksmith or SkillName.Tailoring)
            {
                LootType = LootType.Regular;
            }
        }

        public PowerScroll(Serial serial) : base(serial)
        {
        }

        /* Using a scroll increases the maximum amount of a specific skill or your maximum statistics.
         * When used, the effect is not immediately seen without a gain of points with that skill or statistics.
         * You can view your maximum skill values in your skills window.
         * You can view your maximum statistic value in your statistics window.
         */
        public override int Message =>
            1049469;

        public override int Title
        {
            get
            {
                var level = (Value - 105.0) / 5.0;

                /* Wonderous Scroll (105 Skill): OR
                 * Exalted Scroll (110 Skill): OR
                 * Mythical Scroll (115 Skill): OR
                 * Legendary Scroll (120 Skill):
                 */
                if (level >= 0.0 && level <= 3.0 && Value % 5.0 == 0.0)
                {
                    return 1049635 + (int)level;
                }

                return 0;
            }
        }

        public override string DefaultTitle => $"<basefont color=#FFFFFF>Power Scroll ({Value} Skill):</basefont>";

        public static List<SkillName> Skills
        {
            get
            {
                if (_Skills.Count == 0)
                {
                    _Skills.AddRange(m_Skills);
                    if (Core.AOS)
                    {
                        _Skills.AddRange(m_AOSSkills);
                        if (Core.SE)
                        {
                            _Skills.AddRange(m_SESkills);
                            if (Core.ML)
                            {
                                _Skills.AddRange(m_MLSkills);
                            }
                        }
                    }
                }

                return _Skills;
            }
        }

        public static PowerScroll CreateRandom(int min, int max)
        {
            min /= 5;
            max /= 5;

            return new PowerScroll(Skills.RandomElement(), 100 + Utility.RandomMinMax(min, max) * 5);
        }

        public static PowerScroll CreateRandomNoCraft(int min, int max)
        {
            min /= 5;
            max /= 5;

            SkillName skillName;

            do
            {
                skillName = Skills.RandomElement();
            } while (skillName is SkillName.Blacksmith or SkillName.Tailoring);

            return new PowerScroll(skillName, 100 + Utility.RandomMinMax(min, max) * 5);
        }

        public override void AddNameProperty(IPropertyList list)
        {
            var level = (Value - 105.0) / 5.0;

            if (level >= 0.0 && level <= 3.0 && Value % 5.0 == 0.0)
                /* a wonderous scroll of ~1_type~ (105 Skill) OR
                 * an exalted scroll of ~1_type~ (110 Skill) OR
                 * a mythical scroll of ~1_type~ (115 Skill) OR
                 * a legendary scroll of ~1_type~ (120 Skill)
                 */
            {
                list.Add(1049639 + (int)level, GetNameLocalized());
            }
            else
            {
                list.Add($"a power scroll of {GetName()} ({Value} Skill)");
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            var level = (Value - 105.0) / 5.0;

            if (level >= 0.0 && level <= 3.0 && Value % 5.0 == 0.0)
            {
                LabelTo(from, 1049639 + (int)level, GetNameLocalized());
            }
            else
            {
                LabelTo(from, $"a power scroll of {GetName()} ({Value} Skill)");
            }
        }

        public override bool CanUse(Mobile from)
        {
            if (!base.CanUse(from))
            {
                return false;
            }

            var skill = from.Skills[Skill];

            if (skill == null)
            {
                return false;
            }

            if (skill.Cap >= Value)
            {
                from.SendLocalizedMessage(
                    1049511,
                    GetNameLocalized()
                ); // Your ~1_type~ is too high for this power scroll.
                return false;
            }

            return true;
        }

        public override void Use(Mobile from)
        {
            if (!CanUse(from))
            {
                return;
            }

            from.SendLocalizedMessage(
                1049513,
                GetNameLocalized()
            ); // You feel a surge of magic as the scroll enhances your ~1_type~!

            from.Skills[Skill].Cap = Value;

            Effects.SendLocationParticles(
                EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration),
                0,
                0,
                0,
                0,
                0,
                5060,
                0
            );
            Effects.PlaySound(from.Location, from.Map, 0x243);

            Effects.SendMovingParticles(
                new Entity(Serial.Zero, new Point3D(from.X - 6, from.Y - 6, from.Z + 15), from.Map),
                from,
                0x36D4,
                7,
                0,
                false,
                true,
                0x497,
                0,
                9502,
                1,
                0,
                (EffectLayer)255,
                0x100
            );
            Effects.SendMovingParticles(
                new Entity(Serial.Zero, new Point3D(from.X - 4, from.Y - 6, from.Z + 15), from.Map),
                from,
                0x36D4,
                7,
                0,
                false,
                true,
                0x497,
                0,
                9502,
                1,
                0,
                (EffectLayer)255,
                0x100
            );
            Effects.SendMovingParticles(
                new Entity(Serial.Zero, new Point3D(from.X - 6, from.Y - 4, from.Z + 15), from.Map),
                from,
                0x36D4,
                7,
                0,
                false,
                true,
                0x497,
                0,
                9502,
                1,
                0,
                (EffectLayer)255,
                0x100
            );

            Effects.SendTargetParticles(from, 0x375A, 35, 90, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

            Delete();
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = InheritsItem ? 0 : reader.ReadInt(); // Required for SpecialScroll insertion

            if (Value == 105.0 || Skill is SkillName.Blacksmith or SkillName.Tailoring)
            {
                LootType = LootType.Regular;
            }
            else
            {
                LootType = LootType.Cursed;
                Insured = false;
            }
        }
    }
}
