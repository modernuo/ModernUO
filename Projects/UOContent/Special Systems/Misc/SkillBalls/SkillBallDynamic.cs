using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;


namespace Server.Special_Systems.Misc.SkillBalls
{
    public class SkillBallDynamic : Item
    {
        private SkillListEntry m_Skill { get; set; }

        [Constructible]
        public SkillBallDynamic() : base(0xE73)
        {
            m_Skill = GetRandomItem();
            Weight = 1.0;
            Hue = GetHue();
            Name = GetName();
            Movable = true;
            Stackable = true;
        }

       /* [CommandProperty(AccessLevel.GameMaster)]
        public SkillName SkillName
        {
            get => m_Skill.SkillName;
            set => m_Skill = SkillList.Find(x => x.SkillName == value);
        }*/


        public enum SkillBallType
        {
            Misc,
            Fighter,
            Crafting,
            Unlimited
        }

        public class SkillListEntry
        {
            public SkillName SkillName;
            public SkillBallType Type;
            public int Weight;
        }

        public static List<SkillListEntry> SkillList = new List<SkillListEntry>()
        {
            new SkillListEntry() { SkillName = SkillName.ItemID, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Begging, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Camping, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Cartography, Type = SkillBallType.Misc, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Cooking, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.DetectHidden, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Forensics, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Herding, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Snooping, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Musicianship, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.SpiritSpeak, Type = SkillBallType.Misc, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.TasteID, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Tracking, Type = SkillBallType.Misc, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Veterinary, Type = SkillBallType.Misc, Weight = 70 },
            new SkillListEntry() { SkillName = SkillName.Meditation, Type = SkillBallType.Misc, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Stealth, Type = SkillBallType.Misc, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.RemoveTrap, Type = SkillBallType.Misc, Weight = 70 },
            new SkillListEntry() { SkillName = SkillName.Anatomy, Type = SkillBallType.Fighter, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.ArmsLore, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Parry, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Peacemaking, Type = SkillBallType.Fighter, Weight = 80 },
            new SkillListEntry() { SkillName = SkillName.Discordance, Type = SkillBallType.Fighter, Weight = 80 },
            new SkillListEntry() { SkillName = SkillName.EvalInt, Type = SkillBallType.Fighter, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Healing, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Hiding, Type = SkillBallType.Fighter, Weight = 100 },
            new SkillListEntry() { SkillName = SkillName.Magery, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Tactics, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Archery, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Swords, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Macing, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Fencing, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Wrestling, Type = SkillBallType.Fighter, Weight = 90 },
            new SkillListEntry() { SkillName = SkillName.Alchemy, Type = SkillBallType.Crafting, Weight = 50 },
            new SkillListEntry() { SkillName = SkillName.Blacksmith, Type = SkillBallType.Crafting, Weight = 50 },
            new SkillListEntry() { SkillName = SkillName.Fletching, Type = SkillBallType.Crafting, Weight = 50 },
            new SkillListEntry() { SkillName = SkillName.Carpentry, Type = SkillBallType.Crafting, Weight = 70 },
            new SkillListEntry() { SkillName = SkillName.Inscribe, Type = SkillBallType.Crafting, Weight = 60 },
            new SkillListEntry() { SkillName = SkillName.Poisoning, Type = SkillBallType.Crafting, Weight = 40 },
            new SkillListEntry() { SkillName = SkillName.Tailoring, Type = SkillBallType.Crafting, Weight = 50 },
            new SkillListEntry() { SkillName = SkillName.Tinkering, Type = SkillBallType.Crafting, Weight = 50 },
            new SkillListEntry() { SkillName = SkillName.AnimalLore, Type = SkillBallType.Unlimited, Weight = 50 },
            new SkillListEntry() { SkillName = SkillName.Fishing, Type = SkillBallType.Unlimited, Weight = 60 },
            new SkillListEntry() { SkillName = SkillName.Provocation, Type = SkillBallType.Unlimited, Weight = 70 },
            new SkillListEntry() { SkillName = SkillName.Lockpicking, Type = SkillBallType.Unlimited, Weight = 60 },
            new SkillListEntry() { SkillName = SkillName.MagicResist, Type = SkillBallType.Unlimited, Weight = 50 },
            new SkillListEntry() { SkillName = SkillName.Stealing, Type = SkillBallType.Unlimited, Weight = 50 },
            new SkillListEntry() { SkillName = SkillName.AnimalTaming, Type = SkillBallType.Unlimited, Weight = 30 },
            new SkillListEntry() { SkillName = SkillName.Lumberjacking, Type = SkillBallType.Unlimited, Weight = 50 },
            new SkillListEntry() { SkillName = SkillName.Mining, Type = SkillBallType.Unlimited, Weight = 50 },
        };


        public SkillListEntry GetRandomItem()
        {
            var items = SkillList;

            var totalWeight = items.Sum(x => x.Weight);
            var randomWeightedIndex = Utility.RandomDouble() * totalWeight;
            var itemWeightedIndex = 0;
            foreach (var item in items)
            {
                itemWeightedIndex += item.Weight;
                if (randomWeightedIndex < itemWeightedIndex)
                    return item;
            }

            throw new ArgumentException("Collection count and weights must be greater than 0");
        }

        public int GetHue()
        {
            switch (@m_Skill.Type)
            {
                case SkillBallType.Misc:
                    return 2525;
                    break;
                case SkillBallType.Fighter:
                    return 1266;
                    break;
                case SkillBallType.Crafting:
                    return 5;
                    break;
                case SkillBallType.Unlimited:
                    return 1161;
                    break;
            }

            return 0;
        }

        public string GetName()
        {
            if (Amount > 1)
            {
                return $"Skill Balls of {Enum.GetName(typeof(SkillName), m_Skill.SkillName)}";
            }

            return $"Skill Ball of {Enum.GetName(typeof(SkillName), m_Skill.SkillName)}";
        }

        public override void OnSingleClick(Mobile @from)
        {
            Name = GetName();
            base.OnSingleClick(@from);
        }


        public override void OnDoubleClick(Mobile m)
        {
            if (IsChildOf(m.Backpack))
            {
                if (!m.HasGump<SkillBallGump>())
                {
                    m.SendGump(new SkillBallGump(m, this));
                }
            }
            else
                m.SendLocalizedMessage(1042001);
        }

        public void ApplySkilltoPlayer(PlayerMobile pm)
        {
            Skill skill = pm.Skills[m_Skill.SkillName];

            if (skill == null)
            {
                return;
            }

            double count = pm.Skills.Total / 10;
            double cap = pm.SkillsCap / 10;
            double decreaseamount;
            int bonus = 1;

            List<Skill> decreased = GetDecreasableSkills(
                pm,
                count,
                cap,
                out decreaseamount
            );

            if (decreased.Count > 0 && decreaseamount <= 0)
            {
                pm.SendMessage("You have exceeded the skill cap and do not have a skill set to be decreased.");
            }
            else if ((skill.Base + bonus) > skill.Cap)
            {
                pm.SendMessage("Your skill is too high to raise it further.");
            }
            else if (skill.Lock != SkillLock.Up)
            {
                pm.SendMessage("You must set the skill to be increased in order to raise it further.");
            }
            else
            {
                if ((cap - count + decreaseamount) >= bonus)
                {
                    pm.SendMessage(54, "Your " + skill.SkillName + " has increased by " + 1 + ".");
                    DecreaseSkills(pm, decreased, count, cap, decreaseamount);
                    IncreaseSkill(pm, skill);

                    Consume();
                }
                else
                {
                    pm.SendMessage(
                        "You have exceeded the skill cap and do not have enough skill set to be decreased."
                    );
                }
            }
        }

        public virtual List<Skill> GetDecreasableSkills(
            Mobile from, double count, double cap,
            out double decreaseamount
        )
        {
            Skills skills = from.Skills;
            decreaseamount = 0.0;

            var decreased = new List<Skill>();
            double bonus = 1;

            if ((count + bonus) > cap)
            {
                foreach (Skill t in skills)
                {
                    if (t.Lock == SkillLock.Down && t.Base > 0.0)
                    {
                        decreased.Add(t);
                        decreaseamount += t.Base;
                    }
                }
            }

            return decreased;
        }

        public virtual void DecreaseSkills(
            Mobile from, List<Skill> decreased, double count, double cap,
            double decreaseamount
        )
        {
            double freepool = cap - count;
            double bonus = 1;

            if (freepool < bonus)
            {
                bonus -= freepool;

                foreach (Skill s in decreased)
                {
                    if (s.Base >= bonus)
                    {
                        s.Base -= bonus;
                        bonus = 0;
                    }
                    else
                    {
                        bonus -= s.Base;
                        s.Base = 0;
                    }

                    if (bonus == 0)
                    {
                        break;
                    }
                }
            }
        }

        public virtual void IncreaseSkill(Mobile from, Skill skill)
        {
            skill.Base += 1;
        }

        public SkillBallDynamic(Serial serial) : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
            writer.Write((int)m_Skill.SkillName);
            writer.Write((int)m_Skill.Type);
            writer.Write(m_Skill.Weight);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            m_Skill = new SkillListEntry()
            {
                SkillName = (SkillName)reader.ReadInt(),
                Type = (SkillBallType)reader.ReadInt(),
                Weight = reader.ReadInt()
            };
        }
    }


    public class SkillBallGump : Gump
    {
        public Mobile User { get; }
        public SkillBallDynamic SkillBallDyn { get; }

        public SkillBallGump(Mobile user, SkillBallDynamic sbd)
            : base(0, 0)
        {
            User = user;
            SkillBallDyn = sbd;

            Draggable = true;
            Closable = true;
            Resizable = false;
            Disposable = false;

            AddPage(0);
            AddBackground(221, 76, 508, 196, 30536);
            AddBackground(252, 107, 446, 136, 9270);
            AddImage(183, 43, 10400);
            AddImage(183, 264, 10402);
            AddLabel(288, 120, 1160, "Confirmation");
            AddButton(591, 195, 4014, 4015, (int)Buttons.AcceptButton, GumpButtonType.Reply, 0);
            AddLabel(630, 195, 1150, "Accept");
            AddButton(286, 195, 4014, 4015, (int)Buttons.CloseButton, GumpButtonType.Reply, 0);
            AddLabel(327, 196, 1150, "Close");
            AddLabel(291, 144, 1150, "Are you sure you want to train this skill?");
            AddLabel(291, 164, 1150, "This cannot be undone!");
        }

        public enum Buttons
        {
            AcceptButton = 1,
            CloseButton = 2,
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            switch (@info.ButtonID)
            {
                case 1:
                    SkillBallDyn.ApplySkilltoPlayer(User as PlayerMobile);
                    break;
                case 2:
                    User.CloseGump<SkillBallGump>();
                    break;
            }
        }

        public override void OnServerClose(NetState owner)
        {
        }
    }
}
