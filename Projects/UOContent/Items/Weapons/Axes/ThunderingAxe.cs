namespace Server.Items
{
    [Serializable(0)]
    public partial class ThunderingAxe : OrnateAxe
    {
        [Constructible]
        public ThunderingAxe() => WeaponAttributes.HitLightning = 10;

        public override int LabelNumber => 1073547; // thundering axe
    }
}
