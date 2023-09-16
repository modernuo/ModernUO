using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RedLeaves : Item
{
    [Constructible]
    public RedLeaves(int amount = 1) : base(0x1E85)
    {
        Stackable = true;
        Hue = 0x21;
        Amount = amount;
    }

    public override int LabelNumber => 1053123; // red leaves

    public override double DefaultWeight => 0.1;

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            return;
        }

        from.Target = new InternalTarget(this);
        from.SendLocalizedMessage(1061907); // Choose a book you wish to seal with the wax from the red leaf.
    }

    private class InternalTarget : Target
    {
        private readonly RedLeaves _redLeaves;

        public InternalTarget(RedLeaves redLeaves) : base(3, false, TargetFlags.None) => _redLeaves = redLeaves;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_redLeaves.Deleted)
            {
                return;
            }

            if (!_redLeaves.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
                return;
            }

            if (targeted is not Item item || !item.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042664); // You must have the object in your backpack to use it.
            }
            else if (item is not BaseBook book)
            {
                item.LabelTo(from, 1061911); // You can only use red leaves to seal the ink into book pages!
            }
            else if (!book.Writable)
            {
                book.LabelTo(from, 1061909); // The ink in this book has already been sealed.
            }
            else
            {
                _redLeaves.Consume();
                book.Writable = false;

                book.LabelTo(from, 1061910); // You seal the ink to the page using wax from the red leaf.
            }
        }
    }
}
