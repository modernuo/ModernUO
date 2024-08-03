using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator( 0, false )]
[Flippable( 0x14E7, 0x14E8 )]
public partial class HitchingPost : Item, ISecurable
{
    [SerializedCommandProperty( AccessLevel.GameMaster )] [SerializableField( 1 )]
    private int _charges;

    [SerializedCommandProperty( AccessLevel.GameMaster )] [SerializableField( 0 )]
    private SecureLevel _level;

    [SerializedCommandProperty( AccessLevel.GameMaster )] [SerializableField( 3 )]
    private bool _replica;

    [SerializedCommandProperty( AccessLevel.GameMaster )] [SerializableField( 2 )]
    private int _usesRemaining;

    [Constructible]
    public HitchingPost()
        : this( true )
    {
    }

    [Constructible]
    public HitchingPost( bool replica )
        : base( 0x14E7 )
    {
        Weight = 10;
        Replica = replica;

        Charges = replica ? 2 : -1;
        UsesRemaining = replica ? 15 : 30;

        Level = SecureLevel.CoOwners;
    }

    public override int LabelNumber => _replica ? 1071127 : 1025351; // hitching post (replica)

    public override bool ForceShowProperties => true;

    public override bool HandlesOnSpeech => true;

    public override void GetContextMenuEntries( Mobile from, ref PooledRefList<ContextMenuEntry> list )
    {
        base.GetContextMenuEntries( from, ref list );
        SetSecureLevelEntry.AddTo( from, this, ref list );
    }

    public override void GetProperties( IPropertyList list )
    {
        base.AddNameProperties( list );

        list.Add( 1060584, _usesRemaining.ToString() );

        if ( _replica )
        {
            list.Add( 1071215, _charges.ToString() );
        }
    }

    public void BeginClaimList( Mobile from )
    {
        if ( from is not PlayerMobile player )
        {
            return;
        }

        if ( Deleted || !from.CheckAlive() )
        {
            return;
        }

        if ( UsesRemaining <= 0 )
        {
            if ( !_replica || Charges > 0 )
            {
                from.SendLocalizedMessage( 1071151 ); //Hitching rope is insufficient. You have to supply it.
            }
            else
            {
                from.SendLocalizedMessage( 1071157 ); //This hitching post is damaged. You can't use it any longer.
            }
        }
        else
        {
            var list = new List<BaseCreature>();

            foreach ( var mobile in player.Stabled )
            {
                var pet = mobile as BaseCreature;

                if (pet == null || pet.Deleted)
                {
                    if (pet != null)
                    {
                        pet.IsStabled = false;
                    }

                    player.Stabled.Remove(mobile);
                }

                list.Add(pet);
            }

            if ( list.Count > 0 )
            {
                from.SendGump( new ClaimListGump( this, from, list ) );
            }
            else
            {
                from.SendLocalizedMessage( 502671 ); // But I have no animals stabled with me at the moment!
            }
        }
    }

    public void EndClaimList( Mobile from, BaseCreature pet )
    {
        if ( from is not PlayerMobile player )
        {
            return;
        }

        if ( pet == null || pet.Deleted || from.Map != Map || !from.InRange( this, 14 ) || !player.Stabled.Contains( pet ) ||
             !from.CheckAlive() )
        {
            return;
        }

        if ( from.Followers + pet.ControlSlots <= from.FollowersMax )
        {
            pet.SetControlMaster( from );

            if ( pet.Summoned )
            {
                pet.SummonMaster = from;
            }

            pet.ControlTarget = from;
            pet.ControlOrder = OrderType.Follow;

            pet.MoveToWorld( from.Location, from.Map );

            pet.IsStabled = false;
            player.Stabled.Remove( pet );

            from.SendLocalizedMessage( 1042559 ); // Here you go... and good day to you!
        }
        else
        {
            from.SendLocalizedMessage(
                1049612,
                pet.Name
            ); // ~1_NAME~ remained in the stables because you have too many followers.
        }
    }

    public void BeginStable( Mobile from )
    {
        if ( from is not PlayerMobile player )
        {
            return;
        }

        if ( Deleted || !from.CheckAlive() )
        {
            return;
        }

        if ( UsesRemaining <= 0 )
        {
            if ( !_replica || Charges > 0 )
            {
                from.SendLocalizedMessage( 1071151 ); //Hitching rope is insufficient. You have to supply it.
            }
            else
            {
                from.SendLocalizedMessage( 1071157 ); //This hitching post is damaged. You can't use it any longer.
            }
        }
        else if ( player.Stabled.Count >= AnimalTrainer.GetMaxStabled( from ) )
        {
            from.SendLocalizedMessage( 1042565 ); // You have too many pets in the stables!
        }
        else
        {
            from.SendLocalizedMessage( 1042558 ); /* I charge 30 gold per pet for a real week's stable time.
                                                   * I will withdraw it from thy bank account.
                                                   * Which animal wouldst thou like to stable here?
                                                   */

            from.Target = new StableTarget( this );
        }
    }

    public void EndStable( Mobile from, BaseCreature pet )
    {
        if ( from is not PlayerMobile player )
        {
            return;
        }

        if ( Deleted || !from.CheckAlive() )
        {
            return;
        }

        if ( !pet.Controlled || pet.ControlMaster != from )
        {
            from.SendLocalizedMessage( 1042562 ); // You do not own that pet!
        }
        else if ( pet.IsDeadPet )
        {
            from.SendLocalizedMessage( 1049668 ); // Living pets only, please.
        }
        else if ( pet.Summoned )
        {
            from.SendLocalizedMessage( 502673 ); // I can not stable summoned creatures.
        }
        else if (pet.Allured)
        {
            from.SendLocalizedMessage(1048053); // You can't stable that!
        }
        else if ( pet.Body.IsHuman )
        {
            from.SendLocalizedMessage( 502672 ); // HA HA HA! Sorry, I am not an inn.
        }
        else if ( ( pet is PackLlama || pet is PackHorse || pet is Beetle ) && pet.Backpack != null &&
                  pet.Backpack.Items.Count > 0 )
        {
            from.SendLocalizedMessage( 1042563 ); // You need to unload your pet.
        }
        else if ( pet.Combatant != null && pet.InRange( pet.Combatant, 12 ) && pet.Map == pet.Combatant.Map )
        {
            from.SendLocalizedMessage( 1042564 ); // I'm sorry.  Your pet seems to be busy.
        }
        else if ( player.Stabled.Count >= AnimalTrainer.GetMaxStabled( from ) )
        {
            from.SendLocalizedMessage( 1042565 ); // You have too many pets in the stables!
        }
        else
        {
            Container bank = from.FindBankNoCreate();

            if ( bank != null && bank.ConsumeTotal( typeof( Gold ), 30 ) || Banker.Withdraw( from, 30 ) )
            {
                pet.ControlTarget = null;
                pet.ControlOrder = OrderType.Stay;
                pet.Internalize();

                pet.SetControlMaster( null );
                pet.SummonMaster = null;

                pet.IsStabled = true;

                pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully happy

                player.Stabled.Add( pet );

                if ( _replica )
                {
                    UsesRemaining -= 1;
                }

                from.SendLocalizedMessage(
                    502679
                ); // Very well, thy pet is stabled. Thou mayst recover it by saying 'claim' to me. In one real world week, I shall sell it off if it is not claimed!
            }
            else
            {
                from.SendLocalizedMessage( 502677 ); // But thou hast not the funds in thy bank account!
            }
        }
    }

    public void Claim( Mobile from )
    {
        if ( from is not PlayerMobile player )
        {
            return;
        }

        if ( UsesRemaining <= 0 )
        {
            if ( !Replica || Charges > 0 )
            {
                from.SendLocalizedMessage( 1071151 ); //Hitching rope is insufficient. You have to supply it.
            }
            else
            {
                from.SendLocalizedMessage( 1071157 ); //This hitching post is damaged. You can't use it any longer.
            }
        }
        else
        {
            if ( Deleted || !from.CheckAlive() )
            {
                return;
            }

            var claimed = false;
            var stabled = 0;

            foreach ( var mobile in player.Stabled )
            {
                var pet = mobile as BaseCreature;

                if ( pet == null || pet.Deleted )
                {
                    if ( pet != null )
                    {
                        pet.IsStabled = false;
                    }

                    player.Stabled.Remove(mobile);
                    continue;
                }

                ++stabled;

                if ( from.Followers + pet.ControlSlots <= from.FollowersMax )
                {
                    pet.SetControlMaster( from );

                    if ( pet.Summoned )
                    {
                        pet.SummonMaster = from;
                    }

                    pet.ControlTarget = from;
                    pet.ControlOrder = OrderType.Follow;

                    pet.MoveToWorld( from.Location, from.Map );

                    pet.IsStabled = false;

                    pet.Loyalty = BaseCreature.MaxLoyalty; // Wonderfully Happy

                    player.Stabled.Remove( pet );

                    claimed = true;
                }
                else
                {
                    from.SendLocalizedMessage(
                        1049612,
                        pet.Name
                    ); // ~1_NAME~ remained in the stables because you have too many followers.
                }
            }

            if ( claimed )
            {
                from.SendLocalizedMessage( 1042559 ); // Here you go... and good day to you!
                UsesRemaining -= 1;
            }
            else if ( stabled == 0 )
            {
                from.SendLocalizedMessage( 502671 ); // But I have no animals stabled with me at the moment!
            }
        }
    }

    public bool IsOwner( Mobile mob )
    {
        var house = BaseHouse.FindHouseAt( this );

        return house != null && house.IsOwner( mob );
    }

    public bool CheckAccess( Mobile m )
    {
        if ( !IsLockedDown || m.AccessLevel >= AccessLevel.GameMaster )
        {
            return true;
        }

        var house = BaseHouse.FindHouseAt( this );

        if ( house != null && ( house.Public ? house.IsBanned( m ) : !house.HasAccess( m ) ) )
        {
            return false;
        }

        return house != null && house.HasSecureAccess( m, _level );
    }

    public override void OnSpeech( SpeechEventArgs e )
    {
        if ( CheckAccess( e.Mobile ) && IsLockedDown )
        {
            if ( !e.Handled && e.HasKeyword( 0x0008 ) )
            {
                e.Handled = true;
                BeginStable( e.Mobile );
            }
            else if ( !e.Handled && e.HasKeyword( 0x0009 ) )
            {
                e.Handled = true;

                if ( !Equals( e.Speech, "claim" ) )
                {
                    BeginClaimList( e.Mobile );
                }
                else
                {
                    Claim( e.Mobile );
                }
            }
            else
            {
                base.OnSpeech( e );
            }
        }
    }

    private class StableEntry( HitchingPost post, Mobile from ) : ContextMenuEntry( 6126, 12 )
    {
        private readonly Mobile _from = from;
        private readonly HitchingPost _post = post;
    }

    private class ClaimListGump : Gump
    {
        private readonly Mobile m_From;
        private readonly List<BaseCreature> m_List;
        private readonly HitchingPost m_Post;

        public ClaimListGump( HitchingPost post, Mobile from, List<BaseCreature> list )
            : base( 50, 50 )
        {
            m_Post = post;
            m_From = from;
            m_List = list;

            from.CloseGump<ClaimListGump>();

            AddPage( 0 );

            AddBackground( 0, 0, 325, 50 + list.Count * 20, 9250 );
            AddAlphaRegion( 5, 5, 315, 40 + list.Count * 20 );

            AddHtml( 15, 15, 275, 20, "<BASEFONT COLOR=#FFFFFF>Select a pet to retrieve from the stables:</BASEFONT>" );

            for ( var i = 0; i < list.Count; ++i )
            {
                var pet = list[i];

                if ( pet == null || pet.Deleted )
                {
                    continue;
                }

                AddButton( 15, 39 + i * 20, 10006, 10006, i + 1 );
                AddHtml( 32, 35 + i * 20, 275, 18, string.Format( "<BASEFONT COLOR=#C0C0EE>{0}</BASEFONT>", pet.Name ) );
            }
        }

        public override void OnResponse( NetState sender, in RelayInfo info )
        {
            var index = info.ButtonID - 1;

            if ( index >= 0 && index < m_List.Count )
            {
                m_Post.UsesRemaining -= 1;
                m_Post.EndClaimList( m_From, m_List[index] );
            }
        }
    }

    private class StableTarget : Target
    {
        private readonly HitchingPost m_Post;

        public StableTarget( HitchingPost post )
            : base( 12, false, TargetFlags.None ) =>
            m_Post = post;

        protected override void OnTarget( Mobile from, object targeted )
        {
            if ( targeted is BaseCreature )
            {
                m_Post.EndStable( from, ( BaseCreature )targeted );
            }
            else if ( targeted == from )
            {
                from.SendLocalizedMessage( 502672 ); // HA HA HA! Sorry, I am not an inn.
            }
            else
            {
                from.SendLocalizedMessage( 1048053 ); // You can't stable that!
            }
        }
    }
}
