using ModernUO.Serialization;

namespace Server.Items.Holiday
{
    [SerializationGenerator(0, false)]
    [TypeAlias("Server.Items.ClownMask", "Server.Items.DaemonMask", "Server.Items.PlagueMask")]
    public partial class BasePaintedMask : Item
    {
        private static readonly string[] Staffers =
        {
            "Owyn",
            "Luthius",
            "Kamron",
            "Jaedan",
            "Vorspire"
        };

        [InternString]
        [SerializableField(0, setter: "private")]
        private string _staffer;

        public BasePaintedMask(int itemid) : this(Staffers.RandomElement(), itemid)
        {
        }

        public BasePaintedMask(string staffer, int itemid) : base(itemid + Utility.Random(2)) =>
            _staffer = Utility.Intern(staffer);

        public override string DefaultName => _staffer != null ? $"{MaskName} hand painted by {_staffer}" : MaskName;

        public virtual string MaskName => "A Mask";
    }
}
