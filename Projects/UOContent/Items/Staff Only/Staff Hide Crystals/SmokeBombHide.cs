namespace Server.Items
{
    [Serializable(0)]
    public partial class SmokeBombHide : BaseStaffHide
    {
        public override string DefaultName => "Smoke Bomb Hide";

        public override bool CastHide => false;

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;

            if (from.Hidden)
            {
                Effects.SendLocationParticles(from, 0x3709, 1, 30, 1108, 6, 9904, 0);
            }
            else
            {
                from.FixedParticles(0x3709, 1, 30, 9904, 1108, 6, EffectLayer.RightFoot);
            }

            Effects.PlaySound(from, 0x22F);
        }

        [Constructible]
        public SmokeBombHide() : base(1175)
        {
        }
    }
}
