using ModernUO.Serialization;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class CitadelKey : MasterKey
{
    [Constructible]
    public CitadelKey() : base( 0x1012 ) => Hue = 0x489;

    public override int LabelNumber => 1074344; // black order key

    public override bool CanOfferConfirmation( Mobile from )
    {
        if ( from.Region != null && from.Region.IsPartOf( "The Citadel" ) )
        {
            return base.CanOfferConfirmation( from );
        }

        return false;
    }
}
