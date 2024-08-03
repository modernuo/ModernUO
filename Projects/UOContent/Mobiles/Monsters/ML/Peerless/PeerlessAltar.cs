using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

public class PeerlessKeyArray
{
    public Type Key { get; set; }
    public bool Active { get; set; }
}

[SerializationGenerator( 0, false )]
public abstract partial class PeerlessAltar : Container
{
    [SerializableField( 3 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private Point3D _bossLocation;

    [SerializableField( 6 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private DateTime _deadLine;

    [SerializableField( 5 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private Point3D _exitDest;

    [SerializableField( 8 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private List<Mobile> _fighters;

    [SerializableField( 1 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private List<BaseCreature> _helpers = [];

    private Timer _keyResetTimer;

    [SerializableField( 7 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private List<Item> _masterKeys;

    [SerializableField( 0 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private Mobile _owner;

    [SerializableField( 2 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private BasePeerless _peerless;

    private Timer _slayTimer;

    [SerializableField( 4 )] [SerializedCommandProperty( AccessLevel.GameMaster )]
    private Point3D _teleportDest;

    protected List<PeerlessKeyArray> KeyValidation;
    private Timer m_DeadlineTimer;

    public PeerlessAltar( int itemID )
        : base( itemID )
    {
        Movable = false;

        Fighters = new List<Mobile>();
        MasterKeys = new List<Item>();
    }

    public override bool IsPublicContainer => true;
    public override bool IsDecoContainer => false;

    public virtual TimeSpan TimeToSlay => TimeSpan.FromMinutes( 90 );
    public virtual TimeSpan DelayAfterBossSlain => TimeSpan.FromMinutes( 15 );

    public abstract int KeyCount { get; }
    public abstract MasterKey MasterKey { get; }

    public abstract Type[] Keys { get; }
    public abstract BasePeerless Boss { get; }

    public abstract Rectangle2D[] BossBounds { get; }

    [CommandProperty( AccessLevel.GameMaster )]
    public DateTime Deadline { get; set; }

    [CommandProperty( AccessLevel.Counselor )]
    public bool ResetPeerless
    {
        get => false;
        set
        {
            if ( value )
            {
                FinishSequence();
            }
        }
    }

    [CommandProperty( AccessLevel.GameMaster )]
    public int FighterCount => Fighters != null ? Fighters.Count : 0;

    public override void OnDoubleClick( Mobile from )
    {
        if ( from.AccessLevel > AccessLevel.Player )
        {
            base.OnDoubleClick( from );
        }
    }

    public override bool CheckLift( Mobile from, Item item, ref LRReason reject )
    {
        if ( from.AccessLevel > AccessLevel.Player )
        {
            return base.CheckLift( from, item, ref reject );
        }

        reject = LRReason.CannotLift;

        return false;
    }

    public bool CheckParty( Mobile from )
    {
        if ( Owner == null )
        {
            return false;
        }

        if ( Owner == from )
        {
            return true;
        }

        var party = Party.Get( Owner );

        if ( party == null )
        {
            return false;
        }

        return party == Party.Get( from );
    }

    public override bool OnDragDrop( Mobile from, Item dropped )
    {
        if ( Owner != null && Owner != from )
        {
            if ( Peerless != null && Peerless.CheckAlive() )
            {
                from.SendLocalizedMessage(
                    1075213
                ); // The master of this realm has already been summoned and is engaged in combat.  Your opportunity will come after he has squashed the current batch of intruders!
            }
            else
            {
                from.SendLocalizedMessage( 1072683, Owner.Name ); // ~1_NAME~ has already activated the Prism, please wait...
            }

            return false;
        }

        if ( IsKey( dropped ) && MasterKeys.Count == 0 )
        {
            if ( KeyValidation == null )
            {
                KeyValidation = new List<PeerlessKeyArray>();

                for ( var i = 0; i < Keys.Length; i++ )
                {
                    KeyValidation.Add( new PeerlessKeyArray { Key = Keys[i], Active = false } );
                }
            }

            if ( KeyValidation.Any( x => x.Active ) )
            {
                if ( KeyValidation.Any( x => x.Key == dropped.GetType() && !x.Active ) )
                {
                    KeyValidation.Find( s => s.Key == dropped.GetType() ).Active = true;
                }
                else
                {
                    from.SendLocalizedMessage( 1072682 ); // This is not the proper key.
                    return false;
                }
            }
            else
            {
                Owner = from;
                KeyStartTimer( from );
                from.SendLocalizedMessage( 1074575 ); // You have activated this object!
                KeyValidation.Find( s => s.Key == dropped.GetType() ).Active = true;
            }

            if ( KeysValidated() )
            {
                ActivateEncounter( from );
            }
        }
        else
        {
            from.SendLocalizedMessage( 1072682 ); // This is not the proper key.
            return false;
        }

        return base.OnDragDrop( from, dropped );
    }

    public virtual void ActivateEncounter( Mobile from )
    {
        KeyStopTimer();

        from.SendLocalizedMessage(
            1072678
        ); // You have awakened the master of this realm. You need to hurry to defeat it in time!
        BeginSequence( from );

        for ( var k = 0; k < KeyCount; k++ )
        {
            from.SendLocalizedMessage( 1072680 ); // You have been given the key to the boss.

            var key = MasterKey;

            if ( key != null )
            {
                key.Altar = this;

                if ( !from.AddToBackpack( key ) )
                {
                    key.MoveToWorld( from.Location, from.Map );
                }

                MasterKeys.Add( key );
            }
        }

        Timer.DelayCall( TimeSpan.FromSeconds( 1 ), ClearContainer );
        KeyValidation = null;
    }

    public bool KeysValidated()
    {
        if ( KeyValidation == null )
        {
            return false;
        }

        return KeyValidation.Count( x => x.Active ) == Keys.Length;
    }

    public virtual bool IsKey( Item item )
    {
        if ( Keys == null || item == null )
        {
            return false;
        }

        var isKey = false;

        // check if item is key	
        for ( var i = 0; i < Keys.Length && !isKey; i++ )
        {
            if ( Keys[i].IsAssignableFrom( item.GetType() ) )
            {
                isKey = true;
            }
        }

        // check if item is already in container			
        for ( var i = 0; i < Items.Count && isKey; i++ )
        {
            if ( Items[i].GetType() == item.GetType() )
            {
                return false;
            }
        }

        return isKey;
    }

    public virtual void KeyStartTimer( Mobile from )
    {
        if ( _keyResetTimer != null )
        {
            _keyResetTimer.Stop();
        }

        _keyResetTimer = Timer.DelayCall(
            TimeSpan.FromSeconds( 30 * Keys.Count() ),
            () =>
            {
                from.SendLocalizedMessage( 1072679 ); // Your realm offering has reset. You will need to start over.

                if ( Owner != null )
                {
                    Owner = null;
                }

                KeyValidation = null;

                ClearContainer();
            }
        );
    }

    public virtual void KeyStopTimer()
    {
        if ( _keyResetTimer != null )
        {
            _keyResetTimer.Stop();
        }

        _keyResetTimer = null;
    }

    public virtual void ClearContainer()
    {
        for ( var i = Items.Count - 1; i >= 0; --i )
        {
            if ( i < Items.Count )
            {
                Items[i].Delete();
            }
        }
    }

    public virtual void AddFighter( Mobile fighter )
    {
        if ( !Fighters.Contains( fighter ) )
        {
            Fighters.Add( fighter );
        }
    }

    public virtual void SendConfirmations( Mobile from )
    {
        var party = Party.Get( from );

        if ( party != null )
        {
            foreach ( var m in party.Members.Select( info => info.Mobile ) )
            {
                if ( m.InRange( from.Location, 25 ) && CanEnter( m ) )
                {
                    m.SendGump( new ConfirmEntranceGump( this, from ) );
                }
            }
        }
        else
        {
            from.SendGump( new ConfirmEntranceGump( this, from ) );
        }
    }

    public virtual void BeginSequence( Mobile from )
    {
        SpawnBoss();
    }

    public virtual void SpawnBoss()
    {
        if ( Peerless == null )
        {
            // spawn boss
            Peerless = Boss;

            if ( Peerless == null )
            {
                return;
            }

            Peerless.Home = BossLocation;
            Peerless.RangeHome = 12;
            Peerless.MoveToWorld( BossLocation, Map );
            Peerless.Altar = this;

            StartSlayTimer();
        }
    }

    public void Enter( Mobile fighter )
    {
        if ( CanEnter( fighter ) )
        {
            // teleport party member's pets
            if ( fighter is PlayerMobile { AllFollowers: not null } player )
            {
                foreach ( var pet in player.AllFollowers.OfType<BaseCreature>()
                             .Where(
                                 pet => pet.Alive && pet.InRange( fighter.Location, 5 ) && pet is not BaseMount
                                        {
                                            Rider: not null
                                        } &&
                                        CanEnter( pet )
                             ) )
                {
                    pet.FixedParticles( 0x376A, 9, 32, 0x13AF, EffectLayer.Waist );
                    pet.PlaySound( 0x1FE );
                    pet.MoveToWorld( TeleportDest, Map );
                }
            }

            // teleport party member
            fighter.FixedParticles( 0x376A, 9, 32, 0x13AF, EffectLayer.Waist );
            fighter.PlaySound( 0x1FE );
            fighter.MoveToWorld( TeleportDest, Map );

            AddFighter( fighter );
        }
    }

    public virtual bool CanEnter( Mobile fighter ) => true;

    public virtual bool CanEnter( BaseCreature pet ) => true;

    public virtual void FinishSequence()
    {
        StopTimers();

        if ( Owner != null )
        {
            Owner = null;
        }

        // delete peerless
        if ( Peerless != null )
        {
            if ( Peerless.Corpse != null && !Peerless.Corpse.Deleted )
            {
                Peerless.Corpse.Delete();
            }

            if ( !Peerless.Deleted )
            {
                Peerless.Delete();
            }
        }

        // teleport party to exit if not already there
        if ( Fighters != null )
        {
            var fighters = new List<Mobile>( Fighters );

            fighters.ForEach( x => Exit( x ) );

            //ColUtility.Free(fighters);
        }

        // delete master keys
        if ( MasterKeys != null )
        {
            var keys = new List<Item>( MasterKeys );

            keys.ForEach( x => x.Delete() );

            //ColUtility.Free(keys);
        }

        // delete any remaining helpers
        CleanupHelpers();

        // reset summoner, boss		
        Peerless = null;
        Deadline = DateTime.MinValue;

        //ColUtility.Free(Fighters);
        //ColUtility.Free(MasterKeys);
    }

    public virtual void Exit( Mobile fighter )
    {
        if ( fighter == null )
        {
            return;
        }

        // teleport fighter
        if ( fighter.NetState == null && MobileIsInBossArea( fighter.LogoutLocation ) )
        {
            fighter.LogoutMap = this is CitadelAltar ? Map.Tokuno : Map;
            fighter.LogoutLocation = ExitDest;
        }
        else if ( MobileIsInBossArea( fighter ) && fighter.Map == Map )
        {
            fighter.FixedParticles( 0x376A, 9, 32, 0x13AF, EffectLayer.Waist );
            fighter.PlaySound( 0x1FE );

            if ( this is CitadelAltar )
            {
                fighter.MoveToWorld( ExitDest, Map.Tokuno );
            }
            else
            {
                fighter.MoveToWorld( ExitDest, Map );
            }
        }

        // teleport his pets
        if ( fighter is PlayerMobile { AllFollowers: not null } playerMobile )
        {
            foreach ( var pet in playerMobile.AllFollowers.OfType<BaseCreature>()
                         .Where(
                             pet => ( pet.Alive || pet.IsBonded ) &&
                                    pet.Map != Map.Internal &&
                                    MobileIsInBossArea( pet )
                         ) )
            {
                if ( pet is BaseMount mount )
                {
                    if ( mount.Rider != null && mount.Rider != playerMobile )
                    {
                        mount.Rider.FixedParticles( 0x376A, 9, 32, 0x13AF, EffectLayer.Waist );
                        mount.Rider.PlaySound( 0x1FE );
                        mount.Rider.MoveToWorld( ExitDest, this is CitadelAltar ? Map.Tokuno : Map );

                        continue;
                    }

                    if ( mount.Rider != null )
                    {
                        continue;
                    }
                }

                pet.FixedParticles( 0x376A, 9, 32, 0x13AF, EffectLayer.Waist );
                pet.PlaySound( 0x1FE );
                pet.MoveToWorld( ExitDest, this is CitadelAltar ? Map.Tokuno : Map );
            }
        }

        Fighters.Remove( fighter );
        fighter.SendLocalizedMessage( 1072677 ); // You have been transported out of this room.

        if ( MasterKeys.Count == 0 && Fighters.Count == 0 && Owner != null )
        {
            StopTimers();

            Owner = null;

            if ( Peerless != null )
            {
                if ( Peerless.Corpse != null && !Peerless.Corpse.Deleted )
                {
                    Peerless.Corpse.Delete();
                }

                if ( !Peerless.Deleted )
                {
                    Peerless.Delete();
                }
            }

            CleanupHelpers();

            // reset summoner, boss		
            Peerless = null;

            Deadline = DateTime.MinValue;
        }
    }

    public virtual void OnPeerlessDeath()
    {
        SendMessage( 1072681 ); // The master of this realm has been slain! You may only stay here so long.

        StopSlayTimer();

        //TODO?
        //// delete master keys
        foreach ( var item in MasterKeys.ToArray() )
        {
            item.Delete();
        }
        //ColUtility.SafeDelete( MasterKeys );

        //ColUtility.Free( MasterKeys );
        m_DeadlineTimer = Timer.DelayCall( DelayAfterBossSlain, FinishSequence );
    }

    public virtual bool MobileIsInBossArea( Mobile check ) => MobileIsInBossArea( check.Location );

    public virtual bool MobileIsInBossArea( Point3D loc )
    {
        if ( BossBounds == null || BossBounds.Length == 0 )
        {
            return true;
        }

        foreach ( var rec in BossBounds )
        {
            if ( rec.Contains( loc ) )
            {
                return true;
            }
        }

        return false;
    }

    public virtual void SendMessage( int message )
    {
        Fighters.ForEach( x => x.SendLocalizedMessage( message ) );
    }

    public virtual void SendMessage( int message, object param )
    {
        Fighters.ForEach( x => x.SendLocalizedMessage( message, param.ToString() ) );
    }

    public virtual void StopTimers()
    {
        StopSlayTimer();
        StopDeadlineTimer();
    }

    public virtual void StopDeadlineTimer()
    {
        if ( m_DeadlineTimer != null )
        {
            m_DeadlineTimer.Stop();
        }

        m_DeadlineTimer = null;

        if ( Owner != null )
        {
            Owner = null;
        }
    }

    public virtual void StopSlayTimer()
    {
        if ( _slayTimer != null )
        {
            _slayTimer.Stop();
        }

        _slayTimer = null;
    }

    public virtual void StartSlayTimer()
    {
        _slayTimer?.Stop();

        if ( TimeToSlay != TimeSpan.Zero )
        {
            Deadline = DateTime.UtcNow + TimeToSlay;
        }
        else
        {
            Deadline = DateTime.UtcNow + TimeSpan.FromHours( 1 );
        }

        _slayTimer = Timer.DelayCall( TimeSpan.FromMinutes( 5 ), TimeSpan.FromMinutes( 5 ), DeadlineCheck );
    }

    public virtual void DeadlineCheck()
    {
        if ( DateTime.UtcNow > Deadline )
        {
            SendMessage( 1072258 ); // You failed to complete an objective in time!
            FinishSequence();
            return;
        }

        var timeLeft = Deadline - DateTime.UtcNow;

        if ( timeLeft < TimeSpan.FromMinutes( 30 ) )
        {
            SendMessage( 1075611, timeLeft.TotalSeconds );
        }

        var now = DateTime.UtcNow;
        var remove = Fighters.Count;

        while ( --remove >= 0 )
        {
            if ( remove < Fighters.Count && Fighters[remove] is PlayerMobile player )
            {
                if ( player.NetState == null && ( now - player.LastOnline ).TotalMinutes > 10 )
                {
                    Exit( player );
                }
            }
        }
    }

    #region Helpers

    public void AddHelper( BaseCreature helper )
    {
        if ( helper != null && helper.Alive && !helper.Deleted )
        {
            _helpers.Add( helper );
        }
    }

    public bool AllHelpersDead()
    {
        for ( var i = 0; i < _helpers.Count; i++ )
        {
            var c = _helpers[i];

            if ( c.Alive )
            {
                return false;
            }
        }

        return true;
    }

    public void CleanupHelpers()
    {
        for ( var i = 0; i < _helpers.Count; i++ )
        {
            var c = _helpers[i];

            if ( c != null && c.Alive )
            {
                c.Delete();
            }
        }

        _helpers.Clear();
    }

    #endregion
}
