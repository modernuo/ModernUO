using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Xanthos.Interfaces;
using Xanthos.Utilities;

namespace EvolutionPetSystem
{
	public abstract class BaseEvoEgg : Item
	{
		private DateTime m_Start;
		private double m_HatchDuration;
		private bool m_AllowEvolution;

		[CommandProperty( AccessLevel.GameMaster )]
		public bool AllowEvolution
		{
			get
			{
				if ( !m_AllowEvolution )
					m_AllowEvolution = ( 0 >= TimeSpan.Compare( TimeSpan.FromHours( m_HatchDuration ),
						DateTime.Now.Subtract( m_Start )));				
				return m_AllowEvolution;
			}
			set { m_AllowEvolution = value; }
		}

		[CommandProperty( AccessLevel.GameMaster )]
		public double HatchDuration
		{
			get { return m_HatchDuration; }
			set { m_HatchDuration = value; }
		}

		public abstract IEvoCreature GetEvoCreature();

		public BaseEvoEgg() : base( 0xC5D )
		{
			Weight = 1.0;
			Name = "an evolution egg";
			Hue = 1175;
			m_HatchDuration = 24.0;
			m_AllowEvolution = false;
			m_Start = DateTime.Now;
		}

		public BaseEvoEgg( Serial serial ) : base ( serial )
		{
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !IsChildOf( from.Backpack ) )
				from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
			else if ( AllowEvolution )
				Hatch( from );
			else
				from.SendMessage( "This egg is not yet ready to be hatched." );
		}

		public void Hatch( Mobile from )
		{
			IEvoCreature evo = GetEvoCreature();

			if ( null != evo )
			{
				BaseEvoSpec spec = GetEvoSpec( evo );
				BaseCreature creature = evo as BaseCreature;

				if ( null != spec && null != spec.Stages && !creature.Deleted )
				{
					if ( spec.Tamable && spec.MinTamingToHatch > from.Skills[SkillName.AnimalTaming].Value )
					{
						from.SendMessage( "A minimum animal taming skill of {0} is required to hatch this egg.", spec.Stages[0].MinTameSkill );
						creature.Delete();
					}
					else if ( from.FollowersMax - from.Followers < spec.Stages[0].ControlSlots )
					{
						from.SendMessage( "You have too many followers to hatch this egg." );
						creature.Delete();
					}
					else
					{
						creature.Controlled = true;
						creature.ControlMaster = from;
						creature.IsBonded = true;
						creature.MoveToWorld( from.Location, from.Map );
						creature.ControlOrder = OrderType.Follow;
						Delete();
						from.SendMessage( "You are now the proud owner of " + creature.Name + "!" );
					}
				}
			}
		}

		public static BaseEvoSpec GetEvoSpec( IEvoCreature evo )
		{
			return Xanthos.Utilities.Misc.InvokeParameterlessMethod( evo, "GetEvoSpec" ) as BaseEvoSpec;
		}

		public override void Serialize( IGenericWriter writer )
		{
			base.Serialize( writer );
			writer.Write( (int)0 );
			writer.Write( (DateTime)m_Start );
			writer.Write( (double)m_HatchDuration );
			writer.Write( (bool)m_AllowEvolution );
		}

		public override void Deserialize( IGenericReader reader )
		{
			base.Deserialize( reader );
			int version = reader.ReadInt();

			switch ( version )
			{
				case 0:
				{
					m_Start = reader.ReadDateTime();
					m_HatchDuration = reader.ReadDouble();
					m_AllowEvolution = reader.ReadBool();
					break;
				}
			}
		}
	}
}
