using System;
using Server;
using Server.Engines.MLQuests;
using Server.Mobiles;
using Server.Gumps;
using System.Collections.Generic;

namespace Server.Engines.MLQuests.Objectives
{
	public abstract class BaseObjective
	{
		public virtual bool IsTimed { get { return false; } }
		public virtual TimeSpan Duration { get { return TimeSpan.Zero; } }

		public BaseObjective()
		{
		}

		public virtual bool CanOffer( IQuestGiver quester, PlayerMobile pm, bool message )
		{
			return true;
		}

		public abstract void WriteToGump( Gump g, ref int y );

		public virtual BaseObjectiveInstance CreateInstance( MLQuestInstance instance )
		{
			return null;
		}
	}

	public abstract class BaseObjectiveInstance
	{
		private MLQuestInstance m_Instance;
		private DateTime m_EndTime;
		private bool m_Expired;

		public MLQuestInstance Instance
		{
			get { return m_Instance; }
		}

		public bool IsTimed
		{
			get { return ( m_EndTime != DateTime.MinValue ); }
		}

		public DateTime EndTime
		{
			get { return m_EndTime; }
			set { m_EndTime = value; }
		}

		public bool Expired
		{
			get { return m_Expired; }
			set { m_Expired = value; }
		}

		public BaseObjectiveInstance( MLQuestInstance instance, BaseObjective obj )
		{
			m_Instance = instance;

			if ( obj.IsTimed )
				m_EndTime = DateTime.Now + obj.Duration;
		}

		public virtual void WriteToGump( Gump g, ref int y )
		{
			if ( IsTimed )
				WriteTimeRemaining( g, ref y, ( m_EndTime > DateTime.Now ) ? ( m_EndTime - DateTime.Now ) : TimeSpan.Zero );
		}

		public static void WriteTimeRemaining( Gump g, ref int y, TimeSpan timeRemaining )
		{
			g.AddHtmlLocalized( 103, y, 120, 16, 1062379, 0x15F90, false, false ); // Est. time remaining:
			g.AddLabel( 223, y, 0x481, timeRemaining.TotalSeconds.ToString( "F0" ) );
			y += 16;
		}

		public virtual bool AllowsQuestItem( Item item, Type type )
		{
			return false;
		}

		public virtual bool IsCompleted()
		{
			return false;
		}

		public virtual void CheckComplete()
		{
			if ( IsCompleted() )
			{
				m_Instance.Player.PlaySound( 0x5B6 ); // public sound
				m_Instance.CheckComplete();
			}
		}

		public virtual void OnQuestAccepted()
		{
		}

		public virtual void OnQuestCancelled()
		{
		}

		public virtual void OnQuestCompleted()
		{
		}

		public virtual bool OnBeforeClaimReward()
		{
			return true;
		}

		public virtual void OnClaimReward()
		{
		}

		public virtual void OnAfterClaimReward()
		{
		}

		public virtual void OnRewardClaimed()
		{
		}

		public virtual void OnQuesterDeleted()
		{
		}

		public virtual void OnPlayerDeath()
		{
		}

		public virtual void OnExpire()
		{
		}

		public enum DataType : byte
		{
			None,
			EscortObjective,
			KillObjective,
			DeliverObjective
		}

		public virtual DataType ExtraDataType { get { return DataType.None; } }

		public virtual void Serialize( GenericWriter writer )
		{
			// Version info is written in MLQuestPersistence.Serialize

			if ( IsTimed )
			{
				writer.Write( true );
				writer.WriteDeltaTime( m_EndTime );
			}
			else
			{
				writer.Write( false );
			}

			// For type checks on deserialization
			// (This way quest objectives can be changed without breaking serialization)
			writer.Write( (byte)ExtraDataType );
		}

		public static void Deserialize( GenericReader reader, int version, BaseObjectiveInstance objInstance )
		{
			if ( reader.ReadBool() )
			{
				DateTime endTime = reader.ReadDeltaTime();

				if ( objInstance != null )
					objInstance.EndTime = endTime;
			}

			DataType extraDataType = (DataType)reader.ReadByte();

			switch ( extraDataType )
			{
				case DataType.EscortObjective:
				{
					bool completed = reader.ReadBool();

					if ( objInstance is EscortObjectiveInstance )
						( (EscortObjectiveInstance)objInstance ).HasCompleted = completed;

					break;
				}
				case DataType.KillObjective:
				{
					int slain = reader.ReadInt();

					if ( objInstance is KillObjectiveInstance )
						( (KillObjectiveInstance)objInstance ).Slain = slain;

					break;
				}
				case DataType.DeliverObjective:
				{
					bool completed = reader.ReadBool();

					if ( objInstance is DeliverObjectiveInstance )
						( (DeliverObjectiveInstance)objInstance ).HasCompleted = completed;

					break;
				}
			}
		}
	}
}
