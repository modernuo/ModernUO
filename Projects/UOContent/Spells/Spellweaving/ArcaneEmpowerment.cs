using System;
using System.Collections;

namespace Server.Spells.Spellweaving;

public class ArcaneEmpowermentSpell( Mobile caster, Item scroll ) : ArcanistSpell( caster, scroll, m_Info )
{
    private static readonly SpellInfo m_Info = new(
        "Arcane Empowerment",
        "Aslavdra",
        -1
    );

    private static readonly Hashtable m_Table = new();

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds( 3 );
    public override double RequiredSkill => 24.0;
    public override int RequiredMana => 50;

    public static double GetDispellBonus( Mobile m )
    {
        if ( m_Table[m] is EmpowermentInfo info )
        {
            return 10.0 * info.Focus;
        }

        return 0.0;
    }

    public static int GetSpellBonus( Mobile m, bool playerVsPlayer )
    {
        if ( m_Table[m] is EmpowermentInfo info )
        {
            return info.Bonus + ( playerVsPlayer ? info.Focus : 0 );
        }

        return 0;
    }

    public static void AddHealBonus( Mobile m, ref int toHeal )
    {
        if ( m_Table[m] is EmpowermentInfo info )
        {
            toHeal = ( int )Math.Floor( ( 1 + ( 10 + info.Bonus ) / 100.0 ) * toHeal );
        }
    }

    public static void RemoveBonus( Mobile m )
    {
        if ( m_Table[m] is EmpowermentInfo info && info.Timer != null )
        {
            info.Timer.Stop();
        }

        m_Table.Remove( m );
    }

    public static bool IsUnderEffects( Mobile m ) => m_Table.ContainsKey( m );

    public override void OnCast()
    {
        if ( m_Table.ContainsKey( Caster ) )
        {
            Caster.SendLocalizedMessage( 501775 ); // This spell is already in effect.
        }
        else if ( CheckSequence() )
        {
            Caster.PlaySound( 0x5C1 );

            var level = GetFocusLevel( Caster );
            var skill = Caster.Skills[SkillName.Spellweaving].Value;

            var duration = TimeSpan.FromSeconds( 15 + ( int )( skill / 24 ) + level * 2 );
            var bonus = ( int )Math.Floor( skill / 12 ) + level * 5;

            m_Table[Caster] = new EmpowermentInfo( Caster, duration, bonus, level );

            BuffInfo.AddBuff(
                Caster,
                new BuffInfo(
                    BuffIcon.ArcaneEmpowerment,
                    1031616,
                    1075808,
                    duration,
                    Caster,
                    TextDefinition.Of( bonus, "10" )
                )
            );

            Caster.Delta( MobileDelta.WeaponDamage );
        }

        FinishSequence();
    }

    private class EmpowermentInfo
    {
        public readonly int Bonus;
        public readonly int Focus;
        public readonly ExpireTimer Timer;

        public EmpowermentInfo( Mobile caster, TimeSpan duration, int bonus, int focus )
        {
            Bonus = bonus;
            Focus = focus;

            Timer = new ExpireTimer( caster, duration );
            Timer.Start();
        }
    }

    private class ExpireTimer : Timer
    {
        private readonly Mobile m_Mobile;

        public ExpireTimer( Mobile m, TimeSpan delay )
            : base( delay ) =>
            m_Mobile = m;

        protected override void OnTick()
        {
            m_Mobile.PlaySound( 0x5C2 );
            m_Table.Remove( m_Mobile );

            m_Mobile.Delta( MobileDelta.WeaponDamage );
        }
    }
}
