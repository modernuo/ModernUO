using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Multis;
using Server.Network;
using Server.Prompts;

namespace Server.Gumps;

public enum HouseGumpSection
{
    Info,
    Friends,
    Options,
}

public class HouseGump : DynamicGump
{

    public enum Page
    {
        One = 1,
        ShopSigns = 2,
        GuildSigns = 3,
        // allow for 20 pages of each, page size 10, so 200 max bans, co owners, and friends?
        ListBans = 10,
        ListCoOwners = 30,
        ListFriends = 50,
    }

    private enum Button
    {
        // People Buttons
        AddCoOwner = 1,
        RemoveCoOwners = 2,
        ClearCoOwners = 3,
        AddFriend = 4,
        RemoveFriends = 5,
        ClearFriends = 6,
        Ban = 7,
        Eject = 8,
        RemoveBans = 9,

        // Options Buttons
        ChangeName = 11,
        TransferOwnership = 12,
        Demolish = 13,
        ChangePrivacy = 14,
        ChangeKeys = 15,
        ChangeSign = 16,

        // Table of Contents
        GotoInfo = 21,
        GotoFriends = 22,
        GotoOptions = 23,
    }

    private readonly BaseHouse _house;

    private readonly bool _isOwner;
    private readonly bool _isCoOwner;
    private readonly bool _isFriend;

    private readonly HouseGumpSection _houseGumpSection;

    public override bool Singleton => true;

    public HouseGump(Mobile from, BaseHouse house, HouseGumpSection houseGumpSection = HouseGumpSection.Info) : base(50, 50)
    {
        _house = house;
        _houseGumpSection = houseGumpSection;

        var isCombatRestricted = house.IsCombatRestricted(from);

        _isOwner = _house.IsOwner(from);
        _isCoOwner = _isOwner || _house.IsCoOwner(from);
        _isFriend = _isCoOwner || _house.IsFriend(from);

        if (isCombatRestricted)
        {
            _isFriend = _isCoOwner = _isOwner = false;
        }
    }

    private void AddBackButton(ref DynamicGumpBuilder builder, int buttonId)
    {
        builder.AddButton(15, 389, 0xFAE, 0xFB0, buttonId); // back button
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

        // Table of Contents
        builder.AddHtmlLocalized(340, 17, 75, 20, 1011233); // INFO
        builder.AddButton(374, 15, 0xFAB, 0xFAD, (int)Button.GotoInfo);

        builder.AddHtmlLocalized(319, 47, 75, 20, 1011234); // FRIENDS
        builder.AddButton(374, 45, 0xFA8, 0xFAA, (int)Button.GotoFriends);

        builder.AddHtmlLocalized(319, 77, 75, 20, 1011235); // OPTIONS
        builder.AddButton(374, 75, 0xFBD, 0xFBF, (int)Button.GotoOptions);

        switch (_houseGumpSection)
        {
            case HouseGumpSection.Info:
                {
                    BuildInfoPage(ref builder);
                    break;
                }
            case HouseGumpSection.Friends:
                {
                    BuildPeoplePages(ref builder);
                    break;
                }
            case HouseGumpSection.Options:
                {
                    BuildOptionsPages(ref builder);
                    break;
                }
        }
    }

    private void BuildInfoPage(ref DynamicGumpBuilder builder)
    {
        // Info page
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
    }

    private void BuildPeoplePages(ref DynamicGumpBuilder builder)
    {
        builder.AddPage((int)Page.One);

        builder.AddHtmlLocalized(50, 130, 150, 20, 1011266); // List of co-owners
        builder.AddButton(15, 130, 0xFA8, 0xFAA, 0, GumpButtonType.Page, (int)Page.ListCoOwners);

        builder.AddHtmlLocalized(50, 150, 150, 20, 1011267); // Add a co-owner
        builder.AddButton(15, 150, 0xFBD, 0xFBF, (int)Button.AddCoOwner);

        builder.AddHtmlLocalized(50, 170, 150, 20, 1018036); // Remove a co-owner
        builder.AddButton(15, 170, 0xFA2, 0xFA4, 0, GumpButtonType.Page, (int)Page.ListCoOwners);

        builder.AddHtmlLocalized(50, 190, 150, 20, 1011268); // Clear co-owner list
        builder.AddButton(15, 190, 0xFB1, 0xFB3, (int)Button.ClearCoOwners);

        builder.AddHtmlLocalized(240, 130, 155, 20, 1011243); // List of Friends
        builder.AddButton(200, 130, 0xFA8, 0xFAA, 0, GumpButtonType.Page, (int)Page.ListFriends);

        builder.AddHtmlLocalized(240, 150, 155, 20, 1011244); // Add a Friend
        builder.AddButton(200, 150, 0xFBD, 0xFBF, (int)Button.AddFriend);

        builder.AddHtmlLocalized(240, 170, 155, 20, 1018037); // Remove a Friend
        builder.AddButton(200, 170, 0xFA2, 0xFA4, 0, GumpButtonType.Page, (int)Page.ListFriends);

        builder.AddHtmlLocalized(240, 190, 155, 20, 1011245); // Clear Friends list
        builder.AddButton(200, 190, 0xFB1, 0xFB3, (int)Button.ClearFriends);

        builder.AddHtmlLocalized(50, 235, 280, 20, 1011260); // View a list of banned people
        builder.AddButton(15, 235, 0xFA8, 0xFAA, 0, GumpButtonType.Page, (int)Page.ListBans);

        builder.AddHtmlLocalized(50, 255, 280, 20, 1011258); // Ban someone from the house
        builder.AddButton(15, 255, 0xFA2, 0xFA4, (int)Button.Ban);

        builder.AddHtmlLocalized(50, 275, 280, 20, 1011259); // Eject someone from the house
        builder.AddButton(15, 275, 0xFAE, 0xFB0, (int)Button.Eject);

        builder.AddHtmlLocalized(50, 295, 280, 20, 1011261); // Lift a ban
        builder.AddButton(15, 295, 0xFB1, 0xFA3, 0, GumpButtonType.Page, (int)Page.ListBans);

        AddBackButton(ref builder, (int)Button.GotoInfo);

        BuildBansPage(ref builder);
        BuildCoOwnersPage(ref builder);
        BuildFriendsPage(ref builder);
    }

    private void BuildBansPage(ref DynamicGumpBuilder builder, int pageSize = 10)
    {
        builder.AddPage((int)Page.ListBans);

        if (_house.Bans.Count <= 0)
        {
            AddBackButton(ref builder, (int)Button.GotoFriends);
            return;
        }

        for (var i = 0; i < _house.Bans.Count; ++i)
        {
            if (i % pageSize == 0)
            {
                if (i + pageSize < _house.Bans.Count)
                {
                    builder.AddButton(374, 310, 4005, 4007, 0, GumpButtonType.Page, i / pageSize + (int)Page.ListBans);
                }

                builder.AddPage(i / pageSize + (int)Page.ListBans);

                if (i > 0)
                {
                    builder.AddButton(
                        335,
                        310,
                        4014,
                        4016,
                        0,
                        GumpButtonType.Page,
                        i / pageSize + (int)Page.ListBans - 1
                    );
                }

                builder.AddHtmlLocalized(260, 250, 150, 20, 1011258); // View a list of banned people
                builder.AddButton(370, 250, 0xFBD, 0xFBF, (int)Button.Ban);

                builder.AddHtmlLocalized(245, 280, 150, 20, 1011261); // Lift a ban
                builder.AddButton(374, 280, 0xFA2, 0xFA4, (int)Button.RemoveBans);

                AddBackButton(ref builder, (int)Button.GotoFriends);
            }

            builder.AddCheckbox(15, 130 + i % pageSize * 20, 0xD2, 0xD3, false, i);
            builder.AddLabel(
                35,
                130 + i % pageSize * 20,
                0,
                _house.Bans[i].Name
            );
        }
    }

    private void BuildCoOwnersPage(ref DynamicGumpBuilder builder, int pageSize = 10)
    {
        builder.AddPage((int)Page.ListCoOwners);

        if (_house.CoOwners.Count <= 0)
        {
            AddBackButton(ref builder, (int)Button.GotoFriends);
            return;
        }

        for (var i = 0; i < _house.CoOwners.Count; ++i)
        {
            if (i % pageSize == 0)
            {
                if (i + pageSize < _house.CoOwners.Count)
                {
                    builder.AddButton(370, 310, 4005, 4007, 0, GumpButtonType.Page, i / pageSize + (int)Page.ListCoOwners);
                }

                builder.AddPage(i / pageSize + (int)Page.ListCoOwners);

                if (i > 0)
                {
                    builder.AddButton(
                        335,
                        310,
                        4014,
                        4016,
                        0,
                        GumpButtonType.Page,
                        i / pageSize + (int)Page.ListCoOwners - 1
                    );
                }

                builder.AddHtmlLocalized(265, 250, 150, 20, 1011267); // Add a co-owner
                builder.AddButton(374, 250, 0xFBD, 0xFBF, (int)Button.AddCoOwner);

                builder.AddHtmlLocalized(245, 280, 150, 20, 1018036); // Remove a co-owner
                builder.AddButton(374, 280, 0xFA2, 0xFA4, (int)Button.RemoveCoOwners);

                AddBackButton(ref builder, (int)Button.GotoFriends);
            }

            builder.AddCheckbox(15, 130 + i % pageSize * 20, 0xD2, 0xD3, false, i);
            builder.AddLabel(
                35,
                130 + i % pageSize * 20,
                0,
                _house.CoOwners[i].Name
            );
        }
    }

    private void BuildFriendsPage(ref DynamicGumpBuilder builder, int pageSize = 10)
    {
        builder.AddPage((int)Page.ListFriends);

        if (_house.Friends.Count <= 0)
        {
            AddBackButton(ref builder, (int)Button.GotoFriends);
            return;
        }

        for (var i = 0; i < _house.Friends.Count; ++i)
        {
            if (i % pageSize == 0)
            {
                if (i + pageSize < _house.Friends.Count)
                {
                    builder.AddButton(374, 310, 4005, 4007, 0, GumpButtonType.Page, i / pageSize + (int)Page.ListFriends);
                }

                builder.AddPage(i / pageSize + (int)Page.ListFriends);

                if (i > 0)
                {
                    builder.AddButton(
                        335,
                        310,
                        4014,
                        4016,
                        0,
                        GumpButtonType.Page,
                        i / pageSize + (int)Page.ListFriends - 1
                    );
                }

                builder.AddHtmlLocalized(260, 250, 150, 20, 1011244); // Add a friend
                builder.AddButton(374, 250, 0xFBD, 0xFBF, (int)Button.AddFriend);

                builder.AddHtmlLocalized(240, 280, 150, 20, 1018037); // Remove a friend
                builder.AddButton(374, 280, 0xFA2, 0xFA4, (int)Button.RemoveFriends);

                AddBackButton(ref builder, (int)Button.GotoFriends);
            }

            builder.AddCheckbox(15, 130 + i % pageSize * 20, 0xD2, 0xD3, false, i);
            builder.AddLabel(
                35,
                130 + i % pageSize * 20,
                0,
                _house.Friends[i].Name
            );
        }
    }

    private void BuildOptionsPages(ref DynamicGumpBuilder builder)
    {
        builder.AddPage((int)Page.One);

        builder.AddItem(22, 144, 0xFBF); // scribe's pen
        builder.AddButton(65, 146, 0x16CD, 0x16CE, (int)Button.ChangeName);
        builder.AddHtmlLocalized(95, 145, 200, 20, 1011236); // Change this house's name!

        builder.AddImage(10, 165, 0x1196); // right facing arrow
        builder.AddButton(65, 181, 0x16CD, 0x16CE, (int)Button.TransferOwnership);
        builder.AddHtmlLocalized(95, 180, 200, 30, 1011248); // Transfer ownership of the house

        builder.AddItem(10, 211, 0x14F0); // deed
        builder.AddButton(65, 216, 0x16CD, 0x16CE, (int)Button.Demolish);
        builder.AddHtmlLocalized(95, 215, 300, 30, 1011249); // Demolish house and get deed back

        if (!_house.Public)
        {
            builder.AddItem(14, 252, 0x176B); // keyring with keys
            builder.AddButton(65, 251, 0x16CD, 0x16CE, (int)Button.ChangeKeys);
            builder.AddHtmlLocalized(95, 250, 355, 30, 1011247); // Change the house locks

            builder.AddItem(14, 284, 0xBC4); // inn sign
            builder.AddButton(65, 286, 0x16CD, 0x16CE, (int)Button.ChangePrivacy);
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
            builder.AddButton(65, 251, 0x16CD, 0x16CE, (int)Button.ChangePrivacy);
            builder.AddHtmlLocalized(95, 250, 300, 30, 1011252); // Declare this building to be private.

            builder.AddItem(14, 284, 0xBCA); // artist sign
            // builder.AddButton(65, 286, 0x16CD, 0x16CE, (int)Button.ChangeSign);
            builder.AddButton(65, 286, 0x16CD, 0x16CE, 0, GumpButtonType.Page, (int)Page.ShopSigns);
            builder.AddHtmlLocalized(95, 285, 200, 30, 1011250); // Change the sign type
        }

        AddBackButton(ref builder, (int)Button.GotoInfo);

        builder.AddPage((int)Page.ShopSigns);

        for (var i = 0; i < 24; ++i)
        {
            builder.AddRadio(16 + i / 4 * 65, 137 + i % 4 * 35, 210, 211, false, i + 1);
            builder.AddItem(36 + i / 4 * 65, 130 + i % 4 * 35, 2980 + i * 2);
        }

        builder.AddHtmlLocalized(254, 360, 129, 20, 1011254); // Guild sign choices
        builder.AddButton(374, 359, 0xFA5, 0xFA7, 0, GumpButtonType.Page, (int)Page.GuildSigns);

        builder.AddHtmlLocalized(254, 390, 355, 30, 1011277); // Okay that is fine.
        builder.AddButton(374, 389, 0xFB7, 0xFB9, (int)Button.ChangeSign);

        builder.AddButton(15, 389, 0xFAE, 0xFB0, 0, GumpButtonType.Page, (int)Page.One);

        builder.AddPage((int)Page.GuildSigns);

        for (var i = 0; i < 29; ++i)
        {
            builder.AddRadio(16 + i / 5 * 65, 137 + i % 5 * 35, 210, 211, false, i + 25);
            builder.AddItem(36 + i / 5 * 65, 130 + i % 5 * 35, 3028 + i * 2);
        }

        builder.AddHtmlLocalized(254, 360, 129, 20, 1011255); // Shop sign choices
        builder.AddButton(374, 359, 0xFAE, 0xFB0, 0, GumpButtonType.Page, (int)Page.ShopSigns);

        builder.AddHtmlLocalized(254, 390, 355, 30, 1011277); // Okay that is fine.
        builder.AddButton(374, 389, 0xFB7, 0xFB9, (int)Button.ChangeSign);

        builder.AddButton(15, 389, 0xFAE, 0xFB0, 0, GumpButtonType.Page, (int)Page.One);
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (_house.Deleted)
        {
            return;
        }

        var from = state.Mobile;

        if (info.ButtonID < 0)
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

        switch ((Button)info.ButtonID)
        {
            case Button.AddCoOwner: // Add co-owner
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
            case Button.RemoveCoOwners: // Remove co-owner
                {
                    if (!isOwner)
                    {
                        from.SendLocalizedMessage(501329); // Only the house owner may remove co-owners.
                        break;
                    }

                    var switches = info.Switches;

                    if (switches.Length <= 0)
                    {
                        break;
                    }

                    List<Mobile> _copy = [.._house.CoOwners];

                    for (var i = 0; i < switches.Length; ++i)
                    {
                        var index = switches[i];

                        if (index >= 0 && index < _copy.Count)
                        {
                            _house.CoOwners.Remove(_copy[index]);
                        }
                    }

                    from.SendGump(new HouseGump(from, _house, HouseGumpSection.Friends));

                    break;
                }
            case Button.ClearCoOwners: // Clear co-owners
                {
                    if (!isOwner)
                    {
                        from.SendLocalizedMessage(501329); // Only the house owner may remove co-owners.
                        break;
                    }

                    _house.CoOwners?.Clear();

                    from.SendLocalizedMessage(501333); // All co-owners have been removed from this house.

                    from.SendGump(new HouseGump(from, _house, HouseGumpSection.Friends));

                    break;
                }
            case Button.AddFriend: // Add friend
                {
                    if (!isCoOwner)
                    {
                        from.SendLocalizedMessage(501316); // Only the house owner may add friends.
                        break;
                    }

                    from.SendLocalizedMessage(
                        501317
                    ); // Target the person you wish to name a friend of your household.
                    from.Target = new HouseFriendTarget(true, _house);

                    break;
                }
            case Button.RemoveFriends: // Remove friend
                {
                    if (!isOwner)
                    {
                        from.SendLocalizedMessage(501329); // Only the house owner may remove co-owners.
                        break;
                    }

                    var switches = info.Switches;

                    if (switches.Length <= 0)
                    {
                        break;
                    }

                    List<Mobile> _copy = [.._house.Friends];

                    for (var i = 0; i < switches.Length; ++i)
                    {
                        var index = switches[i];

                        if (index >= 0 && index < _copy.Count)
                        {
                            _house.Friends.Remove(_copy[index]);
                        }
                    }

                    from.SendGump(new HouseGump(from, _house, HouseGumpSection.Friends));

                    break;
                }
            case Button.ClearFriends: // Clear friends
                {
                    if (!isCoOwner)
                    {
                        from.SendLocalizedMessage(501329); // Only the house owner may remove co-owners.
                        break;
                    }

                    _house.Friends?.Clear();

                    from.SendLocalizedMessage(501332); // All friends have been removed from this house.

                    from.SendGump(new HouseGump(from, _house, HouseGumpSection.Friends));

                    break;
                }
            case Button.Ban: // Ban
                {
                    from.SendLocalizedMessage(501325); // Target the individual to ban from this house.
                    from.Target = new HouseBanTarget(true, _house);

                    break;
                }
            case Button.Eject: // Eject
                {
                    from.SendLocalizedMessage(501326); // Target the individual to eject from this house.
                    from.Target = new HouseKickTarget(_house);

                    break;
                }
            case Button.RemoveBans: // Remove ban
                {
                    var switches = info.Switches;

                    if (switches.Length <= 0)
                    {
                        break;
                    }

                    List<Mobile> _copy = [.._house.Bans];

                    for (var i = 0; i < switches.Length; ++i)
                    {
                        var index = switches[i];

                        if (index >= 0 && index < _copy.Count)
                        {
                            _house.Bans.Remove(_copy[index]);
                        }
                    }

                    from.SendGump(new HouseGump(from, _house, HouseGumpSection.Friends));

                    break;
                }
            case Button.ChangeName: // Rename sign
                {
                    from.Prompt = new HouseRenamePrompt(_house);
                    from.SendLocalizedMessage(501302); // What dost thou wish the sign to say?

                    break;
                }
            case Button.TransferOwnership: // Transfer ownership
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
            case Button.Demolish: // Demolish house
                {
                    if (!isOwner)
                    {
                        from.SendLocalizedMessage(501320); // Only the house owner may do this.
                        break;
                    }

                    if (!Guild.NewGuildSystem && _house.FindGuildstone() != null)
                    {
                        from.SendLocalizedMessage(501389); // You cannot redeed a house with a guildstone inside.
                    }
                    else
                    {
                        from.SendGump(new ConfirmDemolishHouseGump(_house));
                    }

                    break;
                }
            case Button.ChangePrivacy: // Declare public/private
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

                    from.SendGump(new HouseGump(from, _house, HouseGumpSection.Options));
                    break;
                }
            case Button.ChangeKeys: // Change locks
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

                    from.SendGump(new HouseGump(from, _house, HouseGumpSection.Options));
                    break;
                }
            case Button.ChangeSign: // Change sign type
                {
                    if (!isOwner)
                    {
                        from.SendLocalizedMessage(501303); // Only the house owner may change the house locks.
                        break;
                    }

                    if (!_house.Public || info.Switches.Length <= 0)
                    {
                        from.SendGump(new HouseGump(from, _house));
                        break;
                    }

                    var index = info.Switches[0] - 1;

                    if (index is >= 0 and < 53)
                    {
                        _house.ChangeSignType(2980 + index * 2);
                    }

                    from.SendGump(new HouseGump(from, _house, HouseGumpSection.Options));
                    break;
                }
            case Button.GotoInfo:
                from.SendGump(new HouseGump(from, _house));
                break;
            case Button.GotoFriends:
                from.SendGump(new HouseGump(from, _house, HouseGumpSection.Friends));
                break;
            case Button.GotoOptions:
                from.SendGump(new HouseGump(from, _house, HouseGumpSection.Options));
                break;
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
}

public class HouseRenamePrompt : Prompt
{
    private readonly BaseHouse _house;

    public HouseRenamePrompt(BaseHouse house) => _house = house;

    public override void OnResponse(Mobile from, string text)
    {
        if (_house.IsFriend(from))
        {
            if (_house.Sign != null)
            {
                _house.Sign.Name = text;
            }

            from.SendGump(new HouseGump(from, _house, HouseGumpSection.Options));
        }
    }
}
