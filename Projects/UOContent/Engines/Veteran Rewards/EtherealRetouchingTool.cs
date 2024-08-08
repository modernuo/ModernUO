using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator( 0, false )]
public partial class EtherealRetouchingTool : Item, IRewardItem
{
    [Constructible]
    public EtherealRetouchingTool()
        : base( 0x42C6 ) =>
        LootType = LootType.Blessed;

    public EtherealRetouchingTool( Serial serial )
        : base( serial )
    {
    }

    public override int LabelNumber => 1113814; // Retouching Tool

    [SerializableField(0)]
    private bool _isRewardItem;

    public override void GetProperties( IPropertyList list )
    {
        base.GetProperties( list );

        if ( IsRewardItem )
        {
            list.Add( 1080458 ); // 11th Year Veteran Reward
        }
    }

    public override void OnDoubleClick( Mobile from )
    {
        if ( IsChildOf( from.Backpack ) )
        {
            from.Target = new InternalTarget( this );
            from.SendLocalizedMessage( 1113815 ); // Target the ethereal mount you wish to retouch.
        }
        else
        {
            from.SendLocalizedMessage( 1042010 ); // You must have the object in your backpack to use it.
        }
    }

    public static void AddProperty( EtherealMount mount, IPropertyList list )
    {
        list.AddLocalized( 1113818, mount.Transparent ? 1078520 : 1153298 );
    }

    private class InternalTarget( EtherealRetouchingTool tool ) : Target( -1, false, TargetFlags.None )
    {
        protected override void OnTarget( Mobile from, object targeted )
        {
            if ( !tool.IsChildOf( from.Backpack ) )
            {
                from.SendLocalizedMessage( 1042001 ); // That must be in your pack for you to use it.
            }
            else if ( targeted is EtherealMount )
            {
                var mount = targeted as EtherealMount;

                if ( !mount.IsChildOf( from.Backpack ) )
                {
                    from.SendLocalizedMessage( 1045158 ); // You must have the item in your backpack to target it.
                }
                else if ( RewardSystem.CheckIsUsableBy( from, tool ) )
                {
                    if ( mount.Transparent )
                    {
                        from.SendLocalizedMessage( 1113816 ); // Your ethereal mount's body has been solidified.
                    }
                    else
                    {
                        from.SendLocalizedMessage( 1113817 ); // Your ethereal mount's transparency has been restored.
                    }

                    mount.Transparent = mount.Transparent ? false : true;
                    mount.InvalidateProperties();
                }
            }
            else
            {
                from.SendLocalizedMessage( 1046439 ); // That is not a valid target.
            }
        }
    }
}
