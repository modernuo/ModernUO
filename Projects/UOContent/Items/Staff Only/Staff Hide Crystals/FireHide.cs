namespace Server.Items
{
    [Serializable(0)]
    public partial class FireHide : BaseStaffHide
    {
        public override bool CastHide => false;

        public override string DefaultName => "Fire Hide";

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;
            if (from.Hidden)
            {
                Effects.SendLocationParticles(from, 0x3709, 1, 30, 0, 7, 9965, 0);
            }
            else
            {
                from.FixedParticles(0x3709, 1, 30, 9965, 0, 7, EffectLayer.Waist);
            }

            Effects.PlaySound(from, 0x225);
            OnEndHideEffects(from);
        }

        [Constructible]
        public FireHide() : base(1161)
        {
        }
    }
}
