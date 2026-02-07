using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Collections;
using Server.Gumps;
using Server.Network;

namespace Server.Systems.FeatureFlags;

public sealed class FeatureFlagAdminGump : DynamicGump
{
    public enum FeatureFlagPage
    {
        Flags,
        GumpBlocks,
        ItemBlocks,
        SkillBlocks,
        SpellBlocks
    }

    private FeatureFlagPage _currentPage;
    private int _pageIndex;
    private int _displayedCount;
    private readonly FeatureFlag[] _displayedFlags = new FeatureFlag[FlagsPerPage];
    private readonly FeatureFlagBlockEntry[] _displayedBlocks = new FeatureFlagBlockEntry[BlocksPerPage];
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
        var itemsColor = _currentPage == FeatureFlagPage.ItemBlocks ? GumpTextColors.Yellow : GumpTextColors.White;
        var skillsColor = _currentPage == FeatureFlagPage.SkillBlocks ? GumpTextColors.Yellow : GumpTextColors.White;
        var spellsColor = _currentPage == FeatureFlagPage.SpellBlocks ? GumpTextColors.Yellow : GumpTextColors.White;

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

        // Content area
        builder.AddAlphaRegion(15, 75, 790, 380);

        if (_currentPage == FeatureFlagPage.Flags)
        {
            BuildFlagsPage(ref builder);
        }
        else if (_currentPage == FeatureFlagPage.GumpBlocks)
        {
            BuildBlockPage(
                ref builder,
                "Gump Type",
                FeatureFlagManager.GetAllGumpBlocks(),
                "Use [BlockGump to add new blocks"
            );
        }
        else if (_currentPage == FeatureFlagPage.ItemBlocks)
        {
            BuildItemBlockPage(
                ref builder,
                FeatureFlagManager.GetAllItemBlocks(),
                "Use [BlockItem to add new blocks"
            );
        }
        else if (_currentPage == FeatureFlagPage.SkillBlocks)
        {
            BuildBlockPage(
                ref builder,
                "Skill",
                FeatureFlagManager.GetAllSkillBlocks(),
                "Use [BlockSkill to add new blocks"
            );
        }
        else if (_currentPage == FeatureFlagPage.SpellBlocks)
        {
            BuildBlockPage(
                ref builder,
                "Spell Type",
                FeatureFlagManager.GetAllSpellBlocks(),
                "Use [BlockSpell to add new blocks"
            );
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
        builder.AddHtml(20, 80, 150, 20, "Flag".Color(GumpTextColors.White));
        builder.AddHtml(180, 80, 150, 20, "Category".Color(GumpTextColors.White));
        builder.AddHtml(275, 80, 350, 20, "Description".Color(GumpTextColors.White));
        builder.AddHtml(690, 80, 60, 20, "Status".Color(GumpTextColors.White));

        var flags = new List<FeatureFlag>(FeatureFlagManager.GetAllFlags());
        flags.Sort((a, b) =>
        {
            var cmp = a.Category.InsensitiveCompare(b.Category);
            return cmp != 0 ? cmp : a.Key.InsensitiveCompare(b.Key);
        });

        var startIndex = _pageIndex * FlagsPerPage;
        var endIndex = Math.Min(startIndex + FlagsPerPage, flags.Count);
        var totalPages = Math.Max(1, (int)Math.Ceiling(flags.Count / (double)FlagsPerPage));

        _displayedCount = 0;
        var y = 105;
        for (var i = startIndex; i < endIndex; i++)
        {
            var flag = flags[i];
            _displayedFlags[_displayedCount] = flag;
            var statusColor = flag.Enabled ? GumpTextColors.Green : GumpTextColors.Red;

            builder.AddButton(20, y, flag.Enabled ? 2154 : 2151, flag.Enabled ? 2151 : 2154, 1000 + _displayedCount);
            builder.AddHtml(60, y + 3, 130, 20, flag.Key.Color(GumpTextColors.White));
            builder.AddHtml(180, y + 3, 150, 20, (flag.Category ?? "").Color(GumpTextColors.LightGray));
            builder.AddHtml(275, y + 3, 350, 20, (flag.Description ?? "").Color(GumpTextColors.LightGray));
            builder.AddHtml(690, y + 3, 60, 20, (flag.Enabled ? "ON" : "OFF").Color(statusColor));

            _displayedCount++;
            y += FlagRowHeight;
        }

        AddPagination(ref builder, totalPages);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BuildBlockPage(
        ref DynamicGumpBuilder builder,
        string headerLabel,
        IReadOnlyCollection<FeatureFlagBlockEntry> blocks,
        string helpText
    )
    {
        using var list = PooledRefList<FeatureFlagBlockEntry>.Create();
        foreach (var b in blocks)
        {
            if (b != null)
            {
                list.Add(b);
            }
        }
        BuildBlockPage(ref builder, headerLabel, list.AsSpan(), helpText);
    }

    private void BuildBlockPage(
        ref DynamicGumpBuilder builder,
        string headerLabel,
        ReadOnlySpan<FeatureFlagBlockEntry> blocks,
        string helpText
    )
    {
        builder.AddHtml(20, 80, 180, 20, headerLabel.Color(GumpTextColors.White));
        builder.AddHtml(200, 80, 400, 20, "Reason".Color(GumpTextColors.White));
        builder.AddHtml(690, 80, 60, 20, "Status".Color(GumpTextColors.White));
        builder.AddHtml(750, 80, 60, 20, "Remove".Color(GumpTextColors.White));

        var startIndex = _pageIndex * BlocksPerPage;
        var count = 0;

        _displayedCount = 0;
        var skipped = 0;
        var y = 105;

        for (var i = 0; i < blocks.Length; i++)
        {
            var block = blocks[i];
            if (block == null)
            {
                continue;
            }

            count++;

            if (skipped < startIndex)
            {
                skipped++;
                continue;
            }

            if (_displayedCount < BlocksPerPage)
            {
                _displayedBlocks[_displayedCount] = block;
                var statusColor = block.Active ? GumpTextColors.Red : GumpTextColors.Green;

                builder.AddButton(20, y, block.Active ? 2151 : 2154, block.Active ? 2154 : 2151, 2000 + _displayedCount);
                builder.AddHtml(60, y + 3, 150, 20, block.DisplayName.Color(GumpTextColors.White));
                builder.AddHtml(200, y + 3, 400, 40, (block.Reason ?? "(default)").Color(GumpTextColors.LightGray));
                builder.AddHtml(690, y + 3, 60, 20, (block.Active ? "OFF" : "ON").Color(statusColor));
                builder.AddButton(750, y + 3, 4017, 4019, 3000 + _displayedCount);

                _displayedCount++;
                y += BlockRowHeight;
            }
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(count / (double)BlocksPerPage));
        AddPagination(ref builder, totalPages);
        builder.AddHtml(20, 430, 400, 20, helpText.Color(GumpTextColors.LightGray));
    }

    private void BuildItemBlockPage(
        ref DynamicGumpBuilder builder,
        IReadOnlyCollection<ItemBlockEntry> blocks,
        string helpText
    )
    {
        builder.AddHtml(20, 80, 150, 20, "Item Type".Color(GumpTextColors.White));
        builder.AddHtml(180, 80, 280, 20, "Reason".Color(GumpTextColors.White));
        builder.AddHtml(530, 80, 50, 20, "Use".Color(GumpTextColors.White));
        builder.AddHtml(580, 80, 50, 20, "Equip".Color(GumpTextColors.White));
        builder.AddHtml(640, 80, 50, 20, "Open".Color(GumpTextColors.White));
        builder.AddHtml(700, 80, 40, 20, "Edit".Color(GumpTextColors.White));
        builder.AddHtml(750, 80, 60, 20, "Remove".Color(GumpTextColors.White));

        var startIndex = _pageIndex * BlocksPerPage;

        _displayedCount = 0;
        var skipped = 0;
        var y = 105;

        foreach (var block in blocks)
        {
            if (skipped < startIndex)
            {
                skipped++;
                continue;
            }

            if (_displayedCount >= BlocksPerPage)
            {
                break;
            }

            _displayedBlocks[_displayedCount] = block;

            builder.AddButton(20, y, block.Active ? 2151 : 2154, block.Active ? 2154 : 2151, 2000 + _displayedCount);
            builder.AddHtml(60, y + 3, 120, 20, block.DisplayName.Color(GumpTextColors.White));
            builder.AddHtml(180, y + 3, 280, 40, (block.Reason ?? "(default)").Color(GumpTextColors.LightGray));

            var useColor = block.BlockUse ? GumpTextColors.Red : GumpTextColors.Green;
            var equipColor = block.BlockEquip ? GumpTextColors.Red : GumpTextColors.Green;
            var containerColor = block.BlockContainerAccess ? GumpTextColors.Red : GumpTextColors.Green;

            builder.AddHtml(530, y + 3, 50, 20, (block.BlockUse ? "X" : "-").Color(useColor));
            builder.AddHtml(580, y + 3, 50, 20, (block.BlockEquip ? "X" : "-").Color(equipColor));
            builder.AddHtml(640, y + 3, 50, 20, (block.BlockContainerAccess ? "X" : "-").Color(containerColor));

            builder.AddButton(700, y + 3, 4011, 4013, 4000 + _displayedCount);
            builder.AddButton(750, y + 3, 4017, 4019, 3000 + _displayedCount);

            _displayedCount++;
            y += BlockRowHeight;
        }

        var totalCount = blocks.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)BlocksPerPage));
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

        if (buttonId == 0)
        {
            return;
        }

        if (buttonId is >= (int)(FeatureFlagPage.Flags + 1) and <= (int)(FeatureFlagPage.SpellBlocks + 1))
        {
            Resend(from, (FeatureFlagPage)(buttonId - 1));
            return;
        }

        if (buttonId == 100)
        {
            FeatureFlagManager.Save();
            from.SendMessage(0x35, "Feature flags saved to disk.");
            from.SendGump(this);
            return;
        }

        if (buttonId == 101)
        {
            from.SendGump(this);
            return;
        }

        if (buttonId == 102)
        {
            Resend(from, _currentPage, _pageIndex - 1);
            return;
        }

        if (buttonId == 103)
        {
            Resend(from, _currentPage, _pageIndex + 1);
            return;
        }

        switch (buttonId)
        {
            // Toggle feature flags
            case >= 1000 and < 1000 + FlagsPerPage:
                {
                    var index = buttonId - 1000;
                    if (index < _displayedCount)
                    {
                        var flag = _displayedFlags[index];
                        FeatureFlagManager.SetFlag(flag.Key, !flag.Enabled, from.Name);
                    }
                    break;
                }
            // Toggle block
            case >= 2000 and < 2000 + BlocksPerPage:
                {
                    var index = buttonId - 2000;
                    if (index < _displayedCount)
                    {
                        var block = _displayedBlocks[index];
                        switch (_currentPage)
                        {
                            case FeatureFlagPage.GumpBlocks:
                                {
                                    FeatureFlagManager.SetGumpBlockActive(block.ResolvedType, !block.Active, from.Name);
                                    break;
                                }
                            case FeatureFlagPage.ItemBlocks:
                                {
                                    FeatureFlagManager.SetItemBlockActive(block.ResolvedType, !block.Active, from.Name);
                                    break;
                                }
                            case FeatureFlagPage.SkillBlocks:
                                {
                                    FeatureFlagManager.SetSkillBlockActive(((SkillBlockEntry)block).Skill, !block.Active, from.Name);
                                    break;
                                }
                            case FeatureFlagPage.SpellBlocks:
                                {
                                    FeatureFlagManager.SetSpellBlockActive(((SpellBlockEntry)block).SpellId, !block.Active, from.Name);
                                    break;
                                }
                        }
                    }

                    break;
                }
            // Remove block
            case >= 3000 and < 3000 + BlocksPerPage:
                {
                    var index = buttonId - 3000;
                    if (index < _displayedCount)
                    {
                        var block = _displayedBlocks[index];
                        switch (_currentPage)
                        {
                            case FeatureFlagPage.GumpBlocks:
                                {
                                    FeatureFlagManager.UnblockGump(block.ResolvedType, from.Name);
                                    break;
                                }
                            case FeatureFlagPage.ItemBlocks:
                                {
                                    FeatureFlagManager.RemoveItemBlock(block.ResolvedType, from.Name);
                                    break;
                                }
                            case FeatureFlagPage.SkillBlocks:
                                {
                                    FeatureFlagManager.UnblockSkill(((SkillBlockEntry)block).Skill, from.Name);
                                    break;
                                }
                            case FeatureFlagPage.SpellBlocks:
                                {
                                    FeatureFlagManager.UnblockSpell(block.ResolvedType, from.Name);
                                    break;
                                }
                        }
                    }
                    break;
                }
            // Edit item block (PropertiesGump)
            case >= 4000 and < 4000 + BlocksPerPage:
                {
                    var index = buttonId - 4000;
                    if (index < _displayedCount && _currentPage == FeatureFlagPage.ItemBlocks)
                    {
                        from.SendGump(new PropertiesGump(from, _displayedBlocks[index]));
                    }
                    break;
                }
        }

        from.SendGump(this);
    }
}
