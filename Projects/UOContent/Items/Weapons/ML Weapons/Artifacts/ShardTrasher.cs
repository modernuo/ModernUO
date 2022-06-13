using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0)]
    public partial class ShardThrasher : DiamondMace
    {
        [Constructible]
        public ShardThrasher()
        {
            Hue = 0x4F2;

            WeaponAttributes.HitPhysicalArea = 30;
            Attributes.BonusStam = 8;
            Attributes.AttackChance = 10;
            Attributes.WeaponSpeed = 35;
            Attributes.WeaponDamage = 40;
        }

        public override int LabelNumber => 1072918; // Shard Thrasher
    }
}
