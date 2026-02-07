using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Systems.FeatureFlags;

public sealed class FeatureFlagAdminGump : DynamicGump
{
    public enum FeatureFlagPage
    {
        Flags,
        GumpBlocks,
        UseReqBlocks,
        SkillBlocks,
        SpellBlocks,
        ContainerBlocks
    }

    private FeatureFlagPage _currentPage;
    private int _pageIndex;
    private const int FlagsPerPage = 10;
    private const int BlocksPerPage = 7;
    private const int FlagRowHeight = 25;
    private const int BlockRowHeight = 45;

    public FeatureFlagAdminGump(FeatureFlagPage page = FeatureFlagPage.Flags, int pageIndex = 0) : base(50, 50)
    {
        _currentPage = page;
        _pageIndex = pageIndex;
    }

    private void Resend(Mobile from, FeatureFlagPage page, int pageIndex = 0)
    {
        _currentPage = page;
        _pageIndex = pageIndex;
        from.SendGump(this);
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        // Background
        builder.AddBackground(0, 0, 820, 500, 9270);

        // Title
        builder.AddHtml(0, 15, 820, 25, "Feature Flag Administration".Center("#00FF00"));

        // Tab buttons
        var flagsColor = _currentPage == FeatureFlagPage.Flags ? GumpTextColors.Yellow : GumpTextColors.White;
        var gumpsColor = _currentPage == FeatureFlagPage.GumpBlocks ? GumpTextColors.Yellow : GumpTextColors.White;
        var itemsColor = _currentPage == FeatureFlagPage.UseReqBlocks ? GumpTextColors.Yellow : GumpTextColors.White;
        var skillsColor = _currentPage == FeatureFlagPage.SkillBlocks ? GumpTextColors.Yellow : GumpTextColors.White;
        var spellsColor = _currentPage == FeatureFlagPage.SpellBlocks ? GumpTextColors.Yellow : GumpTextColors.White;
        var containersColor = _currentPage == FeatureFlagPage.ContainerBlocks ? GumpTextColors.Yellow : GumpTextColors.White;

        builder.AddButton(20, 45, 4005, 4007, 1);
        builder.AddHtml(55, 47, 80, 20, "Flags".Color(flagsColor));

        builder.AddButton(140, 45, 4005, 4007, 2);
        builder.AddHtml(175, 47, 100, 20, "Gumps".Color(gumpsColor));

        builder.AddButton(270, 45, 4005, 4007, 3);
        builder.AddHtml(305, 47, 80, 20, "Items".Color(itemsColor));

        builder.AddButton(390, 45, 4005, 4007, 4);
        builder.AddHtml(425, 47, 80, 20, "Skills".Color(skillsColor));

        builder.AddButton(510, 45, 4005, 4007, 5);
        builder.AddHtml(545, 47, 80, 20, "Spells".Color(spellsColor));

        builder.AddButton(630, 45, 4005, 4007, 6);
        builder.AddHtml(665, 47, 110, 20, "Containers".Color(containersColor));

        // Content area
        builder.AddAlphaRegion(15, 75, 790, 380);

        switch (_currentPage)
        {
            case FeatureFlagPage.Flags:
                {
                    BuildFlagsPage(ref builder);
                    break;
                }
            case FeatureFlagPage.GumpBlocks:
                {
                    BuildBlockPage(
                        ref builder, "Gump Type",
                        new List<FeatureFlagBlockEntry>(FeatureFlagManager.GetAllGumpBlocks()),
                        2000, 3000, "Use [BlockGump to add new blocks");
                    break;
                }
            case FeatureFlagPage.UseReqBlocks:
                {
                    BuildBlockPage(
                        ref builder, "Item Type",
                        new List<FeatureFlagBlockEntry>(FeatureFlagManager.GetAllUseReqBlocks()),
                        4000, 5000, "Use [BlockUse to add new blocks");
                    break;
                }
            case FeatureFlagPage.SkillBlocks:
                {
                    BuildBlockPage(
                        ref builder, "Skill",
                        new List<FeatureFlagBlockEntry>(FeatureFlagManager.GetAllSkillBlocks()),
                        6000, 7000, "Use [BlockSkill to add new blocks");
                    break;
                }
            case FeatureFlagPage.SpellBlocks:
                {
                    BuildBlockPage(
                        ref builder, "Spell Type",
                        new List<FeatureFlagBlockEntry>(FeatureFlagManager.GetAllSpellBlocks()),
                        8000, 9000, "Use [BlockSpell to add new blocks");
                    break;
                }
            case FeatureFlagPage.ContainerBlocks:
                {
                    BuildBlockPage(
                        ref builder, "Container Type",
                        new List<FeatureFlagBlockEntry>(FeatureFlagManager.GetAllContainerBlocks()),
                        10000, 11000, "Use [BlockContainer to add new blocks");
                    break;
                }
        }

        // Close button
        builder.AddButton(770, 460, 4017, 4019, 0);
        builder.AddHtml(720, 462, 50, 20, "Close".Color(GumpTextColors.White));

        // Save button
        builder.AddButton(20, 460, 4023, 4025, 100);
        builder.AddHtml(55, 462, 50, 20, "Save".Color(GumpTextColors.White));

        // Refresh button
        builder.AddButton(120, 460, 4014, 4016, 101);
        builder.AddHtml(155, 462, 60, 20, "Refresh".Color(GumpTextColors.White));
    }

    private void BuildFlagsPage(ref DynamicGumpBuilder builder)
    {
        builder.AddHtml(20, 80, 150, 20, "Flag".Color(GumpTextColors.Blue));
        builder.AddHtml(180, 80, 150, 20, "Category".Color(GumpTextColors.Blue));
        builder.AddHtml(275, 80, 350, 20, "Description".Color(GumpTextColors.Blue));
        builder.AddHtml(700, 80, 60, 20, "Status".Color(GumpTextColors.Blue));

        var flags = new List<FeatureFlag>(FeatureFlagManager.GetAllFlags());
        flags.Sort((a, b) =>
        {
            var cmp = a.Category.InsensitiveCompare(b.Category);
            return cmp != 0 ? cmp : a.Key.InsensitiveCompare(b.Key);
        });

        var startIndex = _pageIndex * FlagsPerPage;
        var endIndex = Math.Min(startIndex + FlagsPerPage, flags.Count);
        var totalPages = Math.Max(1, (int)Math.Ceiling(flags.Count / (double)FlagsPerPage));

        var y = 105;
        for (var i = startIndex; i < endIndex; i++)
        {
            var flag = flags[i];
            var buttonId = 1000 + i;
            var statusColor = flag.Enabled ? GumpTextColors.Blue : GumpTextColors.Red;

            builder.AddButton(20, y, flag.Enabled ? 2154 : 2151, flag.Enabled ? 2151 : 2154, buttonId);
            builder.AddHtml(60, y + 3, 130, 20, flag.Key.Color(GumpTextColors.White));
            builder.AddHtml(180, y + 3, 150, 20, (flag.Category ?? "").Color(GumpTextColors.LightGray));
            builder.AddHtml(275, y + 3, 350, 20, (flag.Description ?? "").Color(GumpTextColors.LightGray));
            builder.AddHtml(700, y + 3, 60, 20, (flag.Enabled ? "ON" : "OFF").Color(statusColor));

            y += FlagRowHeight;
        }

        AddPagination(ref builder, totalPages);
    }

    private void BuildBlockPage(
        ref DynamicGumpBuilder builder,
        string headerLabel,
        IReadOnlyList<FeatureFlagBlockEntry> blocks,
        int toggleBaseId,
        int removeBaseId,
        string helpText)
    {
        builder.AddHtml(20, 80, 180, 20, headerLabel.Color(GumpTextColors.Blue));
        builder.AddHtml(200, 80, 400, 20, "Reason".Color(GumpTextColors.Blue));
        builder.AddHtml(610, 80, 60, 20, "Status".Color(GumpTextColors.Blue));
        builder.AddHtml(680, 80, 60, 20, "Remove".Color(GumpTextColors.Blue));

        var startIndex = _pageIndex * BlocksPerPage;
        var endIndex = Math.Min(startIndex + BlocksPerPage, blocks.Count);
        var totalPages = Math.Max(1, (int)Math.Ceiling(blocks.Count / (double)BlocksPerPage));

        var y = 105;
        for (var i = startIndex; i < endIndex; i++)
        {
            var block = blocks[i];
            var toggleButtonId = toggleBaseId + i;
            var removeButtonId = removeBaseId + i;
            var statusColor = block.Active ? GumpTextColors.Red : GumpTextColors.Blue;

            builder.AddButton(20, y, block.Active ? 2151 : 2154, block.Active ? 2154 : 2151, toggleButtonId);
            builder.AddHtml(60, y + 3, 150, 20, block.DisplayName.Color(GumpTextColors.White));
            builder.AddHtml(200, y + 3, 400, 40, (block.Reason ?? "").Color(GumpTextColors.LightGray));
            builder.AddHtml(610, y + 3, 60, 20, (block.Active ? "OFF" : "ON").Color(statusColor));

            builder.AddButton(680, y + 3, 4017, 4019, removeButtonId);

            y += BlockRowHeight;
        }

        AddPagination(ref builder, totalPages);
        builder.AddHtml(20, 430, 400, 20, helpText.Color(GumpTextColors.LightGray));
    }

    private void AddPagination(ref DynamicGumpBuilder builder, int totalPages)
    {
        if (totalPages <= 1)
        {
            return;
        }

        if (_pageIndex > 0)
        {
            builder.AddButton(300, 425, 4014, 4016, 102);
            builder.AddHtml(335, 425, 50, 20, "Prev".Color(GumpTextColors.White));
        }

        builder.AddHtml(370, 425, 60, 20, $"{_pageIndex + 1}/{totalPages}".Color(GumpTextColors.White));

        if (_pageIndex < totalPages - 1)
        {
            builder.AddButton(420, 425, 4005, 4007, 103);
            builder.AddHtml(455, 425, 50, 20, "Next".Color(GumpTextColors.White));
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        var buttonId = info.ButtonID;

        switch (buttonId)
        {
            case 0:
                {
                    return;
                }
            case >= (int)(FeatureFlagPage.Flags + 1) and <= (int)(FeatureFlagPage.ContainerBlocks + 1):
                {
                    Resend(from, (FeatureFlagPage)(buttonId - 1));
                    return;
                }
            case 100:
                {
                    FeatureFlagManager.Save();
                    from.SendMessage(0x35, "Feature flags saved to disk.");
                    from.SendGump(this);
                    return;
                }
            case 101:
                {
                    from.SendGump(this);
                    return;
                }
            case 102:
                {
                    Resend(from, _currentPage, _pageIndex - 1);
                    return;
                }
            case 103:
                {
                    Resend(from, _currentPage, _pageIndex + 1);
                    return;
                }
        }

        // Toggle feature flags (1000+)
        if (buttonId is >= 1000 and < 2000)
        {
            var flags = new List<FeatureFlag>(FeatureFlagManager.GetAllFlags());
            flags.Sort((a, b) =>
            {
                var cmp = a.Category.InsensitiveCompare(b.Category);
                return cmp != 0 ? cmp : a.Key.InsensitiveCompare(b.Key);
            });
            var index = buttonId - 1000;
            if (index < flags.Count)
            {
                FeatureFlagManager.SetFlag(flags[index].Key, !flags[index].Enabled, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Toggle gump blocks (2000+)
        if (buttonId is >= 2000 and < 3000)
        {
            var blocks = new List<GumpBlockEntry>(FeatureFlagManager.GetAllGumpBlocks());
            var index = buttonId - 2000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.SetGumpBlockActive(blocks[index].ResolvedType, !blocks[index].Active, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Remove gump blocks (3000+)
        if (buttonId is >= 3000 and < 4000)
        {
            var blocks = new List<GumpBlockEntry>(FeatureFlagManager.GetAllGumpBlocks());
            var index = buttonId - 3000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.UnblockGump(blocks[index].ResolvedType, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Toggle useReq blocks (4000+)
        if (buttonId is >= 4000 and < 5000)
        {
            var blocks = new List<UseReqBlockEntry>(FeatureFlagManager.GetAllUseReqBlocks());
            var index = buttonId - 4000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.SetUseReqBlockActive(blocks[index].ResolvedType, !blocks[index].Active, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Remove useReq blocks (5000+)
        if (buttonId is >= 5000 and < 6000)
        {
            var blocks = new List<UseReqBlockEntry>(FeatureFlagManager.GetAllUseReqBlocks());
            var index = buttonId - 5000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.UnblockUseReq(blocks[index].ResolvedType, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Toggle skill blocks (6000+)
        if (buttonId is >= 6000 and < 7000)
        {
            var blocks = FeatureFlagManager.GetAllSkillBlocks();
            var index = buttonId - 6000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.SetSkillBlockActive(blocks[index].Skill, !blocks[index].Active, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Remove skill blocks (7000+)
        if (buttonId is >= 7000 and < 8000)
        {
            var blocks = FeatureFlagManager.GetAllSkillBlocks();
            var index = buttonId - 7000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.UnblockSkill(blocks[index].Skill, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Toggle spell blocks (8000+)
        if (buttonId is >= 8000 and < 9000)
        {
            var blocks = FeatureFlagManager.GetAllSpellBlocks();
            var index = buttonId - 8000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.SetSpellBlockActive(blocks[index].SpellId, !blocks[index].Active, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Remove spell blocks (9000+)
        if (buttonId is >= 9000 and < 10000)
        {
            var blocks = FeatureFlagManager.GetAllSpellBlocks();
            var index = buttonId - 9000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.UnblockSpell(blocks[index].ResolvedType, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Toggle container blocks (10000+)
        if (buttonId is >= 10000 and < 11000)
        {
            var blocks = new List<ContainerBlockEntry>(FeatureFlagManager.GetAllContainerBlocks());
            var index = buttonId - 10000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.SetContainerBlockActive(blocks[index].ResolvedType, !blocks[index].Active, from.Name);
            }
            from.SendGump(this);
            return;
        }

        // Remove container blocks (11000+)
        if (buttonId is >= 11000 and < 12000)
        {
            var blocks = new List<ContainerBlockEntry>(FeatureFlagManager.GetAllContainerBlocks());
            var index = buttonId - 11000;
            if (index < blocks.Count)
            {
                FeatureFlagManager.UnblockContainer(blocks[index].ResolvedType, from.Name);
            }
            from.SendGump(this);
        }
    }
}
