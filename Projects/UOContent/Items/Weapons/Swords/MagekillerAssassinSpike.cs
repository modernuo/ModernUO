namespace Server.Items
{
    [Serializable(0)]
    public partial class MagekillerAssassinSpike : AssassinSpike
    {
        [Constructible]
        public MagekillerAssassinSpike() => WeaponAttributes.HitLeechMana = 16;

        public override int LabelNumber => 1073519; // magekiller assassin spike
    }
}
