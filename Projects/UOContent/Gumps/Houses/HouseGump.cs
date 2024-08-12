using System.Collections.Generic;
using Server.Guilds;
using Server.Multis;
using Server.Network;
using Server.Prompts;

namespace Server.Gumps
{
    public class HouseListGump : DynamicGump
    {
        private readonly BaseHouse _house;
        private readonly bool _accountOf;
        private readonly List<Mobile> _list;
        private readonly int _number;

        public HouseListGump(int number, List<Mobile> list, BaseHouse house, bool accountOf) : base(20, 30)
        {
            _accountOf = accountOf;
            _list = list;
            _number = number;
            _house = house;
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            if (_house.Deleted)
            {
                return;
            }

            builder.AddPage();

            builder.AddBackground(0, 0, 420, 430, 5054);
            builder.AddBackground(10, 10, 400, 410, 3000);

            builder.AddButton(20, 388, 4005, 4007, 0);
            builder.AddHtmlLocalized(55, 388, 300, 20, 1011104); // Return to previous menu

            builder.AddHtmlLocalized(20, 20, 350, 20, _number);

            if (_list == null)
            {
                return;
            }

            for (var i = 0; i < _list.Count; ++i)
            {
                if (i % 16 == 0)
                {
                    if (i != 0)
                    {
                        builder.AddButton(370, 20, 4005, 4007, 0, GumpButtonType.Page, i / 16 + 1);
                    }

                    builder.AddPage(i / 16 + 1);

                    if (i != 0)
                    {
                        builder.AddButton(340, 20, 4014, 4016, 0, GumpButtonType.Page, i / 16);
                    }
                }

                var m = _list[i];

                string name;

                if (m == null || (name = m.Name) == null || (name = name.Trim()).Length <= 0)
                {
                    continue;
                }

                builder.AddLabel(
                    55,
                    55 + i % 16 * 20,
                    0,
                    _accountOf && m.Player && m.Account != null
                        ? $"Account of {name}"
                        : name
                );
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (_house.Deleted)
            {
                return;
            }

            var from = state.Mobile;

            from.SendGump(new HouseGump(from, _house));
        }
    }

    public class HouseRemoveGump : DynamicGump
    {
        private readonly bool _accountOf;
        private readonly List<Mobile> _copy;
        private readonly BaseHouse _house;
        private readonly List<Mobile> _list;
        private readonly int _number;

        public HouseRemoveGump(int number, List<Mobile> list, BaseHouse house, bool accountOf) : base(20, 30)
        {
            if (house.Deleted)
            {
                return;
            }

            _house = house;
            _list = list;
            _number = number;
            _accountOf = accountOf;

            if (list != null)
            {
                _copy = new List<Mobile>(list);
            }
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            if (_house.Deleted)
            {
                return;
            }

            builder.AddPage();

            builder.AddBackground(0, 0, 420, 430, 5054);
            builder.AddBackground(10, 10, 400, 410, 3000);

            builder.AddButton(20, 388, 4005, 4007, 0);
            builder.AddHtmlLocalized(55, 388, 300, 20, 1011104); // Return to previous menu

            builder.AddButton(20, 365, 4005, 4007, 1);
            builder.AddHtmlLocalized(55, 365, 300, 20, 1011270); // Remove now!

            builder.AddHtmlLocalized(20, 20, 350, 20, _number);

            if (_list == null)
            {
                return;
            }

            for (var i = 0; i < _list.Count; ++i)
            {
                if (i % 15 == 0)
                {
                    if (i != 0)
                    {
                        builder.AddButton(370, 20, 4005, 4007, 0, GumpButtonType.Page, i / 15 + 1);
                    }

                    builder.AddPage(i / 15 + 1);

                    if (i != 0)
                    {
                        builder.AddButton(340, 20, 4014, 4016, 0, GumpButtonType.Page, i / 15);
                    }
                }

                var m = _list[i];

                string name;

                if (m == null || (name = m.Name) == null || (name = name.Trim()).Length <= 0)
                {
                    continue;
                }

                builder.AddCheckbox(34, 52 + i % 15 * 20, 0xD2, 0xD3, false, i);
                builder.AddLabel(
                    55,
                    52 + i % 15 * 20,
                    0,
                    _accountOf && m.Player && m.Account != null
                        ? $"Account of {name}"
                        : name
                );
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (_house.Deleted)
            {
                return;
            }

            var from = state.Mobile;

            if (_list != null && info.ButtonID == 1) // Remove now
            {
                var switches = info.Switches;

                if (switches.Length > 0)
                {
                    for (var i = 0; i < switches.Length; ++i)
                    {
                        var index = switches[i];

                        if (index >= 0 && index < _copy.Count)
                        {
                            _list.Remove(_copy[index]);
                        }
                    }

                    if (_list.Count > 0)
                    {
                        var gumps = from.GetGumps();

                        gumps.Close<HouseGump>();
                        gumps.Close<HouseListGump>();
                        gumps.Close<HouseRemoveGump>();
                        gumps.Send(new HouseRemoveGump(_number, _list, _house, _accountOf));
                        return;
                    }
                }
            }

            from.SendGump(new HouseGump(from, _house));
        }
    }

    public class HouseSignSelectionGump : DynamicGump
    {
        private readonly BaseHouse _house;

        public override bool Singleton => true;

        public HouseSignSelectionGump(BaseHouse house) : base(20, 30) => _house = house;

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            if (_house.Deleted)
            {
                return;
            }

            builder.AddPage();

            builder.AddBackground(0, 0, 420, 430, 0x6DB);
            builder.AddBackground(5, 5, 410, 420, 0xBB8);

            builder.AddImage(15, 15, 100);

            if (_house.Sign != null)
            {
                var lines = _house.Sign.GetName().Wrap(10, 6);

                for (int i = 0, y = (101 - lines.Count * 14) / 2; i < lines.Count; ++i, y += 14)
                {
                    var s = lines[i];

                    builder.AddLabel(15 + (143 - s.Length * 8) / 2, 15 + y, 0, s);
                }
            }

            builder.AddPage(1);

            for (var i = 0; i < 24; ++i)
            {
                builder.AddRadio(16 + i / 4 * 65, 137 + i % 4 * 35, 210, 211, false, i + 1);
                builder.AddItem(36 + i / 4 * 65, 130 + i % 4 * 35, 2980 + i * 2);
            }

            builder.AddHtmlLocalized(240, 360, 129, 20, 1011254); // Guild sign choices
            builder.AddButton(360, 359, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 2);

            builder.AddHtmlLocalized(240, 390, 355, 30, 1011277); // Okay that is fine.
            builder.AddButton(360, 389, 0xFB7, 0xFB9, 1);

            builder.AddPage(2);

            for (var i = 0; i < 29; ++i)
            {
                builder.AddRadio(16 + i / 5 * 65, 137 + i % 5 * 35, 210, 211, false, i + 25);
                builder.AddItem(36 + i / 5 * 65, 130 + i % 5 * 35, 3028 + i * 2);
            }

            builder.AddHtmlLocalized(240, 360, 129, 20, 1011255); // Shop sign choices
            builder.AddButton(360, 359, 0xFAE, 0xFB0, 0, GumpButtonType.Page, 1);

            builder.AddHtmlLocalized(240, 390, 355, 30, 1011277); // Okay that is fine.
            builder.AddButton(360, 389, 0xFB7, 0xFB9, 1);
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (_house.Deleted)
            {
                return;
            }

            var from = state.Mobile;

            if (!_house.Public || info.Switches.Length <= 0 || info.ButtonID == 0)
            {
                from.SendGump(new HouseGump(from, _house));
                return;
            }

            var isCombatRestricted = _house.IsCombatRestricted(from);

            var isOwner = _house.IsOwner(from);
            var isCoOwner = isOwner || _house.IsCoOwner(from);
            var isFriend = isCoOwner || _house.IsFriend(from);

            if (isCombatRestricted)
            {
                isFriend = isOwner = false;
            }

            if (!isFriend || !from.Alive)
            {
                return;
            }

            if (!isOwner)
            {
                from.SendLocalizedMessage(501307); // Only the house owner may do this.
                from.SendGump(new HouseGump(from, _house));
                return;
            }

            var index = info.Switches[0] - 1;

            if (index is >= 0 and < 53)
            {
                _house.ChangeSignType(2980 + index * 2);
            }

            // back to HouseOptionsGump because that opened HouseSignSelectionGump
            from.SendGump(new HouseOptionsGump(_house));
        }
    }

    public class HouseOptionsGump : DynamicGump
    {
        private readonly BaseHouse _house;

        public override bool Singleton => true;

        public HouseOptionsGump(BaseHouse house) : base(20, 30) => _house = house;

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            if (_house.Deleted)
            {
                return;
            }

            builder.AddPage();

            builder.AddBackground(0, 0, 420, 430, 0x6DB);
            builder.AddBackground(5, 5, 410, 420, 0xBB8);

            builder.AddImage(15, 15, 100);

            if (_house.Sign != null)
            {
                var lines = _house.Sign.GetName().Wrap(10, 6);

                for (int i = 0, y = (101 - lines.Count * 14) / 2; i < lines.Count; ++i, y += 14)
                {
                    var s = lines[i];

                    builder.AddLabel(15 + (143 - s.Length * 8) / 2, 15 + y, 0, s);
                }
            }

            builder.AddItem(22, 144, 0xFBF); // scribe's pen
            builder.AddButton(65, 146, 0x16CD, 0x16CE, 1);
            builder.AddHtmlLocalized(95, 145, 200, 20, 1011236); // Change this house's name!

            builder.AddImage(10, 165, 0x1196); // right facing arrow
            builder.AddButton(65, 181, 0x16CD, 0x16CE, 2);
            builder.AddHtmlLocalized(95, 180, 200, 30, 1011248); // Transfer ownership of the house

            builder.AddItem(10, 211, 0x14F0); // deed
            builder.AddButton(65, 216, 0x16CD, 0x16CE, 3);
            builder.AddHtmlLocalized(95, 215, 300, 30, 1011249); // Demolish house and get deed back

            if (!_house.Public)
            {
                builder.AddItem(14, 252, 0x176B); // keyring with keys
                builder.AddButton(65, 251, 0x16CD, 0x16CE, 5);
                builder.AddHtmlLocalized(95, 250, 355, 30, 1011247); // Change the house locks

                builder.AddItem(14, 284, 0xBC4); // inn sign
                builder.AddButton(65, 286, 0x16CD, 0x16CE, 4);
                builder.AddHtmlLocalized(
                    95,
                    285,
                    300,
                    90,
                    1011253
                ); // Declare this building to be public. This will make your front door unlockable.
            }
            else
            {
                builder.AddItem(14, 247, 0xBD2); // default sign
                builder.AddButton(65, 251, 0x16CD, 0x16CE, 4);
                builder.AddHtmlLocalized(95, 250, 300, 30, 1011252); // Declare this building to be private.

                builder.AddItem(14, 284, 0xBCA); // artist sign
                builder.AddButton(65, 286, 0x16CD, 0x16CE, 6);
                builder.AddHtmlLocalized(95, 285, 200, 30, 1011250); // Change the sign type
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (_house.Deleted)
            {
                return;
            }

            var from = state.Mobile;

            if (info.ButtonID == 0)
            {
                from.SendGump(new HouseGump(from, _house));
                return;
            }

            var isCombatRestricted = _house.IsCombatRestricted(from);

            var isOwner = _house.IsOwner(from);
            var isCoOwner = isOwner || _house.IsCoOwner(from);
            var isFriend = isCoOwner || _house.IsFriend(from);

            if (isCombatRestricted)
            {
                isFriend = isOwner = false;
            }

            if (!isFriend || !from.Alive)
            {
                return;
            }

            Item sign = _house.Sign;

            if (sign == null || from.Map != sign.Map || !from.InRange(sign.GetWorldLocation(), 18))
            {
                return;
            }

            switch (info.ButtonID)
            {
                case 1: // Rename sign
                    {
                        from.Prompt = new RenamePrompt(_house);
                        from.SendLocalizedMessage(501302); // What dost thou wish the sign to say?

                        break;
                    }
                case 2: // Transfer ownership
                    {
                        if (!isOwner)
                        {
                            from.SendLocalizedMessage(501303); // Only the house owner may change the house locks.
                            break;
                        }

                        from.SendLocalizedMessage(501309); // Target the person to whom you wish to give this house.
                        from.Target = new HouseOwnerTarget(_house);

                        break;
                    }
                case 3: // Demolish house
                    {
                        if (isOwner)
                        {
                            if (!Guild.NewGuildSystem && _house.FindGuildstone() != null)
                            {
                                from.SendLocalizedMessage(501389); // You cannot redeed a house with a guildstone inside.
                            }
                            else
                            {
                                from.SendGump(new ConfirmDemolishHouseGump(_house));
                            }
                        }
                        else
                        {
                            from.SendLocalizedMessage(501320); // Only the house owner may do this.
                        }

                        from.SendGump(new HouseOptionsGump(_house));
                        break;
                    }
                case 4: // Declare public/private
                    {
                        if (!isOwner)
                        {
                            from.SendLocalizedMessage(501303); // Only the house owner may change the house locks.
                            break;
                        }

                        if (_house.Public && _house.PlayerVendors.Count > 0)
                        {
                            from.SendLocalizedMessage(
                                501887
                            ); // You have vendors working out of this building. It cannot be declared private until there are no vendors in place.
                            break;
                        }

                        _house.Public = !_house.Public;
                        if (!_house.Public)
                        {
                            _house.ChangeLocks(from);

                            from.SendLocalizedMessage(501888); // This house is now private.
                            from.SendLocalizedMessage(
                                501306
                            ); // The locks on your front door have been changed, and new master keys have been placed in your bank and your backpack.
                        }
                        else
                        {
                            _house.RemoveKeys(from);
                            _house.RemoveLocks();
                            from.SendLocalizedMessage(
                                501886
                            ); // This house is now public. Friends of the house my now have vendors working out of this building.
                        }

                        from.SendGump(new HouseOptionsGump(_house));
                        break;
                    }
                case 5: // Change locks
                    {
                        if (!isOwner)
                        {
                            from.SendLocalizedMessage(501303); // Only the house owner may change the house locks.
                            break;
                        }

                        if (_house.Public)
                        {
                            from.SendLocalizedMessage(501669); // Public houses are always unlocked.
                        }
                        else
                        {
                            _house.RemoveKeys(from);
                            _house.ChangeLocks(from);

                            from.SendLocalizedMessage(
                                501306
                            ); // The locks on your front door have been changed, and new master keys have been placed in your bank and your backpack.
                        }

                        from.SendGump(new HouseOptionsGump(_house));
                        break;
                    }
                case 6: // Change sign type
                    {
                        if (!isOwner)
                        {
                            from.SendLocalizedMessage(501303); // Only the house owner may change the house locks.
                            break;
                        }

                        from.SendGump(new HouseSignSelectionGump(_house));
                        break;
                    }
            }
        }
    }

    public class HouseGump : DynamicGump
    {
        private readonly BaseHouse _house;

        private readonly bool _isOwner;
        private readonly bool _isCoOwner;
        private readonly bool _isFriend;

        public override bool Singleton => true;


        public HouseGump(Mobile from, BaseHouse house) : base(20, 30)
        {
            if (house.Deleted)
            {
                return;
            }

            _house = house;

            from.CloseGump<HouseListGump>();
            from.CloseGump<HouseRemoveGump>();

            var isCombatRestricted = house.IsCombatRestricted(from);

            _isOwner = _house.IsOwner(from);
            _isCoOwner = _isOwner || _house.IsCoOwner(from);
            _isFriend = _isCoOwner || _house.IsFriend(from);

            if (isCombatRestricted)
            {
                _isFriend = _isCoOwner = _isOwner = false;
            }

            from.SendMessage("HouseGump");
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            if (_house.Deleted)
            {
                return;
            }

            builder.AddPage();

            if (_isFriend)
            {
                builder.AddBackground(0, 0, 420, 430, 0x6DB);
                builder.AddBackground(5, 5, 410, 420, 0xBB8);
            }

            builder.AddImage(15, 15, 100);

            if (_house.Sign != null)
            {
                var lines = _house.Sign.GetName().Wrap(10, 6);

                for (int i = 0, y = (101 - lines.Count * 14) / 2; i < lines.Count; ++i, y += 14)
                {
                    var s = lines[i];

                    builder.AddLabel(15 + (143 - s.Length * 8) / 2, 15 + y, 0, s);
                }
            }

            if (!_isFriend)
            {
                return;
            }

            builder.AddHtmlLocalized(335, 19, 75, 20, 1011233); // INFO
            builder.AddButton(370, 20, 0x16CD, 0x16CE, 0, GumpButtonType.Page, 1);

            builder.AddHtmlLocalized(314, 49, 75, 20, 1011234); // FRIENDS
            builder.AddButton(370, 50, 0x16CD, 0x16CE, 0, GumpButtonType.Page, 2);

            builder.AddHtmlLocalized(314, 79, 75, 20, 1011235); // OPTIONS
            // builder.AddButton(370, 79, 0x16CD, 0x16CE, 0, GumpButtonType.Page, 3);
            builder.AddButton(370, 79, 0x16CD, 0x16CE, 21); // HouseOptionsGump

            builder.AddButton(360, 389, 0xFB1, 0xFB3, 0);
            builder.AddHtmlLocalized(320, 390, 75, 20, 1011441); // EXIT

            // Info page
            builder.AddPage(1);

            builder.AddHtmlLocalized(20, 135, 100, 20, 1011242); // Owned by:
            builder.AddHtml(100, 135, 100, 20, GetOwnerName());

            builder.AddHtmlLocalized(20, 175, 275, 20, 1011237); // Number of locked down items:
            builder.AddHtml(320, 175, 50, 20, _house.LockDownCount.ToString());

            builder.AddHtmlLocalized(20, 200, 275, 20, 1011238); // Maximum locked down items:
            builder.AddHtml(320, 200, 50, 20, _house.MaxLockDowns.ToString());

            builder.AddHtmlLocalized(20, 225, 275, 20, 1011239); // Number of secure containers:
            builder.AddHtml(320, 225, 50, 20, _house.SecureCount.ToString());

            builder.AddHtmlLocalized(20, 250, 275, 20, 1011240); // Maximum number of secure containers:
            builder.AddHtml(320, 250, 50, 20, _house.MaxSecures.ToString());

            builder.AddHtmlLocalized(20, 290, 400, 20, 1018032); // This house is properly placed.
            builder.AddHtmlLocalized(20, 310, 400, 20, 1018035); // This house is of modern design.

            if (_house.Public)
            {
                // TODO: Validate exact placement
                builder.AddHtmlLocalized(20, 345, 275, 20, 1011241); // Number of visits this building has had
                builder.AddHtml(320, 345, 50, 20, _house.Visits.ToString());
            }

            // Friends page
            builder.AddPage(2);

            builder.AddHtmlLocalized(45, 130, 150, 20, 1011266); // List of co-owners
            builder.AddButton(20, 130, 2714, 2715, 2);

            builder.AddHtmlLocalized(45, 150, 150, 20, 1011267); // Add a co-owner
            builder.AddButton(20, 150, 2714, 2715, 3);

            builder.AddHtmlLocalized(45, 170, 150, 20, 1018036); // Remove a co-owner
            builder.AddButton(20, 170, 2714, 2715, 4);

            builder.AddHtmlLocalized(45, 190, 150, 20, 1011268); // Clear co-owner list
            builder.AddButton(20, 190, 2714, 2715, 5);

            builder.AddHtmlLocalized(225, 130, 155, 20, 1011243); // List of Friends
            builder.AddButton(200, 130, 2714, 2715, 6);

            builder.AddHtmlLocalized(225, 150, 155, 20, 1011244); // Add a Friend
            builder.AddButton(200, 150, 2714, 2715, 7);

            builder.AddHtmlLocalized(225, 170, 155, 20, 1018037); // Remove a Friend
            builder.AddButton(200, 170, 2714, 2715, 8);

            builder.AddHtmlLocalized(225, 190, 155, 20, 1011245); // Clear Friends list
            builder.AddButton(200, 190, 2714, 2715, 9);

            builder.AddHtmlLocalized(120, 215, 280, 20, 1011258); // Ban someone from the house
            builder.AddButton(95, 215, 2714, 2715, 10);

            builder.AddHtmlLocalized(120, 235, 280, 20, 1011259); // Eject someone from the house
            builder.AddButton(95, 235, 2714, 2715, 11);

            builder.AddHtmlLocalized(120, 255, 280, 20, 1011260); // View a list of banned people
            builder.AddButton(95, 255, 2714, 2715, 12);

            builder.AddHtmlLocalized(120, 275, 280, 20, 1011261); // Lift a ban
            builder.AddButton(95, 275, 2714, 2715, 13);
        }

        private string GetOwnerName()
        {
            var m = _house.Owner;

            if (m == null)
            {
                return "(unowned)";
            }

            string name;

            if ((name = m.Name) == null || (name = name.Trim()).Length <= 0)
            {
                name = "(no name)";
            }

            return name;
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
                isFriend = isCoOwner = isOwner = false;
            }

            if (!isFriend || !from.Alive)
            {
                return;
            }

            Item sign = _house.Sign;

            if (sign == null || from.Map != sign.Map || !from.InRange(sign.GetWorldLocation(), 18))
            {
                return;
            }

            var gumps = from.GetGumps();

            switch (info.ButtonID)
            {
                case 2: // List of co-owners
                    {
                        gumps.Close<HouseGump>();
                        gumps.Close<HouseRemoveGump>();
                        gumps.Close<HouseListGump>();
                        gumps.Send(new HouseListGump(1011275, _house.CoOwners, _house, false));

                        break;
                    }
                case 3: // Add co-owner
                    {
                        if (isOwner)
                        {
                            from.SendLocalizedMessage(
                                501328
                            ); // Target the person you wish to name a co-owner of your household.
                            from.Target = new CoOwnerTarget(true, _house);
                        }
                        else
                        {
                            from.SendLocalizedMessage(501327); // Only the house owner may add Co-owners.
                        }

                        break;
                    }
                case 4: // Remove co-owner
                    {
                        if (isOwner)
                        {
                            gumps.Close<HouseGump>();
                            gumps.Close<HouseListGump>();
                            gumps.Close<HouseRemoveGump>();
                            gumps.Send(new HouseRemoveGump(1011274, _house.CoOwners, _house, false));
                        }
                        else
                        {
                            from.SendLocalizedMessage(501329); // Only the house owner may remove co-owners.
                        }

                        break;
                    }
                case 5: // Clear co-owners
                    {
                        if (isOwner)
                        {
                            _house.CoOwners?.Clear();

                            from.SendLocalizedMessage(501333); // All co-owners have been removed from this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(501330); // Only the house owner may remove co-owners.
                        }

                        break;
                    }
                case 6: // List friends
                    {
                        gumps.Close<HouseGump>();
                        gumps.Close<HouseRemoveGump>();
                        gumps.Close<HouseListGump>();
                        gumps.Send(new HouseListGump(1011273, _house.Friends, _house, false));

                        break;
                    }
                case 7: // Add friend
                    {
                        if (isCoOwner)
                        {
                            from.SendLocalizedMessage(
                                501317
                            ); // Target the person you wish to name a friend of your household.
                            from.Target = new HouseFriendTarget(true, _house);
                        }
                        else
                        {
                            from.SendLocalizedMessage(501316); // Only the house owner may add friends.
                        }

                        break;
                    }
                case 8: // Remove friend
                    {
                        if (isCoOwner)
                        {
                            gumps.Close<HouseGump>();
                            gumps.Close<HouseListGump>();
                            gumps.Close<HouseRemoveGump>();
                            gumps.Send(new HouseRemoveGump(1011272, _house.Friends, _house, false));
                        }
                        else
                        {
                            from.SendLocalizedMessage(501318); // Only the house owner may remove friends.
                        }

                        break;
                    }
                case 9: // Clear friends
                    {
                        if (isCoOwner)
                        {
                            _house.Friends?.Clear();

                            from.SendLocalizedMessage(501332); // All friends have been removed from this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(501319); // Only the house owner may remove friends.
                        }

                        break;
                    }
                case 10: // Ban
                    {
                        from.SendLocalizedMessage(501325); // Target the individual to ban from this house.
                        from.Target = new HouseBanTarget(true, _house);

                        break;
                    }
                case 11: // Eject
                    {
                        from.SendLocalizedMessage(501326); // Target the individual to eject from this house.
                        from.Target = new HouseKickTarget(_house);

                        break;
                    }
                case 12: // List bans
                    {
                        gumps.Close<HouseGump>();
                        gumps.Close<HouseRemoveGump>();
                        gumps.Close<HouseListGump>();
                        gumps.Send(new HouseListGump(1011271, _house.Bans, _house, true));

                        break;
                    }
                case 13: // Remove ban
                    {
                        gumps.Close<HouseGump>();
                        gumps.Close<HouseListGump>();
                        gumps.Close<HouseRemoveGump>();
                        gumps.Send(new HouseRemoveGump(1011269, _house.Bans, _house, true));

                        break;
                    }

                case 21:
                    from.CloseGump<HouseGump>();
                    from.SendGump(new HouseOptionsGump(_house));
                    break;
            }
        }
    }
}

namespace Server.Prompts
{
    public class RenamePrompt : Prompt
    {
        private readonly BaseHouse _house;

        public RenamePrompt(BaseHouse house) => _house = house;

        public override void OnResponse(Mobile from, string text)
        {
            if (_house.IsFriend(from))
            {
                if (_house.Sign != null)
                {
                    _house.Sign.Name = text;
                }

                from.SendMessage("Sign changed.");
            }
        }
    }
}
