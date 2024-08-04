using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.Items
{
    [SerializationGenerator( 0 )]
    public partial class ImprisonedDog : BaseImprisonedMobile
    {
        [Constructible]
        public ImprisonedDog()
            : base( 0x1F1C )
        {
            Weight = 1.0;
            Hue = 56;
        }

        public override int LabelNumber => 1075091; // An Imprisoned Dog
        public override BaseCreature Summon => new TravestyDog();
    }
}

namespace Server.Mobiles
{
    public class TravestyDog : Dog
    {
        private string m_Name;
        private DateTime m_NextAttempt;

        [Constructible]
        public TravestyDog()
        {
            Hue = 2301;

            m_Name = null;
            m_NextAttempt = DateTime.UtcNow;
        }

        public TravestyDog( Serial serial ) : base( serial )
        {
        }

        public override bool DeleteOnRelease => true;
        public bool Morphed => m_Name != null;

        public override void GetProperties( IPropertyList list )
        {
            base.GetProperties( list );

            list.Add( 1049646 ); // (summoned)
        }

        public void DeleteItems()
        {
            for ( var i = Items.Count - 1; i >= 0; i-- )
            {
                if ( Items[i] is ClonedItem )
                {
                    Items[i].Delete();
                }
            }
        }

        public void BeginMorph( Mobile to )
        {
            if ( to == null || !Alive || Morphed )
            {
                return;
            }

            m_Name = Name;

            Body = to.Body;
            Hue = to.Hue;
            Name = to.Name;
            Female = to.Female;
            Title = to.Title;
            HairItemID = to.HairItemID;
            HairHue = to.HairHue;
            FacialHairItemID = to.FacialHairItemID;
            FacialHairHue = to.FacialHairHue;

            for ( var i = to.Items.Count - 1; i >= 0; i-- )
            {
                var item = to.Items[i];

                if ( item.Layer != Layer.Backpack && item.Layer != Layer.Mount )
                {
                    AddItem( new ClonedItem( item ) );
                }
            }

            PlaySound( 0x511 );
            FixedParticles( 0x376A, 1, 14, 5045, EffectLayer.Waist );

            Timer.DelayCall( TimeSpan.FromSeconds( 60 ), EndMorph );
        }

        public void EndMorph()
        {
            DeleteItems();

            Body = 0xD9;
            Hue = 2301;
            Name = m_Name;
            Female = false;
            Title = null;
            HairItemID = 0;
            HairHue = 0;
            FacialHairItemID = 0;
            FacialHairHue = 0;

            m_Name = null;

            PlaySound( 0x511 );
            FixedParticles( 0x376A, 1, 14, 5045, EffectLayer.Waist );
        }

        public override void Serialize( IGenericWriter writer )
        {
            base.Serialize( writer );

            writer.Write( 0 ); // version

            writer.Write( m_Name );
        }

        public override void Deserialize( IGenericReader reader )
        {
            base.Deserialize( reader );

            var version = reader.ReadInt();

            m_Name = reader.ReadString();
            m_NextAttempt = DateTime.UtcNow;

            if ( Morphed )
            {
                EndMorph();
            }
        }

        protected override bool OnMove( Direction d )
        {
            if ( !Morphed && m_NextAttempt <= DateTime.UtcNow )
            {
                var eable = GetMobilesInRange( 6 );
                foreach ( var m in eable )
                {
                    if ( !m.Hidden && m.Alive && Utility.RandomDouble() < 0.25 )
                    {
                        BeginMorph( m );
                        break;
                    }
                }
                //eable.Free();

                m_NextAttempt = DateTime.UtcNow + TimeSpan.FromSeconds( 90 );
            }

            return base.OnMove( d );
        }

        public override void OnDeath( Container c )
        {
            EndMorph();

            base.OnDeath( c );
        }

        private class ClonedItem : Item
        {
            public ClonedItem( Item item )
                : base( item.ItemID )
            {
                Name = item.Name;
                Weight = item.Weight;
                Hue = item.Hue;
                Layer = item.Layer;
            }

            public ClonedItem( Serial serial )
                : base( serial )
            {
            }

            public override DeathMoveResult OnParentDeath( Mobile parent )
            {
                Delete();

                return DeathMoveResult.RemainEquipped;
            }

            public override void Serialize( IGenericWriter writer )
            {
                base.Serialize( writer );

                writer.Write( 0 ); // version
            }

            public override void Deserialize( IGenericReader reader )
            {
                base.Deserialize( reader );

                var version = reader.ReadInt();
            }
        }
    }
}
