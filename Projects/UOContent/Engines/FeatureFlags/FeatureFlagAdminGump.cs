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
    private const int ItemsPerPage = 10;

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
        builder.AddBackground(0, 0, 820, 450, 9270);

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
        builder.AddHtml(55, 45, 80, 20, "Flags".Color(flagsColor));

        builder.AddButton(140, 45, 4005, 4007, 2);
        builder.AddHtml(175, 45, 100, 20, "Gumps".Color(gumpsColor));

        builder.AddButton(270, 45, 4005, 4007, 3);
        builder.AddHtml(305, 45, 80, 20, "Items".Color(itemsColor));

        builder.AddButton(390, 45, 4005, 4007, 4);
        builder.AddHtml(425, 45, 80, 20, "Skills".Color(skillsColor));

        builder.AddButton(510, 45, 4005, 4007, 5);
        builder.AddHtml(545, 45, 80, 20, "Spells".Color(spellsColor));

        builder.AddButton(630, 45, 4005, 4007, 6);
        builder.AddHtml(665, 45, 110, 20, "Containers".Color(containersColor));

        // Content area
        builder.AddAlphaRegion(15, 75, 790, 330);

        switch (_currentPage)
        {
            case FeatureFlagPage.Flags:
                BuildFlagsPage(ref builder);
                break;
            case FeatureFlagPage.GumpBlocks:
                BuildGumpBlocksPage(ref builder);
                break;
            case FeatureFlagPage.UseReqBlocks:
                BuildUseReqBlocksPage(ref builder);
                break;
            case FeatureFlagPage.SkillBlocks:
                BuildSkillBlocksPage(ref builder);
                break;
            case FeatureFlagPage.SpellBlocks:
                BuildSpellBlocksPage(ref builder);
                break;
            case FeatureFlagPage.ContainerBlocks:
                BuildContainerBlocksPage(ref builder);
                break;
        }

        // Close button
        builder.AddButton(770, 415, 4017, 4019, 0);
        builder.AddHtml(720, 415, 50, 20, "Close".Color(GumpTextColors.White));

        // Save button
        builder.AddButton(20, 415, 4023, 4025, 100);
        builder.AddHtml(55, 415, 50, 20, "Save".Color(GumpTextColors.White));

        // Refresh button
        builder.AddButton(120, 415, 4014, 4016, 101);
        builder.AddHtml(155, 415, 60, 20, "Refresh".Color(GumpTextColors.White));
    }

    private void BuildFlagsPage(ref DynamicGumpBuilder builder)
    {
        builder.AddHtml(20, 80, 150, 20, "Flag Key".Color(GumpTextColors.Blue));
        builder.AddHtml(180, 80, 150, 20, "Category".Color(GumpTextColors.Blue));
        builder.AddHtml(340, 80, 200, 20, "Description".Color(GumpTextColors.Blue));
        builder.AddHtml(560, 80, 60, 20, "Status".Color(GumpTextColors.Blue));

        var flags = new List<FeatureFlag>(FeatureFlagManager.GetAllFlags());
        flags.Sort((a, b) =>
        {
            var cmp = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
            return cmp != 0 ? cmp : string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
        });

        var startIndex = _pageIndex * ItemsPerPage;
        var endIndex = Math.Min(startIndex + ItemsPerPage, flags.Count);
        var totalPages = (int)Math.Ceiling(flags.Count / (double)ItemsPerPage);

        var y = 100;
        for (var i = startIndex; i < endIndex; i++)
        {
            var flag = flags[i];
            var buttonId = 1000 + i;
            var statusColor = flag.Enabled ? GumpTextColors.Blue : GumpTextColors.Red;

            builder.AddButton(20, y, flag.Enabled ? 2154 : 2151, flag.Enabled ? 2151 : 2154, buttonId);
            builder.AddHtml(45, y, 130, 20, TruncateText(flag.Key, 20).Color(GumpTextColors.White));
            builder.AddHtml(180, y, 150, 20, TruncateText(flag.Category, 18).Color(GumpTextColors.LightGray));
            builder.AddHtml(340, y, 200, 20, TruncateText(flag.Description, 30).Color(GumpTextColors.LightGray));
            builder.AddHtml(560, y, 60, 20, (flag.Enabled ? "ON" : "OFF").Color(statusColor));

            y += 25;
        }

        AddPagination(ref builder, totalPages, 200);
    }

    private void BuildGumpBlocksPage(ref DynamicGumpBuilder builder)
    {
        builder.AddHtml(20, 80, 200, 20, "Gump Type".Color(GumpTextColors.Blue));
        builder.AddHtml(230, 80, 200, 20, "Reason".Color(GumpTextColors.Blue));
        builder.AddHtml(440, 80, 80, 20, "Status".Color(GumpTextColors.Blue));
        builder.AddHtml(530, 80, 60, 20, "Remove".Color(GumpTextColors.Blue));

        var blocks = new List<GumpBlockEntry>(FeatureFlagManager.GetAllGumpBlocks());

        var startIndex = _pageIndex * ItemsPerPage;
        var endIndex = Math.Min(startIndex + ItemsPerPage, blocks.Count);
        var totalPages = (int)Math.Ceiling(blocks.Count / (double)ItemsPerPage);

        var y = 100;
        for (var i = startIndex; i < endIndex; i++)
        {
            var block = blocks[i];
            var toggleButtonId = 2000 + i;
            var removeButtonId = 3000 + i;
            var statusColor = block.Active ? GumpTextColors.Blue : GumpTextColors.Red;

            builder.AddButton(20, y, block.Active ? 2154 : 2151, block.Active ? 2151 : 2154, toggleButtonId);
            var name = block.GumpType?.Name ?? block.GumpTypeName;
            builder.AddHtml(45, y, 180, 20, TruncateText(name, 25).Color(GumpTextColors.White));
            builder.AddHtml(230, y, 200, 20, TruncateText(block.Reason, 30).Color(GumpTextColors.LightGray));
            builder.AddHtml(440, y, 80, 20, (block.Active ? "ACTIVE" : "INACTIVE").Color(statusColor));

            builder.AddButton(530, y, 4017, 4019, removeButtonId);

            y += 25;
        }

        AddPagination(ref builder, totalPages, 300);
        builder.AddHtml(20, 380, 200, 20, "Use [BlockGump to add new blocks".Color(GumpTextColors.LightGray));
    }

    private void BuildUseReqBlocksPage(ref DynamicGumpBuilder builder)
    {
        builder.AddHtml(20, 80, 200, 20, "Item Type".Color(GumpTextColors.Blue));
        builder.AddHtml(230, 80, 200, 20, "Reason".Color(GumpTextColors.Blue));
        builder.AddHtml(440, 80, 80, 20, "Status".Color(GumpTextColors.Blue));
        builder.AddHtml(530, 80, 60, 20, "Remove".Color(GumpTextColors.Blue));

        var blocks = new List<UseReqBlockEntry>(FeatureFlagManager.GetAllUseReqBlocks());

        var startIndex = _pageIndex * ItemsPerPage;
        var endIndex = Math.Min(startIndex + ItemsPerPage, blocks.Count);
        var totalPages = (int)Math.Ceiling(blocks.Count / (double)ItemsPerPage);

        var y = 100;
        for (var i = startIndex; i < endIndex; i++)
        {
            var block = blocks[i];
            var toggleButtonId = 4000 + i;
            var removeButtonId = 5000 + i;
            var statusColor = block.Active ? GumpTextColors.Blue : GumpTextColors.Red;

            builder.AddButton(20, y, block.Active ? 2154 : 2151, block.Active ? 2151 : 2154, toggleButtonId);
            var name = block.ItemType?.Name ?? block.ItemTypeName;
            builder.AddHtml(45, y, 180, 20, TruncateText(name, 25).Color(GumpTextColors.White));
            builder.AddHtml(230, y, 200, 20, TruncateText(block.Reason, 30).Color(GumpTextColors.White));
            builder.AddHtml(440, y, 80, 20, (block.Active ? "ACTIVE" : "INACTIVE").Color(statusColor));

            builder.AddButton(530, y, 4017, 4019, removeButtonId);

            y += 25;
        }

        AddPagination(ref builder, totalPages, 400);
        builder.AddHtml(20, 380, 250, 20, "Use [BlockUse to add new blocks".Color(GumpTextColors.LightGray));
    }

    private void BuildSkillBlocksPage(ref DynamicGumpBuilder builder)
    {
        builder.AddHtml(20, 80, 150, 20, "Skill".Color(GumpTextColors.Blue));
        builder.AddHtml(180, 80, 200, 20, "Reason".Color(GumpTextColors.Blue));
        builder.AddHtml(400, 80, 80, 20, "Status".Color(GumpTextColors.Blue));
        builder.AddHtml(500, 80, 60, 20, "Remove".Color(GumpTextColors.Blue));

        var blocks = FeatureFlagManager.GetAllSkillBlocks();

        var startIndex = _pageIndex * ItemsPerPage;
        var endIndex = Math.Min(startIndex + ItemsPerPage, blocks.Count);
        var totalPages = Math.Max(1, (int)Math.Ceiling(blocks.Count / (double)ItemsPerPage));

        var y = 100;
        for (var i = startIndex; i < endIndex; i++)
        {
            var block = blocks[i];
            var toggleButtonId = 6000 + i;
            var removeButtonId = 7000 + i;
            var statusColor = block.Active ? GumpTextColors.Blue : GumpTextColors.Red;

            builder.AddButton(20, y, block.Active ? 2154 : 2151, block.Active ? 2151 : 2154, toggleButtonId);
            builder.AddHtml(45, y, 130, 20, block.SkillName.Color(GumpTextColors.White));
            builder.AddHtml(180, y, 210, 20, TruncateText(block.Reason, 30).Color(GumpTextColors.LightGray));
            builder.AddHtml(400, y, 80, 20, (block.Active ? "ACTIVE" : "INACTIVE").Color(statusColor));

            builder.AddButton(500, y, 4017, 4019, removeButtonId);

            y += 25;
        }

        AddPagination(ref builder, totalPages, 500);
        builder.AddHtml(20, 380, 250, 20, "Use [BlockSkill to add new blocks".Color(GumpTextColors.LightGray));
    }

    private void BuildSpellBlocksPage(ref DynamicGumpBuilder builder)
    {
        builder.AddHtml(20, 80, 200, 20, "Spell Type".Color(GumpTextColors.Blue));
        builder.AddHtml(230, 80, 200, 20, "Reason".Color(GumpTextColors.Blue));
        builder.AddHtml(440, 80, 80, 20, "Status".Color(GumpTextColors.Blue));
        builder.AddHtml(530, 80, 60, 20, "Remove".Color(GumpTextColors.Blue));

        var blocks = FeatureFlagManager.GetAllSpellBlocks();

        var startIndex = _pageIndex * ItemsPerPage;
        var endIndex = Math.Min(startIndex + ItemsPerPage, blocks.Count);
        var totalPages = Math.Max(1, (int)Math.Ceiling(blocks.Count / (double)ItemsPerPage));

        var y = 100;
        for (var i = startIndex; i < endIndex; i++)
        {
            var block = blocks[i];
            var toggleButtonId = 8000 + i;
            var removeButtonId = 9000 + i;
            var statusColor = block.Active ? GumpTextColors.Blue : GumpTextColors.Red;

            builder.AddButton(20, y, block.Active ? 2154 : 2151, block.Active ? 2151 : 2154, toggleButtonId);
            var name = block.SpellType?.Name ?? block.SpellTypeName;
            builder.AddHtml(45, y, 180, 20, TruncateText(name, 25).Color(GumpTextColors.White));
            builder.AddHtml(230, y, 200, 20, TruncateText(block.Reason, 30).Color(GumpTextColors.LightGray));
            builder.AddHtml(440, y, 80, 20, (block.Active ? "ACTIVE" : "INACTIVE").Color(statusColor));

            builder.AddButton(530, y, 4017, 4019, removeButtonId);

            y += 25;
        }

        AddPagination(ref builder, totalPages, 600);
        builder.AddHtml(20, 380, 250, 20, "Use [BlockSpell to add new blocks".Color(GumpTextColors.LightGray));
    }

    private void BuildContainerBlocksPage(ref DynamicGumpBuilder builder)
    {
        builder.AddHtml(20, 80, 200, 20, "Container Type".Color(GumpTextColors.Blue));
        builder.AddHtml(230, 80, 200, 20, "Reason".Color(GumpTextColors.Blue));
        builder.AddHtml(440, 80, 80, 20, "Status".Color(GumpTextColors.Blue));
        builder.AddHtml(530, 80, 60, 20, "Remove".Color(GumpTextColors.Blue));

        var blocks = new List<ContainerBlockEntry>(FeatureFlagManager.GetAllContainerBlocks());

        var startIndex = _pageIndex * ItemsPerPage;
        var endIndex = Math.Min(startIndex + ItemsPerPage, blocks.Count);
        var totalPages = Math.Max(1, (int)Math.Ceiling(blocks.Count / (double)ItemsPerPage));

        var y = 100;
        for (var i = startIndex; i < endIndex; i++)
        {
            var block = blocks[i];
            var toggleButtonId = 10000 + i;
            var removeButtonId = 11000 + i;
            var statusColor = block.Active ? GumpTextColors.Blue : GumpTextColors.Red;

            builder.AddButton(20, y, block.Active ? 2154 : 2151, block.Active ? 2151 : 2154, toggleButtonId);
            var name = block.ContainerType?.Name ?? block.ContainerTypeName;
            builder.AddHtml(45, y, 180, 20, TruncateText(name, 25).Color(GumpTextColors.White));
            builder.AddHtml(230, y, 200, 20, TruncateText(block.Reason, 30).Color(GumpTextColors.LightGray));
            builder.AddHtml(440, y, 80, 20, (block.Active ? "ACTIVE" : "INACTIVE").Color(statusColor));

            builder.AddButton(530, y, 4017, 4019, removeButtonId);

            y += 25;
        }

        AddPagination(ref builder, totalPages, 700);
        builder.AddHtml(20, 380, 300, 20, "Use [BlockContainer to add new blocks".Color(GumpTextColors.LightGray));
    }

    private void AddPagination(ref DynamicGumpBuilder builder, int totalPages, int baseButtonId)
    {
        if (totalPages <= 1)
        {
            return;
        }

        if (_pageIndex > 0)
        {
            builder.AddButton(300, 375, 4014, 4016, baseButtonId + 1);
            builder.AddHtml(335, 375, 50, 20, "Prev".Color(GumpTextColors.White));
        }

        builder.AddHtml(370, 375, 60, 20, $"{_pageIndex + 1}/{totalPages}".Color(GumpTextColors.White));

        if (_pageIndex < totalPages - 1)
        {
            builder.AddButton(420, 375, 4005, 4007, baseButtonId + 2);
            builder.AddHtml(455, 375, 50, 20, "Next".Color(GumpTextColors.White));
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        var buttonId = info.ButtonID;

        switch (buttonId)
        {
            case 0: return;
            case 1: Resend(from, FeatureFlagPage.Flags); return;
            case 2: Resend(from, FeatureFlagPage.GumpBlocks); return;
            case 3: Resend(from, FeatureFlagPage.UseReqBlocks); return;
            case 4: Resend(from, FeatureFlagPage.SkillBlocks); return;
            case 5: Resend(from, FeatureFlagPage.SpellBlocks); return;
            case 6: Resend(from, FeatureFlagPage.ContainerBlocks); return;
            case 100:
                FeatureFlagManager.Save();
                from.SendMessage(0x35, "Feature flags saved to disk.");
                from.SendGump(this);
                return;
            case 101:
                from.SendGump(this);
                return;
        }

        // Pagination
        if (buttonId == 201) { Resend(from, FeatureFlagPage.Flags, _pageIndex - 1); return; }
        if (buttonId == 202) { Resend(from, FeatureFlagPage.Flags, _pageIndex + 1); return; }
        if (buttonId == 301) { Resend(from, FeatureFlagPage.GumpBlocks, _pageIndex - 1); return; }
        if (buttonId == 302) { Resend(from, FeatureFlagPage.GumpBlocks, _pageIndex + 1); return; }
        if (buttonId == 401) { Resend(from, FeatureFlagPage.UseReqBlocks, _pageIndex - 1); return; }
        if (buttonId == 402) { Resend(from, FeatureFlagPage.UseReqBlocks, _pageIndex + 1); return; }
        if (buttonId == 501) { Resend(from, FeatureFlagPage.SkillBlocks, _pageIndex - 1); return; }
        if (buttonId == 502) { Resend(from, FeatureFlagPage.SkillBlocks, _pageIndex + 1); return; }
        if (buttonId == 601) { Resend(from, FeatureFlagPage.SpellBlocks, _pageIndex - 1); return; }
        if (buttonId == 602) { Resend(from, FeatureFlagPage.SpellBlocks, _pageIndex + 1); return; }
        if (buttonId == 701) { Resend(from, FeatureFlagPage.ContainerBlocks, _pageIndex - 1); return; }
        if (buttonId == 702) { Resend(from, FeatureFlagPage.ContainerBlocks, _pageIndex + 1); return; }

        // Toggle feature flags (1000+)
        if (buttonId is >= 1000 and < 2000)
        {
            var flags = new List<FeatureFlag>(FeatureFlagManager.GetAllFlags());
            flags.Sort((a, b) =>
            {
                var cmp = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
                return cmp != 0 ? cmp : string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase);
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
                FeatureFlagManager.SetGumpBlockActive(blocks[index].GumpTypeName, !blocks[index].Active, from.Name);
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
                FeatureFlagManager.UnblockGumpByName(blocks[index].GumpTypeName, from.Name);
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
                FeatureFlagManager.SetUseReqBlockActive(blocks[index].ItemTypeName, !blocks[index].Active, from.Name);
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
                FeatureFlagManager.UnblockUseReqByName(blocks[index].ItemTypeName, from.Name);
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
            if (index < blocks.Count && blocks[index].SpellType != null)
            {
                FeatureFlagManager.UnblockSpell(blocks[index].SpellType, from.Name);
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
                FeatureFlagManager.SetContainerBlockActive(blocks[index].ContainerTypeName, !blocks[index].Active, from.Name);
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
                FeatureFlagManager.UnblockContainerByName(blocks[index].ContainerTypeName, from.Name);
            }
            from.SendGump(this);
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
