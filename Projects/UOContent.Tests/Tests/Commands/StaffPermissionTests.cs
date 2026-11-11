using System;
using System.Collections.Generic;
using Server;
using Server.Accounting;
using Server.Accounting.Security;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Commands;

[Collection("Sequential UOContent Tests")]
public class StaffPermissionTests : IDisposable
{
    private readonly List<IEntity> _created = [];
    private readonly List<Account> _accounts = [];
    private readonly PasswordProtectionAlgorithm _originalAlgorithm = AccountSecurity.CurrentAlgorithm;

    public StaffPermissionTests() => AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.SHA2;

    public void Dispose()
    {
        for (var i = _created.Count - 1; i >= 0; i--)
        {
            _created[i].Delete();
        }

        for (var i = 0; i < _accounts.Count; i++)
        {
            _accounts[i].Delete();
        }

        AccountSecurity.CurrentAlgorithm = _originalAlgorithm;
    }

    private PlayerMobile CreateCharacter(AccessLevel characterLevel, AccessLevel accountLevel)
    {
        var account = new Account($"perm-{Guid.NewGuid():N}", "hunter2") { AccessLevel = accountLevel };
        _accounts.Add(account);

        var m = new PlayerMobile { AccessLevel = characterLevel };
        m.MoveToWorld(new Point3D(1500, 1600, 0), Map.Felucca);
        account[0] = m;
        _created.Add(m);
        return m;
    }

    private static void StaffAccessCommand(Mobile m, string arg) =>
        StaffAccess.StaffAccess_OnCommand(
            new CommandEventArgs(m, "StaffAccess", arg, string.IsNullOrEmpty(arg) ? [] : [arg])
        );

    [Fact]
    public void StaffAccess_DemoteAndRestore()
    {
        var admin = CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator);

        StaffAccessCommand(admin, "player");
        Assert.Equal(AccessLevel.Player, admin.AccessLevel);

        StaffAccessCommand(admin, "counselor");
        Assert.Equal(AccessLevel.Counselor, admin.AccessLevel);

        StaffAccessCommand(admin, "");
        Assert.Equal(AccessLevel.Administrator, admin.AccessLevel);
        Assert.False(StaffAccess.TryGetOriginalAccessLevel(admin, out _));
    }

    [Fact]
    public void StaffAccess_CannotRaiseAboveOriginal()
    {
        var gm = CreateCharacter(AccessLevel.GameMaster, AccessLevel.GameMaster);

        StaffAccessCommand(gm, "player");
        StaffAccessCommand(gm, "owner");

        Assert.Equal(AccessLevel.Player, gm.AccessLevel);
    }

    [Fact]
    public void StaffAccess_IgnoresAccountTags()
    {
        var admin = CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator);
        ((Account)admin.Account).AddTag($"OriginalStaffAccess:{admin.Serial}", "owner");

        StaffAccessCommand(admin, "player");
        StaffAccessCommand(admin, "");

        Assert.Equal(AccessLevel.Administrator, admin.AccessLevel);
    }

    [Fact]
    public void StaffAccess_InitializeRemovesLegacyTags()
    {
        var admin = CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator);
        var account = (Account)admin.Account;
        account.AddTag($"OriginalStaffAccess:{admin.Serial}", "owner");
        account.AddTag("Unrelated", "kept");

        StaffAccess.Initialize();

        Assert.Null(account.GetTag($"OriginalStaffAccess:{admin.Serial}"));
        Assert.Equal("kept", account.GetTag("Unrelated"));
    }

    [Fact]
    public void StaffAccess_ExternalChangeCancelsRestore()
    {
        var admin = CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator);

        StaffAccessCommand(admin, "player");
        admin.AccessLevel = AccessLevel.Counselor;
        Assert.False(StaffAccess.TryGetOriginalAccessLevel(admin, out _));

        StaffAccessCommand(admin, "");
        Assert.Equal(AccessLevel.Counselor, admin.AccessLevel);
    }

    [Fact]
    public void StaffAccess_DeletedCharacterIsForgotten()
    {
        var admin = CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator);

        StaffAccessCommand(admin, "player");
        admin.Delete();

        Assert.False(StaffAccess.TryGetOriginalAccessLevel(admin, out _));
    }

    [Theory]
    [InlineData(AccessLevel.Administrator, AccessLevel.Seer, AccessLevel.Seer, true)]
    [InlineData(AccessLevel.Administrator, AccessLevel.Administrator, AccessLevel.Administrator, false)]
    [InlineData(AccessLevel.Administrator, AccessLevel.Player, AccessLevel.Owner, false)]
    [InlineData(AccessLevel.Developer, AccessLevel.Administrator, AccessLevel.Administrator, true)]
    [InlineData(AccessLevel.Owner, AccessLevel.Owner, AccessLevel.Owner, true)]
    public void CanManageAccount_RequiresTargetBelowEditor(
        AccessLevel editorLevel, AccessLevel targetAccountLevel, AccessLevel targetCharacterLevel, bool expected
    )
    {
        var editor = CreateCharacter(editorLevel, editorLevel);
        var target = CreateCharacter(targetCharacterLevel, targetAccountLevel);

        Assert.Equal(expected, AdminGump.CanManageAccount(editor, target.Account));
    }

    [Fact]
    public void CanManageAccount_OwnAccountIsAllowed()
    {
        var admin = CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator);

        Assert.True(AdminGump.CanManageAccount(admin, admin.Account));
    }

    [Fact]
    public void AccountDelete_EqualLevelIsRefused()
    {
        var admin = CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator);
        var other = CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator);
        var otherAccount = (Account)other.Account;

        AdminGump.AccountDelete_Callback(admin, true, otherAccount);

        Assert.NotNull(Accounts.GetAccount(otherAccount.Username));
        Assert.False(other.Deleted);
    }

    [Fact]
    public void MarkedBan_SkipsAccountsNotBelowEditor()
    {
        var admin = CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator);
        var peer = (Account)CreateCharacter(AccessLevel.Administrator, AccessLevel.Administrator).Account;
        var player = (Account)CreateCharacter(AccessLevel.Player, AccessLevel.Player).Account;

        List<object> list = [peer, player];
        AdminGump.Marked_Callback(admin, true, true, list, [peer, player], 0);

        Assert.False(peer.Banned);
        Assert.True(player.Banned);
    }

    [Fact]
    public void Batch_ObjectRedirectThroughHigherPrivileged_IsRefused()
    {
        var gm = CreateCharacter(AccessLevel.GameMaster, AccessLevel.GameMaster);
        var owner = CreateCharacter(AccessLevel.Owner, AccessLevel.Owner);
        owner.Skills.Magery.Base = 50.0;

        var box = new WoodenBox { Picker = owner };
        box.MoveToWorld(new Point3D(1502, 1600, 0), Map.Felucca);
        _created.Add(box);

        var scope = new SingleCommandImplementor();
        scope.Commands["set"] = new SetCommand();

        foreach (var obj in (string[])["Picker.Skills.Magery", "Picker.Skills", "Picker"])
        {
            var batch = new Batch { Scope = scope };
            batch.BatchCommands.Add(new BatchCommand(obj == "Picker.Skills.Magery" ? "set Base 0" : "set Magery.Base 0", obj));
            batch.ExecuteList(new CommandEventArgs(gm, "Batch", "", []), [box]);
        }

        Assert.Equal(50.0, owner.Skills.Magery.Base);
    }

    [Fact]
    public void Batch_ObjectRedirectThroughPlayer_StillWorks()
    {
        var gm = CreateCharacter(AccessLevel.GameMaster, AccessLevel.GameMaster);
        var player = CreateCharacter(AccessLevel.Player, AccessLevel.Player);
        player.Skills.Magery.Base = 50.0;

        var box = new WoodenBox { Picker = player };
        box.MoveToWorld(new Point3D(1502, 1600, 0), Map.Felucca);
        _created.Add(box);

        var scope = new SingleCommandImplementor();
        scope.Commands["set"] = new SetCommand();

        var batch = new Batch { Scope = scope };
        batch.BatchCommands.Add(new BatchCommand("set Base 0", "Picker.Skills.Magery"));
        batch.ExecuteList(new CommandEventArgs(gm, "Batch", "", []), [box]);

        Assert.Equal(0.0, player.Skills.Magery.Base);
    }
}
