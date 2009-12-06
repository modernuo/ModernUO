using System;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using Server.Engines.Quests;

namespace Server.Items
{
	public class ScrollofTranscendence : Item
	{
		public override int LabelNumber { get { return 1094934; } } // Scroll of Transcendence

		private SkillName m_Skill;
		private double m_Value;

		private static SkillName[] m_Skills = new SkillName[]
			{
				SkillName.Alchemy,
				SkillName.Anatomy,
				SkillName.AnimalLore,
				SkillName.ItemID,
				SkillName.ArmsLore,
				SkillName.Parry,
				SkillName.Begging,
				SkillName.Blacksmith,
				SkillName.Fletching,
				SkillName.Peacemaking,
				SkillName.Camping,
				SkillName.Carpentry,
				SkillName.Cartography,
				SkillName.Cooking,
				SkillName.DetectHidden,
				SkillName.Discordance,
				SkillName.EvalInt,
				SkillName.Healing,
				SkillName.Fishing,
				SkillName.Forensics,
				SkillName.Herding,
				SkillName.Hiding,
				SkillName.Provocation,
				SkillName.Inscribe,
				SkillName.Lockpicking,
				SkillName.Magery,
				SkillName.MagicResist,
				SkillName.Tactics,
				SkillName.Snooping,
				SkillName.Musicianship,
				SkillName.Poisoning,
				SkillName.Archery,
				SkillName.SpiritSpeak,
				SkillName.Stealing,
				SkillName.Tailoring,
				SkillName.AnimalTaming,
				SkillName.TasteID,
				SkillName.Tinkering,
				SkillName.Tracking,
				SkillName.Veterinary,
				SkillName.Swords,
				SkillName.Macing,
				SkillName.Fencing,
				SkillName.Wrestling,
				SkillName.Lumberjacking,
				SkillName.Mining,
				SkillName.Meditation,
				SkillName.Stealth,
				SkillName.RemoveTrap,
				SkillName.Necromancy,
				SkillName.Focus,
				SkillName.Chivalry,
				SkillName.Bushido,
				SkillName.Ninjitsu,
				SkillName.Spellweaving
			};

		public static SkillName[] Skills { get { return (m_Skills); } }

		public static ScrollofTranscendence CreateRandom(int min, int max)
		{
			min /= 1;
			max /= 1;

			SkillName[] skills = ScrollofTranscendence.Skills;

			return new ScrollofTranscendence(skills[Utility.Random(skills.Length)], Utility.RandomMinMax(min, max) * 0.1);
		}

		[Constructable]
		public ScrollofTranscendence(SkillName skill, double value)
			: base(0x14EF)
		{
			Hue = 0x490;
			Weight = 1.0;

			m_Skill = skill;
			m_Value = value;
			if (m_Value > 0.0)
				LootType = LootType.Cursed;
		}

		public ScrollofTranscendence(Serial serial)
			: base(serial)
		{
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public SkillName Skill
		{
			get
			{
				return m_Skill;
			}
			set
			{
				m_Skill = value;
			}
		}

		[CommandProperty(AccessLevel.GameMaster)]
		public double Value
		{
			get
			{
				return m_Value;
			}
			set
			{
				m_Value = value;
			}
		}

		private string GetNameLocalized()
		{
			return String.Concat("#", (1044060 + (int)m_Skill).ToString());
		}

		private string GetName()
		{
			int index = (int)m_Skill;
			SkillInfo[] table = SkillInfo.Table;

			if (index >= 0 && index < table.Length)
				return table[index].Name.ToLower();
			else
				return "???";
		}

		public override void GetProperties(ObjectPropertyList list)
		{
			base.GetProperties(list);

			if (m_Value == 1)
				list.Add(1076759, "{0}\t{1}.0 Skill Points", GetName(), m_Value);
			else
				list.Add(1076759, "{0}\t{1} Skill Points", GetName(), m_Value);
		}

		public void Use(Mobile from, bool firstStage)
		{
			PlayerMobile pm = from as PlayerMobile;

			if (Deleted)
				return;

			/* to uncomment when skillgain quests will be implementes
			
			#region Mondain's Legacy
			for (int i = pm.Quests.Count - 1; i >= 0; i--)
			{
				BaseQuest quest = pm.Quests[i];

				for (int j = quest.Objectives.Count - 1; j >= 0; j--)
				{
					BaseObjective objective = quest.Objectives[j];

					if (objective is ApprenticeObjective)
					{
						from.SendMessage("You are already under the effect of an enhanced skillgain quest.");
						return;
					}
				}
			}
			#endregion
			*/

			if (!IsChildOf(from.Backpack))
			{
				from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
			}
			#region Scroll of Alacrity
			else if (pm.AcceleratedStart > DateTime.Now)
			{
				from.SendLocalizedMessage(1077951); // You are already under the effect of an accelerated skillgain scroll.
				return;
			}
			#endregion
			else
			{
				if (firstStage)
				{
					from.CloseGump(typeof(StatCapScroll.InternalGump));
					from.CloseGump(typeof(PowerScroll.InternalGump));
					from.CloseGump(typeof(ScrollofTranscendence.InternalGump));
					from.CloseGump(typeof(ScrollofAlacrity.InternalGump));
					from.SendGump(new InternalGump(from, this));
				}
				else
				{
					double tskill = from.Skills[m_Skill].Base;
					double tcap = from.Skills[m_Skill].Cap;
					bool canGain = true;

					if (tskill >= tcap || from.Skills[m_Skill].Lock == SkillLock.Locked || from.Skills[m_Skill].Lock == SkillLock.Down)
					{
						from.SendLocalizedMessage(1094935); /*You cannot increase this skill at this time. The skill may be locked or set to lower in your skill menu.
															*If you are at your total skill cap, you must use a Powerscroll to increase your current skill cap.*/
						return;
					}

					if ((tskill + m_Value) > tcap)
						m_Value = tcap - tskill;

					if ((from.SkillsTotal + m_Value * 10) > from.SkillsCap)
					{
						canGain = false;
						for (int i = 0; i <= 54; i++)
						{
							if (from.Skills[i].Lock == SkillLock.Down && from.Skills[i].Base >= m_Value)
							{
								from.Skills[i].Base = from.Skills[i].Base - m_Value;
								canGain = true;
								break;
							}
						}

						if (!canGain)
						{
							from.SendLocalizedMessage(1094935); /*You cannot increase this skill at this time. The skill may be locked or set to lower in your skill menu.
																*If you are at your total skill cap, you must use a Powerscroll to increase your current skill cap.*/
							return;
						}
					}

					if (tskill + m_Value > tcap)
					{
						from.Skills[m_Skill].Base = tcap;
					}
					else
					{
						from.Skills[m_Skill].Base = tskill + m_Value;
					}

					from.SendLocalizedMessage(1049513, GetNameLocalized()); // You feel a surge of magic as the scroll enhances your ~1_type~!

					Effects.PlaySound(from.Location, from.Map, 0x1F7);

					Effects.SendTargetParticles(from, 0x373A, 35, 45, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);
					Effects.SendTargetParticles(from, 0x376A, 35, 45, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

					Delete();
				}
			}
		}

		public override void OnDoubleClick(Mobile from)
		{
			Use(from, true);
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int)0); // version

			writer.Write((int)m_Skill);
			writer.Write((double)m_Value);
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = reader.ReadInt();

			switch (version)
			{
				case 0:
					{
						m_Skill = (SkillName)reader.ReadInt();
						m_Value = reader.ReadDouble();
						break;
					}
			}

			if (m_Value >= 0.1)
			{
				LootType = LootType.Cursed;

				if (Insured)
					Insured = false;
			}

			if (Hue == 0x7E)
				Hue = 0x490;
		}

		public class InternalGump : Gump
		{
			private Mobile m_Mobile;
			private ScrollofTranscendence m_Scroll;

			public InternalGump(Mobile mobile, ScrollofTranscendence scroll)
				: base(25, 50)
			{
				m_Mobile = mobile;
				m_Scroll = scroll;

				AddPage(0);

				AddBackground(25, 10, 420, 200, 5054);

				AddImageTiled(33, 20, 401, 181, 2624);
				AddAlphaRegion(33, 20, 401, 181);

				AddHtmlLocalized(40, 48, 387, 100, 1094933, true, true); /*Using a Scroll of Transcendence for a given skill will permanently increase your current 
																		*level in that skill by the amount of points displayed on the scroll.
																		*As you may not gain skills beyond your maximum skill cap, any excess points will be lost.*/

				AddHtmlLocalized(125, 148, 200, 20, 1049478, 0xFFFFFF, false, false); // Do you wish to use this scroll?

				AddButton(100, 172, 4005, 4007, 1, GumpButtonType.Reply, 0);
				AddHtmlLocalized(135, 172, 120, 20, 1046362, 0xFFFFFF, false, false); // Yes

				AddButton(275, 172, 4005, 4007, 0, GumpButtonType.Reply, 0);
				AddHtmlLocalized(310, 172, 120, 20, 1046363, 0xFFFFFF, false, false); // No

				double value = scroll.m_Value;

				AddHtml(40, 20, 260, 20, String.Format("<basefont color=#FFFFFF>Scroll of Transcendence ({0} Skill):</basefont>", value), false, false);

				AddHtmlLocalized(310, 20, 120, 20, 1044060 + (int)scroll.m_Skill, 0xFFFFFF, false, false);
			}

			public override void OnResponse(NetState state, RelayInfo info)
			{
				if (info.ButtonID == 1)
					m_Scroll.Use(m_Mobile, false);
			}
		}
	}
}
