using System;
using System.Collections;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Guilds;
using Server.Multis;
using Server.Mobiles;
using Server.Engines.PartySystem;
using Server.Factions;
using Server.Spells.Necromancy;
using Server.Spells.Ninjitsu;
using Server.Spells;

namespace Server.Misc
{
	public class NotorietyHandlers
	{
		public static void Initialize()
		{
			Notoriety.Hues[Notoriety.Innocent]		= 0x59;
			Notoriety.Hues[Notoriety.Ally]			= 0x3F;
			Notoriety.Hues[Notoriety.CanBeAttacked]	= 0x3B2;
			Notoriety.Hues[Notoriety.Criminal]		= 0x3B2;
			Notoriety.Hues[Notoriety.Enemy]			= 0x90;
			Notoriety.Hues[Notoriety.Murderer]		= 0x22;
			Notoriety.Hues[Notoriety.Invulnerable]	= 0x35;

			Notoriety.Handler = new NotorietyHandler( MobileNotoriety );

			Mobile.AllowBeneficialHandler = new AllowBeneficialHandler( Mobile_AllowBeneficial );
			Mobile.AllowHarmfulHandler = new AllowHarmfulHandler( Mobile_AllowHarmful );
		}

		private enum GuildStatus { None, Peaceful, Waring }

		private static GuildStatus GetGuildStatus( Mobile m )
		{
			if( m.Guild == null )
				return GuildStatus.None;
			else if( ((Guild)m.Guild).Enemies.Count == 0 && m.Guild.Type == GuildType.Regular )
				return GuildStatus.Peaceful;

			return GuildStatus.Waring;
		}

		private static bool CheckBeneficialStatus( GuildStatus from, GuildStatus target )
		{
			if( from == GuildStatus.Waring || target == GuildStatus.Waring )
				return false;

			return true;
		}

		/*private static bool CheckHarmfulStatus( GuildStatus from, GuildStatus target )
		{
			if ( from == GuildStatus.Waring && target == GuildStatus.Waring )
				return true;

			return false;
		}*/

		public static bool Mobile_AllowBeneficial( Mobile from, Mobile target )
		{
			if( from == null || target == null || from.AccessLevel > AccessLevel.Player || target.AccessLevel > AccessLevel.Player )
				return true;

			#region Factions
			Faction targetFaction = Faction.Find( target, true );

			if( targetFaction != null )
			{
				if( Faction.Find( from, true ) != targetFaction )
					return false;
			}
			#endregion

			Map map = from.Map;

			if( map != null && (map.Rules & MapRules.BeneficialRestrictions) == 0 )
				return true; // In felucca, anything goes

			if( !from.Player )
				return true; // NPCs have no restrictions

			if( target is BaseCreature && !((BaseCreature)target).Controlled )
				return false; // Players cannot heal uncontrolled mobiles

			if( from is PlayerMobile && ((PlayerMobile)from).Young && (!(target is PlayerMobile) || !((PlayerMobile)target).Young) )
				return false; // Young players cannot perform beneficial actions towards older players

			Guild fromGuild = from.Guild as Guild;
			Guild targetGuild = target.Guild as Guild;

			if( fromGuild != null && targetGuild != null && (targetGuild == fromGuild || fromGuild.IsAlly( targetGuild )) )
				return true; // Guild members can be beneficial

			return CheckBeneficialStatus( GetGuildStatus( from ), GetGuildStatus( target ) );
		}

		public static bool Mobile_AllowHarmful( Mobile from, Mobile target )
		{
			if( from == null || target == null || from.AccessLevel > AccessLevel.Player || target.AccessLevel > AccessLevel.Player )
				return true;

			Map map = from.Map;

			if( map != null && (map.Rules & MapRules.HarmfulRestrictions) == 0 )
				return true; // In felucca, anything goes

			if( !from.Player && !(from is BaseCreature && (((BaseCreature)from).Controlled || ((BaseCreature)from).Summoned)) )
			{
				if( !CheckAggressor( from.Aggressors, target ) && !CheckAggressed( from.Aggressed, target ) && target is PlayerMobile && ((PlayerMobile)target).CheckYoungProtection( from ) )
					return false;

				return true; // Uncontrolled NPCs are only restricted by the young system
			}

			Guild fromGuild = GetGuildFor( from.Guild as Guild, from );
			Guild targetGuild = GetGuildFor( target.Guild as Guild, target );

			if( fromGuild != null && targetGuild != null && (fromGuild == targetGuild || fromGuild.IsAlly( targetGuild ) || fromGuild.IsEnemy( targetGuild )) )
				return true; // Guild allies or enemies can be harmful

			if( target is BaseCreature && (((BaseCreature)target).Controlled || (((BaseCreature)target).Summoned && from != ((BaseCreature)target).SummonMaster)) )
				return false; // Cannot harm other controlled mobiles

			if( target.Player )
				return false; // Cannot harm other players

			if( !(target is BaseCreature && ((BaseCreature)target).InitialInnocent) )
			{
				if( Notoriety.Compute( from, target ) == Notoriety.Innocent )
					return false; // Cannot harm innocent mobiles
			}

			return true;
		}

		public static Guild GetGuildFor( Guild def, Mobile m )
		{
			Guild g = def;

			BaseCreature c = m as BaseCreature;

			if( c != null && c.Controlled && c.ControlMaster != null )
			{
				c.DisplayGuildTitle = false;

				if( c.Map != Map.Internal && (Core.AOS || Guild.NewGuildSystem || c.ControlOrder == OrderType.Attack || c.ControlOrder == OrderType.Guard) )
					g = (Guild)(c.Guild = c.ControlMaster.Guild);
				else if( c.Map == Map.Internal || c.ControlMaster.Guild == null )
					g = (Guild)(c.Guild = null);
			}

			return g;
		}

		public static int CorpseNotoriety( Mobile source, Corpse target )
		{
			if( target.AccessLevel > AccessLevel.Player )
				return Notoriety.CanBeAttacked;

			Body body = (Body)target.Amount;

			BaseCreature cretOwner = target.Owner as BaseCreature;

			if( cretOwner != null )
			{
				Guild sourceGuild = GetGuildFor( source.Guild as Guild, source );
				Guild targetGuild = GetGuildFor( target.Guild as Guild, target.Owner );

				if( sourceGuild != null && targetGuild != null )
				{
					if( sourceGuild == targetGuild || sourceGuild.IsAlly( targetGuild ) )
						return Notoriety.Ally;
					else if( sourceGuild.IsEnemy( targetGuild ) )
						return Notoriety.Enemy;
				}

				Faction srcFaction = Faction.Find( source, true, true );
				Faction trgFaction = Faction.Find( target.Owner, true, true );

				if( srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet )
					return Notoriety.Enemy;

				if( CheckHouseFlag( source, target.Owner, target.Location, target.Map ) )
					return Notoriety.CanBeAttacked;

				int actual = Notoriety.CanBeAttacked;

				if( target.Kills >= 5 || (body.IsMonster && IsSummoned( target.Owner as BaseCreature )) || (target.Owner is BaseCreature && (((BaseCreature)target.Owner).AlwaysMurderer || ((BaseCreature)target.Owner).IsAnimatedDead)) )
					actual = Notoriety.Murderer;

				if( DateTime.Now >= (target.TimeOfDeath + Corpse.MonsterLootRightSacrifice) )
					return actual;

				Party sourceParty = Party.Get( source );

				List<Mobile> list = target.Aggressors;

				for( int i = 0; i < list.Count; ++i )
				{
					if( list[i] == source || (sourceParty != null && Party.Get( list[i] ) == sourceParty) )
						return actual;
				}

				return Notoriety.Innocent;
			}
			else
			{
				if( target.Kills >= 5 || (body.IsMonster && IsSummoned( target.Owner as BaseCreature )) || (target.Owner is BaseCreature && (((BaseCreature)target.Owner).AlwaysMurderer || ((BaseCreature)target.Owner).IsAnimatedDead)) )
					return Notoriety.Murderer;

				if( target.Criminal )
					return Notoriety.Criminal;

				Guild sourceGuild = GetGuildFor( source.Guild as Guild, source );
				Guild targetGuild = GetGuildFor( target.Guild as Guild, target.Owner );

				if( sourceGuild != null && targetGuild != null )
				{
					if( sourceGuild == targetGuild || sourceGuild.IsAlly( targetGuild ) )
						return Notoriety.Ally;
					else if( sourceGuild.IsEnemy( targetGuild ) )
						return Notoriety.Enemy;
				}

				Faction srcFaction = Faction.Find( source, true, true );
				Faction trgFaction = Faction.Find( target.Owner, true, true );

				if( srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet )
				{
					List<Mobile> secondList = target.Aggressors;

					for( int i = 0; i < secondList.Count; ++i )
					{
						if( secondList[i] == source || secondList[i] is BaseFactionGuard )
							return Notoriety.Enemy;
					}
				}

				if( target.Owner != null && target.Owner is BaseCreature && ((BaseCreature)target.Owner).AlwaysAttackable )
					return Notoriety.CanBeAttacked;

				if( CheckHouseFlag( source, target.Owner, target.Location, target.Map ) )
					return Notoriety.CanBeAttacked;

				if( !(target.Owner is PlayerMobile) && !IsPet( target.Owner as BaseCreature ) )
					return Notoriety.CanBeAttacked;

				List<Mobile> list = target.Aggressors;

				for( int i = 0; i < list.Count; ++i )
				{
					if( list[i] == source )
						return Notoriety.CanBeAttacked;
				}

				return Notoriety.Innocent;
			}
		}

		public static int MobileNotoriety( Mobile source, Mobile target )
		{
			if( Core.AOS && (target.Blessed || (target is BaseVendor && ((BaseVendor)target).IsInvulnerable) || target is PlayerVendor || target is TownCrier) )
				return Notoriety.Invulnerable;

			if( target.AccessLevel > AccessLevel.Player )
				return Notoriety.CanBeAttacked;

			if( source.Player && !target.Player && source is PlayerMobile && target is BaseCreature )
			{
				BaseCreature bc = (BaseCreature)target;

				if( !bc.Summoned && !bc.Controlled && ((PlayerMobile)source).EnemyOfOneType == target.GetType() )
					return Notoriety.Enemy;
			}

            if ( target.Kills >= 5 || ( target.Body.IsMonster && IsSummoned( target as BaseCreature ) && !( target is BaseFamiliar ) && !( target is ArcaneFey ) && !( target is Golem ) ) || ( target is BaseCreature && ( ( (BaseCreature)target ).AlwaysMurderer || ( (BaseCreature)target ).IsAnimatedDead ) ) )
				return Notoriety.Murderer;

			if( target.Criminal )
				return Notoriety.Criminal;

			Guild sourceGuild = GetGuildFor( source.Guild as Guild, source );
			Guild targetGuild = GetGuildFor( target.Guild as Guild, target );

			if( sourceGuild != null && targetGuild != null )
			{
				if( sourceGuild == targetGuild || sourceGuild.IsAlly( targetGuild ) )
					return Notoriety.Ally;
				else if( sourceGuild.IsEnemy( targetGuild ) )
					return Notoriety.Enemy;
			}

			Faction srcFaction = Faction.Find( source, true, true );
			Faction trgFaction = Faction.Find( target, true, true );

			if( srcFaction != null && trgFaction != null && srcFaction != trgFaction && source.Map == Faction.Facet )
				return Notoriety.Enemy;

			if( SkillHandlers.Stealing.ClassicMode && target is PlayerMobile && ((PlayerMobile)target).PermaFlags.Contains( source ) )
				return Notoriety.CanBeAttacked;

			if( target is BaseCreature && ((BaseCreature)target).AlwaysAttackable )
				return Notoriety.CanBeAttacked;

			if( CheckHouseFlag( source, target, target.Location, target.Map ) )
				return Notoriety.CanBeAttacked;

			if( !(target is BaseCreature && ((BaseCreature)target).InitialInnocent) )
			{
				if( !target.Body.IsHuman && !target.Body.IsGhost && !IsPet( target as BaseCreature ) && !TransformationSpellHelper.UnderTransformation( target ) && !AnimalForm.UnderTransformation( target ) )
					return Notoriety.CanBeAttacked;
			}

			if( CheckAggressor( source.Aggressors, target ) )
				return Notoriety.CanBeAttacked;

			if( CheckAggressed( source.Aggressed, target ) )
				return Notoriety.CanBeAttacked;

			if( target is BaseCreature )
			{
				BaseCreature bc = (BaseCreature)target;

				if( bc.Controlled && bc.ControlOrder == OrderType.Guard && bc.ControlTarget == source )
					return Notoriety.CanBeAttacked;
			}

			if( source is BaseCreature )
			{
				BaseCreature bc = (BaseCreature)source;

				Mobile master = bc.GetMaster();
				if( master != null && CheckAggressor( master.Aggressors, target ) )
					return Notoriety.CanBeAttacked;
			}

			return Notoriety.Innocent;
		}

		public static bool CheckHouseFlag( Mobile from, Mobile m, Point3D p, Map map )
		{
			BaseHouse house = BaseHouse.FindHouseAt( p, map, 16 );

			if( house == null || house.Public || !house.IsFriend( from ) )
				return false;

			if( m != null && house.IsFriend( m ) )
				return false;

			BaseCreature c = m as BaseCreature;

			if( c != null && !c.Deleted && c.Controlled && c.ControlMaster != null )
				return !house.IsFriend( c.ControlMaster );

			return true;
		}

		public static bool IsPet( BaseCreature c )
		{
			return (c != null && c.Controlled);
		}

		public static bool IsSummoned( BaseCreature c )
		{
			return (c != null && /*c.Controlled &&*/ c.Summoned);
		}

		public static bool CheckAggressor( List<AggressorInfo> list, Mobile target )
		{
			for( int i = 0; i < list.Count; ++i )
				if( list[i].Attacker == target )
					return true;

			return false;
		}

		public static bool CheckAggressed( List<AggressorInfo> list, Mobile target )
		{
			for( int i = 0; i < list.Count; ++i )
			{
				AggressorInfo info = list[i];

				if( !info.CriminalAggression && info.Defender == target )
					return true;
			}

			return false;
		}
	}
}