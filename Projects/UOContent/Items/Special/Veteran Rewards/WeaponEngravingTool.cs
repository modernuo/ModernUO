using ModernUO.Serialization;
using Server.Engines.VeteranRewards;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class WeaponEngravingTool : Item, IUsesRemaining, IRewardItem
{
    [InvalidateProperties]
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private int _usesRemaining;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private bool _isRewardItem;

    [Constructible]
    public WeaponEngravingTool(int uses = 10) : base(0x32F8)
    {
        LootType = LootType.Blessed;
        Weight = 1.0;

        _usesRemaining = uses;
    }

    public override int LabelNumber => 1076158; // Weapon Engraving Tool

    public bool ShowUsesRemaining
    {
        get => true;
        set { }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_isRewardItem && !RewardSystem.CheckIsUsableBy(from, this))
        {
            return;
        }

        if (_usesRemaining > 0)
        {
            from.SendLocalizedMessage(1072357); // Select an object to engrave.
            from.Target = new TargetWeapon(this);
            return;
        }

        if (from.Skills.Tinkering.Value == 0)
        {
            // Since you have no tinkering skill, you will need to find an NPC tinkerer to repair this for you.
            from.SendLocalizedMessage(1076179);
            return;
        }

        if (from.Skills.Tinkering.Value < 75.0)
        {
            // Your tinkering skill is too low to fix this yourself.  An NPC tinkerer can help you repair this for a fee.
            from.SendLocalizedMessage(1076178);
            return;
        }

        if (from.Backpack.FindItemByType<BlueDiamond>() == null)
        {
            // You do not have a blue diamond needed to recharge the engraving tool.
            from.SendLocalizedMessage(1076166);
            return;
        }

        from.SendLocalizedMessage(1076163); // There are no charges left on this engraving tool.
        from.SendGump(new ConfirmGump(this, null));
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        if (_isRewardItem)
        {
            list.Add(1076224); // 8th Year Veteran Reward
        }

        if (ShowUsesRemaining)
        {
            list.Add(1060584, _usesRemaining); // uses remaining: ~1_val~
        }
    }

    public virtual void Recharge(Mobile from, Mobile guildmaster)
    {
        if (from.Backpack == null)
        {
            return;
        }

        var diamond = from.Backpack.FindItemByType<BlueDiamond>();

        if (guildmaster != null)
        {
            if (_usesRemaining > 0)
            {
                // I can only help with this if you are carrying an engraving tool that needs repair.s
                guildmaster.Say(1076164);
            }
            else if (diamond != null && Banker.Withdraw(from, 100000))
            {
                diamond.Consume();
                UsesRemaining = 10;
                guildmaster.Say(1076165); // Your weapon engraver should be good as new!
            }
            else
            {
                // You need a 100,000 gold and a blue diamond to recharge the weapon engraver.
                guildmaster.Say(1076167);
            }

            return;
        }

        if (from.Skills.Tinkering.Value == 0)
        {
            // Since you have no tinkering skill, you will need to find an NPC tinkerer to repair this for you.
            from.SendLocalizedMessage(1076179);
            return;
        }

        if (from.Skills.Tinkering.Value < 75.0)
        {
            // Your tinkering skill is too low to fix this yourself.  An NPC tinkerer can help you repair this for a fee.
            from.SendLocalizedMessage(1076178);
            return;
        }

        if (diamond == null)
        {
            // You do not have a blue diamond needed to recharge the engraving tool.
            from.SendLocalizedMessage(1076166);
            return;
        }

        diamond.Consume();

        if (Utility.RandomDouble() < from.Skills.Tinkering.Value / 100)
        {
            UsesRemaining = 10;
            from.SendLocalizedMessage(1076165); // Your weapon engraver should be good as new! ?????
        }
        else
        {
            // You cracked the diamond attempting to fix the weapon engraver.
            from.SendLocalizedMessage(1076175);
        }
    }

    public static WeaponEngravingTool Find(Mobile from) => from.Backpack?.FindItemByType<WeaponEngravingTool>();

    private class TargetWeapon : Target
    {
        private readonly WeaponEngravingTool _tool;

        public TargetWeapon(WeaponEngravingTool tool) : base(-1, true, TargetFlags.None) => _tool = tool;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_tool?.Deleted != false)
            {
                return;
            }

            if (targeted is BaseWeapon item)
            {
                from.CloseGump<InternalGump>();
                from.SendGump(new InternalGump(_tool, item));
            }
            else
            {
                from.SendLocalizedMessage(1072309); // The selected item cannot be engraved by this engraving tool.
            }
        }
    }

    private class InternalGump : Gump
    {
        private readonly BaseWeapon _target;
        private readonly WeaponEngravingTool _tool;

        public InternalGump(WeaponEngravingTool tool, BaseWeapon target) : base(0, 0)
        {
            _tool = tool;
            _target = target;

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddBackground(50, 50, 400, 300, 0xA28);

            AddPage(0);

            AddHtmlLocalized(50, 70, 400, 20, 1072359, 0x0); // <CENTER>Engraving Tool</CENTER>
            // Please enter the text to add to the selected object. Leave the text area blank to remove any existing text.  Removing text does not use a charge.
            AddHtmlLocalized(75, 95, 350, 145, 1076229, 0x0, true, true);
            AddButton(125, 300, 0x81A, 0x81B, (int)Buttons.Okay);
            AddButton(320, 300, 0x819, 0x818, (int)Buttons.Cancel);
            AddImageTiled(75, 245, 350, 40, 0xDB0);
            AddImageTiled(76, 245, 350, 2, 0x23C5);
            AddImageTiled(75, 245, 2, 40, 0x23C3);
            AddImageTiled(75, 285, 350, 2, 0x23C5);
            AddImageTiled(425, 245, 2, 42, 0x23C3);

            AddTextEntry(75, 245, 350, 40, 0x0, (int)Buttons.Text, "");
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (_tool?.Deleted != false || _target?.Deleted != false)
            {
                return;
            }

            if (info.ButtonID != (int)Buttons.Okay)
            {
                state.Mobile.SendLocalizedMessage(1072363); // The object was not engraved.
                return;
            }

            var relay = info.GetTextEntry((int)Buttons.Text);

            if (relay == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(relay.Text))
            {
                _target.EngravedText = null;
                state.Mobile.SendLocalizedMessage(1072362); // You remove the engraving from the object.
            }
            else
            {
                _target.EngravedText =
                    Utility.FixHtml(relay.Text.Length > 64 ? relay.Text[..64] : relay.Text);
                state.Mobile.SendLocalizedMessage(1072361); // You engraved the object.
                _target.InvalidateProperties();
                _tool.UsesRemaining -= 1;
                _tool.InvalidateProperties();
            }
        }

        private enum Buttons
        {
            Cancel,
            Okay,
            Text
        }
    }

    public class ConfirmGump : Gump
    {
        private readonly WeaponEngravingTool _engraver;
        private readonly Mobile _guildmaster;

        public ConfirmGump(WeaponEngravingTool engraver, Mobile guildmaster) : base(200, 200)
        {
            _engraver = engraver;
            _guildmaster = guildmaster;

            Closable = false;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(0, 0, 291, 133, 0x13BE);
            AddImageTiled(5, 5, 280, 100, 0xA40);

            if (guildmaster != null)
            {
                // It will cost you 100,000 gold and a blue diamond to recharge your weapon engraver with 10 charges.
                AddHtmlLocalized(9, 9, 272, 100, 1076169, 0x7FFF);
                AddHtmlLocalized(195, 109, 120, 20, 1076172, 0x7FFF); // Recharge it
            }
            else
            {
                // You will need a blue diamond to repair the tip of the engraver.  A successful repair will give the engraver 10 charges.
                AddHtmlLocalized(9, 9, 272, 100, 1076176, 0x7FFF);
                AddHtmlLocalized(195, 109, 120, 20, 1076177, 0x7FFF); // Replace the tip.
            }

            AddButton(160, 107, 0xFB7, 0xFB8, (int)Buttons.Confirm);
            AddButton(5, 107, 0xFB1, 0xFB2, (int)Buttons.Cancel);
            AddHtmlLocalized(40, 109, 100, 20, 1060051, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (_engraver?.Deleted != false || info.ButtonID != (int)Buttons.Confirm)
            {
                return;
            }

            _engraver.Recharge(state.Mobile, _guildmaster);
        }

        private enum Buttons
        {
            Cancel,
            Confirm
        }
    }
}
