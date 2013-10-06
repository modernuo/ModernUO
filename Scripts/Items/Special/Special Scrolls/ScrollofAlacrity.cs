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
	public class ScrollofAlacrity : SpecialScroll
	{
		public override int LabelNumber { get { return 1078604; } } // Scroll of Alacrity
		
		public override int Message { get { return 1078602; } } /*Using a Scroll of Transcendence for a given skill will permanently increase your current 
																*level in that skill by the amount of points displayed on the scroll.
																*As you may not gain skills beyond your maximum skill cap, any excess points will be lost.*/
		
		public override string DefaultTitle { get { return String.Format( "<basefont color=#FFFFFF>Scroll of Alacrity:</basefont>" ); } }

		public ScrollofAlacrity() : this( SkillName.Alchemy )
		{
		}
		
		[Constructable]
		public ScrollofAlacrity( SkillName skill ) : base( skill, 0.0 )
		{
			ItemID = 0x14EF;
			Hue = 0x4AB;
		}

		public ScrollofAlacrity(Serial serial) : base(serial)
		{
		}

		public override void GetProperties(ObjectPropertyList list)
		{
			base.GetProperties(list);

			list.Add(1071345, "{0} 15 Minutes", GetName()); // Skill: ~1_val~
		}
		
		public override bool CanUse( Mobile from )
		{
			if ( !base.CanUse( from ) )
				return false;
			
			PlayerMobile pm = from as PlayerMobile;
			
			if ( pm == null )
				return false;
			
			#region Mondain's Legacy
			/* to add when skillgain quests will be implemented
			
			for (int i = pm.Quests.Count - 1; i >= 0; i--)
			{
				BaseQuest quest = pm.Quests[i];

				for (int j = quest.Objectives.Count - 1; j >= 0; j--)
				{
					BaseObjective objective = quest.Objectives[j];

					if (objective is ApprenticeObjective)
					{
						from.SendMessage("You are already under the effect of an enhanced skillgain quest.");
						return false;
					}
				}
			}
			
			*/
			#endregion
			
			#region Scroll of Alacrity
			if (pm.AcceleratedStart > DateTime.UtcNow)
			{
				from.SendLocalizedMessage(1077951); // You are already under the effect of an accelerated skillgain scroll.
				return false;
			}
			#endregion
			
			return true;
		}

		public override void Use( Mobile from )
		{
			if ( !CanUse( from ) )
				return;
			
			PlayerMobile pm = from as PlayerMobile;
			
			if ( pm == null )
				return;
			
			double tskill = from.Skills[Skill].Base;
			double tcap = from.Skills[Skill].Cap;

			if ( tskill >= tcap || from.Skills[Skill].Lock != SkillLock.Up )
			{
				from.SendLocalizedMessage( 1094935 );	/*You cannot increase this skill at this time. The skill may be locked or set to lower in your skill menu.
														*If you are at your total skill cap, you must use a Powerscroll to increase your current skill cap.*/
				return;
			}
			
			from.SendLocalizedMessage( 1077956 ); // You are infused with intense energy. You are under the effects of an accelerated skillgain scroll.
			
			Effects.PlaySound( from.Location, from.Map, 0x1E9 );
			Effects.SendTargetParticles( from, 0x373A, 35, 45, 0x00, 0x00, 9502, (EffectLayer)255, 0x100 );

			pm.AcceleratedStart = DateTime.UtcNow + TimeSpan.FromMinutes(15);

			Timer t = (Timer)m_Table[from];

			m_Table[from] = Timer.DelayCall( TimeSpan.FromMinutes( 15 ), new TimerStateCallback( Expire_Callback ), from );

			pm.AcceleratedSkill = Skill;

			Delete();
		}

		private static Hashtable m_Table = new Hashtable();

		private static void Expire_Callback(object state)
		{
			Mobile m = (Mobile)state;

			m_Table.Remove(m);

			m.PlaySound(0x1F8);

			m.SendLocalizedMessage(1077957);// The intense energy dissipates. You are no longer under the effects of an accelerated skillgain scroll.
		}

		public override void Serialize(GenericWriter writer)
		{
			base.Serialize(writer);

			writer.Write((int)0); // version
		}

		public override void Deserialize(GenericReader reader)
		{
			base.Deserialize(reader);

			int version = ( InheritsItem ? 0 : reader.ReadInt() ); //Required for SpecialScroll insertion

			LootType = LootType.Cursed;
			Insured = false;
		}
	}
}
