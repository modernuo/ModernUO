namespace Server.Items
{
    [Serializable(0)]
    public partial class SmokeHide : BaseStaffHide
    {
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _effectHue = 1149;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _effectSound = 0x228;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _effectId = 0x3728;

        public override string DefaultName => "Smoke Hide";

        public override bool CastHide => true;

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;

            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y, from.Z + 4), from.Map, _effectId, 13, 1, _effectHue, 4);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y, from.Z), from.Map, _effectId, 13, 1, _effectHue, 4);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y, from.Z - 4), from.Map, _effectId, 13, 1, _effectHue, 4);
            Effects.SendLocationEffect(new Point3D(from.X, from.Y + 1, from.Z + 4), from.Map, _effectId, 13, 1, _effectHue, 4);
            Effects.SendLocationEffect(new Point3D(from.X, from.Y + 1, from.Z), from.Map, _effectId, 13, 1, _effectHue, 4);
            Effects.SendLocationEffect(new Point3D(from.X, from.Y + 1, from.Z - 4), from.Map, _effectId, 13, 1, _effectHue, 4);

            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y + 1, from.Z + 11), from.Map, _effectId, 13, 1, _effectHue, 4);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y + 1, from.Z + 7), from.Map, _effectId, 13, 1, _effectHue, 4);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y + 1, from.Z + 3), from.Map, _effectId, 13, 1, _effectHue, 4);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y + 1, from.Z - 1), from.Map, _effectId, 13, 1, _effectHue, 4);

            Effects.PlaySound(from, _effectSound);
        }

        [Constructible]
        public SmokeHide() : base(2119)
        {
        }
    }
}
