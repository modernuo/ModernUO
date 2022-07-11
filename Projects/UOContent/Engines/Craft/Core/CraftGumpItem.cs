using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Craft
{
    public class CraftGumpItem : Gump
    {
        private const int LabelHue = 0x480; // 0x384
        private const int RedLabelHue = 0x20;

        private const int LabelColor = 0x7FFF;
        private const int RedLabelColor = 0x6400;

        private const int GreyLabelColor = 0x3DEF;

        private static readonly Type typeofBlankScroll = typeof(BlankScroll);
        private static readonly Type typeofSpellScroll = typeof(SpellScroll);
        private readonly CraftItem m_CraftItem;
        private readonly CraftSystem m_CraftSystem;
        private readonly Mobile m_From;
        private readonly BaseTool m_Tool;

        private int m_OtherCount;

        private bool m_ShowExceptionalChance;

        public CraftGumpItem(Mobile from, CraftSystem craftSystem, CraftItem craftItem, BaseTool tool) : base(40, 40)
        {
            m_From = from;
            m_CraftSystem = craftSystem;
            m_CraftItem = craftItem;
            m_Tool = tool;

            from.CloseGump<CraftGump>();
            from.CloseGump<CraftGumpItem>();

            AddPage(0);
            AddBackground(0, 0, 530, 417, 5054);
            AddImageTiled(10, 10, 510, 22, 2624);
            AddImageTiled(10, 37, 150, 148, 2624);
            AddImageTiled(165, 37, 355, 90, 2624);
            AddImageTiled(10, 190, 155, 22, 2624);
            AddImageTiled(10, 217, 150, 53, 2624);
            AddImageTiled(165, 132, 355, 80, 2624);
            AddImageTiled(10, 275, 155, 22, 2624);
            AddImageTiled(10, 302, 150, 53, 2624);
            AddImageTiled(165, 217, 355, 80, 2624);
            AddImageTiled(10, 360, 155, 22, 2624);
            AddImageTiled(165, 302, 355, 80, 2624);
            AddImageTiled(10, 387, 510, 22, 2624);
            AddAlphaRegion(10, 10, 510, 399);

            AddHtmlLocalized(170, 40, 150, 20, 1044053, LabelColor); // ITEM
            AddHtmlLocalized(10, 192, 150, 22, 1044054, LabelColor); // <CENTER>SKILLS</CENTER>
            AddHtmlLocalized(10, 277, 150, 22, 1044055, LabelColor); // <CENTER>MATERIALS</CENTER>
            AddHtmlLocalized(10, 362, 150, 22, 1044056, LabelColor); // <CENTER>OTHER</CENTER>

            if (craftSystem.GumpTitle.Number > 0)
            {
                AddHtmlLocalized(10, 12, 510, 20, craftSystem.GumpTitle.Number, LabelColor);
            }
            else
            {
                AddHtml(10, 12, 510, 20, craftSystem.GumpTitle.String);
            }

            AddButton(15, 387, 4014, 4016, 0);
            AddHtmlLocalized(50, 390, 150, 18, 1044150, LabelColor); // BACK

            var needsRecipe = craftItem.Recipe != null && from is PlayerMobile mobile &&
                              !mobile.HasRecipe(craftItem.Recipe);

            if (needsRecipe)
            {
                AddButton(270, 387, 4005, 4007, 0, GumpButtonType.Page);
                AddHtmlLocalized(305, 390, 150, 18, 1044151, GreyLabelColor); // MAKE NOW
            }
            else
            {
                AddButton(270, 387, 4005, 4007, 1);
                AddHtmlLocalized(305, 390, 150, 18, 1044151, LabelColor); // MAKE NOW
            }

            if (craftItem.NameNumber > 0)
            {
                AddHtmlLocalized(330, 40, 180, 18, craftItem.NameNumber, LabelColor);
            }
            else
            {
                AddLabel(330, 40, LabelHue, craftItem.NameString);
            }

            if (craftItem.UseAllRes)
            {
                AddHtmlLocalized(
                    170,
                    302 + m_OtherCount++ * 20,
                    310,
                    18,
                    1048176, // Makes as many as possible at once
                    LabelColor
                );
            }

            DrawItem();
            DrawSkill();
            DrawResource();

            if (craftItem.RequiredExpansion != Expansion.None)
            {
                var supportsEx = from.NetState?.SupportsExpansion(craftItem.RequiredExpansion) == true;
                TextDefinition.AddHtmlText(
                    this,
                    170,
                    302 + m_OtherCount++ * 20,
                    310,
                    18,
                    RequiredExpansionMessage(craftItem.RequiredExpansion),
                    false,
                    false,
                    supportsEx ? LabelColor : RedLabelColor,
                    supportsEx ? LabelHue : RedLabelHue
                );
            }

            if (needsRecipe)
            {
                AddHtmlLocalized(
                    170,
                    302 + m_OtherCount++ * 20,
                    310,
                    18,
                    1073620, // You have not learned this recipe.
                    RedLabelColor
                );
            }
        }

        private static TextDefinition RequiredExpansionMessage(Expansion expansion)
        {
            return expansion switch
            {
                Expansion.SE => 1063363, // * Requires the "Samurai Empire" expansion
                Expansion.ML => 1072651, // * Requires the "Mondain's Legacy" expansion
                _            => $"* Requires the \"{ExpansionInfo.GetInfo(expansion).Name}\" expansion"
            };
        }

        public void DrawItem()
        {
            var type = m_CraftItem.ItemType;

            AddItem(20, 50, m_CraftItem.NameNumber, m_CraftItem.ItemHue);

            if (m_CraftItem.IsMarkable(type))
            {
                AddHtmlLocalized(
                    170,
                    302 + m_OtherCount++ * 20,
                    310,
                    18,
                    1044059, // This item may hold its maker's mark
                    LabelColor
                );
                m_ShowExceptionalChance = true;
            }
        }

        public void DrawSkill()
        {
            for (var i = 0; i < m_CraftItem.Skills.Count; i++)
            {
                var skill = m_CraftItem.Skills[i];
                var minSkill = Math.Max(skill.MinSkill, 0);

                AddHtmlLocalized(170, 132 + i * 20, 200, 18, AosSkillBonuses.GetLabel(skill.SkillToMake), LabelColor);
                AddLabel(430, 132 + i * 20, LabelHue, $"{minSkill:F1}");
            }

            var res = m_CraftItem.UseSubRes2 ? m_CraftSystem.CraftSubRes2 : m_CraftSystem.CraftSubRes;
            var resIndex = -1;

            var context = m_CraftSystem.GetContext(m_From);

            if (context != null)
            {
                resIndex = m_CraftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            }

            var chance = m_CraftItem.GetSuccessChance(
                m_From,
                resIndex > -1 ? res.GetAt(resIndex).ItemType : null,
                m_CraftSystem,
                false,
                out _
            );

            AddHtmlLocalized(170, 80, 250, 18, 1044057, LabelColor); // Success Chance:
            AddLabel(430, 80, LabelHue, $"{Math.Clamp(chance, 0, 1) * 100:F1}%");

            if (m_ShowExceptionalChance)
            {
                var exceptChance = Math.Clamp(m_CraftItem.GetExceptionalChance(m_CraftSystem, chance, m_From), 0, 1.0);

                AddHtmlLocalized(170, 100, 250, 18, 1044058, 32767); // Exceptional Chance:
                AddLabel(430, 100, LabelHue, $"{exceptChance * 100:F1}%");
            }
        }

        public void DrawResource()
        {
            var retainedColor = false;

            var context = m_CraftSystem.GetContext(m_From);

            var res = m_CraftItem.UseSubRes2 ? m_CraftSystem.CraftSubRes2 : m_CraftSystem.CraftSubRes;
            var resIndex = -1;

            if (context != null)
            {
                resIndex = m_CraftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;
            }

            var cropScroll = m_CraftItem.Resources.Count > 1
                             && m_CraftItem.Resources[^1].ItemType == typeofBlankScroll
                             && typeofSpellScroll.IsAssignableFrom(m_CraftItem.ItemType);

            for (var i = 0; i < m_CraftItem.Resources.Count - (cropScroll ? 1 : 0) && i < 4; i++)
            {
                var craftResource = m_CraftItem.Resources[i];

                var type = craftResource.ItemType;
                var nameString = craftResource.Name.String;
                var nameNumber = craftResource.Name.Number;

                // Resource Mutation
                if (type == res.ResType && resIndex > -1)
                {
                    var subResource = res.GetAt(resIndex);

                    type = subResource.ItemType;

                    nameString = subResource.Name.String;
                    nameNumber = subResource.GenericNameNumber;

                    if (nameNumber <= 0)
                    {
                        nameNumber = subResource.Name.Number;
                    }
                }
                // ******************

                if (!retainedColor && m_CraftItem.RetainsColorFrom(m_CraftSystem, type))
                {
                    retainedColor = true;
                    AddHtmlLocalized(
                        170,
                        302 + m_OtherCount++ * 20,
                        310,
                        18,
                        1044152, // * The item retains the color of this material
                        LabelColor
                    );
                    AddLabel(500, 219 + i * 20, LabelHue, "*");
                }

                if (nameNumber > 0)
                {
                    AddHtmlLocalized(170, 219 + i * 20, 310, 18, nameNumber, LabelColor);
                }
                else
                {
                    AddLabel(170, 219 + i * 20, LabelHue, nameString);
                }

                AddLabel(430, 219 + i * 20, LabelHue, craftResource.Amount.ToString());
            }

            if (m_CraftItem.NameNumber == 1041267) // runebook
            {
                AddHtmlLocalized(170, 219 + m_CraftItem.Resources.Count * 20, 310, 18, 1044447, LabelColor);
                AddLabel(430, 219 + m_CraftItem.Resources.Count * 20, LabelHue, "1");
            }

            if (cropScroll)
            {
                AddHtmlLocalized(
                    170,
                    302 + m_OtherCount++ * 20,
                    360,
                    18,
                    1044379, // Inscribing scrolls also requires a blank scroll and mana.
                    LabelColor
                );
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            // Back Button
            if (info.ButtonID == 0)
            {
                var craftGump = new CraftGump(m_From, m_CraftSystem, m_Tool, null);
                m_From.SendGump(craftGump);
            }
            else // Make Button
            {
                var num = m_CraftSystem.CanCraft(m_From, m_Tool, m_CraftItem.ItemType);

                if (num > 0)
                {
                    m_From.SendGump(new CraftGump(m_From, m_CraftSystem, m_Tool, num));
                }
                else
                {
                    Type type = null;

                    var context = m_CraftSystem.GetContext(m_From);

                    if (context != null)
                    {
                        var res = m_CraftItem.UseSubRes2 ? m_CraftSystem.CraftSubRes2 : m_CraftSystem.CraftSubRes;
                        var resIndex = m_CraftItem.UseSubRes2 ? context.LastResourceIndex2 : context.LastResourceIndex;

                        if (resIndex > -1)
                        {
                            type = res.GetAt(resIndex).ItemType;
                        }
                    }

                    m_CraftSystem.CreateItem(m_From, m_CraftItem.ItemType, type, m_Tool, m_CraftItem);
                }
            }
        }
    }
}
