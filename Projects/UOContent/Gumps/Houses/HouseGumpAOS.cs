using System;
using System.Collections.Generic;
using System.Linq;
using Server.Guilds;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Prompts;

namespace Server.Gumps
{
    public enum HouseGumpPageAOS
    {
        Information,
        Security,
        Storage,
        Customize,
        Ownership,
        ChangeHanger,
        ChangeFoundation,
        ChangeSign,
        RemoveCoOwner,
        ListCoOwner,
        RemoveFriend,
        ListFriend,
        RemoveBan,
        ListBan,
        RemoveAccess,
        ListAccess,
        ChangePost,
        Vendors
    }

    public class HouseGumpAOS : DynamicGump
    {
        private const int LabelColor = 0x7FFF;
        private const int SelectedColor = 0x421F;
        private const int DisabledColor = 0x4210;
        private const int WarningColor = 0x7E10;

        private const int LabelHue = 0x481;
        private const int HighlightedLabelHue = 0x64;

        private static readonly int[] _hangerNumbers =
        [
            2968, 2970, 2972,
            2974, 2976, 2978
        ];

        private static readonly int[] _foundationNumbers =
            Core.ML ? [20, 189, 765, 65, 101, 0x2DF7, 0x2DFB, 0x3672, 0x3676] : [20, 189, 765, 65, 101];

        private static readonly int[] _postNumbers =
        [
            9, 29, 54, 90, 147, 169,
            177, 204, 251, 257, 263,
            298, 347, 424, 441, 466,
            514, 600, 601, 602, 603,
            660, 666, 672, 898, 970,
            974, 982
        ];

        // Sign graphics for the Change House Sign menu. Each pair of consecutive item IDs in
        // the art is the same sign with a different orientation; we expose only the even ID.
        // The last two entries (2966 Library, 3140 Beekeeper) are ML-only and are appended at
        // the end so the older clients can take a contiguous slice of the first 54 entries.
        private static readonly int[] _houseSigns =
        [
            2980, // Bakery
            2982, // Tailor
            2984, // Tinker
            2986, // Butcher
            2988, // Healer
            2990, // Mage
            2992, // Woodworker
            2994, // Customs
            2996, // Inn
            2998, // Shipwright
            3000, // Stables
            3002, // Barber Shop
            3004, // Bard
            3006, // Fletcher
            3008, // Armourer
            3010, // Jeweler
            3012, // Tavern
            3014, // Reagent Shop
            3016, // Blacksmith
            3018, // Painter
            3020, // Provisioner
            3022, // Bowyer
            3024, // Wooden sign
            3026, // Brass sign
            3028, // Armaments Guild
            3030, // Armourers' Guild
            3032, // Blacksmiths' Guild
            3034, // Weapons Guild
            3036, // Bardic Guild
            3038, // Barters' Guild
            3040, // Provisioner Guild
            3042, // Traders' Guild
            3044, // Cooks' Guild
            3046, // Healers' Guild
            3048, // Mages' Guild
            3050, // Sorcerers' Guild
            3052, // Illusionist Guild
            3054, // Miners' Guild
            3056, // Archers' Guild
            3058, // Seamens' Guild
            3060, // Fishermens' Guild
            3062, // Sailors' Guild
            3064, // Shipwrights' Guild
            3066, // Tailors' Guild
            3068, // Thieves' Guild
            3070, // Rogues' Guild
            3072, // Assassins' Guild
            3074, // Tinkers' Guild
            3076, // Warriors' Guild
            3078, // Cavalry Guild
            3080, // Fighters' Guild
            3082, // Merchants' Guild
            3084, // Bank
            3086, // Theatre
            2966, // Library (ML)
            3140  // Beekeeper (ML)
        ];

        private readonly BaseHouse _house;
        private readonly HouseGumpPageAOS _page;
        private readonly Mobile _from;

        private List<Mobile> _list;

        public override bool Singleton => true;

        private HouseGumpAOS(HouseGumpPageAOS page, Mobile from, BaseHouse house) : base(50, 40)
        {
            _house = house;
            _page = page;
            _from = from;
        }

        public static void DisplayTo(Mobile from, BaseHouse house, HouseGumpPageAOS page = HouseGumpPageAOS.Information)
        {
            if (from?.NetState == null || house == null || house.Deleted)
            {
                return;
            }

            from.SendGump(new HouseGumpAOS(page, from, house));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            var isCombatRestricted = _house.IsCombatRestricted(_from);

            var isOwner = _house.IsOwner(_from);
            var isCoOwner = isOwner || _house.IsCoOwner(_from);
            var isFriend = isCoOwner || _house.IsFriend(_from);

            if (isCombatRestricted)
            {
                isFriend = isCoOwner = isOwner = false;
            }

            builder.AddPage();

            if (isFriend || _page == HouseGumpPageAOS.Vendors)
            {
                builder.AddBackground(0, 0, 420, _page != HouseGumpPageAOS.Vendors ? 440 : 420, 5054);

                builder.AddImageTiled(10, 10, 400, 100, 2624);
                builder.AddAlphaRegion(10, 10, 400, 100);

                builder.AddImageTiled(10, 120, 400, 260, 2624);
                builder.AddAlphaRegion(10, 120, 400, 260);

                builder.AddImageTiled(10, 390, 400, _page != HouseGumpPageAOS.Vendors ? 40 : 20, 2624);
                builder.AddAlphaRegion(10, 390, 400, _page != HouseGumpPageAOS.Vendors ? 40 : 20);

                AddButtonLabeled(ref builder, 250, _page != HouseGumpPageAOS.Vendors ? 410 : 390, 0, 1060675); // CLOSE
            }

            builder.AddImage(10, 10, 100);

            var lines = _house.Sign?.GetName().Wrap(10, 6);

            if (lines != null)
            {
                for (int i = 0, y = (114 - lines.Count * 14) / 2; i < lines.Count; ++i, y += 14)
                {
                    var s = lines[i];

                    builder.AddLabel(10 + (160 - s.Length * 8) / 2, y, 0, s);
                }
            }

            if (_page == HouseGumpPageAOS.Vendors)
            {
                builder.AddHtmlLocalized(10, 120, 400, 20, 1062428, LabelColor); // <CENTER>SHOPS</CENTER>

                AddList(ref builder, _house.AvailableVendorsFor(_from), 1, false, false);
                return;
            }

            if (!isFriend)
            {
                return;
            }

            if (_house.Public)
            {
                AddButtonLabeled(ref builder, 10, 390, GetButtonID(0, 0), 1060674); // Banish
                AddButtonLabeled(ref builder, 10, 410, GetButtonID(0, 1), 1011261); // Lift a Ban
            }
            else
            {
                AddButtonLabeled(ref builder, 10, 390, GetButtonID(0, 2), 1060676); // Grant Access
                AddButtonLabeled(ref builder, 10, 410, GetButtonID(0, 3), 1060677); // Revoke Access
            }

            AddPageButton(ref builder, 150, 10, GetButtonID(1, 0), 1060668, HouseGumpPageAOS.Information);
            AddPageButton(ref builder, 150, 30, GetButtonID(1, 1), 1060669, HouseGumpPageAOS.Security);
            AddPageButton(ref builder, 150, 50, GetButtonID(1, 2), 1060670, HouseGumpPageAOS.Storage);
            AddPageButton(ref builder, 150, 70, GetButtonID(1, 3), 1060671, HouseGumpPageAOS.Customize);
            AddPageButton(ref builder, 150, 90, GetButtonID(1, 4), 1060672, HouseGumpPageAOS.Ownership);

            switch (_page)
            {
                case HouseGumpPageAOS.Information:
                    {
                        builder.AddHtmlLocalized(20, 130, 200, 20, 1011242, LabelColor); // Owned By:
                        builder.AddLabel(210, 130, LabelHue, GetOwnerName());

                        builder.AddHtmlLocalized(20, 170, 380, 20, 1018032, SelectedColor); // This house is properly placed.
                        builder.AddHtmlLocalized(20, 190, 380, 20, 1018035, SelectedColor); // This house is of modern design.
                        builder.AddHtmlLocalized(
                            20,
                            210,
                            380,
                            20,
                            _house is HouseFoundation ? 1060681 : 1060680,
                            SelectedColor
                        ); // This is a (pre | custom)-built house.
                        builder.AddHtmlLocalized(
                            20,
                            230,
                            380,
                            20,
                            _house.Public ? 1060678 : 1060679,
                            SelectedColor
                        ); // This house is (private | open to the public).

                        switch (_house.DecayType)
                        {
                            case DecayType.Ageless:
                            case DecayType.AutoRefresh:
                                {
                                    // This house is <a href = "?ForceTopic97">Automatically</a> refreshed.
                                    builder.AddHtmlLocalized(20, 250, 380, 20, 1062209, SelectedColor);
                                    break;
                                }
                            case DecayType.ManualRefresh:
                                {
                                    // This house is <a href = "?ForceTopic97">Grandfathered</a>.
                                    builder.AddHtmlLocalized(20, 250, 380, 20, 1062208, SelectedColor);
                                    break;
                                }
                            case DecayType.Condemned:
                                {
                                    // This house is <a href = "?ForceTopic97">Condemned</a>.
                                    builder.AddHtmlLocalized(20, 250, 380, 20, 1062207, WarningColor);
                                    break;
                                }
                        }

                        builder.AddHtmlLocalized(20, 290, 200, 20, 1060692, SelectedColor); // Built On:
                        builder.AddLabel(250, 290, LabelHue, $"{_house.BuiltOn:yyyy'-'MM'-'dd HH':'mm':'ss}");

                        builder.AddHtmlLocalized(20, 310, 200, 20, 1060693, SelectedColor); // Last Traded:
                        builder.AddLabel(250, 310, LabelHue, $"{_house.LastTraded:yyyy'-'MM'-'dd HH':'mm':'ss}");

                        builder.AddHtmlLocalized(20, 330, 200, 20, 1061793, SelectedColor); // House Value
                        builder.AddLabel(250, 330, LabelHue, $"{_house.Price}");

                        // Number of visits this building has had:
                        builder.AddHtmlLocalized(20, 360, 300, 20, 1011241, SelectedColor);
                        builder.AddLabel(350, 360, LabelHue, $"{_house.Visits}");

                        break;
                    }
                case HouseGumpPageAOS.Security:
                    {
                        AddButtonLabeled(ref builder, 10, 130, GetButtonID(3, 0), 1011266, isCoOwner); // View Co-Owner List
                        AddButtonLabeled(ref builder, 10, 150, GetButtonID(3, 1), 1011267, isOwner);   // Add a Co-Owner
                        AddButtonLabeled(ref builder, 10, 170, GetButtonID(3, 2), 1018036, isOwner);   // Remove a Co-Owner
                        AddButtonLabeled(ref builder, 10, 190, GetButtonID(3, 3), 1011268, isOwner);   // Clear Co-Owner List

                        AddButtonLabeled(ref builder, 10, 220, GetButtonID(3, 4), 1011243);            // View Friends List
                        AddButtonLabeled(ref builder, 10, 240, GetButtonID(3, 5), 1011244, isCoOwner); // Add a Friend
                        AddButtonLabeled(ref builder, 10, 260, GetButtonID(3, 6), 1018037, isCoOwner); // Remove a Friend
                        AddButtonLabeled(ref builder, 10, 280, GetButtonID(3, 7), 1011245, isCoOwner); // Clear Friend List

                        if (_house.Public)
                        {
                            AddButtonLabeled(ref builder, 10, 310, GetButtonID(3, 8), 1011260); // View Ban List
                            AddButtonLabeled(ref builder, 10, 330, GetButtonID(3, 9), 1060698); // Clear Ban List

                            AddButtonLabeled(ref builder, 210, 130, GetButtonID(3, 12), 1060695, isOwner); // Change to Private

                            builder.AddHtmlLocalized(245, 150, 240, 20, 1060694, SelectedColor); // Change to Public
                        }
                        else
                        {
                            AddButtonLabeled(ref builder, 10, 310, GetButtonID(3, 10), 1060699); // View Access List
                            AddButtonLabeled(ref builder, 10, 330, GetButtonID(3, 11), 1060700); // Clear Access List

                            builder.AddHtmlLocalized(245, 130, 240, 20, 1060695, SelectedColor); // Change to Private

                            AddButtonLabeled(ref builder, 210, 150, GetButtonID(3, 13), 1060694, isOwner); // Change to Public
                        }

                        break;
                    }
                case HouseGumpPageAOS.Storage:
                    {
                        builder.AddHtmlLocalized(10, 130, 400, 20, 1060682, LabelColor); // <CENTER>HOUSE STORAGE SUMMARY</CENTER>

                        // This is not as OSI; storage changes not yet implemented

                        var maxSecures = _house.GetAosMaxSecures();
                        var curSecures = _house.GetAosCurSecures(
                            out var fromSecures,
                            out var fromVendors,
                            out var fromLockdowns,
                            out var fromMovingCrate
                        );

                        var maxLockdowns = _house.GetAosMaxLockdowns();
                        var curLockdowns = _house.GetAosCurLockdowns();

                        var bonusStorage = (int)(_house.BonusStorageScalar * 100 - 100);

                        if (bonusStorage > 0)
                        {
                            builder.AddHtmlLocalized(10, 150, 300, 20, 1072519, LabelColor); // Increased Storage
                            builder.AddLabel(310, 150, LabelHue, $"{bonusStorage}%");
                        }

                        builder.AddHtmlLocalized(10, 170, 300, 20, 1060683, LabelColor); // Maximum Secure Storage
                        builder.AddLabel(310, 170, LabelHue, $"{maxSecures}");

                        builder.AddHtmlLocalized(10, 190, 300, 20, 1060685, LabelColor); // Used by Moving Crate
                        builder.AddLabel(310, 190, LabelHue, $"{fromMovingCrate}");

                        builder.AddHtmlLocalized(10, 210, 300, 20, 1060686, LabelColor); // Used by Lockdowns
                        builder.AddLabel(310, 210, LabelHue, $"{fromLockdowns}");

                        if (BaseHouse.NewVendorSystem)
                        {
                            builder.AddHtmlLocalized(10, 230, 300, 20, 1060688, LabelColor); // Used by Secure Containers
                            builder.AddLabel(310, 230, LabelHue, $"{fromSecures}");

                            builder.AddHtmlLocalized(10, 250, 300, 20, 1060689, LabelColor); // Available Storage
                            builder.AddLabel(310, 250, LabelHue, $"{Math.Max(maxSecures - curSecures, 0)}");

                            builder.AddHtmlLocalized(10, 290, 300, 20, 1060690, LabelColor); // Maximum Lockdowns
                            builder.AddLabel(310, 290, LabelHue, $"{maxLockdowns}");

                            builder.AddHtmlLocalized(10, 310, 300, 20, 1060691, LabelColor); // Available Lockdowns
                            builder.AddLabel(310, 310, LabelHue, $"{Math.Max(maxLockdowns - curLockdowns, 0)}");

                            var maxVendors = _house.GetNewVendorSystemMaxVendors();
                            var vendors = _house.PlayerVendors.Count + _house.VendorRentalContracts.Count;

                            builder.AddHtmlLocalized(10, 350, 300, 20, 1062391, LabelColor); // Vendor Count
                            builder.AddLabel(310, 350, LabelHue, $"{vendors} / {maxVendors}");
                        }
                        else
                        {
                            builder.AddHtmlLocalized(10, 230, 300, 20, 1060687, LabelColor); // Used by Vendors
                            builder.AddLabel(310, 230, LabelHue, $"{fromVendors}");

                            builder.AddHtmlLocalized(10, 250, 300, 20, 1060688, LabelColor); // Used by Secure Containers
                            builder.AddLabel(310, 250, LabelHue, $"{fromSecures}");

                            builder.AddHtmlLocalized(10, 270, 300, 20, 1060689, LabelColor); // Available Storage
                            builder.AddLabel(310, 270, LabelHue, $"{Math.Max(maxSecures - curSecures, 0)}");

                            builder.AddHtmlLocalized(10, 330, 300, 20, 1060690, LabelColor); // Maximum Lockdowns
                            builder.AddLabel(310, 330, LabelHue, $"{maxLockdowns}");

                            builder.AddHtmlLocalized(10, 350, 300, 20, 1060691, LabelColor); // Available Lockdowns
                            builder.AddLabel(310, 350, LabelHue, $"{Math.Max(maxLockdowns - curLockdowns, 0)}");
                        }

                        break;
                    }
                case HouseGumpPageAOS.Customize:
                    {
                        var isCustomizable = isOwner && _house is HouseFoundation;

                        AddButtonLabeled(
                            ref builder,
                            10,
                            120,
                            GetButtonID(5, 0),
                            1060759,
                            isOwner && !isCustomizable && _house.ConvertEntry != null
                        ); // Convert Into Customizable House
                        AddButtonLabeled(
                            ref builder,
                            10,
                            160,
                            GetButtonID(5, 1),
                            1060765,
                            isOwner && isCustomizable
                        ); // Customize This House
                        AddButtonLabeled(
                            ref builder,
                            10,
                            180,
                            GetButtonID(5, 2),
                            1060760,
                            isOwner && _house.MovingCrate != null
                        ); // Relocate Moving Crate
                        AddButtonLabeled(ref builder, 10, 210, GetButtonID(5, 3), 1060761, isOwner && _house.Public); // Change House Sign
                        AddButtonLabeled(
                            ref builder,
                            10,
                            230,
                            GetButtonID(5, 4),
                            1060762,
                            isOwner && isCustomizable
                        ); // Change House Sign Hanger
                        AddButtonLabeled(
                            ref builder,
                            10,
                            250,
                            GetButtonID(5, 5),
                            1060763,
                            isOwner && isCustomizable && ((HouseFoundation)_house).Signpost != null
                        ); // Change Signpost
                        AddButtonLabeled(
                            ref builder,
                            10,
                            280,
                            GetButtonID(5, 6),
                            1062004,
                            isOwner && isCustomizable
                        ); // Change Foundation Style
                        AddButtonLabeled(ref builder, 10, 310, GetButtonID(5, 7), 1060764, isCoOwner); // Rename House

                        break;
                    }
                case HouseGumpPageAOS.Ownership:
                    {
                        AddButtonLabeled(
                            ref builder,
                            10,
                            130,
                            GetButtonID(6, 0),
                            1061794,
                            isOwner && _house.MovingCrate == null && _house.InternalizedVendors.Count == 0
                        ); // Demolish House
                        AddButtonLabeled(ref builder, 10, 150, GetButtonID(6, 1), 1061797, isOwner); // Trade House
                        AddButtonLabeled(ref builder, 10, 190, GetButtonID(6, 2), 1061798, false);   // Make Primary

                        break;
                    }
                case HouseGumpPageAOS.ChangeHanger:
                    {
                        for (var i = 0; i < _hangerNumbers.Length; ++i)
                        {
                            var x = 50 + i % 3 * 100;
                            var y = 180 + i / 3 * 80;

                            builder.AddButton(x, y, 4005, 4007, GetButtonID(7, i));
                            builder.AddItem(x + 20, y, _hangerNumbers[i]);
                        }

                        break;
                    }
                case HouseGumpPageAOS.ChangeFoundation:
                    {
                        for (var i = 0; i < _foundationNumbers.Length; ++i)
                        {
                            var x = 15 + i % 5 * 80;
                            var y = 180 + i / 5 * 100;

                            builder.AddButton(x, y, 4005, 4007, GetButtonID(8, i));
                            builder.AddItem(x + 25, y, _foundationNumbers[i]);
                        }

                        break;
                    }
                case HouseGumpPageAOS.ChangeSign:
                    {
                        var signsPerPage = Core.ML ? 24 : 18;
                        var totalSigns = Core.ML ? 56 : 54;
                        var pages = (int)Math.Ceiling((double)totalSigns / signsPerPage);
                        var index = 0;

                        for (var i = 0; i < pages; ++i)
                        {
                            builder.AddPage(i + 1);

                            builder.AddButton(10, 360, 4005, 4007, 0, GumpButtonType.Page, (i + 1) % pages + 1);

                            for (var j = 0; j < signsPerPage && totalSigns - signsPerPage * i - j > 0; ++j)
                            {
                                var x = 30 + j % 6 * 60;
                                var y = 130 + j / 6 * 60;

                                builder.AddButton(x, y, 4005, 4007, GetButtonID(9, index));
                                builder.AddItem(x + 20, y, _houseSigns[index++]);
                            }
                        }

                        break;
                    }
                case HouseGumpPageAOS.RemoveCoOwner:
                    {
                        builder.AddHtmlLocalized(10, 120, 400, 20, 1060730, LabelColor); // <CENTER>CO-OWNER LIST</CENTER>
                        AddList(ref builder, _house.CoOwners, 10, false, true);
                        break;
                    }
                case HouseGumpPageAOS.ListCoOwner:
                    {
                        builder.AddHtmlLocalized(10, 120, 400, 20, 1060730, LabelColor); // <CENTER>CO-OWNER LIST</CENTER>
                        AddList(ref builder, _house.CoOwners, -1, false, true);
                        break;
                    }
                case HouseGumpPageAOS.RemoveFriend:
                    {
                        builder.AddHtmlLocalized(10, 120, 400, 20, 1060731, LabelColor); // <CENTER>FRIENDS LIST</CENTER>
                        AddList(ref builder, _house.Friends, 11, false, true);
                        break;
                    }
                case HouseGumpPageAOS.ListFriend:
                    {
                        builder.AddHtmlLocalized(10, 120, 400, 20, 1060731, LabelColor); // <CENTER>FRIENDS LIST</CENTER>
                        AddList(ref builder, _house.Friends, -1, false, true);
                        break;
                    }
                case HouseGumpPageAOS.RemoveBan:
                    {
                        builder.AddHtmlLocalized(10, 120, 400, 20, 1060733, LabelColor); // <CENTER>BAN LIST</CENTER>
                        AddList(ref builder, _house.Bans, 12, true, true);
                        break;
                    }
                case HouseGumpPageAOS.ListBan:
                    {
                        builder.AddHtmlLocalized(10, 120, 400, 20, 1060733, LabelColor); // <CENTER>BAN LIST</CENTER>
                        AddList(ref builder, _house.Bans, -1, true, true);
                        break;
                    }
                case HouseGumpPageAOS.RemoveAccess:
                    {
                        builder.AddHtmlLocalized(10, 120, 400, 20, 1060732, LabelColor); // <CENTER>ACCESS LIST</CENTER>
                        AddList(ref builder, _house.Access, 13, false, true);
                        break;
                    }
                case HouseGumpPageAOS.ListAccess:
                    {
                        builder.AddHtmlLocalized(10, 120, 400, 20, 1060732, LabelColor); // <CENTER>ACCESS LIST</CENTER>
                        AddList(ref builder, _house.Access, -1, false, true);
                        break;
                    }
                case HouseGumpPageAOS.ChangePost:
                    {
                        var index = 0;

                        for (var i = 0; i < 2; ++i)
                        {
                            builder.AddPage(i + 1);

                            builder.AddButton(10, 360, 4005, 4007, 0, GumpButtonType.Page, (i + 1) % 2 + 1);

                            for (var j = 0; j < 16 && index < _postNumbers.Length; ++j)
                            {
                                var x = 15 + j % 8 * 50;
                                var y = 130 + j / 8 * 110;

                                builder.AddButton(x, y, 4005, 4007, GetButtonID(14, index));
                                builder.AddItem(x + 10, y, _postNumbers[index++]);
                            }
                        }

                        break;
                    }
            }
        }

        private string GetOwnerName()
        {
            var m = _house.Owner;

            return m?.Deleted != false ? "(unowned)" : m.Name.Trim().DefaultIfNullOrEmpty("(no name)");
        }

        private void AddPageButton(ref DynamicGumpBuilder builder, int x, int y, int buttonID, int number, HouseGumpPageAOS page)
        {
            var isSelection = _page == page;

            builder.AddButton(x, y, isSelection ? 4006 : 4005, 4007, buttonID);
            builder.AddHtmlLocalized(x + 45, y, 200, 20, number, isSelection ? SelectedColor : LabelColor);
        }

        private static void AddButtonLabeled(ref DynamicGumpBuilder builder, int x, int y, int buttonID, int number, bool enabled = true)
        {
            if (enabled)
            {
                builder.AddButton(x, y, 4005, 4007, buttonID);
            }

            builder.AddHtmlLocalized(x + 35, y, 240, 20, number, enabled ? LabelColor : DisabledColor);
        }

        private void AddList(ref DynamicGumpBuilder builder, List<Mobile> list, int button, bool accountOf, bool leadingStar)
        {
            if (list == null)
            {
                return;
            }

            _list = new List<Mobile>(list);

            var lastPage = 0;
            var index = 0;

            for (var i = 0; i < list.Count; ++i)
            {
                var xoffset = index % 20 / 10 * 200;
                var yoffset = index % 10 * 20;
                var page = 1 + index / 20;

                if (page != lastPage)
                {
                    if (lastPage != 0)
                    {
                        builder.AddButton(40, 360, 4005, 4007, 0, GumpButtonType.Page, page);
                    }

                    builder.AddPage(page);

                    if (lastPage != 0)
                    {
                        builder.AddButton(10, 360, 4014, 4016, 0, GumpButtonType.Page, lastPage);
                    }

                    lastPage = page;
                }

                var m = list[i];

                string name;
                var labelHue = LabelHue;

                if (m is PlayerVendor vendor)
                {
                    name = vendor.ShopName;

                    if (vendor.IsOwner(_from))
                    {
                        labelHue = HighlightedLabelHue;
                    }
                }
                else if (m != null)
                {
                    name = m.Name;
                }
                else
                {
                    continue;
                }

                if ((name = name.Trim()).Length <= 0)
                {
                    continue;
                }

                if (button != -1)
                {
                    builder.AddButton(10 + xoffset, 150 + yoffset, 4005, 4007, GetButtonID(button, i));
                }

                if (accountOf && m.Player && m.Account != null)
                {
                    name = $"Account of {name}";
                }

                if (leadingStar)
                {
                    name = $"* {name}";
                }

                builder.AddLabel(button > 0 ? 45 + xoffset : 10 + xoffset, 150 + yoffset, labelHue, name);
                ++index;
            }
        }

        public static int GetButtonID(int type, int index) => 1 + index * 15 + type;

        public static void PublicPrivateNotice_Callback(Mobile from, BaseHouse house)
        {
            if (!house.Deleted)
            {
                DisplayTo(from, house, HouseGumpPageAOS.Security);
            }
        }

        public static void CustomizeNotice_Callback(Mobile from, BaseHouse house)
        {
            if (!house.Deleted)
            {
                DisplayTo(from, house, HouseGumpPageAOS.Customize);
            }
        }

        public static void ClearCoOwners_Callback(Mobile from, bool okay, BaseHouse house)
        {
            if (house.Deleted)
            {
                return;
            }

            if (okay && house.IsOwner(from))
            {
                house.CoOwners?.Clear();

                from.SendLocalizedMessage(501333); // All co-owners have been removed from this house.
            }

            DisplayTo(from, house, HouseGumpPageAOS.Security);
        }

        public static void ClearFriends_Callback(Mobile from, bool okay, BaseHouse house)
        {
            if (house.Deleted)
            {
                return;
            }

            if (okay && house.IsCoOwner(from))
            {
                house.Friends?.Clear();

                from.SendLocalizedMessage(501332); // All friends have been removed from this house.
            }

            DisplayTo(from, house, HouseGumpPageAOS.Security);
        }

        public static void ClearBans_Callback(Mobile from, bool okay, BaseHouse house)
        {
            if (house.Deleted)
            {
                return;
            }

            if (okay && house.IsFriend(from))
            {
                house.Bans?.Clear();

                from.SendLocalizedMessage(1060754); // All bans for this house have been lifted.
            }

            DisplayTo(from, house, HouseGumpPageAOS.Security);
        }

        public static void ClearAccess_Callback(Mobile from, bool okay, BaseHouse house)
        {
            if (house.Deleted)
            {
                return;
            }

            if (okay && house.IsFriend(from))
            {
                var list = house.Access.ToList();

                house.Access?.Clear();

                for (var i = 0; i < list.Count; ++i)
                {
                    var m = list[i];

                    if (!house.HasAccess(m) && house.IsInside(m))
                    {
                        m.Location = house.BanLocation;
                        m.SendLocalizedMessage(1060734); // Your access to this house has been revoked.
                    }
                }

                from.SendLocalizedMessage(1061843); // This house's Access List has been cleared.
            }

            DisplayTo(from, house, HouseGumpPageAOS.Security);
        }

        public static void ConvertHouse_Callback(Mobile from, bool okay, BaseHouse house)
        {
            if (house.Deleted)
            {
                return;
            }

            if (okay && house.IsOwner(from) && !house.HasRentedVendors)
            {
                var e = house.ConvertEntry;

                if (e == null)
                {
                    return;
                }

                var cost = e.Cost - house.Price;

                if (cost > 0)
                {
                    if (Banker.Withdraw(from, cost))
                    {
                        from.SendLocalizedMessage(
                            1060398,
                            cost.ToString()
                        ); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                    }
                    else
                    {
                        from.SendLocalizedMessage(
                            1061624
                        ); // You do not have enough funds in your bank to cover the difference between your old house and your new one.
                        return;
                    }
                }
                else if (cost < 0)
                {
                    if (Banker.Deposit(from, -cost))
                    {
                        from.SendLocalizedMessage(
                            1060397,
                            (-cost).ToString()
                        ); // ~1_AMOUNT~ gold has been deposited into your bank box.
                    }
                    else
                    {
                        return;
                    }
                }

                var newHouse = e.ConstructHouse(from);

                if (newHouse != null)
                {
                    newHouse.Price = e.Cost;

                    house.MoveAllToCrate();

                    newHouse.Friends = new List<Mobile>(house.Friends);
                    newHouse.CoOwners = new List<Mobile>(house.CoOwners);
                    newHouse.Bans = new List<Mobile>(house.Bans);
                    newHouse.Access = new List<Mobile>(house.Access);
                    newHouse.BuiltOn = house.BuiltOn;
                    newHouse.LastTraded = house.LastTraded;
                    newHouse.Public = house.Public;

                    newHouse.VendorInventories.AddRange(house.VendorInventories);
                    house.VendorInventories.Clear();

                    foreach (var inventory in newHouse.VendorInventories)
                    {
                        inventory.House = newHouse;
                    }

                    newHouse.InternalizedVendors.AddRange(house.InternalizedVendors);
                    house.InternalizedVendors.Clear();

                    foreach (var mobile in newHouse.InternalizedVendors)
                    {
                        if (mobile is PlayerVendor vendor)
                        {
                            vendor.House = newHouse;
                        }
                        else if (mobile is PlayerBarkeeper barkeeper)
                        {
                            barkeeper.House = newHouse;
                        }
                    }

                    if (house.MovingCrate != null)
                    {
                        newHouse.MovingCrate = house.MovingCrate;
                        newHouse.MovingCrate.House = newHouse;
                        house.MovingCrate = null;
                    }

                    var items = house.GetItems();
                    using var mobiles = house.GetMobilesPooled();

                    newHouse.MoveToWorld(
                        new Point3D(
                            house.X + house.ConvertOffsetX,
                            house.Y + house.ConvertOffsetY,
                            house.Z + house.ConvertOffsetZ
                        ),
                        house.Map
                    );
                    house.Delete();

                    foreach (var item in items)
                    {
                        item.Location = newHouse.BanLocation;
                    }

                    foreach (var mobile in mobiles)
                    {
                        mobile.Location = newHouse.BanLocation;
                    }

                    /* You have successfully replaced your original house with a new house.
                     * The value of the replaced house has been deposited into your bank box.
                     * All of the items in your original house have been relocated to a Moving Crate in the new house.
                     * Any deed-based house add-ons have been converted back into deeds.
                     * Vendors and barkeeps in the house, if any, have been stored in the Moving Crate as well.
                     * Use the <B>Get Vendor</B> context-sensitive menu option on your character to retrieve them.
                     * These containers can be used to re-create the vendor in a new location.
                     * Any barkeepers have been converted into deeds.
                     */
                    from.SendGump(new ReplaceHouseNoticeGump());
                    return;
                }
            }

            DisplayTo(from, house, HouseGumpPageAOS.Security);
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_house.Deleted)
            {
                return;
            }

            var from = sender.Mobile;

            var isCombatRestricted = _house.IsCombatRestricted(from);

            var isOwner = _house.IsOwner(from);
            var isCoOwner = isOwner || _house.IsCoOwner(from);
            var isFriend = isCoOwner || _house.IsFriend(from);

            if (isCombatRestricted)
            {
                isCoOwner = isFriend = false;
            }

            if (!from.CheckAlive())
            {
                return;
            }

            Item sign = _house.Sign;

            if (sign == null || from.Map != sign.Map || !from.InRange(sign.GetWorldLocation(), 18))
            {
                return;
            }

            var foundation = _house as HouseFoundation;
            var isCustomizable = foundation != null;

            var val = info.ButtonID - 1;

            if (val < 0)
            {
                return;
            }

            var type = val % 15;
            var index = val / 15;

            if (_page == HouseGumpPageAOS.Vendors)
            {
                if (index < _list.Count)
                {
                    var vendor = (PlayerVendor)_list[index];

                    if (!vendor.CanInteractWith(from, false))
                    {
                        return;
                    }

                    if (from.Map != sign.Map || !from.InRange(sign, 5))
                    {
                        from.SendLocalizedMessage(
                            1062429
                        ); // You must be within five paces of the house sign to use this option.
                    }
                    else if (vendor.IsOwner(from))
                    {
                        vendor.SendOwnerGump(from);
                    }
                    else
                    {
                        vendor.OpenBackpack(from);
                    }
                }

                return;
            }

            if (!isFriend)
            {
                return;
            }

            switch (type)
            {
                case 0:
                    {
                        switch (index)
                        {
                            case 0: // Banish
                                {
                                    if (_house.Public)
                                    {
                                        from.SendLocalizedMessage(501325); // Target the individual to ban from this house.
                                        from.Target = new HouseBanTarget(true, _house);
                                    }

                                    break;
                                }
                            case 1: // Lift Ban
                                {
                                    if (_house.Public)
                                    {
                                        DisplayTo(from, _house, HouseGumpPageAOS.RemoveBan);
                                    }

                                    break;
                                }
                            case 2: // Grant Access
                                {
                                    if (!_house.Public)
                                    {
                                        from.SendLocalizedMessage(
                                            1060711
                                        ); // Target the person you would like to grant access to.
                                        from.Target = new HouseAccessTarget(_house);
                                    }

                                    break;
                                }
                            case 3: // Revoke Access
                                {
                                    if (!_house.Public)
                                    {
                                        DisplayTo(from, _house, HouseGumpPageAOS.RemoveAccess);
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 1:
                    {
                        HouseGumpPageAOS page;

                        switch (index)
                        {
                            case 0:
                                {
                                    page = HouseGumpPageAOS.Information;
                                    break;
                                }
                            case 1:
                                {
                                    page = HouseGumpPageAOS.Security;
                                    break;
                                }
                            case 2:
                                {
                                    page = HouseGumpPageAOS.Storage;
                                    break;
                                }
                            case 3:
                                {
                                    page = HouseGumpPageAOS.Customize;
                                    break;
                                }
                            case 4:
                                {
                                    page = HouseGumpPageAOS.Ownership;
                                    break;
                                }
                            default:
                                {
                                    return;
                                }
                        }

                        DisplayTo(from, _house, page);
                        break;
                    }
                case 3:
                    {
                        switch (index)
                        {
                            case 0: // View Co-Owner List
                                {
                                    if (isCoOwner)
                                    {
                                        DisplayTo(from, _house, HouseGumpPageAOS.ListCoOwner);
                                    }

                                    break;
                                }
                            case 1: // Add a Co-Owner
                                {
                                    if (isOwner)
                                    {
                                        from.SendLocalizedMessage(
                                            501328
                                        ); // Target the person you wish to name a co-owner of your household.
                                        from.Target = new CoOwnerTarget(true, _house);
                                    }

                                    break;
                                }
                            case 2: // Remove a Co-Owner
                                {
                                    if (isOwner)
                                    {
                                        DisplayTo(from, _house, HouseGumpPageAOS.RemoveCoOwner);
                                    }

                                    break;
                                }
                            case 3: // Clear Co-Owner List
                                {
                                    if (isOwner)
                                    {
                                        from.SendGump(
                                            new RemoveAllCoOwnersWarningGump(
                                                okay => ClearCoOwners_Callback(from, okay, _house)
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 4: // View Friends List
                                {
                                    DisplayTo(from, _house, HouseGumpPageAOS.ListFriend);

                                    break;
                                }
                            case 5: // Add a Friend
                                {
                                    if (isCoOwner)
                                    {
                                        from.SendLocalizedMessage(
                                            501317
                                        ); // Target the person you wish to name a friend of your household.
                                        from.Target = new HouseFriendTarget(true, _house);
                                    }

                                    break;
                                }
                            case 6: // Remove a Friend
                                {
                                    if (isCoOwner)
                                    {
                                        DisplayTo(from, _house, HouseGumpPageAOS.RemoveFriend);
                                    }

                                    break;
                                }
                            case 7: // Clear Friend List
                                {
                                    if (isCoOwner)
                                    {
                                        from.SendGump(
                                            new RemoveAllFriendsWarningGump(
                                                okay => ClearFriends_Callback(from, okay, _house)
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 8: // View Ban List
                                {
                                    DisplayTo(from, _house, HouseGumpPageAOS.ListBan);

                                    break;
                                }
                            case 9: // Clear Ban List
                                {
                                    from.SendGump(
                                        new LifeAllBansWarningGump(okay => ClearBans_Callback(from, okay, _house))
                                    );

                                    break;
                                }
                            case 10: // View Access List
                                {
                                    DisplayTo(from, _house, HouseGumpPageAOS.ListAccess);

                                    break;
                                }
                            case 11: // Clear Access List
                                {
                                    from.SendGump(
                                        new RevokeAccessWarning(okay => ClearAccess_Callback(from, okay, _house))
                                    );

                                    break;
                                }
                            case 12: // Make Private
                                {
                                    if (isOwner)
                                    {
                                        if (_house.PlayerVendors.Count > 0)
                                        {
                                            from.SendGump(
                                                new CannotConvertPrivateNoticeGump(
                                                    () => PublicPrivateNotice_Callback(from, _house)
                                                )
                                            );
                                            break;
                                        }

                                        if (_house.VendorRentalContracts.Count > 0)
                                        {
                                            from.SendGump(
                                                new CannotPerformActionContractsNoticeGump(
                                                    () => PublicPrivateNotice_Callback(from, _house)
                                                )
                                            );
                                            break;
                                        }

                                        _house.Public = false;

                                        _house.ChangeLocks(from);

                                        from.SendGump(
                                            new HouseConvertedPrivateNoticeGump(
                                                () => PublicPrivateNotice_Callback(from, _house)
                                            )
                                        );

                                        var r = _house.Region;
                                        using var list = r.GetMobilesPooled();

                                        for (var i = 0; i < list.Count; ++i)
                                        {
                                            var m = list[i];

                                            if (!_house.HasAccess(m) && _house.IsInside(m))
                                            {
                                                m.Location = _house.BanLocation;
                                            }
                                        }
                                    }

                                    break;
                                }
                            case 13: // Make Public
                                {
                                    if (isOwner)
                                    {
                                        _house.Public = true;

                                        _house.RemoveKeys(from);
                                        _house.RemoveLocks();

                                        if (BaseHouse.NewVendorSystem)
                                        {
                                            from.SendGump(
                                                new HouseConvertedPublicNoticeGump(
                                                    () => PublicPrivateNotice_Callback(from, _house)
                                                )
                                            );
                                        }
                                        else
                                        {
                                            from.SendGump(
                                                new HouseConvertedPublicOldSystemNoticeGump(
                                                    () => PublicPrivateNotice_Callback(from, _house)
                                                )
                                            );
                                        }

                                        var r = _house.Region;
                                        using var list = r.GetMobilesPooled();

                                        for (var i = 0; i < list.Count; ++i)
                                        {
                                            var m = list[i];

                                            if (_house.IsBanned(m) && _house.IsInside(m))
                                            {
                                                m.Location = _house.BanLocation;
                                            }
                                        }
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 5:
                    {
                        switch (index)
                        {
                            case 0: // Convert Into Customizable House
                                {
                                    if (isOwner && !isCustomizable)
                                    {
                                        if (_house.HasRentedVendors)
                                        {
                                            from.SendGump(
                                                new CannotPerformActionVendorsNoticeGump(
                                                    () => CustomizeNotice_Callback(from, _house)
                                                )
                                            );
                                        }
                                        else if (_house.ConvertEntry != null)
                                        {
                                            from.SendGump(
                                                new ConvertToCustomHouseWarningGump(
                                                    okay => ConvertHouse_Callback(from, okay, _house)
                                                )
                                            );
                                        }
                                    }

                                    break;
                                }
                            case 1: // Customize This House
                                {
                                    if (isOwner && isCustomizable)
                                    {
                                        if (_house.HasRentedVendors)
                                        {
                                            from.SendGump(
                                                new CannotPerformActionVendorsNoticeGump(
                                                    () => CustomizeNotice_Callback(from, _house)
                                                )
                                            );
                                        }
                                        else if (_house.HasAddonContainers)
                                        {
                                            from.SendGump(
                                                new CannotCustomizeAddonsNoticeGump(
                                                    () => CustomizeNotice_Callback(from, _house)
                                                )
                                            );
                                        }
                                        else
                                        {
                                            foundation.BeginCustomize(from);
                                        }
                                    }

                                    break;
                                }
                            case 2: // Relocate Moving Crate
                                {
                                    var crate = _house.MovingCrate;

                                    if (isOwner && crate != null)
                                    {
                                        if (!_house.IsInside(from))
                                        {
                                            from.SendLocalizedMessage(502092); // You must be in your house to do this.
                                        }
                                        else
                                        {
                                            crate.MoveToWorld(from.Location, from.Map);
                                            crate.RestartTimer();
                                        }
                                    }

                                    break;
                                }
                            case 3: // Change House Sign
                                {
                                    if (isOwner && _house.Public)
                                    {
                                        DisplayTo(from, _house, HouseGumpPageAOS.ChangeSign);
                                    }

                                    break;
                                }
                            case 4: // Change House Sign Hanger
                                {
                                    if (isOwner && isCustomizable)
                                    {
                                        DisplayTo(from, _house, HouseGumpPageAOS.ChangeHanger);
                                    }

                                    break;
                                }
                            case 5: // Change Signpost
                                {
                                    if (isOwner && isCustomizable && foundation.Signpost != null)
                                    {
                                        DisplayTo(from, _house, HouseGumpPageAOS.ChangePost);
                                    }

                                    break;
                                }
                            case 6: // Change Foundation Style
                                {
                                    if (isOwner && isCustomizable)
                                    {
                                        DisplayTo(from, _house, HouseGumpPageAOS.ChangeFoundation);
                                    }

                                    break;
                                }
                            case 7: // Rename House
                                {
                                    if (isCoOwner)
                                    {
                                        from.Prompt = new RenamePrompt(_house);
                                        from.SendLocalizedMessage(501302); // What dost thou wish the sign to say?
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 6:
                    {
                        switch (index)
                        {
                            case 0: // Demolish
                                {
                                    if (isOwner && _house.MovingCrate == null && _house.InternalizedVendors.Count == 0)
                                    {
                                        if (!Guild.NewGuildSystem && _house.FindGuildstone() != null)
                                        {
                                            from.SendLocalizedMessage(
                                                501389
                                            ); // You cannot redeed a house with a guildstone inside.
                                        }
                                        else if (Core.ML && from.AccessLevel < AccessLevel.GameMaster &&
                                                 Core.Now <= _house.BuiltOn.AddHours(1))
                                        {
                                            from.SendLocalizedMessage(
                                                1080178
                                            ); // You must wait one hour between each house demolition.
                                        }
                                        else
                                        {
                                            from.SendGump(new ConfirmDemolishHouseGump(_house));
                                        }
                                    }

                                    break;
                                }
                            case 1: // Trade House
                                {
                                    if (isOwner)
                                    {
                                        if (BaseHouse.NewVendorSystem && _house.HasPersonalVendors)
                                        {
                                            from.SendLocalizedMessage(
                                                1062467
                                            ); // You cannot trade this house while you still have personal vendors inside.
                                        }
                                        else if (_house.DecayLevel == DecayLevel.DemolitionPending)
                                        {
                                            from.SendLocalizedMessage(
                                                1005321
                                            ); // This house has been marked for demolition, and it cannot be transferred.
                                        }
                                        else
                                        {
                                            from.SendLocalizedMessage(
                                                501309
                                            ); // Target the person to whom you wish to give this house.
                                            from.Target = new HouseOwnerTarget(_house);
                                        }
                                    }

                                    break;
                                }
                            case 2: // Make Primary
                                {
                                    break;
                                }
                        }

                        break;
                    }
                case 7:
                    {
                        if (isOwner && isCustomizable && index < _hangerNumbers.Length)
                        {
                            var hanger = foundation.SignHanger;

                            if (hanger != null)
                            {
                                hanger.ItemID = _hangerNumbers[index];
                            }

                            DisplayTo(from, _house, HouseGumpPageAOS.Customize);
                        }

                        break;
                    }
                case 8:
                    {
                        if (isOwner && isCustomizable)
                        {
                            FoundationType newType;

                            if (Core.ML && index >= 5)
                            {
                                switch (index)
                                {
                                    case 5:
                                        {
                                            newType = FoundationType.ElvenGrey;
                                            break;
                                        }
                                    case 6:
                                        {
                                            newType = FoundationType.ElvenNatural;
                                            break;
                                        }
                                    case 7:
                                        {
                                            newType = FoundationType.Crystal;
                                            break;
                                        }
                                    case 8:
                                        {
                                            newType = FoundationType.Shadow;
                                            break;
                                        }
                                    default:
                                        {
                                            return;
                                        }
                                }
                            }
                            else
                            {
                                switch (index)
                                {
                                    case 0:
                                        {
                                            newType = FoundationType.DarkWood;
                                            break;
                                        }
                                    case 1:
                                        {
                                            newType = FoundationType.LightWood;
                                            break;
                                        }
                                    case 2:
                                        {
                                            newType = FoundationType.Dungeon;
                                            break;
                                        }
                                    case 3:
                                        {
                                            newType = FoundationType.Brick;
                                            break;
                                        }
                                    case 4:
                                        {
                                            newType = FoundationType.Stone;
                                            break;
                                        }
                                    default:
                                        {
                                            return;
                                        }
                                }
                            }

                            foundation.Type = newType;

                            var state = foundation.BackupState;
                            HouseFoundation.ApplyFoundation(newType, state.Components);
                            state.OnRevised();

                            state = foundation.DesignState;
                            HouseFoundation.ApplyFoundation(newType, state.Components);
                            state.OnRevised();

                            state = foundation.CurrentState;
                            HouseFoundation.ApplyFoundation(newType, state.Components);
                            state.OnRevised();

                            foundation.Delta(ItemDelta.Update);

                            DisplayTo(from, _house, HouseGumpPageAOS.Customize);
                        }

                        break;
                    }
                case 9:
                    {
                        if (isOwner && _house.Public && index < _houseSigns.Length)
                        {
                            _house.ChangeSignType(_houseSigns[index]);
                            DisplayTo(from, _house, HouseGumpPageAOS.Customize);
                        }

                        break;
                    }
                case 10:
                    {
                        if (isOwner && index < _list?.Count)
                        {
                            _house.RemoveCoOwner(from, _list[index]);

                            if (_house.CoOwners.Count > 0)
                            {
                                DisplayTo(from, _house, HouseGumpPageAOS.RemoveCoOwner);
                            }
                            else
                            {
                                DisplayTo(from, _house, HouseGumpPageAOS.Security);
                            }
                        }

                        break;
                    }
                case 11:
                    {
                        if (isCoOwner && index < _list?.Count)
                        {
                            _house.RemoveFriend(from, _list[index]);

                            if (_house.Friends.Count > 0)
                            {
                                DisplayTo(from, _house, HouseGumpPageAOS.RemoveFriend);
                            }
                            else
                            {
                                DisplayTo(from, _house, HouseGumpPageAOS.Security);
                            }
                        }

                        break;
                    }
                case 12:
                    {
                        if (index < _list?.Count)
                        {
                            _house.RemoveBan(from, _list[index]);

                            if (_house.Bans.Count > 0)
                            {
                                DisplayTo(from, _house, HouseGumpPageAOS.RemoveBan);
                            }
                            else
                            {
                                DisplayTo(from, _house, HouseGumpPageAOS.Security);
                            }
                        }

                        break;
                    }
                case 13:
                    {
                        if (index < _list?.Count)
                        {
                            _house.RemoveAccess(from, _list[index]);

                            if (_house.Access.Count > 0)
                            {
                                DisplayTo(from, _house, HouseGumpPageAOS.RemoveAccess);
                            }
                            else
                            {
                                DisplayTo(from, _house, HouseGumpPageAOS.Security);
                            }
                        }

                        break;
                    }
                case 14:
                    {
                        if (isOwner && isCustomizable && index < _postNumbers.Length)
                        {
                            foundation.SignpostGraphic = _postNumbers[index];
                            foundation.CheckSignpost();

                            DisplayTo(from, _house, HouseGumpPageAOS.Customize);
                        }

                        break;
                    }
            }
        }

        private class RemoveAllCoOwnersWarningGump : StaticWarningGump<RemoveAllCoOwnersWarningGump>
        {
            public override int Width => 420;
            public override int Height => 280;

            // You are about to remove ALL co-owners from your house.  Are you certain you wish to clear the co-owner list?
            public override int StaticLocalizedContent => 1060736;

            public RemoveAllCoOwnersWarningGump(Action<bool> callback) : base(callback)
            {
            }
        }

        private class RemoveAllFriendsWarningGump : StaticWarningGump<RemoveAllFriendsWarningGump>
        {
            public override int Width => 420;
            public override int Height => 280;

            /*
             * If you go ahead with this option, all of the current friendships will be removed from the house,
             * and any vendors associated with said friends will be made unwelcome. Are you sure you want to do this?
             */
            public override int StaticLocalizedContent => 1018039;

            public RemoveAllFriendsWarningGump(Action<bool> callback) : base(callback)
            {
            }
        }

        private class LifeAllBansWarningGump : StaticWarningGump<LifeAllBansWarningGump>
        {
            public override int Width => 420;
            public override int Height => 280;

            // You are about to lift all bans for this house.  Are you sure you wish to do this?
            public override int StaticLocalizedContent => 1060753;

            public LifeAllBansWarningGump(Action<bool> callback) : base(callback)
            {
            }
        }

        private class RevokeAccessWarning : StaticWarningGump<RevokeAccessWarning>
        {
            public override int Width => 420;
            public override int Height => 280;

            /*
             * This will revoke access from everyone on this list and immediately eject them from the house.
             * Those players will no longer be able to enter the house.
             * Are you sure you wish to clear the house access list?
             */
            public override int StaticLocalizedContent => 1061842;

            public RevokeAccessWarning(Action<bool> callback) : base(callback)
            {
            }
        }

        private class ConvertToCustomHouseWarningGump : StaticWarningGump<ConvertToCustomHouseWarningGump>
        {
            public override int Width => 420;
            public override int Height => 280;

            /*
             * You are about to turn your house into a customizable house.
             * You will be refunded or charged the value of this house minus the cost of the equivalent customizable dirt lot.
             * All of your possessions in the house will be transported to a Moving Crate.
             * Deed-based house add-ons will be converted back into deeds.
             * Vendors and barkeeps will also be stored in the Moving Crate.
             * Your house will be leveled to its foundation, and you will be able to build new walls, windows, doors, and stairs.
             * Are you sure you wish to continue?
             */
            public override int StaticLocalizedContent => 1060013;

            public ConvertToCustomHouseWarningGump(Action<bool> callback) : base(callback)
            {
            }
        }

        private class CannotPerformActionVendorsNoticeGump : StaticNoticeGump<CannotPerformActionVendorsNoticeGump>
        {
            public override int Width => 320;
            public override int Height => 180;

            // You cannot perform this action while you still have vendors rented out in this house.
            public override int StaticLocalizedContent => 1062395;

            public CannotPerformActionVendorsNoticeGump(Action callback) : base(callback)
            {
            }
        }

        private class CannotPerformActionContractsNoticeGump : StaticNoticeGump<CannotPerformActionContractsNoticeGump>
        {
            public override int Width => 320;
            public override int Height => 180;

            /*
             * You cannot currently take this action because you have vendor contracts locked down in your home.
             * You must remove them first.
             */
            public override int StaticLocalizedContent => 1062351;

            public CannotPerformActionContractsNoticeGump(Action callback) : base(callback)
            {
            }
        }

        private class CannotCustomizeAddonsNoticeGump : StaticNoticeGump<CannotCustomizeAddonsNoticeGump>
        {
            public override int Width => 320;
            public override int Height => 180;

            /*
             * The house cannot be customized when certain special house add-ons including aquariums, raised garden beds,
             * and special temporary add-ons are present in the house.
             * Please re-deed the special add-ons before customizing the house.
             */
            public override int StaticLocalizedContent => 1074863;

            public CannotCustomizeAddonsNoticeGump(Action callback) : base(callback)
            {
            }
        }

        private class ReplaceHouseNoticeGump : StaticNoticeGump<ReplaceHouseNoticeGump>
        {
            public override int Width => 420;
            public override int Height => 280;

            /*
             * You have successfully replaced your original house with a new house.
             * The value of the replaced house has been deposited into your bank box.
             * All of the items in your original house have been relocated to a Moving Crate in the new house.
             *  Any deed-based house add-ons have been converted back into deeds.
             * Vendors and barkeeps in the house, if any, have been stored in the Moving Crate as well.
             * Use the <B>Get Vendor</B> context-sensitive menu option on your character to retrieve them.
             * These containers can be used to re-create the vendor in a new location.
             * Any barkeepers have been converted into deeds.
             */
            public override int StaticLocalizedContent => 1060012;
        }

        private class HouseConvertedPublicNoticeGump : StaticNoticeGump<HouseConvertedPublicNoticeGump>
        {
            public override int Width => 320;
            public override int Height => 180;

            // This house is now public. The owner may now place vendors and vendor rental contracts.
            public override int StaticLocalizedContent => 501886;

            public HouseConvertedPublicNoticeGump(Action callback) : base(callback)
            {
            }
        }

        private class CannotConvertPrivateNoticeGump : StaticNoticeGump<CannotConvertPrivateNoticeGump>
        {
            public override int Width => 320;
            public override int Height => 180;

            /*
             * You have vendors working out of this building.
             * It cannot be declared private until there are no vendors in place.
             */
            public override int StaticLocalizedContent => 501887;

            public CannotConvertPrivateNoticeGump(Action callback) : base(callback)
            {
            }
        }

        private class HouseConvertedPrivateNoticeGump : StaticNoticeGump<HouseConvertedPrivateNoticeGump>
        {
            public override int Width => 320;
            public override int Height => 180;

            public override int StaticLocalizedContent => 501888; // This house is now private.

            public HouseConvertedPrivateNoticeGump(Action callback) : base(callback)
            {
            }
        }

        private class HouseConvertedPublicOldSystemNoticeGump : StaticNoticeGump<HouseConvertedPublicOldSystemNoticeGump>
        {
            public override int Width => 320;
            public override int Height => 180;

            public override string Content { get; }

            public HouseConvertedPublicOldSystemNoticeGump(Action callback) : base(callback) =>
                Content =
                    "This house is now public. Friends of the house may now have vendors working out of this building.";
        }
    }
}
