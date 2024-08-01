using System;
using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class BaseEngravingTool : Item, IUsesRemaining, IRewardItem
{
    [SerializableField( 1 )] private bool _isRewardItem;

    [SerializableField( 0 )] private int _usesRemaining;

    [Constructible]
    public BaseEngravingTool( int itemID )
        : this( itemID, 1 )
    {
    }

    [Constructible]
    public BaseEngravingTool( int itemID, int uses )
        : base( itemID )
    {
        Weight = 1.0;
        Hue = 0x48D;

        LootType = LootType.Blessed;

        _usesRemaining = uses;
    }

    public BaseEngravingTool( Serial serial )
        : base( serial )
    {
    }

    public virtual bool DeletedItem => true;
    public virtual int LowSkillMessage => 0;
    public virtual int VeteranRewardCliloc => 0;

    public virtual Type[] Engraves => null;
    public virtual int GumpTitle => 1072359; // <CENTER>Engraving Tool</CENTER>

    public virtual int SuccessMessage => 1072361; // You engraved the object.
    public virtual int TargetMessage => 1072357; // Select an object to engrave.
    public virtual int RemoveMessage => 1072362; // You remove the engraving from the object.
    public virtual int ReChargesMessage => 1076166; // You do not have a blue diamond needed to recharge the engraving tool.
    public virtual int OutOfChargesMessage => 1076163; // There are no charges left on this engraving tool.
    public virtual int NotAccessibleMessage => 1072310; // The selected item is not accessible to engrave.
    public virtual int CannotEngraveMessage => 1072309; // The selected item cannot be engraved by this engraving tool.
    public virtual int ObjectWasNotMessage => 1072363; // The object was not engraved.        

    public virtual bool ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public virtual bool CheckType( IEntity entity )
    {
        if ( Engraves == null || entity == null )
        {
            return false;
        }

        var type = entity.GetType();

        for ( var i = 0; i < Engraves.Length; i++ )
        {
            if ( type == Engraves[i] || type.IsSubclassOf( Engraves[i] ) )
            {
                return true;
            }
        }

        return false;
    }

    public static BaseEngravingTool Find( Mobile from )
    {
        if ( from.Backpack != null )
        {
            var tool = from.Backpack.FindItemByType( typeof( BaseEngravingTool ) ) as BaseEngravingTool;

            if ( tool != null && !tool.DeletedItem && tool.UsesRemaining <= 0 )
            {
                return tool;
            }

            return null;
        }

        return null;
    }

    public override void OnDoubleClick( Mobile from )
    {
        base.OnDoubleClick( from );

        if ( _usesRemaining > 0 )
        {
            from.SendLocalizedMessage( TargetMessage );
            from.Target = new InternalTarget( this );
        }
        else
        {
            if ( !DeletedItem )
            {
                if ( CheckSkill( from ) )
                {
                    var diamond = from.Backpack.FindItemByType( typeof( BlueDiamond ) );

                    if ( diamond != null )
                    {
                        from.SendGump( new ConfirmGump( this, null ) );
                    }
                    else
                    {
                        from.SendLocalizedMessage( ReChargesMessage );
                    }
                }
            }

            from.SendLocalizedMessage( OutOfChargesMessage );
        }
    }

    private bool IsValid( IEntity entity, Mobile m )
    {
        if ( entity is Item )
        {
            var item = entity as Item;

            var house = BaseHouse.FindHouseAt( item );

            if ( m.InRange( item.GetWorldLocation(), 3 ) )
            {
                if ( item.Movable && !item.IsLockedDown && !item.IsSecure &&
                     ( item.RootParent == null || item.RootParent == m ) )
                {
                    return true;
                }

                if ( house != null && house.IsFriend( m ) )
                {
                    return true;
                }
            }
        }
        else if ( entity is BaseCreature )
        {
            var bc = entity as BaseCreature;

            if ( bc.Controlled && bc.ControlMaster == m )
            {
                return true;
            }
        }

        return false;
    }

    public bool CheckSkill( Mobile from )
    {
        if ( from.Skills[SkillName.Tinkering].Value < 75.0 )
        {
            from.SendLocalizedMessage( LowSkillMessage );
            return false;
        }

        return true;
    }

    public virtual void Recharge( Mobile from, Mobile guildmaster )
    {
        if ( from.Backpack != null )
        {
            var diamond = from.Backpack.FindItemByType( typeof( BlueDiamond ) );

            if ( guildmaster != null )
            {
                if ( _usesRemaining <= 0 )
                {
                    if ( diamond != null && Banker.Withdraw( from, 100000 ) )
                    {
                        diamond.Consume();
                        UsesRemaining = 10;
                        guildmaster.Say( 1076165 ); // Your weapon engraver should be good as new!
                    }
                    else
                    {
                        guildmaster.Say(
                            1076167
                        ); // You need a 100,000 gold and a blue diamond to recharge the weapon engraver.
                    }
                }
                else
                {
                    guildmaster.Say(
                        1076164
                    ); // I can only help with this if you are carrying an engraving tool that needs repair.
                }
            }
            else
            {
                if ( CheckSkill( from ) )
                {
                    if ( diamond != null )
                    {
                        diamond.Consume();

                        if ( Utility.RandomDouble() < from.Skills[SkillName.Tinkering].Value / 100 )
                        {
                            UsesRemaining = 10;
                            from.SendLocalizedMessage( 1076165 ); // Your engraver should be good as new!
                        }
                        else
                        {
                            from.SendLocalizedMessage( 1076175 ); // You cracked the diamond attempting to fix the engraver.
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(
                            1076166
                        ); // You do not have a blue diamond needed to recharge the engraving tool.
                    }
                }
            }
        }
    }

    public override void GetProperties( IPropertyList list )
    {
        base.GetProperties( list );

        if ( _isRewardItem )
        {
            list.Add( VeteranRewardCliloc );
        }

        if ( ShowUsesRemaining )
        {
            list.Add( 1060584, _usesRemaining.ToString() ); // uses remaining: ~1_val~
        }
    }

    private class InternalTarget( BaseEngravingTool? tool ) : Target( 3, true, TargetFlags.None )
    {
        protected override void OnTarget( Mobile from, object targeted )
        {
            if ( tool == null || tool.Deleted )
            {
                return;
            }

            if ( targeted is IEntity entity )
            {
                if ( tool.IsValid( entity, from ) )
                {
                    if ( entity is IEngravable && tool.CheckType( entity ) )
                    {
                        from.CloseGump<InternalGump>();
                        from.SendGump( new InternalGump( tool, entity ) );
                    }
                    else
                    {
                        from.SendLocalizedMessage( tool.CannotEngraveMessage );
                    }
                }
                else
                {
                    from.SendLocalizedMessage( tool.CannotEngraveMessage );
                }
            }
            else
            {
                from.SendLocalizedMessage( tool.CannotEngraveMessage );
            }
        }

        protected override void OnTargetOutOfRange( Mobile from, object targeted )
        {
            from.SendLocalizedMessage( tool.NotAccessibleMessage );
        }
    }

    public class ConfirmGump : Gump
    {
        private readonly Mobile? m_NPC;
        private readonly BaseEngravingTool? Tool;

        public ConfirmGump( BaseEngravingTool? tool, Mobile? npc )
            : base( 200, 200 )
        {
            Tool = tool;
            m_NPC = npc;

            AddPage( 0 );

            AddBackground( 0, 0, 291, 133, 0x13BE );
            AddImageTiled( 5, 5, 280, 100, 0xA40 );

            if ( npc != null )
            {
                AddHtmlLocalized(
                    9,
                    9,
                    272,
                    100,
                    1076169,
                    0x7FFF
                ); // It will cost you 100,000 gold and a blue diamond to recharge your weapon engraver with 10 charges.
                AddHtmlLocalized( 195, 109, 120, 20, 1076172, 0x7FFF ); // Recharge it
            }
            else
            {
                AddHtmlLocalized(
                    9,
                    9,
                    272,
                    100,
                    1076176,
                    0x7FFF
                ); // You will need a blue diamond to repair the tip of the engraver.  A successful repair will give the engraver 10 charges.
                AddHtmlLocalized( 195, 109, 120, 20, 1076177, 0x7FFF ); // Replace the tip.
            }

            AddButton( 160, 107, 0xFB7, 0xFB8, 1 );
            AddButton( 5, 107, 0xFB1, 0xFB2, 0 );
            AddHtmlLocalized( 40, 109, 100, 20, 1060051, 0x7FFF ); // CANCEL
        }

        public override void OnResponse( NetState sender, in RelayInfo info )
        {
            if ( Tool == null || Tool.Deleted )
            {
                return;
            }

            if ( info.ButtonID == 1 )
            {
                Tool.Recharge( sender.Mobile, m_NPC );
            }
        }
    }

    private class InternalGump : Gump
    {
        private readonly IEntity? m_Target;
        private readonly BaseEngravingTool? m_Tool;

        public InternalGump( BaseEngravingTool tool, IEntity target )
            : base( 0, 0 )
        {
            m_Tool = tool;
            m_Target = target;

            AddBackground( 50, 50, 400, 300, 0xA28 );

            AddPage( 0 );

            AddHtmlLocalized( 50, 70, 400, 20, m_Tool.GumpTitle, 0x0 );
            AddHtmlLocalized( 75, 95, 350, 145, 1072360, 0x0, true, true );

            AddButton( 125, 300, 0x81A, 0x81B, 1 );
            AddButton( 320, 300, 0x819, 0x818, 0 );

            AddImageTiled( 75, 245, 350, 40, 0xDB0 );
            AddImageTiled( 76, 245, 350, 2, 0x23C5 );
            AddImageTiled( 75, 245, 2, 40, 0x23C3 );
            AddImageTiled( 75, 285, 350, 2, 0x23C5 );
            AddImageTiled( 425, 245, 2, 42, 0x23C3 );

            AddTextEntry( 78, 246, 343, 37, 0x4FF, 15, "", 78 );
        }

        public override void OnResponse( NetState state, in RelayInfo info )
        {
            if ( m_Tool == null || m_Tool.Deleted || m_Target == null || m_Target.Deleted )
            {
                return;
            }

            var from = state.Mobile;

            if ( info.ButtonID == 1 )
            {
                if ( !m_Tool.IsChildOf( from.Backpack ) )
                {
                    from.SendLocalizedMessage( 1062334 ); // This item must be in your backpack to be used.
                    return;
                }

                if ( !m_Tool.IsValid( m_Target, from ) )
                {
                    from.SendLocalizedMessage( 1072311 ); // The engraving failed.
                    return;
                }

                var relay = info.GetTextEntry( 15 );

                var item = ( IEngravable )m_Target;

                if ( relay != null )
                {
                    if ( relay == null || relay.Equals( "" ) )
                    {
                        if ( item.EngravedText != null )
                        {
                            item.EngravedText = null;
                            from.SendLocalizedMessage( m_Tool.RemoveMessage );
                        }
                        else
                        {
                            from.SendLocalizedMessage( m_Tool.ObjectWasNotMessage );
                        }
                    }
                    else
                    {
                        var text = relay.Length > 40 ? relay[..40] : relay;

                        item.EngravedText = text;

                        from.SendLocalizedMessage( m_Tool.SuccessMessage );

                        m_Tool.UsesRemaining--;

                        if ( m_Tool.UsesRemaining < 1 )
                        {
                            if ( m_Tool.DeletedItem )
                            {
                                m_Tool.Delete();
                                from.SendLocalizedMessage( 1044038 ); // You have worn out your tool!
                            }
                        }
                    }
                }
            }
        }
    }
}

[SerializationGenerator( 0 )]
public partial class LeatherContainerEngraver : BaseEngravingTool
{
    [Constructible]
    public LeatherContainerEngraver()
        : base( 0xF9D, 1 )
    {
    }

    public override int LabelNumber => 1072152; // leather container engraving tool

    public override Type[] Engraves => new[]
    {
        typeof( Pouch ), typeof( Backpack ), typeof( Bag )
    };
}

[SerializationGenerator( 0 )]
public partial class WoodenContainerEngraver : BaseEngravingTool
{
    [Constructible]
    public WoodenContainerEngraver()
        : base( 0x1026, 1 )
    {
    }

    public override int LabelNumber => 1072153; // wooden container engraving tool

    public override Type[] Engraves => new[]
    {
        typeof( WoodenBox ), typeof( LargeCrate ), typeof( MediumCrate ),
        typeof( SmallCrate ), typeof( WoodenChest ), typeof( EmptyBookcase ),
        typeof( Armoire ), typeof( FancyArmoire ), typeof( PlainWoodenChest ),
        typeof( OrnateWoodenChest ), typeof( GildedWoodenChest ), typeof( WoodenFootLocker ),
        typeof( FinishedWoodenChest ), typeof( TallCabinet ), typeof( ShortCabinet ),
        typeof( RedArmoire ), typeof( CherryArmoire ), typeof( MapleArmoire ),
        typeof( ElegantArmoire ), typeof( Keg ), typeof( SimpleElvenArmoire ),
        typeof( DecorativeBox ), typeof( FancyElvenArmoire ), typeof( RarewoodChest ),
        typeof( RewardSign )
    };
}

[SerializationGenerator( 0 )]
public partial class MetalContainerEngraver : BaseEngravingTool
{
    [Constructible]
    public MetalContainerEngraver()
        : base( 0x1EB8, 1 )
    {
    }

    public override int LabelNumber => 1072154; // metal container engraving tool

    public override Type[] Engraves =>
    [
        typeof( ParagonChest ), typeof( MetalChest ), typeof( MetalGoldenChest ), typeof( MetalBox )
    ];
}

[SerializationGenerator( 0 )]
public partial class FoodEngraver : BaseEngravingTool
{
    [Constructible]
    public FoodEngraver()
        : base( 0x1BD1, 1 )
    {
    }

    public override int LabelNumber => 1072951; // food decoration tool

    public override Type[] Engraves => new[]
    {
        typeof( Cake ), typeof( CheesePizza ), typeof( SausagePizza ),
        typeof( Cookies )
    };
}

[SerializationGenerator( 0 )]
public partial class SpellbookEngraver : BaseEngravingTool
{
    [Constructible]
    public SpellbookEngraver()
        : base( 0xFBF, 1 )
    {
    }

    public override int LabelNumber => 1072151; // spellbook engraving tool

    public override Type[] Engraves => new[]
    {
        typeof( Spellbook )
    };
}

[SerializationGenerator( 0 )]
public partial class StatuetteEngravingTool : BaseEngravingTool
{
    [Constructible]
    public StatuetteEngravingTool()
        : base( 0x12B3, 10 ) =>
        Hue = 0;

    public override int LabelNumber => 1080201; // Statuette Engraving Tool

    public override Type[] Engraves => new[]
    {
        typeof( MonsterStatuette )
    };
}

[SerializationGenerator( 0 )]
public partial class ArmorEngravingTool : BaseEngravingTool
{
    [Constructible]
    public ArmorEngravingTool()
        : base( 0x32F8, 30 ) =>
        Hue = 0x490;

    public override int LabelNumber => 1080547; // Armor Engraving Tool

    public override int GumpTitle => 1071163; // <center>Armor Engraving Tool</center>

    public override Type[] Engraves => new[] { typeof( BaseArmor ) };

    public override bool CheckType( IEntity entity )
    {
        var check = base.CheckType( entity );

        if ( check && entity.GetType().IsSubclassOf( typeof( BaseShield ) ) )
        {
            check = false;
        }

        return check;
    }
}

[SerializationGenerator( 0 )]
public partial class ShieldEngravingTool : BaseEngravingTool
{
    [Constructible]
    public ShieldEngravingTool()
        : base( 0x1EB8, 10 ) =>
        Hue = 1165;

    public override int LabelNumber => 1159004; // Shield Engraving Tool

    public override bool DeletedItem => false;

    public override int LowSkillMessage =>
        1076178; // // Your tinkering skill is too low to fix this yourself.  An NPC tinkerer can help you repair this for a fee.

    public override int VeteranRewardCliloc => 0;

    public override Type[] Engraves => new[]
    {
        typeof( BaseShield )
    };
}
