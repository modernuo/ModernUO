using System;
using Server;

namespace Server.Items
{
    public class ArcaneFocus : TransientItem
    {
        public override int LabelNumber { get { return 1032629; } } // Arcane Focus

        private int m_StrengthBonus;

        [CommandProperty( AccessLevel.GameMaster )]
        public int StrengthBonus
        {
            get { return m_StrengthBonus; }
            set { m_StrengthBonus = value; }
        }

        public ArcaneFocus( TimeSpan lifeSpan, int strengthBonus ) : base( 0x3155, lifeSpan )
        {
            LootType = LootType.Blessed;
            m_StrengthBonus = strengthBonus;
        }

        public ArcaneFocus( Serial serial ) : base( serial )
        {
        }
        
        public override void GetProperties( ObjectPropertyList list )
        {
            base.GetProperties( list );

            list.Add( 1060485, m_StrengthBonus.ToString() ); // strength bonus ~1_val~
        }

		public override TextDefinition InvalidTransferMessage{ get { return 1073480; } } // Your arcane focus disappears.
		public override bool Nontransferable { get { return true; } }

        public override void Serialize( GenericWriter writer )
        {
            base.Serialize( writer );
            writer.Write( (int)0 );

            writer.Write( m_StrengthBonus );
        }

        public override void Deserialize( GenericReader reader )
        {
            base.Deserialize( reader );
            int version = reader.ReadInt();

            m_StrengthBonus = reader.ReadInt();
        }
    }
}