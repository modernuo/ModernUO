namespace Server.Items
{
    [Serializable(0)]
    public partial class ExplosionHide : BaseStaffHide
    {
        public override bool CastHide => false;

        public override string DefaultName => "Explosion Hide";

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;
            if (from.Hidden)
            {
                Effects.SendLocationParticles(from, 0x36BD, 20, 10, 5044);
            }
            else
            {
                from.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Waist);
            }

            Effects.PlaySound(from, 0x307);
            OnEndHideEffects(from);
        }

        [Constructible]
        public ExplosionHide() : base(2113)
        {
        }
    }
}
