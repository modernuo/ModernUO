using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public abstract partial class PromotionalToken : Item
{
    public PromotionalToken() : base(0x2AAA)
    {
        LootType = LootType.Blessed;
        Light = LightType.Circle300;
        Weight = 5.0;
    }

    public abstract TextDefinition ItemName { get; }
    public abstract TextDefinition ItemReceiveMessage { get; }
    public abstract TextDefinition ItemGumpName { get; }

    public override int LabelNumber => 1070997; // A promotional token
    public abstract Item CreateItemFor(Mobile from);

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (ItemName != null)
        {
            if (ItemName.Number > 0)
            {
                list.Add(1070998, ItemName.Number); // Use this to redeem<br>your ~1_PROMO~
            }
            else
            {
                list.Add(1070998, ItemName.String); // Use this to redeem<br>your ~1_PROMO~
            }
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
        else
        {
            from.CloseGump<PromotionalTokenGump>();
            from.SendGump(new PromotionalTokenGump(this));
        }
    }

    public override void OnRemoved(IEntity parent)
    {
        Mobile m = null;

        if (parent is Item item)
        {
            m = item.RootParent as Mobile;
        }
        else if (parent is Mobile mobile)
        {
            m = mobile;
        }

        m?.CloseGump<PromotionalTokenGump>();
    }

    private class PromotionalTokenGump : Gump
    {
        private readonly PromotionalToken m_Token;

        public PromotionalTokenGump(PromotionalToken token) : base(10, 10)
        {
            m_Token = token;

            AddPage(0);

            AddBackground(0, 0, 240, 135, 0x2422);
            // Click "OKAY" to redeem the following promotional item:
            AddHtmlLocalized(15, 15, 210, 75, 1070972, 0x0, true);
            m_Token.ItemGumpName.AddHtmlText(this, 15, 60, 210, 75, false, false);

            AddButton(160, 95, 0xF7, 0xF8, 1); // Okay
            AddButton(90, 95, 0xF2, 0xF1, 0);  // Cancel
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID != 1)
            {
                return;
            }

            var from = sender.Mobile;

            if (!m_Token.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            }
            else
            {
                var i = m_Token.CreateItemFor(from);

                if (i != null)
                {
                    from.BankBox.AddItem(i);
                    m_Token.ItemReceiveMessage.SendMessageTo(from);
                    m_Token.Delete();
                }
            }
        }
    }
}

[SerializationGenerator(0, false)]
public partial class SoulstoneFragmentToken : PromotionalToken
{
    [Constructible]
    public SoulstoneFragmentToken()
    {
    }

    public override TextDefinition ItemGumpName { get; } = 1070999; // <center>Soulstone Fragment</center>
    public override TextDefinition ItemName { get; } = 1071000;     // soulstone fragment

    // A soulstone fragment has been created in your bank box.
    public override TextDefinition ItemReceiveMessage { get; } = 1070976;

    public override Item CreateItemFor(Mobile from) =>
        from?.Account != null ? new SoulstoneFragment(from.Account.ToString()) : null;
}
