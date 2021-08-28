namespace Server.Items
{
    [Serializable(0)]
    public partial class ThunderHide : BaseStaffHide
    {
        public override string DefaultName => "Lighting Bolt Hide";

        public override bool CastHide => false;

        public override void HideEffects(Mobile from)
        {
            from.Hidden = !from.Hidden;
            var entity = new Entity(from.Serial, from.Location, from.Map);
            Effects.SendBoltEffect(entity);
        }

        [Constructible]
        public ThunderHide() : base(1153)
        {
        }
    }
}
