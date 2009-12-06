/***************************************************************************
*							ScrollofAlacrity.cs
*							-------------------
*	begin				: June 1, 2009
*	copyright			: (C) Shai'Tan Malkier aka Callandor2k
*	email				: ShaiTanMalkier@gmail.com
*
*	$Id: ScrollofAlacrity.cs 1 2009-06-1 04:28:39Z Callandor2k $
*
***************************************************************************/

/***************************************************************************
*
*	This Script/File is free software; you can redistribute it and/or modify
*	it under the terms of the GNU General Public License as published by
*	the Free Software Foundation; either version 2 of the License, or
*	(at your option) any later version.
*
***************************************************************************/

using System;
using Server;
using Server.Gumps;
using Server.Network;
using Server.Mobiles;
using System.Collections;
using Server.Engines.Quests;
using System.Collections.Generic;

namespace Server.Items
{
	public class ScrollofAlacrity : Item
	{
		public override int LabelNumber { get { return 1078604; } } // Scroll of Alacrity

		private SkillName m_Skill;

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

		[Constructable]
		public ScrollofAlacrity(SkillName skill)
			: base(0x14EF)
		{
			base.Hue = 0x4AB;
			base.Weight = 1.0;

			LootType = LootType.Cursed;

			m_Skill = skill;
		}

		public ScrollofAlacrity(Serial serial)
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

			list.Add(1071345, "{0} 15 Minutes", GetName());// Skill: ~1_val~
		}

		public void Use(Mobile from, bool firstStage)
		{
			PlayerMobile pm = from as PlayerMobile;

			if (Deleted)
				return;

			/* to add when skillgain quests will be implemented
			
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

			else if (pm.AcceleratedStart > DateTime.Now)
			{
				from.SendLocalizedMessage(1077951); // You are already under the effect of an accelerated skillgain scroll.
				return;
			}

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

					if (tskill >= tcap || from.Skills[m_Skill].Lock == SkillLock.Locked || from.Skills[m_Skill].Lock == SkillLock.Down)
					{
						from.SendLocalizedMessage(1094935); /*You cannot increase this skill at this time. The skill may be locked or set to lower in your skill menu.
															*If you are at your total skill cap, you must use a Powerscroll to increase your current skill cap.*/
						return;
					}
					else
					{
						Effects.PlaySound(from.Location, from.Map, 0x1E9);

						Effects.SendTargetParticles(from, 0x373A, 35, 45, 0x00, 0x00, 9502, (EffectLayer)255, 0x100);

						from.SendLocalizedMessage(1077956); // You are infused with intense energy. You are under the effects of an accelerated skillgain scroll.

						pm.AcceleratedStart = DateTime.Now + TimeSpan.FromMinutes(15);

						Timer t = (Timer)m_Table[from];

						m_Table[from] = Timer.DelayCall(TimeSpan.FromMinutes(15), new TimerStateCallback(Expire_Callback), from);

						pm.AcceleratedSkill = m_Skill;

						Delete();
					}
				}
			}
		}

		private static Hashtable m_Table = new Hashtable();

		private static void Expire_Callback(object state)
		{
			Mobile m = (Mobile)state;

			m_Table.Remove(m);

			m.PlaySound(0x1F8);

			m.SendLocalizedMessage(1077957);// The intense energy dissipates. You are no longer under the effects of an accelerated skillgain scroll.
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
						break;
					}
			}

			LootType = LootType.Cursed;

			if (Insured)
				Insured = false;
		}

		public class InternalGump : Gump
		{
			private Mobile m_Mobile;
			private ScrollofAlacrity m_Scroll;

			public InternalGump(Mobile mobile, ScrollofAlacrity scroll)
				: base(25, 50)
			{
				m_Mobile = mobile;
				m_Scroll = scroll;

				AddPage(0);

				AddBackground(25, 10, 420, 200, 5054);

				AddImageTiled(33, 20, 401, 181, 2624);
				AddAlphaRegion(33, 20, 401, 181);

				AddHtmlLocalized(40, 48, 387, 100, 1078602, true, true); /* Using a Scroll of Alacrity for a given skill will increase the amount of skillgain
																		* you receive for that skill. Once the Scroll of Alacrity duration has expired,
																		* skillgain will return to normal for that skill. */

				AddHtmlLocalized(125, 148, 200, 20, 1049478, 0xFFFFFF, false, false); // Do you wish to use this scroll?

				AddButton(100, 172, 4005, 4007, 1, GumpButtonType.Reply, 0);
				AddHtmlLocalized(135, 172, 120, 20, 1046362, 0xFFFFFF, false, false); // Yes

				AddButton(275, 172, 4005, 4007, 0, GumpButtonType.Reply, 0);
				AddHtmlLocalized(310, 172, 120, 20, 1046363, 0xFFFFFF, false, false); // No

				AddHtml(40, 20, 260, 20, String.Format("<basefont color=#FFFFFF>Scroll of Alacrity:</basefont>"), false, false);

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
