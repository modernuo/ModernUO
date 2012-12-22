using System;
using Server.Targeting;
using Server.HuePickers;

namespace Server.Items
{
	public class Dyes : Item /* , IUsesRemaining */ /* TODO complete usesremaing */
	{
		/*
		public bool ShowUsesRemaining { get { return false; } set { } }

		[CommandProperty( AccessLevel.GameMaster )]
		public virtual int UsesRemaining { get { return m_UsesRemaining; } set { m_UsesRemaining = value; } }

		private int m_UsesRemaining;
		*/

		[Constructable]
		public Dyes() : base( 0xFA9 )
		{
			Weight = 3.0;

			/* m_UsesRemaining = 25; */
		}

		public Dyes( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 1 ); // version

			/* writer.Write( ( int )m_UsesRemaining );  */
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( Weight == 0.0 )
				Weight = 3.0;

			/* m_UsesRemaining = ( version == 0 ) ? 25 : reader.ReadInt(); */
		}

		public override void OnDoubleClick( Mobile from )
		{
			from.SendLocalizedMessage( 500856 ); // Select the dye tub to use the dyes on.
			from.Target = new InternalTarget();
		}

		private class InternalTarget : Target
		{
			public InternalTarget() : base( 1, false, TargetFlags.None )
			{
			}

			private class InternalPicker : HuePicker
			{
				private DyeTub m_Tub;

				public InternalPicker( DyeTub tub ) : base( tub.ItemID )
				{
					m_Tub = tub;
				}

				public override void OnResponse( int hue )
				{
					m_Tub.DyedHue = hue;
				}
			}

			public virtual void SetTubHue( Mobile from, object state,  int hue )
			{
				if( state is DyeTub )
				{
					DyeTub tub = state as DyeTub;

					tub.DyedHue = hue;

					/* dyes.m_UsesRemaining--; let this change ride till the overhaul */
				}
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( targeted is DyeTub )
				{
					DyeTub tub = (DyeTub) targeted;

					if ( tub.Redyable )
					{
						if( tub.MetallicHues )   /* OSI has three metallic tubs now */
						{
							from.SendGump( new MetallicHuePicker( from, new MetallicHuePicker.MetallicHuePickerCallback( SetTubHue ), tub ) );
						}
						else if( tub.CustomHuePicker != null )
						{
							from.SendGump( new CustomHuePickerGump( from, tub.CustomHuePicker, new CustomHuePickerCallback( SetTubHue ), tub ) );
						}
						else
						{
							from.SendHuePicker( new InternalPicker( tub ) );
						}
					}
					else if ( tub is BlackDyeTub )
					{
						from.SendLocalizedMessage( 1010092 ); // You can not use this on a black dye tub.
					}
					else
					{
						from.SendMessage( "That dye tub may not be redyed." );
					}
				}
				else
				{
					from.SendLocalizedMessage( 500857 ); // Use this on a dye tub.
				}
			}
		}
	}
}