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

    public class HouseGump : Gump
    {
        private readonly BaseHouse _house;

        public override bool Singleton => true;

        public HouseGump(Mobile from, BaseHouse house) : base(20, 30)
        {
            if (house.Deleted)
            {
                return;
            }

            _house = house;

            var gumps = from.GetGumps();

            gumps.Close<HouseListGump>();
            gumps.Close<HouseRemoveGump>();

            var isCombatRestricted = house.IsCombatRestricted(from);

            var isOwner = _house.IsOwner(from);
            var isCoOwner = isOwner || _house.IsCoOwner(from);
            var isFriend = isCoOwner || _house.IsFriend(from);

            if (isCombatRestricted)
            {
                isFriend = isCoOwner = isOwner = false;
            }

            AddPage(0);

            if (isFriend)
            {
                AddBackground(0, 0, 420, 430, 5054);
                AddBackground(10, 10, 400, 410, 3000);
            }

            AddImage(130, 0, 100);

            if (_house.Sign != null)
            {
                var lines = _house.Sign.GetName().Wrap(10, 6);

                for (int i = 0, y = (101 - lines.Count * 14) / 2; i < lines.Count; ++i, y += 14)
                {
                    var s = lines[i];

                    AddLabel(130 + (143 - s.Length * 8) / 2, y, 0, s);
                }
            }

            if (!isFriend)
            {
                return;
            }

            AddHtmlLocalized(55, 103, 75, 20, 1011233); // INFO
            AddButton(20, 103, 4005, 4007, 0, GumpButtonType.Page, 1);

            AddHtmlLocalized(170, 103, 75, 20, 1011234); // FRIENDS
            AddButton(135, 103, 4005, 4007, 0, GumpButtonType.Page, 2);

            AddHtmlLocalized(295, 103, 75, 20, 1011235); // OPTIONS
            AddButton(260, 103, 4005, 4007, 0, GumpButtonType.Page, 3);

            AddHtmlLocalized(295, 390, 75, 20, 1011441); // EXIT
            AddButton(260, 390, 4005, 4007, 0);

            AddHtmlLocalized(55, 390, 200, 20, 1011236); // Change this house's name!
            AddButton(20, 390, 4005, 4007, 1);

            // Info page
            AddPage(1);

            AddHtmlLocalized(20, 135, 100, 20, 1011242); // Owned by:
            AddHtml(120, 135, 100, 20, GetOwnerName());

            AddHtmlLocalized(20, 170, 275, 20, 1011237); // Number of locked down items:
            AddHtml(320, 170, 50, 20, _house.LockDownCount.ToString());

            AddHtmlLocalized(20, 190, 275, 20, 1011238); // Maximum locked down items:
            AddHtml(320, 190, 50, 20, _house.MaxLockDowns.ToString());

            AddHtmlLocalized(20, 210, 275, 20, 1011239); // Number of secure containers:
            AddHtml(320, 210, 50, 20, _house.SecureCount.ToString());

            AddHtmlLocalized(20, 230, 275, 20, 1011240); // Maximum number of secure containers:
            AddHtml(320, 230, 50, 20, _house.MaxSecures.ToString());

            AddHtmlLocalized(20, 260, 400, 20, 1018032); // This house is properly placed.
            AddHtmlLocalized(20, 280, 400, 20, 1018035); // This house is of modern design.

            if (_house.Public)
            {
                // TODO: Validate exact placement
                AddHtmlLocalized(20, 305, 275, 20, 1011241); // Number of visits this building has had
                AddHtml(320, 305, 50, 20, _house.Visits.ToString());
            }

            // Friends page
            AddPage(2);

            AddHtmlLocalized(45, 130, 150, 20, 1011266); // List of co-owners
            AddButton(20, 130, 2714, 2715, 2);

            AddHtmlLocalized(45, 150, 150, 20, 1011267); // Add a co-owner
            AddButton(20, 150, 2714, 2715, 3);

            AddHtmlLocalized(45, 170, 150, 20, 1018036); // Remove a co-owner
            AddButton(20, 170, 2714, 2715, 4);

            AddHtmlLocalized(45, 190, 150, 20, 1011268); // Clear co-owner list
            AddButton(20, 190, 2714, 2715, 5);

            AddHtmlLocalized(225, 130, 155, 20, 1011243); // List of Friends
            AddButton(200, 130, 2714, 2715, 6);

            AddHtmlLocalized(225, 150, 155, 20, 1011244); // Add a Friend
            AddButton(200, 150, 2714, 2715, 7);

            AddHtmlLocalized(225, 170, 155, 20, 1018037); // Remove a Friend
            AddButton(200, 170, 2714, 2715, 8);

            AddHtmlLocalized(225, 190, 155, 20, 1011245); // Clear Friends list
            AddButton(200, 190, 2714, 2715, 9);

            AddHtmlLocalized(120, 215, 280, 20, 1011258); // Ban someone from the house
            AddButton(95, 215, 2714, 2715, 10);

            AddHtmlLocalized(120, 235, 280, 20, 1011259); // Eject someone from the house
            AddButton(95, 235, 2714, 2715, 11);

            AddHtmlLocalized(120, 255, 280, 20, 1011260); // View a list of banned people
            AddButton(95, 255, 2714, 2715, 12);

            AddHtmlLocalized(120, 275, 280, 20, 1011261); // Lift a ban
            AddButton(95, 275, 2714, 2715, 13);

            // Options page
            AddPage(3);

            AddHtmlLocalized(45, 150, 355, 30, 1011248); // Transfer ownership of the house
            AddButton(20, 150, 2714, 2715, 14);

            AddHtmlLocalized(45, 180, 355, 30, 1011249); // Demolish house and get deed back
            AddButton(20, 180, 2714, 2715, 15);

            if (!_house.Public)
            {
                AddHtmlLocalized(45, 210, 355, 30, 1011247); // Change the house locks
                AddButton(20, 210, 2714, 2715, 16);

                AddHtmlLocalized(
                    45,
                    240,
                    350,
                    90,
                    1011253
                ); // Declare this building to be public. This will make your front door unlockable.
                AddButton(20, 240, 2714, 2715, 17);
            }
            else
            {
                // AddHtmlLocalized( 45, 280, 350, 30, 1011250, false, false ); // Change the sign type
                AddHtmlLocalized(45, 210, 350, 30, 1011250); // Change the sign type
                AddButton(20, 210, 2714, 2715, 0, GumpButtonType.Page, 4);

                AddHtmlLocalized(45, 240, 350, 30, 1011252); // Declare this building to be private.
                AddButton(20, 240, 2714, 2715, 17);

                // Change the sign type
                AddPage(4);

                for (var i = 0; i < 24; ++i)
                {
                    AddRadio(53 + i / 4 * 50, 137 + i % 4 * 35, 210, 211, false, i + 1);
                    AddItem(60 + i / 4 * 50, 130 + i % 4 * 35, 2980 + i * 2);
                }

                AddHtmlLocalized(200, 305, 129, 20, 1011254); // Guild sign choices
                AddButton(350, 305, 252, 253, 0, GumpButtonType.Page, 5);

                AddHtmlLocalized(200, 340, 355, 30, 1011277); // Okay that is fine.
                AddButton(350, 340, 4005, 4007, 18);

                AddPage(5);

                for (var i = 0; i < 29; ++i)
                {
                    AddRadio(53 + i / 5 * 50, 137 + i % 5 * 35, 210, 211, false, i + 25);
                    AddItem(60 + i / 5 * 50, 130 + i % 5 * 35, 3028 + i * 2);
                }

                AddHtmlLocalized(200, 305, 129, 20, 1011255); // Shop sign choices
                AddButton(350, 305, 250, 251, 0, GumpButtonType.Page, 4);

                AddHtmlLocalized(200, 340, 355, 30, 1011277); // Okay that is fine.
                AddButton(350, 340, 4005, 4007, 18);
            }
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
                case 1: // Rename sign
                    {
                        from.Prompt = new RenamePrompt(_house);
                        from.SendLocalizedMessage(501302); // What dost thou wish the sign to say?

                        break;
                    }
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
                case 14: // Transfer ownership
                    {
                        if (isOwner)
                        {
                            from.SendLocalizedMessage(501309); // Target the person to whom you wish to give this house.
                            from.Target = new HouseOwnerTarget(_house);
                        }
                        else
                        {
                            from.SendLocalizedMessage(501310); // Only the house owner may do this.
                        }

                        break;
                    }
                case 15: // Demolish house
                    {
                        if (isOwner)
                        {
                            if (!Guild.NewGuildSystem && _house.FindGuildstone() != null)
                            {
                                from.SendLocalizedMessage(501389); // You cannot redeed a house with a guildstone inside.
                            }
                            else
                            {
                                gumps.Send(new ConfirmDemolishHouseGump(_house));
                            }
                        }
                        else
                        {
                            from.SendLocalizedMessage(501320); // Only the house owner may do this.
                        }

                        break;
                    }
                case 16: // Change locks
                    {
                        if (_house.Public)
                        {
                            from.SendLocalizedMessage(501669); // Public houses are always unlocked.
                        }
                        else
                        {
                            if (isOwner)
                            {
                                _house.RemoveKeys(from);
                                _house.ChangeLocks(from);

                                from.SendLocalizedMessage(
                                    501306
                                ); // The locks on your front door have been changed, and new master keys have been placed in your bank and your backpack.
                            }
                            else
                            {
                                from.SendLocalizedMessage(501303); // Only the house owner may change the house locks.
                            }
                        }

                        break;
                    }
                case 17: // Declare public/private
                    {
                        if (isOwner)
                        {
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
                        }
                        else
                        {
                            from.SendLocalizedMessage(501307); // Only the house owner may do this.
                        }

                        break;
                    }
                case 18: // Change type
                    {
                        if (isOwner)
                        {
                            if (_house.Public && info.Switches.Length > 0)
                            {
                                var index = info.Switches[0] - 1;

                                if (index >= 0 && index < 53)
                                {
                                    _house.ChangeSignType(2980 + index * 2);
                                }
                            }
                        }
                        else
                        {
                            from.SendLocalizedMessage(501307); // Only the house owner may do this.
                        }

                        break;
                    }
            }
        }
    }
}

namespace Server.Prompts
{
    public class RenamePrompt : Prompt
    {
        private readonly BaseHouse m_House;

        public RenamePrompt(BaseHouse house) => m_House = house;

        public override void OnResponse(Mobile from, string text)
        {
            if (m_House.IsFriend(from))
            {
                if (m_House.Sign != null)
                {
                    m_House.Sign.Name = text;
                }

                from.SendMessage("Sign changed.");
            }
        }
    }
}
