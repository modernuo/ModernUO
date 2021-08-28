namespace Server.Items
{
    [Serializable(0)]
    public partial class PoisonHide : BaseStaffHide
    {
        public override string DefaultName => "Poison Hide";

        public override bool CastHide => false;

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;
            if (from.Hidden)
            {
                Effects.SendLocationParticles(from, 0x36CB, 1, 9, 67, 5, 9911, 0);
                Effects.SendLocationParticles(from, 0x374A, 1, 17, 1108, 4, 9502, 0);
            }
            else
            {
                from.FixedParticles(0x36CB, 1, 9, 9911, 67, 5, EffectLayer.Waist);
                from.FixedParticles(0x374A, 1, 17, 9502, 1108, 4, (EffectLayer)255);
            }

            Effects.PlaySound(from, 0x22F);
        }

        [Constructible]
        public PoisonHide() : base(1268)
        {
        }
    }
}
