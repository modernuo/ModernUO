namespace Server.Items
{
    [Serializable(0)]
    public partial class ShinyHide : BaseStaffHide
    {
        public override string DefaultName => "Shiny Hide";

        public override bool CastHide => false;

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;

            if (from.Hidden)
            {
                Effects.SendLocationParticles(from, 0x375A, 1, 30, 33, 2, 9966, 0);
                Effects.SendLocationParticles(from, 0x37B9, 1, 30, 43, 3, 9502, 0);
            }
            else
            {
                from.FixedParticles(0x375A, 1, 30, 9966, 33, 2, EffectLayer.Waist);
                from.FixedParticles(0x37B9, 1, 30, 9502, 43, 3, (EffectLayer)255);
            }

            Effects.PlaySound(from, 0x0F5);
            Effects.PlaySound(from, 0x1ED);
        }

        [Constructible]
        public ShinyHide() : base(1150)
        {
        }
    }
}
