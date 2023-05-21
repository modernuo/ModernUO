using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

public interface TranslocationItem
{
    int Charges { get; set; }
    int Recharges { get; set; }
    int MaxCharges { get; }
    int MaxRecharges { get; }
    TextDefinition TranslocationItemName { get; }
}

[SerializationGenerator(0)]
public partial class PowderOfTranslocation : Item
{
    [Constructible]
    public PowderOfTranslocation(int amount = 1) : base(0x26B8)
    {
        Stackable = true;
        Weight = 0.1;
        Amount = amount;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 2))
        {
            from.Target = new InternalTarget(this);
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
    }

    private class InternalTarget : Target
    {
        private readonly PowderOfTranslocation _powder;

        public InternalTarget(PowderOfTranslocation powder) : base(-1, false, TargetFlags.None) => _powder = powder;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_powder.Deleted)
            {
                return;
            }

            if (!from.InRange(_powder.GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (targeted is TranslocationItem transItem)
            {
                if (transItem.Charges >= transItem.MaxCharges)
                {
                    // This item cannot absorb any more powder of translocation.
                    _powder.SendLocalizedMessageTo(from, 1054137, 0x59);
                }
                else if (transItem.Recharges >= transItem.MaxRecharges)
                {
                    // This item has been oversaturated with powder of translocation and can no longer be recharged.
                    _powder.SendLocalizedMessageTo(from, 1054138, 0x59);
                }
                else
                {
                    if (transItem.Charges + _powder.Amount > transItem.MaxCharges)
                    {
                        var delta = transItem.MaxCharges - transItem.Charges;

                        _powder.Amount -= delta;
                        transItem.Charges = transItem.MaxCharges;
                        transItem.Recharges += delta;
                    }
                    else
                    {
                        transItem.Charges += _powder.Amount;
                        transItem.Recharges += _powder.Amount;
                        _powder.Delete();
                    }

                    if (transItem is Item item)
                    {
                        var _transItemName = transItem.TranslocationItemName;
                        // The ~1_translocationItem~ glows with green energy and absorbs magical power from the powder.
                        if (_transItemName.Number > 0)
                        {
                            item.SendLocalizedMessageTo(from, 1054139, $"#{_transItemName.Number}", 0x43);
                        }
                        else if (_transItemName.String != null)
                        {
                            item.SendLocalizedMessageTo(from, 1054139, _transItemName.String, 0x43);
                        }
                    }
                }
            }
            else
            {
                // Powder of translocation has no effect on this item.
                _powder.SendLocalizedMessageTo(from, 1054140, 0x59);
            }
        }
    }
}
