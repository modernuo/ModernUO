namespace Server.Items
{
    /*
    first seen halloween 2009.  subsequently in 2010,
    2011 and 2012. GM Beggar-only Semi-Rare Treats
    */
    [Serializable(0, false)]
    public partial class CreepyCake : Food
    {
        [Constructible]
        public CreepyCake() : base(0x9e9) => Hue = 0x3E4;

        public override string DefaultName => "Creepy Cake";
    }
}
