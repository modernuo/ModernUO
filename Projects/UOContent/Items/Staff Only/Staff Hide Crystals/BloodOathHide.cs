namespace Server.Items
{
    [Serializable(0)]
    public partial class BloodOathHide : BaseStaffHide
    {
        public override bool CastHide => false;

        public override string DefaultName => "Blood Oath Hide";

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;

            if (from.Hidden)
            {
                Effects.SendLocationParticles(from, 0x375A, 1, 17, 33, 7, 9919, 0);
                Effects.SendLocationParticles(from, 0x3728, 1, 13, 33, 7, 9502, 0);
            }
            else
            {
                from.FixedParticles(0x375A, 1, 17, 9919, 33, 7, EffectLayer.Waist);
                from.FixedParticles(0x3728, 1, 13, 9502, 33, 7, (EffectLayer)255);
            }

            Effects.PlaySound(from, 0x175);
            OnEndHideEffects(from);
        }

        [Constructible]
        public BloodOathHide() : base(2117)
        {
        }
    }
}
