using System;
using System.Reflection;
using Server.HuePickers;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
    public class PlayerVendorBuyGump : StaticGump<PlayerVendorBuyGump>
    {
        private readonly PlayerVendor _vendor;
        private readonly VendorItem _vi;

        public override bool Singleton => true;

        private PlayerVendorBuyGump(PlayerVendor vendor, VendorItem vi) : base(100, 200)
        {
            _vendor = vendor;
            _vi = vi;
        }

        public static void DisplayTo(Mobile from, PlayerVendor vendor, VendorItem vi)
        {
            if (from == null || vendor == null || vi == null)
            {
                return;
            }

            from.SendGump(new PlayerVendorBuyGump(vendor, vi));
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(100, 10, 300, 150, 5054);

            builder.AddHtmlLocalized(125, 20, 250, 24, 1019070); // You have agreed to purchase:

            builder.AddHtmlPlaceholder(125, 45, 250, 24, "description");

            builder.AddHtmlLocalized(125, 70, 250, 24, 1019071); // for the amount of:
            builder.AddLabelPlaceholder(125, 95, 0, "price");

            builder.AddButton(250, 130, 4005, 4007, 0);
            builder.AddHtmlLocalized(282, 130, 100, 24, 1011012); // CANCEL

            builder.AddButton(120, 130, 4005, 4007, 1);
            builder.AddHtmlLocalized(152, 130, 100, 24, 1011036); // OKAY
        }

        protected override void BuildStrings(ref GumpStringsBuilder builder)
        {
            // 1019072: an item without a description
            var description = !string.IsNullOrEmpty(_vi.Description)
                ? _vi.Description
                : Localization.GetText(1019072) ?? "an item without a description";

            builder.SetHtmlText("description", description);

            builder.SetStringSlot("price", $"{_vi.Price}");
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            var from = state.Mobile;

            if (!_vendor.CanInteractWith(from, false))
            {
                return;
            }

            if (_vendor.IsOwner(from))
            {
                _vendor.SayTo(from, 503212); // You own this shop, just take what you want.
                return;
            }

            if (info.ButtonID == 1)
            {
                _vendor.Say(from.Name);

                if (!_vi.Valid || !_vi.Item.IsChildOf(_vendor.Backpack))
                {
                    _vendor.SayTo(from, 503216); // You can't buy that.
                    return;
                }

                var totalGold = 0;

                if (from.Backpack != null)
                {
                    totalGold += from.Backpack.GetAmount(typeof(Gold));
                }

                totalGold += Banker.GetBalance(from);

                if (totalGold < _vi.Price)
                {
                    _vendor.SayTo(from, 503205); // You cannot afford this item.
                }
                else if (!from.PlaceInBackpack(_vi.Item))
                {
                    _vendor.SayTo(from, 503204); // You do not have room in your backpack for this.
                }
                else
                {
                    var leftPrice = _vi.Price;

                    if (from.Backpack != null)
                    {
                        leftPrice -= from.Backpack.ConsumeUpTo(typeof(Gold), leftPrice);
                    }

                    if (leftPrice > 0)
                    {
                        Banker.Withdraw(from, leftPrice);
                    }

                    _vendor.HoldGold += _vi.Price;

                    from.SendLocalizedMessage(503201); // You take the item.
                }
            }
            else
            {
                from.SendLocalizedMessage(503207); // Cancelled purchase.
            }
        }
    }

    public class PlayerVendorOwnerGump : StaticGump<PlayerVendorOwnerGump>
    {
        private readonly PlayerVendor _vendor;

        private PlayerVendorOwnerGump(PlayerVendor vendor) : base(50, 200) => _vendor = vendor;

        public static void DisplayTo(Mobile from, PlayerVendor vendor)
        {
            if (from != null && vendor != null)
            {
                from.SendGump(new PlayerVendorOwnerGump(vendor));
            }
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddBackground(25, 10, 530, 140, 5054);

            builder.AddHtmlLocalized(425, 25, 120, 20, 1019068); // See goods
            builder.AddButton(390, 25, 4005, 4007, 1);
            builder.AddHtmlLocalized(425, 48, 120, 20, 1019069); // Customize
            builder.AddButton(390, 48, 4005, 4007, 2);
            builder.AddHtmlLocalized(425, 72, 120, 20, 1011012); // CANCEL
            builder.AddButton(390, 71, 4005, 4007, 0);

            builder.AddHtmlLocalized(40, 72, 260, 20, 1038321); // Gold held for you:
            builder.AddLabelPlaceholder(300, 72, 0, "holdGold");
            builder.AddHtmlLocalized(40, 96, 260, 20, 1038322); // Gold held in my account:
            builder.AddLabelPlaceholder(300, 96, 0, "bankAccount");

            // Client 1038324 replaced:
            builder.AddHtml(40, 120, 260, 20, "My charge per day is:");
            builder.AddLabelPlaceholder(300, 120, 0, "perDay");

            builder.AddHtmlLocalized(40, 25, 260, 20, 1038318); // Amount of days I can work:
            builder.AddLabelPlaceholder(300, 25, 0, "days");
            builder.AddHtmlLocalized(40, 48, 260, 20, 1038319); // Earth days:
            builder.AddLabelPlaceholder(300, 48, 0, "earthDays");
        }

        protected override void BuildStrings(ref GumpStringsBuilder builder)
        {
            var perDay = _vendor.ChargePerDay;
            var days = (_vendor.HoldGold + _vendor.BankAccount) / (double)perDay;

            builder.SetStringSlot("holdGold", $"{_vendor.HoldGold}");
            builder.SetStringSlot("bankAccount", $"{_vendor.BankAccount}");
            builder.SetStringSlot("perDay", $"{perDay}");
            builder.SetStringSlot("days", $"{(int)days}");
            builder.SetStringSlot("earthDays", $"{(int)(days / 12.0)}");
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            var from = state.Mobile;

            if (!_vendor.CanInteractWith(from, true))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1:
                    {
                        _vendor.OpenBackpack(from);

                        break;
                    }
                case 2:
                    {
                        PlayerVendorCustomizeGump.DisplayTo(from, _vendor);

                        break;
                    }
            }
        }
    }

    public class NewPlayerVendorOwnerGump : DynamicGump
    {
        private readonly PlayerVendor _vendor;

        public override bool Singleton => true;

        private NewPlayerVendorOwnerGump(PlayerVendor vendor) : base(50, 200) => _vendor = vendor;

        public static void DisplayTo(Mobile from, PlayerVendor vendor)
        {
            if (from != null && vendor != null)
            {
                from.SendGump(new NewPlayerVendorOwnerGump(vendor));
            }
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            var perRealWorldDay = _vendor.ChargePerRealWorldDay;
            var goldHeld = _vendor.HoldGold;

            builder.AddPage();

            builder.AddBackground(25, 10, 530, 180, 0x13BE);

            builder.AddImageTiled(35, 20, 510, 160, 0xA40);
            builder.AddAlphaRegion(35, 20, 510, 160);

            builder.AddImage(10, 0, 0x28DC);
            builder.AddImage(537, 175, 0x28DC);
            builder.AddImage(10, 175, 0x28DC);
            builder.AddImage(537, 0, 0x28DC);

            if (goldHeld < perRealWorldDay)
            {
                var goldNeeded = perRealWorldDay - goldHeld;

                builder.AddHtmlLocalized(40, 35, 260, 20, 1038320, 0x7FFF); // Gold needed for 1 day of vendor salary:
                builder.AddLabel(300, 35, 0x1F, $"{goldNeeded}");
            }
            else
            {
                var days = goldHeld / perRealWorldDay;

                builder.AddHtmlLocalized(40, 35, 260, 20, 1038318, 0x7FFF); // # of days Vendor salary is paid for:
                builder.AddLabel(300, 35, 0x480, $"{days}");
            }

            builder.AddHtmlLocalized(40, 58, 260, 20, 1038324, 0x7FFF); // My charge per real world day is:
            builder.AddLabel(300, 58, 0x480, $"{perRealWorldDay}");

            builder.AddHtmlLocalized(40, 82, 260, 20, 1038322, 0x7FFF); // Gold held in my account:
            builder.AddLabel(300, 82, 0x480, $"{goldHeld}");

            builder.AddHtmlLocalized(40, 108, 260, 20, 1062509, 0x7FFF); // Shop Name:
            builder.AddLabel(140, 106, 0x66D, _vendor.ShopName);

            if (_vendor is RentedVendor rentedVendor)
            {
                rentedVendor.ComputeRentalExpireDelay(out var days, out var hours);

                builder.AddLabel(
                    38,
                    132,
                    0x480,
                    $"Location rental will expire in {days} day{(days != 1 ? "s" : "")} and {hours} hour{(hours != 1 ? "s" : "")}."
                );
            }

            builder.AddButton(390, 24, 0x15E1, 0x15E5, 1);
            builder.AddHtmlLocalized(408, 21, 120, 20, 1019068, 0x7FFF); // See goods

            builder.AddButton(390, 44, 0x15E1, 0x15E5, 2);
            builder.AddHtmlLocalized(408, 41, 120, 20, 1019069, 0x7FFF); // Customize

            builder.AddButton(390, 64, 0x15E1, 0x15E5, 3);
            builder.AddHtmlLocalized(408, 61, 120, 20, 1062434, 0x7FFF); // Rename Shop

            builder.AddButton(390, 84, 0x15E1, 0x15E5, 4);
            builder.AddHtmlLocalized(408, 81, 120, 20, 3006217, 0x7FFF); // Rename Vendor

            builder.AddButton(390, 104, 0x15E1, 0x15E5, 5);
            builder.AddHtmlLocalized(408, 101, 120, 20, 3006123, 0x7FFF); // Open Paperdoll

            builder.AddButton(390, 124, 0x15E1, 0x15E5, 6);
            builder.AddLabel(408, 121, 0x480, "Collect Gold");

            builder.AddButton(390, 144, 0x15E1, 0x15E5, 7);
            builder.AddLabel(408, 141, 0x480, "Dismiss Vendor");

            builder.AddButton(390, 162, 0x15E1, 0x15E5, 0);
            builder.AddHtmlLocalized(408, 161, 120, 20, 1011012, 0x7FFF); // CANCEL
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            if (info.ButtonID is 1 or 2) // See goods or Customize
            {
                _vendor.CheckTeleport(from);
            }

            if (!_vendor.CanInteractWith(from, true))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // See goods
                    {
                        _vendor.OpenBackpack(from);

                        break;
                    }
                case 2: // Customize
                    {
                        NewPlayerVendorCustomizeGump.DisplayTo(from, _vendor);

                        break;
                    }
                case 3: // Rename Shop
                    {
                        _vendor.RenameShop(from);

                        break;
                    }
                case 4: // Rename Vendor
                    {
                        _vendor.Rename(from);

                        break;
                    }
                case 5: // Open Paperdoll
                    {
                        _vendor.DisplayPaperdollTo(from);

                        break;
                    }
                case 6: // Collect Gold
                    {
                        _vendor.CollectGold(from);

                        break;
                    }
                case 7: // Dismiss Vendor
                    {
                        _vendor.Dismiss(from);

                        break;
                    }
            }
        }
    }

    public class PlayerVendorCustomizeGump : StaticGump<PlayerVendorCustomizeGump>
    {
        private static readonly CustomCategory[] Categories =
        [
            new(
                Layer.InnerTorso,
                1011357,
                true,
                [
                    // Upper Torso
                    new CustomItem(typeof(Shirt), 1011359, 5399),
                    new CustomItem(typeof(FancyShirt), 1011360, 7933),
                    new CustomItem(typeof(PlainDress), 1011363, 7937),
                    new CustomItem(typeof(FancyDress), 1011364, 7935),
                    new CustomItem(typeof(Robe), 1011365, 7939)
                ]
            ),

            new(
                Layer.MiddleTorso,
                1011371,
                true,
                [
                    // Over chest
                    new CustomItem(typeof(Doublet), 1011358, 8059),
                    new CustomItem(typeof(Tunic), 1011361, 8097),
                    new CustomItem(typeof(JesterSuit), 1011366, 8095),
                    new CustomItem(typeof(BodySash), 1011372, 5441),
                    new CustomItem(typeof(Surcoat), 1011362, 8189),
                    new CustomItem(typeof(HalfApron), 1011373, 5435),
                    new CustomItem(typeof(FullApron), 1011374, 5437)
                ]
            ),

            new(
                Layer.Shoes,
                1011388,
                true,
                [
                    // Footwear
                    new CustomItem(typeof(Sandals), 1011389, 5901),
                    new CustomItem(typeof(Shoes), 1011390, 5904),
                    new CustomItem(typeof(Boots), 1011391, 5899),
                    new CustomItem(typeof(ThighBoots), 1011392, 5906)
                ]
            ),

            new(
                Layer.Helm,
                1011375,
                true,
                [
                    // Hats
                    new CustomItem(typeof(SkullCap), 1011376, 5444),
                    new CustomItem(typeof(Bandana), 1011377, 5440),
                    new CustomItem(typeof(FloppyHat), 1011378, 5907),
                    new CustomItem(typeof(WideBrimHat), 1011379, 5908),
                    new CustomItem(typeof(Cap), 1011380, 5909),
                    new CustomItem(typeof(TallStrawHat), 1011382, 5910)
                ]
            ),

            new(
                Layer.Helm,
                1015319,
                true,
                [
                    // More Hats
                    new CustomItem(typeof(StrawHat), 1011382, 5911),
                    new CustomItem(typeof(WizardsHat), 1011383, 5912),
                    new CustomItem(typeof(Bonnet), 1011384, 5913),
                    new CustomItem(typeof(FeatheredHat), 1011385, 5914),
                    new CustomItem(typeof(TricorneHat), 1011386, 5915),
                    new CustomItem(typeof(JesterHat), 1011387, 5916)
                ]
            ),

            new(
                Layer.Pants,
                1011367,
                true,
                [
                    // Lower Torso
                    new CustomItem(typeof(LongPants), 1011368, 5433),
                    new CustomItem(typeof(Kilt), 1011369, 5431),
                    new CustomItem(typeof(Skirt), 1011370, 5398)
                ]
            ),

            new(
                Layer.Cloak,
                1011393,
                true,
                [
                    // Back
                    new CustomItem(typeof(Cloak), 1011394, 5397)
                ]
            ),

            new(
                Layer.Hair,
                1011395,
                true,
                [
                    // Hair
                    new CustomItem(0x203B, 1011052),
                    new CustomItem(0x203C, 1011053),
                    new CustomItem(0x203D, 1011054),
                    new CustomItem(0x2044, 1011055),
                    new CustomItem(0x2045, 1011047),
                    new CustomItem(0x204A, 1011050),
                    new CustomItem(0x2047, 1011396),
                    new CustomItem(0x2048, 1011048),
                    new CustomItem(0x2049, 1011049)
                ]
            ),

            new(
                Layer.FacialHair,
                1015320,
                true,
                [
                    // Facial Hair
                    new CustomItem(0x2041, 1011062),
                    new CustomItem(0x203F, 1011060),
                    new CustomItem(0x204B, 1015321, true),
                    new CustomItem(0x203E, 1011061),
                    new CustomItem(0x204C, 1015322, true),
                    new CustomItem(0x2040, 1015323),
                    new CustomItem(0x204D, 1011401)
                ]
            ),

            new(
                Layer.FirstValid,
                1011397,
                false,
                [
                    // Held items
                    new CustomItem(typeof(FishingPole), 1011406, 3520),
                    new CustomItem(typeof(Pickaxe), 1011407, 3717),
                    new CustomItem(typeof(Pitchfork), 1011408, 3720),
                    new CustomItem(typeof(Cleaver), 1015324, 3778),
                    new CustomItem(typeof(Mace), 1011409, 3933),
                    new CustomItem(typeof(Torch), 1011410, 3940),
                    new CustomItem(typeof(Hammer), 1011411, 4020),
                    new CustomItem(typeof(Longsword), 1011412, 3936),
                    new CustomItem(typeof(GnarledStaff), 1011413, 5113)
                ]
            ),

            new(
                Layer.FirstValid,
                1015325,
                false,
                [
                    // More held items
                    new CustomItem(typeof(Crossbow), 1011414, 3920),
                    new CustomItem(typeof(WarMace), 1011415, 5126),
                    new CustomItem(typeof(TwoHandedAxe), 1011416, 5186),
                    new CustomItem(typeof(Spear), 1011417, 3939),
                    new CustomItem(typeof(Katana), 1011418, 5118),
                    new CustomItem(typeof(Spellbook), 1011419, 3834)
                ]
            )
        ];

        private readonly Mobile _vendor;

        public override bool Singleton => true;

        private PlayerVendorCustomizeGump(Mobile v) : base(30, 40) => _vendor = v;

        public static void DisplayTo(Mobile from, Mobile vendor)
        {
            if (from != null && vendor != null && !vendor.Deleted)
            {
                from.SendGump(new PlayerVendorCustomizeGump(vendor));
            }
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddPage();
            builder.AddBackground(0, 0, 585, 393, 5054);
            builder.AddBackground(195, 36, 387, 275, 3000);
            builder.AddHtmlLocalized(10, 10, 565, 18, 1011356);  // <center>VENDOR CUSTOMIZATION MENU</center>
            builder.AddHtmlLocalized(60, 355, 150, 18, 1011036); // OKAY
            builder.AddButton(25, 355, 4005, 4007, 1);
            builder.AddHtmlLocalized(320, 355, 150, 18, 1011012); // CANCEL
            builder.AddButton(285, 355, 4005, 4007, 0);

            var y = 35;
            for (var i = 0; i < Categories.Length; i++)
            {
                var cat = Categories[i];
                builder.AddHtmlLocalized(5, y, 150, 25, cat.LocNumber, true);
                builder.AddButton(155, y, 4005, 4007, 0, GumpButtonType.Page, i + 1);
                y += 25;
            }

            for (var i = 0; i < Categories.Length; i++)
            {
                var cat = Categories[i];
                builder.AddPage(i + 1);

                for (var c = 0; c < cat.Entries.Length; c++)
                {
                    var entry = cat.Entries[c];
                    var x = 198 + c % 3 * 129;
                    y = 38 + c / 3 * 67;

                    builder.AddHtmlLocalized(x, y, 100, entry.LongText ? 36 : 18, entry.LocNumber);

                    if (entry.ArtNumber != 0)
                    {
                        builder.AddItem(x + 20, y + 25, entry.ArtNumber);
                    }

                    builder.AddRadio(x, y + (entry.LongText ? 40 : 20), 210, 211, false, (c << 8) + i);
                }

                if (cat.CanDye)
                {
                    builder.AddHtmlLocalized(327, 239, 100, 18, 1011402); // Color
                    builder.AddRadio(327, 259, 210, 211, false, 100 + i);
                }

                builder.AddHtmlLocalized(456, 239, 100, 18, 1011403); // Remove
                builder.AddRadio(456, 259, 210, 211, false, 200 + i);
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (_vendor.Deleted)
            {
                return;
            }

            var from = state.Mobile;

            if (_vendor is PlayerVendor vendor && !vendor.CanInteractWith(from, true))
            {
                return;
            }

            if (_vendor is PlayerBarkeeper barkeeper && !barkeeper.IsOwner(from))
            {
                return;
            }

            if (info.ButtonID == 0)
            {
                if (_vendor is PlayerVendor) // do nothing for barkeeps
                {
                    _vendor.Direction = _vendor.GetDirectionTo(from);
                    _vendor.Animate(32, 5, 1, true, false, 0);         // bow
                    _vendor.SayTo(from, 1043310 + Utility.Random(12)); // a little random speech
                }
            }
            else if (info.ButtonID == 1 && info.Switches.Length > 0)
            {
                var cnum = info.Switches[0];
                var cat = cnum % 256;
                var ent = cnum >> 8;

                if (cat < Categories.Length && cat >= 0)
                {
                    if (ent < Categories[cat].Entries.Length && ent >= 0)
                    {
                        var item = _vendor.FindItemOnLayer(Categories[cat].Layer);

                        item?.Delete();

                        var items = _vendor.Items;

                        for (var i = 0; item == null && i < items.Count; ++i)
                        {
                            var checkitem = items[i];
                            var type = checkitem.GetType();

                            for (var j = 0; item == null && j < Categories[cat].Entries.Length; ++j)
                            {
                                if (type == Categories[cat].Entries[j].Type)
                                {
                                    item = checkitem;
                                }
                            }
                        }

                        item?.Delete();

                        if (Categories[cat].Layer == Layer.FacialHair)
                        {
                            if (_vendor.Female)
                            {
                                from.SendLocalizedMessage(1010639); // You cannot place facial hair on a woman!
                            }
                            else
                            {
                                var hue = _vendor.FacialHairHue;

                                _vendor.FacialHairItemID = 0;
                                _vendor.ProcessDelta(); // invalidate item ID for clients

                                _vendor.FacialHairItemID = Categories[cat].Entries[ent].ItemID;
                                _vendor.FacialHairHue = hue;
                            }
                        }
                        else if (Categories[cat].Layer == Layer.Hair)
                        {
                            var hue = _vendor.HairHue;

                            _vendor.HairItemID = 0;
                            _vendor.ProcessDelta(); // invalidate item ID for clients

                            _vendor.HairItemID = Categories[cat].Entries[ent].ItemID;
                            _vendor.HairHue = hue;
                        }
                        else
                        {
                            item = Categories[cat].Entries[ent].Create();

                            if (item != null)
                            {
                                item.Layer = Categories[cat].Layer;

                                if (!_vendor.EquipItem(item))
                                {
                                    item.Delete();
                                }
                            }
                        }

                        DisplayTo(from, _vendor);
                    }
                }
                else
                {
                    cat -= 100;

                    if (cat < 100)
                    {
                        if (cat < Categories.Length && cat >= 0)
                        {
                            var category = Categories[cat];

                            if (category.Layer == Layer.Hair)
                            {
                                new PVHairHuePicker(false, _vendor, from).SendTo(state);
                            }
                            else if (category.Layer == Layer.FacialHair)
                            {
                                new PVHairHuePicker(true, _vendor, from).SendTo(state);
                            }
                            else
                            {
                                Item item = null;

                                var items = _vendor.Items;

                                for (var i = 0; item == null && i < items.Count; ++i)
                                {
                                    var checkitem = items[i];
                                    var type = checkitem.GetType();

                                    for (var j = 0; item == null && j < category.Entries.Length; ++j)
                                    {
                                        if (type == category.Entries[j].Type)
                                        {
                                            item = checkitem;
                                        }
                                    }
                                }

                                if (item != null)
                                {
                                    new PVHuePicker(item, _vendor, from).SendTo(state);
                                }
                            }
                        }
                    }
                    else
                    {
                        cat -= 100;

                        if (cat < Categories.Length)
                        {
                            var category = Categories[cat];

                            if (category.Layer == Layer.Hair)
                            {
                                _vendor.HairItemID = 0;
                            }
                            else if (category.Layer == Layer.FacialHair)
                            {
                                _vendor.FacialHairItemID = 0;
                            }
                            else
                            {
                                Item item = null;

                                var items = _vendor.Items;

                                for (var i = 0; item == null && i < items.Count; ++i)
                                {
                                    var checkitem = items[i];
                                    var type = checkitem.GetType();

                                    for (var j = 0; item == null && j < category.Entries.Length; ++j)
                                    {
                                        if (type == category.Entries[j].Type)
                                        {
                                            item = checkitem;
                                        }
                                    }
                                }

                                item?.Delete();
                            }

                            DisplayTo(from, _vendor);
                        }
                    }
                }
            }
        }

        private class CustomItem
        {
            private ConstructorInfo _ctor;
            private object[] _params;

            public CustomItem(int itemID, int loc, bool longText = false) : this(null, itemID, loc, 0, longText)
            {
            }

            public CustomItem(Type type, int loc, int art = 0) : this(type, 0, loc, art)
            {
            }

            public CustomItem(Type type, int itemID = 0, int loc = 0, int art = 0, bool longText = false)
            {
                Type = type;
                ItemID = itemID;
                LocNumber = loc;
                ArtNumber = art;
                LongText = longText;
            }

            public Type Type { get; }

            public int ItemID { get; }

            public int LocNumber { get; }

            public int ArtNumber { get; }

            public bool LongText { get; }

            public Item Create()
            {
                if (Type == null)
                {
                    return null;
                }

                if (_ctor == null)
                {
                    _ctor = Type.GetConstructor(out var paramCount);
                    _params = paramCount == 0 ? null : new object[paramCount];
                    if (_params != null)
                    {
                        Array.Fill(_params, Type.Missing);
                    }
                }

                try
                {
                    return _ctor?.Invoke(_params) as Item;
                }
                catch
                {
                    return null;
                }
            }
        }

        private class CustomCategory
        {
            public CustomCategory(Layer layer, int loc, bool canDye, CustomItem[] items)
            {
                Entries = items;
                CanDye = canDye;
                Layer = layer;
                LocNumber = loc;
            }

            public bool CanDye { get; }

            public CustomItem[] Entries { get; }

            public Layer Layer { get; }

            public int LocNumber { get; }
        }

        private class PVHuePicker : HuePicker
        {
            private readonly Item m_Item;
            private readonly Mobile m_Mob;
            private readonly Mobile m_Vendor;

            public PVHuePicker(Item item, Mobile v, Mobile from) : base(item.ItemID)
            {
                m_Item = item;
                m_Vendor = v;
                m_Mob = from;
            }

            public override void OnResponse(int hue)
            {
                if (m_Item.Deleted)
                {
                    return;
                }

                if (m_Vendor is PlayerVendor vendor && !vendor.CanInteractWith(m_Mob, true))
                {
                    return;
                }

                if (m_Vendor is PlayerBarkeeper barkeeper && !barkeeper.IsOwner(m_Mob))
                {
                    return;
                }

                m_Item.Hue = hue;
                DisplayTo(m_Mob, m_Vendor);
            }
        }

        private class PVHairHuePicker : HuePicker
        {
            private readonly bool m_FacialHair;
            private readonly Mobile m_Mob;
            private readonly Mobile m_Vendor;

            public PVHairHuePicker(bool facialHair, Mobile v, Mobile from) : base(0xFAB)
            {
                m_FacialHair = facialHair;
                m_Vendor = v;
                m_Mob = from;
            }

            public override void OnResponse(int hue)
            {
                if (m_Vendor.Deleted)
                {
                    return;
                }

                if (m_Vendor is PlayerVendor vendor && !vendor.CanInteractWith(m_Mob, true))
                {
                    return;
                }

                if (m_Vendor is PlayerBarkeeper barkeeper && !barkeeper.IsOwner(m_Mob))
                {
                    return;
                }

                if (m_FacialHair)
                {
                    m_Vendor.FacialHairHue = hue;
                }
                else
                {
                    m_Vendor.HairHue = hue;
                }

                DisplayTo(m_Mob, m_Vendor);
            }
        }
    }

    public class NewPlayerVendorCustomizeGump : DynamicGump
    {
        private static readonly HairOrBeard[] _hairStyles =
        [
            new(0x203B, 1011052), // Short
            new(0x203C, 1011053), // Long
            new(0x203D, 1011054), // Ponytail
            new(0x2044, 1011055), // Mohawk
            new(0x2045, 1011047), // Pageboy
            new(0x204A, 1011050), // Topknot
            new(0x2047, 1011396), // Curly
            new(0x2048, 1011048), // Receding
            new(0x2049, 1011049)  // 2-tails
        ];

        private static readonly HairOrBeard[] _beardStyles =
        [
            new(0x2041, 1011062), // Mustache
            new(0x203F, 1011060), // Short beard
            new(0x204B, 1015321), // Short Beard & Moustache
            new(0x203E, 1011061), // Long beard
            new(0x204C, 1015322), // Long Beard & Moustache
            new(0x2040, 1015323), // Goatee
            new(0x204D, 1011401)  // Vandyke
        ];

        private readonly PlayerVendor _vendor;

        public override bool Singleton => true;

        private NewPlayerVendorCustomizeGump(PlayerVendor vendor) : base(50, 50) => _vendor = vendor;

        public static void DisplayTo(Mobile from, PlayerVendor vendor)
        {
            if (from != null && vendor != null && !vendor.Deleted)
            {
                from.SendGump(new NewPlayerVendorCustomizeGump(vendor));
            }
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage();

            builder.AddBackground(0, 0, 370, 370, 0x13BE);

            builder.AddImageTiled(10, 10, 350, 20, 0xA40);
            builder.AddImageTiled(10, 40, 350, 20, 0xA40);
            builder.AddImageTiled(10, 70, 350, 260, 0xA40);
            builder.AddImageTiled(10, 340, 350, 20, 0xA40);

            builder.AddAlphaRegion(10, 10, 350, 350);

            builder.AddHtmlLocalized(10, 12, 350, 18, 1011356, 0x7FFF); // <center>VENDOR CUSTOMIZATION MENU</center>

            builder.AddHtmlLocalized(10, 42, 150, 18, 1062459, 0x421F); // <CENTER>HAIR</CENTER>

            for (var i = 0; i < _hairStyles.Length; i++)
            {
                var hair = _hairStyles[i];

                builder.AddButton(10, 70 + i * 20, 0xFA5, 0xFA7, 0x100 | i);
                builder.AddHtmlLocalized(45, 72 + i * 20, 110, 18, hair.Name, 0x7FFF);
            }

            builder.AddButton(10, 70 + _hairStyles.Length * 20, 0xFB1, 0xFB3, 2);
            builder.AddHtmlLocalized(45, 72 + _hairStyles.Length * 20, 110, 18, 1011403, 0x7FFF); // Remove

            builder.AddButton(10, 70 + (_hairStyles.Length + 1) * 20, 0xFA5, 0xFA7, 3);
            builder.AddHtmlLocalized(45, 72 + (_hairStyles.Length + 1) * 20, 110, 18, 1011402, 0x7FFF); // Color

            if (_vendor.Female)
            {
                builder.AddButton(160, 290, 0xFA5, 0xFA7, 1);
                builder.AddHtmlLocalized(195, 292, 160, 18, 1015327, 0x7FFF); // Male

                builder.AddHtmlLocalized(195, 312, 160, 18, 1015328, 0x421F); // Female
            }
            else
            {
                builder.AddHtmlLocalized(160, 42, 210, 18, 1062460, 0x421F); // <CENTER>BEARD</CENTER>

                for (var i = 0; i < _beardStyles.Length; i++)
                {
                    var beard = _beardStyles[i];

                    builder.AddButton(160, 70 + i * 20, 0xFA5, 0xFA7, 0x200 | i);
                    builder.AddHtmlLocalized(195, 72 + i * 20, 160, 18, beard.Name, 0x7FFF);
                }

                builder.AddButton(160, 70 + _beardStyles.Length * 20, 0xFB1, 0xFB3, 4);
                builder.AddHtmlLocalized(195, 72 + _beardStyles.Length * 20, 160, 18, 1011403, 0x7FFF); // Remove

                builder.AddButton(160, 70 + (_beardStyles.Length + 1) * 20, 0xFA5, 0xFA7, 5);
                builder.AddHtmlLocalized(195, 72 + (_beardStyles.Length + 1) * 20, 160, 18, 1011402, 0x7FFF); // Color

                builder.AddHtmlLocalized(195, 292, 160, 18, 1015327, 0x421F); // Male

                builder.AddButton(160, 310, 0xFA5, 0xFA7, 1);
                builder.AddHtmlLocalized(195, 312, 160, 18, 1015328, 0x7FFF); // Female
            }

            builder.AddButton(10, 340, 0xFA5, 0xFA7, 0);
            builder.AddHtmlLocalized(45, 342, 305, 18, 1060675, 0x7FFF); // CLOSE
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;

            if (!_vendor.CanInteractWith(from, true))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 0: // CLOSE
                    {
                        _vendor.Direction = _vendor.GetDirectionTo(from);
                        _vendor.Animate(32, 5, 1, true, false, 0);         // bow
                        _vendor.SayTo(from, 1043310 + Utility.Random(12)); // a little random speech

                        break;
                    }
                case 1: // Female/Male
                    {
                        if (_vendor.Female)
                        {
                            _vendor.Body = 400;
                            _vendor.Female = false;
                        }
                        else
                        {
                            _vendor.Body = 401;
                            _vendor.Female = true;

                            _vendor.FacialHairItemID = 0;
                        }

                        DisplayTo(from, _vendor);

                        break;
                    }
                case 2: // Remove hair
                    {
                        _vendor.HairItemID = 0;

                        DisplayTo(from, _vendor);

                        break;
                    }
                case 3: // Color hair
                    {
                        if (_vendor.HairItemID > 0)
                        {
                            new PVHuePicker(_vendor, false, from).SendTo(from.NetState);
                        }
                        else
                        {
                            DisplayTo(from, _vendor);
                        }

                        break;
                    }
                case 4: // Remove beard
                    {
                        _vendor.FacialHairItemID = 0;

                        DisplayTo(from, _vendor);

                        break;
                    }
                case 5: // Color beard
                    {
                        if (_vendor.FacialHairItemID > 0)
                        {
                            new PVHuePicker(_vendor, true, from).SendTo(from.NetState);
                        }
                        else
                        {
                            DisplayTo(from, _vendor);
                        }

                        break;
                    }
                default:
                    {
                        int hairhue;

                        if ((info.ButtonID & 0x100) != 0) // Hair style selected
                        {
                            var index = info.ButtonID & 0xFF;

                            if (index >= _hairStyles.Length)
                            {
                                return;
                            }

                            var hairStyle = _hairStyles[index];

                            hairhue = _vendor.HairHue;

                            _vendor.HairItemID = 0;
                            _vendor.ProcessDelta();

                            _vendor.HairItemID = hairStyle.ItemID;

                            _vendor.HairHue = hairhue;

                            DisplayTo(from, _vendor);
                        }
                        else if ((info.ButtonID & 0x200) != 0) // Beard style selected
                        {
                            if (_vendor.Female)
                            {
                                return;
                            }

                            var index = info.ButtonID & 0xFF;

                            if (index >= _beardStyles.Length)
                            {
                                return;
                            }

                            var beardStyle = _beardStyles[index];

                            hairhue = _vendor.FacialHairHue;

                            _vendor.FacialHairItemID = 0;
                            _vendor.ProcessDelta();

                            _vendor.FacialHairItemID = beardStyle.ItemID;

                            _vendor.FacialHairHue = hairhue;

                            DisplayTo(from, _vendor);
                        }

                        break;
                    }
            }
        }

        private class HairOrBeard
        {
            public HairOrBeard(int itemID, int name)
            {
                ItemID = itemID;
                Name = name;
            }

            public int ItemID { get; }

            public int Name { get; }
        }

        private class PVHuePicker : HuePicker
        {
            private readonly bool m_FacialHair;
            private readonly Mobile m_From;
            private readonly PlayerVendor m_Vendor;

            public PVHuePicker(PlayerVendor vendor, bool facialHair, Mobile from) : base(0xFAB)
            {
                m_Vendor = vendor;
                m_FacialHair = facialHair;
                m_From = from;
            }

            public override void OnResponse(int hue)
            {
                if (!m_Vendor.CanInteractWith(m_From, true))
                {
                    return;
                }

                if (m_FacialHair)
                {
                    m_Vendor.FacialHairHue = hue;
                }
                else
                {
                    m_Vendor.HairHue = hue;
                }

                DisplayTo(m_From, m_Vendor);
            }
        }
    }
}
