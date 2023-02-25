using ModernUO.Serialization;
using Server.HuePickers;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Dyes : Item
{
    // TODO: Add uses remaining

    [Constructible]
    public Dyes() : base(0xFA9) => Weight = 3.0;

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(500856); // Select the dye tub to use the dyes on.
        from.Target = new InternalTarget();
    }

    private class InternalTarget : Target
    {
        public InternalTarget() : base(1, false, TargetFlags.None)
        {
        }

        public virtual void SetTubHue(Mobile from, DyeTub tub, int hue)
        {
            tub.DyedHue = hue;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is DyeTub tub)
            {
                if (tub.Redyable)
                {
                    if (tub.MetallicHues) /* OSI has three metallic tubs now */
                    {
                        from.SendGump(new MetallicHuePicker<DyeTub>(from, SetTubHue, tub));
                    }
                    else if (tub.CustomHuePicker != null)
                    {
                        from.SendGump(new CustomHuePickerGump<DyeTub>(from, tub.CustomHuePicker, SetTubHue, tub));
                    }
                    else
                    {
                        from.SendHuePicker(new InternalPicker(tub));
                    }
                }
                else if (tub is BlackDyeTub)
                {
                    from.SendLocalizedMessage(1010092); // You can not use this on a black dye tub.
                }
                else
                {
                    from.SendMessage("That dye tub may not be redyed.");
                }
            }
            else
            {
                from.SendLocalizedMessage(500857); // Use this on a dye tub.
            }
        }

        private class InternalPicker : HuePicker
        {
            private readonly DyeTub m_Tub;

            public InternalPicker(DyeTub tub) : base(tub.ItemID) => m_Tub = tub;

            public override void OnResponse(int hue)
            {
                m_Tub.DyedHue = hue;
            }
        }
    }
}
