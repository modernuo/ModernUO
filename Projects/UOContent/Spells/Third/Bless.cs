using System;
using System.Collections.Generic;
using Server.Engines.ConPVP;
using Server.Targeting;

namespace Server.Spells.Third;

public class BlessSpell( Mobile caster, Item scroll = null ) : MagerySpell( caster, scroll, _info ),
    ISpellTargetingMobile
{
    private static readonly SpellInfo _info = new(
        "Bless",
        "Rel Sanct",
        203,
        9061,
        Reagent.Garlic,
        Reagent.MandrakeRoot
    );

    private static Dictionary<Mobile, InternalTimer> _Table;

    public override SpellCircle Circle => SpellCircle.Third;

    public void Target( Mobile m )
    {
        if ( CheckBSequence( m ) )
        {
            SpellHelper.Turn( Caster, m );

            var length = SpellHelper.GetDuration( Caster, m );
            SpellHelper.AddStatBonus( Caster, m, StatType.Str, length, false );
            SpellHelper.AddStatBonus( Caster, m, StatType.Dex, length );
            SpellHelper.AddStatBonus( Caster, m, StatType.Int, length );

            m.FixedParticles( 0x373A, 10, 15, 5018, EffectLayer.Waist );
            m.PlaySound( 0x1EA );

            var percentage = ( int )( SpellHelper.GetOffsetScalar( Caster, m, false ) * 100 );

            var args = $"{percentage}\t{percentage}\t{percentage}";

            BuffInfo.AddBuff( m, new BuffInfo( BuffIcon.Bless, 1075847, 1075848, length, m, args ) );

            AddBless(Caster, length + TimeSpan.FromMilliseconds(50));
        }
    }

    public static bool IsBlessed( Mobile m ) => _Table != null && _Table.ContainsKey( m );

    public static void AddBless( Mobile m, TimeSpan duration )
    {
        if ( _Table == null )
        {
            _Table = new Dictionary<Mobile, InternalTimer>();
        }

        if ( _Table.ContainsKey( m ) )
        {
            _Table[m].Stop();
        }

        _Table[m] = new InternalTimer( m, duration );
    }

    public static void RemoveBless( Mobile m, bool early = false )
    {
        if ( _Table != null && _Table.ContainsKey( m ) )
        {
            _Table[m].Stop();
            m.Delta( MobileDelta.Stat );

            _Table.Remove( m );
        }
    }

    public override bool CheckCast()
    {
        if ( DuelContext.CheckSuddenDeath( Caster ) )
        {
            Caster.SendMessage( 0x22, "You cannot cast this spell when in sudden death." );
            return false;
        }

        return base.CheckCast();
    }

    public override void OnCast()
    {
        Caster.Target = new SpellTargetMobile( this, TargetFlags.Beneficial, Core.ML ? 10 : 12 );
    }

    private class InternalTimer : Timer
    {
        public InternalTimer( Mobile m, TimeSpan duration )
            : base( duration )
        {
            Mobile = m;
            Start();
        }

        public Mobile Mobile { get; }

        protected override void OnTick()
        {
            RemoveBless( Mobile );
        }
    }
}
