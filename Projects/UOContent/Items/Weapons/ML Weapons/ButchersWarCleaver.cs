namespace Server.Items
{
    [Serializable(0)]
    public partial class ButchersWarCleaver : WarCleaver
    {
        [Constructible]
        public ButchersWarCleaver()
        {
        }

        public override int LabelNumber => 1073526; // butcher's war cleaver

        public override void AppendChildNameProperties(ObjectPropertyList list)
        {
            base.AppendChildNameProperties(list);

            list.Add(1072512); // Bovine Slayer
        }
    }
}
