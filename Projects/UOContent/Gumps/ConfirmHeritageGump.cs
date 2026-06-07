using System;
using Server.Items;
using Server.Logging;
using Server.Network;

namespace Server.Gumps
{
    public class ConfirmHeritageGump : DynamicGump
    {
        private static readonly ILogger _logger = LogFactory.GetLogger(typeof(ConfirmHeritageGump));

        private readonly Type[] _selected;
        private readonly HeritageToken _token;
        private readonly int _cliloc;

        public override bool Singleton => true;

        private ConfirmHeritageGump(HeritageToken token, Type[] selected, int cliloc) : base(60, 36)
        {
            _token = token;
            _selected = selected;
            _cliloc = cliloc;
        }

        public static void DisplayTo(Mobile from, HeritageToken token, Type[] selected, int cliloc)
        {
            if (from?.NetState == null || token?.Deleted != false || selected == null || selected.Length == 0)
            {
                return;
            }

            from.SendGump(new ConfirmHeritageGump(token, selected, cliloc));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(0, 0, 291, 99, 0x13BE);
            builder.AddImageTiled(5, 6, 280, 20, 0xA40);
            builder.AddHtmlLocalized(9, 8, 280, 20, 1070972, 0x7FFF); // Click "OKAY" to redeem the following promotional item:
            builder.AddImageTiled(5, 31, 280, 40, 0xA40);
            builder.AddHtmlLocalized(9, 35, 272, 40, _cliloc, 0x7FFF);
            builder.AddButton(180, 73, 0xFB7, 0xFB8, (int)Buttons.Okay);
            builder.AddHtmlLocalized(215, 75, 100, 20, 1011036, 0x7FFF); // OKAY
            builder.AddButton(5, 73, 0xFB1, 0xFB2, (int)Buttons.Cancel);
            builder.AddHtmlLocalized(40, 75, 100, 20, 1060051, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_token?.Deleted != false)
            {
                return;
            }

            switch (info.ButtonID)
            {
                case (int)Buttons.Okay:
                    {
                        Item item = null;

                        foreach (var type in _selected)
                        {
                            try
                            {
                                item = type.CreateInstance<Item>();
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, "Failed to create heritage item of type {Type}", type);
                            }

                            if (item != null)
                            {
                                _token.Delete();
                                sender.Mobile.AddToBackpack(item);
                            }
                        }

                        break;
                    }

                case (int)Buttons.Cancel:
                    {
                        HeritageTokenGump.DisplayTo(sender.Mobile, _token);
                        break;
                    }
            }
        }

        private enum Buttons
        {
            Cancel,
            Okay
        }
    }
}
