namespace Server.Items
{
    [Serializable(1, false)]
    public partial class VacationWafer : Item
    {
        public const int VacationDays = 7;

        [Constructible]
        public VacationWafer() : base(0x973)
        {
        }

        public override int LabelNumber => 1074431; // An aquarium flake sphere

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1074432, VacationDays.ToString()); // Vacation days: ~1_DAYS~
        }

        private void Deserialize(IGenericReader reader, int version)
        {
        }
    }
}
