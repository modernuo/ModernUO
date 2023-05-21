using System;
using ModernUO.Serialization;
using Server.Factions;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.Ninjitsu;
using Server.Spells.Seventh;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class TribalPaint : Item
{
    [Constructible]
    public TribalPaint() : base(0x9EC)
    {
        Hue = 2101;
        Weight = 2.0;
        Stackable = Core.ML;
    }

    public override int LabelNumber => 1040000; // savage kin paint

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return;
        }

        if (Sigil.ExistsOn(from))
        {
            from.SendLocalizedMessage(1010465); // You cannot disguise yourself while holding a sigil.
        }
        else if (!from.CanBeginAction<IncognitoSpell>())
        {
            from.SendLocalizedMessage(501698); // You cannot disguise yourself while incognitoed.
        }
        else if (!from.CanBeginAction<PolymorphSpell>())
        {
            from.SendLocalizedMessage(501699); // You cannot disguise yourself while polymorphed.
        }
        else if (TransformationSpellHelper.UnderTransformation(from))
        {
            from.SendLocalizedMessage(501699); // You cannot disguise yourself while polymorphed.
        }
        else if (AnimalForm.UnderTransformation(from))
        {
            from.SendLocalizedMessage(1061634); // You cannot disguise yourself while in that form.
        }
        else if (from.IsBodyMod || from.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
        {
            from.SendLocalizedMessage(501605); // You are already disguised.
        }
        else
        {
            from.BodyMod = from.Female ? 184 : 183;
            from.HueMod = 0;

            if (from is PlayerMobile mobile)
            {
                mobile.SavagePaintExpiration = TimeSpan.FromDays(7.0);
            }

            // You now bear the markings of the savage tribe.  Your body paint will last about a week or you can remove it with an oil cloth.
            from.SendLocalizedMessage(1042537);

            Consume();
        }
    }
}
