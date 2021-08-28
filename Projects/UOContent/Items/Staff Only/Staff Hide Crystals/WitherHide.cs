namespace Server.Items
{
    [Serializable(0)]
    public partial class WitherHide : BaseStaffHide
    {
        public override string DefaultName => "Gate Hide";

        public override bool CastHide => false;

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;

            if (from.Hidden)
            {
                Effects.SendLocationParticles(from, 0x37CC, 1, 40, 3, 9917, 97, 0);
                Effects.SendLocationParticles(from, 0x374A, 1, 15, 97, 3, 9502, 0);
            }
            else
            {
                from.FixedParticles(0x37CC, 1, 40, 97, 3, 9917, EffectLayer.Waist);
                from.FixedParticles(0x374A, 1, 15, 9502, 97, 3, (EffectLayer)255);
            }

            Effects.PlaySound(from, 0x10B);
        }

        [Constructible]
        public WitherHide() : base(1152)
        {
        }
    }
}
