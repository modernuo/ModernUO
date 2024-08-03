#region References

using Server.Spells;
using Server.Spells.Necromancy;

#endregion

namespace Server.Mobiles;

public class NecroAI : MageAI
{
    public NecroAI( BaseCreature m ) : base( m )
    {
    }

    //public override SkillName CastSkill => SkillName.Necromancy;
    public override bool UsesMagery => false;

    public override Spell GetRandomDamageSpell()
    {
        var mana = m_Mobile.Mana;
        var select = 1;

        if ( mana >= 29 )
        {
            select = 4;
        }
        else if ( mana >= 23 )
        {
            select = 3;
        }
        else if ( mana >= 17 )
        {
            select = 2;
        }

        return Utility.Random( select ) switch
        {
            0 => new PainSpikeSpell( m_Mobile ),
            1 => new PoisonStrikeSpell( m_Mobile ),
            2 => new WitherSpell( m_Mobile ),
            3 => new StrangleSpell( m_Mobile ),
            _ => null
        };
    }

    public override Spell GetRandomSummonSpell()
    {
        if ( !m_Mobile.Controlled && !m_Mobile.Summoned && m_Mobile.Mana >= 23 )
        {
            return new AnimateDeadSpell( m_Mobile );
        }

        return null;
    }

    public override Spell GetRandomCurseSpell()
    {
        var mana = m_Mobile.Mana;
        var select = 1;

        if ( mana >= 17 )
        {
            select = 5;
        }
        else if ( mana >= 13 )
        {
            select = 4;
        }
        else if ( mana >= 11 )
        {
            select = 3;
        }

        switch ( Utility.Random( select ) )
        {
            case 0:
                return new CurseWeaponSpell( m_Mobile );
            case 1:
                Spell spell;

                if ( NecroMageAI.CheckCastCorpseSkin( m_Mobile ) )
                {
                    spell = new CorpseSkinSpell( m_Mobile );
                }
                else
                {
                    spell = new CurseWeaponSpell( m_Mobile );
                }

                return spell;
            case 2:
                return new EvilOmenSpell( m_Mobile );
            case 3:
                return new BloodOathSpell( m_Mobile );
            case 4:
                return new MindRotSpell( m_Mobile );
        }

        return null;
    }

    public override Spell GetCureSpell() => null;

    public override Spell GetRandomBuffSpell() => new CurseWeaponSpell( m_Mobile );

    public override Spell GetHealSpell()
    {
        m_Mobile.UseSkill( SkillName.SpiritSpeak );

        return null;
    }
}
