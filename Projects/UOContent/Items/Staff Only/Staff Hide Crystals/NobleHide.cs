namespace Server.Items
{
    [Serializable(0)]
    public partial class NobleHide : BaseStaffHide
    {
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _effectHue = 5;

        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private int _effectSound = 0x244;

        public override string DefaultName => "Noble Hide";

        public override bool CastHide => false;

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;

            if (from.Hidden)
            {
                Effects.SendLocationParticles(from, 0x3709, 1, 30, _effectHue, 7, 9965, 0);
                Effects.SendLocationParticles(from, 0x376A, 1, 30, _effectHue, 3, 9502, 0);
            }
            else
            {
                from.FixedParticles(0x3709, 1, 30, 9965, _effectHue, 7, EffectLayer.Waist);
                from.FixedParticles(0x376A, 1, 30, 9502, _effectHue, 3, (EffectLayer)255);
            }

            Effects.PlaySound(from, _effectSound);
        }

        [Constructible]
        public NobleHide() : base(2119)
        {
        }
    }
}
