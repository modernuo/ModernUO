using System;
using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Engines.Avatar;

public static class AvatarCommand
{
    public static void Configure()
    {
        CommandSystem.Register("avatar-enable", AccessLevel.Player, EnableAvatarCommand);
        CommandSystem.Register("avatar-draft-enable", AccessLevel.Player, EnableDraftCommand);
        CommandSystem.Register("avatar-shop", AccessLevel.Player, OpenAvatarShopCommand);
        TargetCommands.Register(new DraftBanUnbanSkillCommand(true));
        TargetCommands.Register(new DraftBanUnbanSkillCommand(false));
    }

    [Usage("avatar-enable")]
    [Description("Enables the Avatar status for the Player.")]
    public static void EnableAvatarCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile from || !CanEnable(from, false))
        {
            return;
        }

        AvatarConfirm.Send(
            from,
            "Enable Avatar Status?",
            "Are you sure you wish to enable the Avatar status? This will reset your character and allow you to use the Avatar features.",
            () => Enable(from, false)
        );
    }

    [Usage("avatar-draft-enable")]
    [Description("Enables the Avatar status and Draft mode for the Player.")]
    public static void EnableDraftCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile from || !CanEnable(from, true))
        {
            return;
        }

        AvatarConfirm.Send(
            from,
            "Enable Avatar Draft Mode?",
            "Are you sure you wish to enable the Avatar Draft mode? This will reset your character and allow you to use the Avatar Draft features.",
            () => Enable(from, true)
        );
    }

    private static bool CanEnable(PlayerMobile from, bool draft)
    {
        if (!AvatarEngine.IsEnabled)
        {
            from.SendMessage("The Avatar system is disabled.");
            return false;
        }

        if (!AvatarSanctuary.IsIn(from))
        {
            from.SendMessage(
                draft
                    ? "You must be in the Sanctuary to enable Draft mode."
                    : "You must be in the Sanctuary to become an Avatar."
            );
            return false;
        }

        if (!from.Alive)
        {
            from.SendMessage("You must be alive to do that.");
            return false;
        }

        if (from.Avatar.Active && (!draft || from.AccessLevel <= AccessLevel.Player))
        {
            from.SendMessage("You already have the Avatar status enabled.");
            return false;
        }

        return true;
    }

    private static void Enable(PlayerMobile from, bool draft)
    {
        from.SendMessage("Your character will be reborn shortly...");

        Timer.DelayCall(
            TimeSpan.FromSeconds(1),
            () =>
            {
                if (from.Deleted)
                {
                    return;
                }

                var context = AvatarEngine.GetOrCreateContext(from);
                AvatarEngine.ResetInPlace(from);

                if (draft)
                {
                    context.SetDraftModeEnabled(from, true);
                }
            }
        );
    }

    [Usage("avatar-shop")]
    [Description("Opens the Avatar Shop for the Player.")]
    public static void OpenAvatarShopCommand(CommandEventArgs e)
    {
        if (e.Mobile is not PlayerMobile from)
        {
            return;
        }

        if (!from.Avatar.Active)
        {
            from.SendMessage("You do not have the Avatar status enabled.");
            return;
        }

        from.SendGump(new AvatarShopGump(from));
    }

    private class DraftBanUnbanSkillCommand : BaseCommand
    {
        private readonly bool _isBan;

        public DraftBanUnbanSkillCommand(bool isBan)
        {
            _isBan = isBan;

            var command = isBan ? "avatar-draft-ban" : "avatar-draft-unban";
            AccessLevel = AccessLevel.GameMaster;
            Supports = CommandSupport.AllMobiles;
            Commands = [command];
            ObjectTypes = ObjectTypes.Mobiles;
            Usage = $"{command} <skill>";
            Description = isBan
                ? "Prevents the specified skill from showing up as an option in the Avatar Draft mode."
                : "Removes the ban on the specified skill to allow it to show up as an option in the Avatar Draft mode.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (e.Length != 1)
            {
                e.Mobile.SendMessage(Usage);
                return;
            }

            if (!Enum.TryParse(e.GetString(0), true, out SkillName skill))
            {
                e.Mobile.SendMessage("Invalid skill name.");
                return;
            }

            if (obj is not PlayerMobile pm)
            {
                LogFailure("That is not a player.");
                return;
            }

            if (!pm.Avatar.Active)
            {
                LogFailure("That is not an Avatar.");
                return;
            }

            if (!pm.Avatar.DraftModeEnabled)
            {
                LogFailure("That character is not in Draft mode.");
                return;
            }

            if (_isBan)
            {
                pm.Avatar.AddDraftBannedSkill(skill);
                e.Mobile.SendMessage($"'{pm.Name}' will no longer see '{skill}' when drafting skills.");
            }
            else
            {
                pm.Avatar.RemoveDraftBannedSkill(skill);
                e.Mobile.SendMessage($"'{pm.Name}' may now see '{skill}' when drafting skills.");
            }
        }
    }
}
