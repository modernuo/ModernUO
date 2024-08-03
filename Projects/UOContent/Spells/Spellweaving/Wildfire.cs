using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using Server.Targeting;

namespace Server.Spells.Spellweaving;

public class WildfireSpell( Mobile caster, Item scroll ) : ArcanistSpell( caster, scroll, m_Info )
{
    private static readonly SpellInfo m_Info = new(
        "Wildfire",
        "Haelyn",
        -1,
        false
    );

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds( 2.5 );
    public override double RequiredSkill => 66.0;
    public override int RequiredMana => 50;
    public static Dictionary<Mobile, long> Table { get; } = new();

    public override void OnCast()
    {
        Caster.Target = new InternalTarget( this );
    }

    public void Target( Point3D p )
    {
        if ( !Caster.CanSee( p ) )
        {
            Caster.SendLocalizedMessage( 500237 ); // Target can not be seen.
        }
        else if ( CheckSequence() )
        {
            var level = GetFocusLevel( Caster );
            var skill = Caster.Skills[CastSkill].Value;

            var tiles = 5 + level;
            var damage = 10 + ( int )Math.Max( 1, skill / 24 ) + level;
            var duration = ( int )Math.Max( 1, skill / 24 ) + level;

            for ( var x = p.X - tiles; x <= p.X + tiles; x += tiles )
            {
                for ( var y = p.Y - tiles; y <= p.Y + tiles; y += tiles )
                {
                    if ( p.X == x && p.Y == y )
                    {
                        continue;
                    }

                    var p3d = new Point3D( x, y, Caster.Map.GetAverageZ( x, y ) );

                    if ( CanFitFire( p3d, Caster ) )
                    {
                        new FireItem( duration ).MoveToWorld( p3d, Caster.Map );
                    }
                }
            }

            Effects.PlaySound( p, Caster.Map, 0x5CF );

            //NegativeAttributes.OnCombatAction( Caster );

            new InternalTimer( this, Caster, p, damage, tiles, duration ).Start();
        }

        FinishSequence();
    }

    private bool CanFitFire( Point3D p, Mobile caster )
    {
        if ( !Caster.Map.CanFit( p, 12, true, false ) )
        {
            return false;
        }

        if ( BaseHouse.FindHouseAt( p, caster.Map, 20 ) != null )
        {
            return false;
        }

        foreach ( Region r in caster.Map.GetSector( p ).Regions )
        {
            if ( !r.Contains( p ) )
            {
                continue;
            }

            var reg = ( GuardedRegion )Region.Find( p, caster.Map ).GetRegion( typeof( GuardedRegion ) );
            if ( reg is { GuardsDisabled: false } )
            {
                return false;
            }
        }

        return true;
    }

    public static void DefragTable()
    {
        var mobiles = new List<Mobile>( Table.Keys );

        foreach ( var m in mobiles )
        {
            if ( Core.TickCount - Table[m] >= 0 )
            {
                Table.Remove( m );
            }
        }

        //ColUtility.Free( mobiles );
    }

    public class InternalTarget : Target
    {
        private readonly WildfireSpell m_Owner;

        public InternalTarget( WildfireSpell owner )
            : base( 12, true, TargetFlags.None ) =>
            m_Owner = owner;

        protected override void OnTarget( Mobile m, object o )
        {
            if ( o is IPoint3D )
            {
                m_Owner.Target( new Point3D( ( IPoint3D )o ) );
            }
        }

        protected override void OnTargetFinish( Mobile m )
        {
            m_Owner.FinishSequence();
        }
    }

    public class InternalTimer : Timer
    {
        private readonly int m_Damage;
        private readonly Point3D m_Location;
        private readonly Map m_Map;
        private readonly Mobile m_Owner;
        private readonly int m_Range;
        private readonly Spell m_Spell;
        private int m_LifeSpan;

        public InternalTimer( Spell spell, Mobile owner, Point3D location, int damage, int range, int duration )
            : base( TimeSpan.FromSeconds( 1 ), TimeSpan.FromSeconds( 1 ), duration )
        {
            m_Spell = spell;
            m_Owner = owner;
            m_Location = location;
            m_Damage = damage;
            m_Range = range;
            m_LifeSpan = duration;
            m_Map = owner.Map;
        }

        protected override void OnTick()
        {
            if ( m_Owner == null || m_Map == null || m_Map == Map.Internal )
            {
                return;
            }

            m_LifeSpan -= 1;
            var targets = GetTargets().Where( m => BaseHouse.FindHouseAt( m.Location, m.Map, 20 ) == null ).ToList();
            var count = targets.Count;

            foreach ( var m in targets )
            {
                m_Owner.DoHarmful( m );

                if ( m_Map.CanFit( m.Location, 12, true, false ) )
                {
                    new FireItem( m_LifeSpan ).MoveToWorld( m.Location, m_Map );
                }

                Effects.PlaySound( m.Location, m_Map, 0x5CF );
                var sdiBonus = ( double )AosAttributes.GetValue( m_Owner, AosAttribute.SpellDamage ) / 100;

                if ( m is PlayerMobile && sdiBonus > .15 )
                {
                    sdiBonus = .15;
                }

                var damage = m_Damage + ( int )( m_Damage * sdiBonus );

                if ( count > 1 )
                {
                    damage /= Math.Min( 3, count );
                }

                //TODO Check agaisnt Servuo
                AOS.Damage( m, m_Owner, damage, false, 100, 0, 0, 0, 0, 0/*, DamageType.SpellAOE*/ );
                Table[m] = Core.TickCount + 1000;
            }

            //ColUtility.Free( targets );
        }

        private IEnumerable<Mobile> GetTargets()
        {
            DefragTable();

            return m_Spell.AcquireIndirectTargets( m_Location, m_Range )
                .OfType<Mobile>()
                .Where( m => !Table.ContainsKey( m ) );
        }
    }
}

[SerializationGenerator(0, false)]
public partial class FireItem : Item
{
    public FireItem(int duration)
        : base(Utility.RandomBool() ? 0x398C : 0x3996)
    {
        Movable = false;
        Timer.DelayCall(TimeSpan.FromSeconds(duration), Delete);
    }
}
