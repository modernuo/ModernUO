using System;
using Server.Items;

namespace Server.Engines.Craft
{
	public class DefBowFletching : CraftSystem
	{
		public override SkillName MainSkill
		{
			get	{ return SkillName.Fletching; }
		}

		public override int GumpTitleNumber
		{
			get { return 1044006; } // <CENTER>BOWCRAFT AND FLETCHING MENU</CENTER>
		}

		private static CraftSystem m_CraftSystem;

		public static CraftSystem CraftSystem
		{
			get
			{
				if ( m_CraftSystem == null )
					m_CraftSystem = new DefBowFletching();

				return m_CraftSystem;
			}
		}

		public override double GetChanceAtMin( CraftItem item )
		{
			return 0.5; // 50%
		}

		private DefBowFletching() : base( 1, 1, 1.25 )// base( 1, 2, 1.7 )
		{
		}

		public override int CanCraft( Mobile from, BaseTool tool, Type itemType )
		{
			if( tool == null || tool.Deleted || tool.UsesRemaining < 0 )
				return 1044038; // You have worn out your tool!
			else if ( !BaseTool.CheckAccessible( tool, from ) )
				return 1044263; // The tool must be on your person to use.

			return 0;
		}

		public override void PlayCraftEffect( Mobile from )
		{
			// no animation
			//if ( from.Body.Type == BodyType.Human && !from.Mounted )
			//	from.Animate( 33, 5, 1, true, false, 0 );

			from.PlaySound( 0x55 );
		}

		public override int PlayEndingEffect( Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item )
		{
			if ( toolBroken )
				from.SendLocalizedMessage( 1044038 ); // You have worn out your tool

			if ( failed )
			{
				if ( lostMaterial )
					return 1044043; // You failed to create the item, and some of your materials are lost.
				else
					return 1044157; // You failed to create the item, but no materials were lost.
			}
			else
			{
				if ( quality == 0 )
					return 502785; // You were barely able to make this item.  It's quality is below average.
				else if ( makersMark && quality == 2 )
					return 1044156; // You create an exceptional quality item and affix your maker's mark.
				else if ( quality == 2 )
					return 1044155; // You create an exceptional quality item.
				else				
					return 1044154; // You create the item.
			}
		}

		public override CraftECA ECA{ get{ return CraftECA.FiftyPercentChanceMinusTenPercent; } }

		public override void InitCraftList()
		{
			int index = -1;

			// Materials
			AddCraft( typeof( Kindling ), 1044457, 1023553, 0.0, 00.0, typeof( Log ), 1044041, 1, 1044351 );

			index = AddCraft( typeof( Shaft ), 1044457, 1027124, 0.0, 40.0, typeof( Log ), 1044041, 1, 1044351 );
			SetUseAllRes( index, true );

			// Ammunition
			index = AddCraft( typeof( Arrow ), 1044565, 1023903, 0.0, 40.0, typeof( Shaft ), 1044560, 1, 1044561 );
			AddRes( index, typeof( Feather ), 1044562, 1, 1044563 );
			SetUseAllRes( index, true );

			index = AddCraft( typeof( Bolt ), 1044565, 1027163, 0.0, 40.0, typeof( Shaft ), 1044560, 1, 1044561 );
			AddRes( index, typeof( Feather ), 1044562, 1, 1044563 );
			SetUseAllRes( index, true );

			if( Core.SE )
			{
				index = AddCraft( typeof( FukiyaDarts ), 1044565, 1030246, 50.0, 90.0, typeof( Log ), 1044041, 1, 1044351 );
				SetUseAllRes( index, true );
				SetNeededExpansion( index, Expansion.SE );
			}

			// Weapons
			AddCraft( typeof( Bow ), 1044566, 1025042, 30.0, 70.0, typeof( Log ), 1044041, 7, 1044351 );
			AddCraft( typeof( Crossbow ), 1044566, 1023919, 60.0, 100.0, typeof( Log ), 1044041, 7, 1044351 );
			AddCraft( typeof( HeavyCrossbow ), 1044566, 1025117, 80.0, 120.0, typeof( Log ), 1044041, 10, 1044351 );

			if ( Core.AOS )
			{
				AddCraft( typeof( CompositeBow ), 1044566, 1029922, 70.0, 110.0, typeof( Log ), 1044041, 7, 1044351 );
				AddCraft( typeof( RepeatingCrossbow ), 1044566, 1029923, 90.0, 130.0, typeof( Log ), 1044041, 10, 1044351 );
			}

			if( Core.SE )
			{
				index = AddCraft( typeof( Yumi ), 1044566, 1030224, 90.0, 130.0, typeof( Log ), 1044041, 10, 1044351 );
				SetNeededExpansion( index, Expansion.SE );
			}

			MarkOption = true;
			Repair = Core.AOS;
		}
	}
}