using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Items;
using Server.Spells.Ninjitsu;

namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class Travesty : BasePeerless
{
    private readonly Point3D[] _warpLocs =
    [
        new Point3D( 71, 1939, 0 ),
        new Point3D( 71, 1955, 0 ),
        new Point3D( 69, 1972, 0 ),
        new Point3D( 86, 1971, 0 ),
        new Point3D( 103, 1972, 0 ),
        new Point3D( 86, 1939, 0 ),
        new Point3D( 102, 1938, 0 )
    ];

    //private bool _CanDiscord;
    //private bool _CanPeace;
    //private bool _CanProvoke;

    private DateTime m_NextBodyChange;
    private DateTime m_NextMirrorImage;
    private bool m_SpawnedHelpers;
    private Timer m_Timer;

    [Constructible]
    public Travesty()
        : base( AIType.AI_Mage, FightMode.Closest, 10, 1 )
    {
        Name = "Travesty";
        Body = 0x108;

        BaseSoundID = 0x46E;

        SetStr( 900, 950 );
        SetDex( 900, 950 );
        SetInt( 900, 950 );

        SetHits( 35000 );

        SetDamage( 11, 18 );

        SetDamageType( ResistanceType.Physical, 100 );

        SetResistance( ResistanceType.Physical, 50, 70 );
        SetResistance( ResistanceType.Fire, 50, 70 );
        SetResistance( ResistanceType.Cold, 50, 70 );
        SetResistance( ResistanceType.Poison, 50, 70 );
        SetResistance( ResistanceType.Energy, 50, 70 );

        SetSkill( SkillName.Wrestling, 300.0, 320.0 );
        SetSkill( SkillName.Tactics, 100.0, 120.0 );
        SetSkill( SkillName.MagicResist, 100.0, 120.0 );
        SetSkill( SkillName.Anatomy, 100.0, 120.0 );
        SetSkill( SkillName.Healing, 100.0, 120.0 );
        SetSkill( SkillName.Poisoning, 100.0, 120.0 );
        SetSkill( SkillName.DetectHidden, 100.0 );
        SetSkill( SkillName.Hiding, 100.0 );
        SetSkill( SkillName.Parry, 100.0, 110.0 );
        SetSkill( SkillName.Magery, 100.0, 120.0 );
        SetSkill( SkillName.EvalInt, 100.0, 120.0 );
        SetSkill( SkillName.Meditation, 100.0, 120.0 );
        SetSkill( SkillName.Necromancy, 100.0, 120.0 );
        SetSkill( SkillName.SpiritSpeak, 100.0, 120.0 );
        SetSkill( SkillName.Focus, 100.0, 120.0 );
        SetSkill( SkillName.Spellweaving, 100.0, 120.0 );
        SetSkill( SkillName.Discordance, 100.0, 120.0 );
        SetSkill( SkillName.Bushido, 100.0, 120.0 );
        SetSkill( SkillName.Ninjitsu, 100.0, 120.0 );
        SetSkill( SkillName.Chivalry, 100.0, 120.0 );

        SetSkill( SkillName.Musicianship, 100.0, 120.0 );
        SetSkill( SkillName.Discordance, 100.0, 120.0 );
        SetSkill( SkillName.Provocation, 100.0, 120.0 );
        SetSkill( SkillName.Peacemaking, 100.0, 120.0 );

        Fame = 30000;
        Karma = -30000;
    }

    public Travesty( Serial serial )
        : base( serial )
    {
    }

    public override string CorpseName => "a travesty's corpse";
    public override double WeaponAbilityChance => IsBodyMod ? base.WeaponAbilityChance : 0.1;

    //public override bool CanDiscord => _CanDiscord;
    //public override bool CanPeace => _CanPeace;
    //public override bool CanProvoke => _CanProvoke;
    public override bool AlwaysAttackable => true;

    public override bool ShowFameTitle => false;

    public override WeaponAbility GetWeaponAbility()
    {
        if ( Weapon is null or Fists )
        {
            return null;
        }

        if ( Weapon is BaseWeapon weapon )
        {
            return Utility.RandomBool() ? weapon.PrimaryAbility : weapon.SecondaryAbility;
        }

        return null;
    }

    public override void GenerateLoot()
    {
        AddLoot( LootPack.SuperBoss, 8 );
        AddLoot( LootPack.ArcanistScrolls, Utility.RandomMinMax( 1, 6 ) );
        AddLoot( LootPack.PeerlessResource, 8 );
        AddLoot( LootPack.Talisman, 5 );
        AddLoot( LootPack.LootItem<EyeOfTheTravesty>() );
        AddLoot( LootPack.LootItem<OrdersFromMinax>() );

        AddLoot(
            LootPack.RandomLootItem(
                [
                    typeof( TravestysSushiPreparations ), typeof( TravestysFineTeakwoodTray ),
                    typeof( TravestysCollectionOfShells )
                ]
            )
        );

        AddLoot( LootPack.LootItem<ParrotItem>( 60.0 ) );
        AddLoot( LootPack.LootItem<TragicRemainsOfTravesty>( 10.0 ) );
        AddLoot( LootPack.LootItem<ImprisonedDog>( 5.0 ) );
        AddLoot( LootPack.LootItem<MarkOfTravesty>( 5.0 ) );

        //TODO ?
        //AddLoot( LootPack.LootItem<MalekisHonor>( 2.5 ) );
    }

    public override void OnDamage( int amount, Mobile from, bool willKill )
    {
        if ( 0.1 > Utility.RandomDouble() && m_NextMirrorImage < DateTime.UtcNow )
        {
            new MirrorImage( this, null ).Cast();

            m_NextMirrorImage = DateTime.UtcNow + TimeSpan.FromSeconds( Utility.RandomMinMax( 20, 45 ) );
        }

        if ( /*0.25 > Utility.RandomDouble() && */DateTime.UtcNow > m_NextBodyChange )
        {
            ChangeBody();
        }

        base.OnDamage( amount, from, willKill );
    }

    public override void ClearHands()
    {
    }

    public void ChangeBody()
    {
        var list = new List<Mobile>();

        var eable = Map.GetMobilesInRange( Location, 5 );

        foreach ( var m in eable )
        {
            if ( m.Player && m.AccessLevel == AccessLevel.Player && m.Alive )
            {
                list.Add( m );
            }
        }


        if ( list.Count == 0 || IsBodyMod )
        {
            return;
        }

        var attacker = list[Utility.Random( list.Count )];

        BodyMod = attacker.Body;
        HueMod = attacker.Hue;
        NameMod = attacker.Name;
        Female = attacker.Female;
        Title = "(Travesty)";
        HairItemID = attacker.HairItemID;
        HairHue = attacker.HairHue;
        FacialHairItemID = attacker.FacialHairItemID;
        FacialHairHue = attacker.FacialHairHue;

        foreach ( var item in attacker.Items )
        {
            if ( item.Layer < Layer.Mount &&
                 item.Layer != Layer.Backpack &&
                 item.Layer != Layer.Mount &&
                 item.Layer != Layer.Bank &&
                 item.Layer != Layer.Hair &&
                 //item.Layer != Layer.Face &&
                 item.Layer != Layer.FacialHair )
            {
                if ( FindItemOnLayer( item.Layer ) == null )
                {
                    //TODO ?
                    //if (item is BaseWeapon)
                    //{
                    //    var crItem = CraftItem.GetCraftItem(item.GetType(), true);

                    //    if (crItem != null)
                    //    {
                    //        SetWearable(Loot.Construct(crItem.ItemType));
                    //    }
                    //    else
                    //    {
                    //        SetWearable(new ClonedItem(item));
                    //    }
                    //}
                    //else
                    //{
                    //    SetWearable(new ClonedItem(item));
                    //}
                    SetWearable( new ClonedItem( item ) );
                }
            }
        }

        if ( attacker.Skills[SkillName.Swords].Value >= 50.0 || attacker.Skills[SkillName.Fencing].Value >= 50.0 ||
             attacker.Skills[SkillName.Macing].Value >= 50.0 )
        {
            ChangeAIType( AIType.AI_Melee );
        }

        if ( attacker.Skills[SkillName.Archery].Value >= 50.0 )
        {
            ChangeAIType( AIType.AI_Archer );
        }

        if ( attacker.Skills[SkillName.Spellweaving].Value >= 50.0 )
        {
            ChangeAIType( AIType.AI_Spellweaving );
        }

        if ( attacker.Skills[SkillName.Magery].Value >= 50.0 )
        {
            ChangeAIType( AIType.AI_Mage );
        }

        if (attacker.Skills[SkillName.Necromancy].Value >= 50.0)
        {
            ChangeAIType(AIType.AI_Necro);
        }

        if (attacker.Skills[SkillName.Ninjitsu].Value >= 50.0)
        {
            ChangeAIType(AIType.AI_Ninja);
        }

        if (attacker.Skills[SkillName.Bushido].Value >= 50.0)
        {
            ChangeAIType(AIType.AI_Samurai);
        }

        if (attacker.Skills[SkillName.Necromancy].Value >= 50.0 && attacker.Skills[SkillName.Magery].Value >= 50.0)
        {
            ChangeAIType(AIType.AI_NecroMage);
        }

        PlaySound( 0x511 );
        FixedParticles( 0x376A, 1, 14, 5045, EffectLayer.Waist );

        m_NextBodyChange = DateTime.UtcNow + TimeSpan.FromSeconds( 10.0 );

        //if (attacker.Skills[SkillName.Healing].Base > 20)
        //{
        //    SetSpecialAbility(SpecialAbility.Heal);
        //}

        if (attacker.Skills[SkillName.Discordance].Base > 50)
        {
            CanDiscord = true;
        }

        if (attacker.Skills[SkillName.Peacemaking].Base > 50)
        {
            CanPeace = true;
        }

        if (attacker.Skills[SkillName.Provocation].Base > 50)
        {
            CanProvoke = true;
        }

        if ( m_Timer != null )
        {
            m_Timer.Stop();
        }

        m_Timer = Timer.DelayCall( TimeSpan.FromMinutes( 1.0 ), RestoreBody );
    }

    public bool CanProvoke { get; set; }

    public bool CanPeace { get; set; }

    public bool CanDiscord { get; set; }

    public void DeleteItems()
    {
        foreach ( var item in Items.ToList() )
        {
            if ( item is { Deleted: false } )
            {
                item.Delete();
            }
        }

        //TODO
        //ColUtility.SafeDelete( Items, item => item is ClonedItem || item is BaseWeapon );

        //if ( Backpack != null )
        //{
        //    ColUtility.SafeDelete( Backpack.Items, item => item is ClonedItem || item is BaseWeapon );
        //}
    }

    public virtual void RestoreBody()
    {
        BodyMod = 0;
        HueMod = -1;
        NameMod = null;
        Female = false;
        Title = null;

        CanDiscord = false;
        CanPeace = false;
        CanProvoke = false;

        //if (HasAbility(SpecialAbility.Heal))
        //{
        //    RemoveSpecialAbility(SpecialAbility.Heal);
        //}

        DeleteItems();

        ChangeAIType( AIType.AI_Mage );

        if ( m_Timer != null )
        {
            m_Timer.Stop();
            m_Timer = null;
        }
    }

    public override bool OnBeforeDeath()
    {
        RestoreBody();

        return base.OnBeforeDeath();
    }

    public override void OnAfterDelete()
    {
        if ( m_Timer != null )
        {
            m_Timer.Stop();
        }

        base.OnAfterDelete();
    }

    #region Spawn Helpers

    public override bool CanSpawnHelpers => true;
    public override int MaxHelpersWaves => 1;

    public override bool CanSpawnWave()
    {
        if ( Hits > 2000 )
        {
            m_SpawnedHelpers = false;
        }

        return !m_SpawnedHelpers && Hits < 2000;
    }

    public override void SpawnHelpers()
    {
        m_SpawnedHelpers = true;

        SpawnNinjaGroup( new Point3D( 80, 1964, 0 ) );
        SpawnNinjaGroup( new Point3D( 80, 1949, 0 ) );
        SpawnNinjaGroup( new Point3D( 92, 1948, 0 ) );
        SpawnNinjaGroup( new Point3D( 92, 1962, 0 ) );

        if ( Map != null && Map != Map.Internal && Region.IsPartOf( "The Citadel" ) )
        {
            var loc = _warpLocs[Utility.Random( _warpLocs.Length )];
            MoveToWorld( loc, Map );
        }
    }

    public void SpawnNinjaGroup( Point3D _location )
    {
        SpawnHelper(new DragonsFlameMage(), _location);
        SpawnHelper(new SerpentsFangAssassin(), _location);
        SpawnHelper(new TigersClawThief(), _location);
    }

    #endregion
}

[SerializationGenerator( 0 )]
public partial class ClonedItem : Item
{
    public ClonedItem( Item item ) : base( item.ItemID )
    {
        Name = item.Name;
        Weight = item.Weight;
        Hue = item.Hue;
        Layer = item.Layer;
    }

    public override DeathMoveResult OnParentDeath( Mobile parent ) => DeathMoveResult.RemainEquipped;

    public override DeathMoveResult OnInventoryDeath( Mobile parent )
    {
        Delete();
        return base.OnInventoryDeath( parent );
    }
}
