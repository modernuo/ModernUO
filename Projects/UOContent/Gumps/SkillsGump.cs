using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Network;

using static Server.Gumps.PropsConfig;

namespace Server.Gumps
{
    public class EditSkillGump : Gump
    {
        private static readonly int EntryWidth = 160;

        private static readonly int TotalWidth = OffsetSize + EntryWidth + OffsetSize + SetWidth + OffsetSize;
        private static readonly int TotalHeight = OffsetSize + 2 * (EntryHeight + OffsetSize);

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;
        private static readonly int BackHeight = BorderSize + TotalHeight + BorderSize;

        private readonly Mobile m_From;

        private readonly SkillsGumpGroup m_Selected;
        private readonly Skill m_Skill;
        private readonly Mobile m_Target;

        public EditSkillGump(Mobile from, Mobile target, Skill skill, SkillsGumpGroup selected) : base(
            GumpOffsetX,
            GumpOffsetY
        )
        {
            m_From = from;
            m_Target = target;
            m_Skill = skill;
            m_Selected = selected;

            var initialText = m_Skill.Base.ToString("F1");

            AddPage(0);

            AddBackground(0, 0, BackWidth, BackHeight, BackGumpID);
            AddImageTiled(
                BorderSize,
                BorderSize,
                TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0),
                TotalHeight,
                OffsetGumpID
            );

            var x = BorderSize + OffsetSize;
            var y = BorderSize + OffsetSize;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddLabelCropped(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, skill.Name);
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            x = BorderSize + OffsetSize;
            y += EntryHeight + OffsetSize;

            AddImageTiled(x, y, EntryWidth, EntryHeight, EntryGumpID);
            AddTextEntry(x + TextOffsetX, y, EntryWidth - TextOffsetX, EntryHeight, TextHue, 0, initialText);
            x += EntryWidth + OffsetSize;

            if (SetGumpID != 0)
            {
                AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
            }

            AddButton(x + SetOffsetX, y + SetOffsetY, SetButtonID1, SetButtonID2, 1);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
            {
                try
                {
                    if (m_From.AccessLevel >= AccessLevel.GameMaster)
                    {
                        var text = info.GetTextEntry(0);

                        if (text != null)
                        {
                            m_Skill.Base = Convert.ToDouble(text.Text);
                            CommandLogging.LogChangeProperty(m_From, m_Target, $"{m_Skill}.Base", m_Skill.Base.ToString());
                        }
                    }
                    else
                    {
                        m_From.SendMessage("You may not change that.");
                    }

                    m_From.SendGump(new SkillsGump(m_From, m_Target, m_Selected));
                }
                catch
                {
                    m_From.SendMessage("Bad format. ###.# expected.");
                    m_From.SendGump(new EditSkillGump(m_From, m_Target, m_Skill, m_Selected));
                }
            }
            else
            {
                m_From.SendGump(new SkillsGump(m_From, m_Target, m_Selected));
            }
        }
    }

    public class SkillsGump : Gump
    {
        /*
        private static bool PrevLabel = OldStyle, NextLabel = OldStyle;

        private static readonly int PrevLabelOffsetX = PrevWidth + 1;

        private static readonly int PrevLabelOffsetY = 0;

        private static readonly int NextLabelOffsetX = -29;
        private static readonly int NextLabelOffsetY = 0;
         * */

        private static readonly int NameWidth = 107;
        private static readonly int ValueWidth = 128;

        private static readonly int TotalWidth =
            OffsetSize + NameWidth + OffsetSize + ValueWidth + OffsetSize + SetWidth + OffsetSize;

        private static readonly int BackWidth = BorderSize + TotalWidth + BorderSize;

        private static readonly int IndentWidth = 12;

        private readonly Mobile m_From;

        private readonly SkillsGumpGroup[] m_Groups;
        private readonly SkillsGumpGroup m_Selected;
        private readonly Mobile m_Target;

        public SkillsGump(Mobile from, Mobile target, SkillsGumpGroup selected = null) : base(GumpOffsetX, GumpOffsetY)
        {
            m_From = from;
            m_Target = target;

            m_Groups = SkillsGumpGroup.Groups;
            m_Selected = selected;

            var count = m_Groups.Length;

            if (selected != null)
            {
                count += selected.Skills.Length;
            }

            var totalHeight = OffsetSize + (EntryHeight + OffsetSize) * (count + 1);

            AddPage(0);

            AddBackground(0, 0, BackWidth, BorderSize + totalHeight + BorderSize, BackGumpID);
            AddImageTiled(
                BorderSize,
                BorderSize,
                TotalWidth - (OldStyle ? SetWidth + OffsetSize : 0),
                totalHeight,
                OffsetGumpID
            );

            var x = BorderSize + OffsetSize;
            var y = BorderSize + OffsetSize;

            var emptyWidth = TotalWidth - PrevWidth - NextWidth - OffsetSize * 4 - (OldStyle ? SetWidth + OffsetSize : 0);

            if (OldStyle)
            {
                AddImageTiled(x, y, TotalWidth - OffsetSize * 3 - SetWidth, EntryHeight, HeaderGumpID);
            }
            else
            {
                AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);
            }

            x += PrevWidth + OffsetSize;

            if (!OldStyle)
            {
                AddImageTiled(
                    x - (OldStyle ? OffsetSize : 0),
                    y,
                    emptyWidth + (OldStyle ? OffsetSize * 2 : 0),
                    EntryHeight,
                    HeaderGumpID
                );
            }

            x += emptyWidth + OffsetSize;

            if (!OldStyle)
            {
                AddImageTiled(x, y, NextWidth, EntryHeight, HeaderGumpID);
            }

            for (var i = 0; i < m_Groups.Length; ++i)
            {
                x = BorderSize + OffsetSize;
                y += EntryHeight + OffsetSize;

                var group = m_Groups[i];

                AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);

                if (group == selected)
                {
                    AddButton(x + PrevOffsetX, y + PrevOffsetY, 0x15E2, 0x15E6, GetButtonID(0, i));
                }
                else
                {
                    AddButton(x + PrevOffsetX, y + PrevOffsetY, 0x15E1, 0x15E5, GetButtonID(0, i));
                }

                x += PrevWidth + OffsetSize;

                x -= OldStyle ? OffsetSize : 0;

                AddImageTiled(x, y, emptyWidth + (OldStyle ? OffsetSize * 2 : 0), EntryHeight, EntryGumpID);
                AddLabel(x + TextOffsetX, y, TextHue, group?.Name ?? "");

                x += emptyWidth + (OldStyle ? OffsetSize * 2 : 0);
                x += OffsetSize;

                if (SetGumpID != 0)
                {
                    AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                }

                if (group != selected)
                {
                    continue;
                }

                var indentMaskX = BorderSize;
                var indentMaskY = y + EntryHeight + OffsetSize;

                for (var j = 0; j < group!.Skills.Length; ++j)
                {
                    var sk = target.Skills[group.Skills[j]];

                    x = BorderSize + OffsetSize;
                    y += EntryHeight + OffsetSize;

                    x += OffsetSize;
                    x += IndentWidth;

                    AddImageTiled(x, y, PrevWidth, EntryHeight, HeaderGumpID);

                    AddButton(x + PrevOffsetX, y + PrevOffsetY, 0x15E1, 0x15E5, GetButtonID(1, j));

                    x += PrevWidth + OffsetSize;

                    x -= OldStyle ? OffsetSize : 0;

                    AddImageTiled(
                        x,
                        y,
                        emptyWidth + (OldStyle ? OffsetSize * 2 : 0) - OffsetSize - IndentWidth,
                        EntryHeight,
                        EntryGumpID
                    );
                    AddLabel(x + TextOffsetX, y, TextHue, sk == null ? "(null)" : sk.Name);

                    x += emptyWidth + (OldStyle ? OffsetSize * 2 : 0) - OffsetSize - IndentWidth;
                    x += OffsetSize;

                    if (SetGumpID != 0)
                    {
                        AddImageTiled(x, y, SetWidth, EntryHeight, SetGumpID);
                    }

                    if (sk != null)
                    {
                        int buttonID1, buttonID2;
                        int xOffset, yOffset;

                        switch (sk.Lock)
                        {
                            default:
                                buttonID1 = 0x983;
                                buttonID2 = 0x983;
                                xOffset = 6;
                                yOffset = 4;
                                break;
                            case SkillLock.Down:
                                buttonID1 = 0x985;
                                buttonID2 = 0x985;
                                xOffset = 6;
                                yOffset = 4;
                                break;
                            case SkillLock.Locked:
                                buttonID1 = 0x82C;
                                buttonID2 = 0x82C;
                                xOffset = 5;
                                yOffset = 2;
                                break;
                        }

                        AddButton(x + xOffset, y + yOffset, buttonID1, buttonID2, GetButtonID(2, j));

                        y += 1;
                        x -= OffsetSize;
                        x -= 1;
                        x -= 50;

                        AddImageTiled(x, y, 50, EntryHeight - 2, OffsetGumpID);

                        x += 1;
                        y += 1;

                        AddImageTiled(x, y, 48, EntryHeight - 4, EntryGumpID);

                        AddLabelCropped(
                            x + TextOffsetX,
                            y - 1,
                            48 - TextOffsetX,
                            EntryHeight - 3,
                            TextHue,
                            sk.Base.ToString("F1")
                        );

                        y -= 2;
                    }
                }

                AddImageTiled(
                    indentMaskX,
                    indentMaskY,
                    IndentWidth + OffsetSize,
                    group.Skills.Length * (EntryHeight + OffsetSize) - (i < m_Groups.Length - 1 ? OffsetSize : 0),
                    BackGumpID + 4
                );
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var buttonID = info.ButtonID - 1;

            var index = buttonID / 3;
            var type = buttonID % 3;

            switch (type)
            {
                case 0:
                    {
                        if (index >= 0 && index < m_Groups.Length)
                        {
                            var newSelection = m_Groups[index];

                            if (m_Selected != newSelection)
                            {
                                m_From.SendGump(new SkillsGump(m_From, m_Target, newSelection));
                            }
                            else
                            {
                                m_From.SendGump(new SkillsGump(m_From, m_Target));
                            }
                        }

                        break;
                    }
                case 1:
                    {
                        if (m_Selected != null && index >= 0 && index < m_Selected.Skills.Length)
                        {
                            var sk = m_Target.Skills[m_Selected.Skills[index]];

                            if (sk != null)
                            {
                                if (m_From.AccessLevel >= AccessLevel.GameMaster)
                                {
                                    m_From.SendGump(new EditSkillGump(m_From, m_Target, sk, m_Selected));
                                }
                                else
                                {
                                    m_From.SendMessage("You may not change that.");
                                    m_From.SendGump(new SkillsGump(m_From, m_Target, m_Selected));
                                }
                            }
                            else
                            {
                                m_From.SendGump(new SkillsGump(m_From, m_Target, m_Selected));
                            }
                        }

                        break;
                    }
                case 2:
                    {
                        if (m_Selected != null && index >= 0 && index < m_Selected.Skills.Length)
                        {
                            var sk = m_Target.Skills[m_Selected.Skills[index]];

                            if (sk != null)
                            {
                                if (m_From.AccessLevel >= AccessLevel.GameMaster)
                                {
                                    switch (sk.Lock)
                                    {
                                        case SkillLock.Up:
                                            sk.SetLockNoRelay(SkillLock.Down);
                                            sk.Update();
                                            break;
                                        case SkillLock.Down:
                                            sk.SetLockNoRelay(SkillLock.Locked);
                                            sk.Update();
                                            break;
                                        case SkillLock.Locked:
                                            sk.SetLockNoRelay(SkillLock.Up);
                                            sk.Update();
                                            break;
                                    }
                                }
                                else
                                {
                                    m_From.SendMessage("You may not change that.");
                                }

                                m_From.SendGump(new SkillsGump(m_From, m_Target, m_Selected));
                            }
                        }

                        break;
                    }
            }
        }

        public int GetButtonID(int type, int index) => 1 + index * 3 + type;
    }

    public class SkillsGumpGroup
    {
        public SkillsGumpGroup(string name, SkillName[] skills)
        {
            Name = name;
            Skills = skills;

            Array.Sort(Skills, new SkillNameComparer());
        }

        public string Name { get; }

        public SkillName[] Skills { get; }

        public static SkillsGumpGroup[] Groups { get; } =
        {
            new(
                "Crafting",
                new[]
                {
                    SkillName.Alchemy,
                    SkillName.Blacksmith,
                    SkillName.Cartography,
                    SkillName.Carpentry,
                    SkillName.Cooking,
                    SkillName.Fletching,
                    SkillName.Inscribe,
                    SkillName.Tailoring,
                    SkillName.Tinkering,
                    SkillName.Imbuing
                }
            ),
            new(
                "Bardic",
                new[]
                {
                    SkillName.Discordance,
                    SkillName.Musicianship,
                    SkillName.Peacemaking,
                    SkillName.Provocation
                }
            ),
            new(
                "Magical",
                new[]
                {
                    SkillName.Chivalry,
                    SkillName.EvalInt,
                    SkillName.Magery,
                    SkillName.MagicResist,
                    SkillName.Meditation,
                    SkillName.Necromancy,
                    SkillName.SpiritSpeak,
                    SkillName.Ninjitsu,
                    SkillName.Bushido,
                    SkillName.Spellweaving,
                    SkillName.Mysticism
                }
            ),
            new(
                "Miscellaneous",
                new[]
                {
                    SkillName.Camping,
                    SkillName.Fishing,
                    SkillName.Focus,
                    SkillName.Healing,
                    SkillName.Herding,
                    SkillName.Lockpicking,
                    SkillName.Lumberjacking,
                    SkillName.Mining,
                    SkillName.Snooping,
                    SkillName.Veterinary
                }
            ),
            new(
                "Combat Ratings",
                new[]
                {
                    SkillName.Archery,
                    SkillName.Fencing,
                    SkillName.Macing,
                    SkillName.Parry,
                    SkillName.Swords,
                    SkillName.Tactics,
                    SkillName.Wrestling,
                    SkillName.Throwing
                }
            ),
            new(
                "Actions",
                new[]
                {
                    SkillName.AnimalTaming,
                    SkillName.Begging,
                    SkillName.DetectHidden,
                    SkillName.Hiding,
                    SkillName.RemoveTrap,
                    SkillName.Poisoning,
                    SkillName.Stealing,
                    SkillName.Stealth,
                    SkillName.Tracking
                }
            ),
            new(
                "Lore & Knowledge",
                new[]
                {
                    SkillName.Anatomy,
                    SkillName.AnimalLore,
                    SkillName.ArmsLore,
                    SkillName.Forensics,
                    SkillName.ItemID,
                    SkillName.TasteID
                }
            )
        };

        private class SkillNameComparer : IComparer<SkillName>
        {
            public int Compare(SkillName a, SkillName b)
            {
                var aName = SkillInfo.Table[(int)a].Name;
                var bName = SkillInfo.Table[(int)b].Name;

                return string.CompareOrdinal(aName, bName);
            }
        }
    }
}
