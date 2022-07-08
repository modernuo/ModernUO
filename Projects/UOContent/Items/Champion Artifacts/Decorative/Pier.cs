using ModernUO.Serialization;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Pier : Item
    {
        /*
         * This does not make a lot of sense, being a "Pier"
         * and having possible itemids that have nothing
         * to do with piers. The three items here are basically
         * permutations of the same "drop", or item that
         * will be randomly selected when the item drops.
         *
         * It was either this, or make 2
         * new classes named to reflect that they are rocks
         * in water, or put them all in one class. Either
         * is kind of senseless, so it is what it is.
         *
         */
        private static readonly int[] ItemIds = { 0x3486, 0x348b, 0x3ae };

        [Constructible]
        public Pier() : base(ItemIds[Utility.Random(3)])
        {
        }

        public Pier(int itemid)
            : base(itemid)
        {
        }
    }
}
