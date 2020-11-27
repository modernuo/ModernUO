using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    public class CustomHueGroup
    {
        public CustomHueGroup(int name, int[] hues)
        {
            Name = name;
            Hues = hues;
        }

        public CustomHueGroup(string name, int[] hues)
        {
            NameString = name;
            Hues = hues;
        }

        public int Name { get; }

        public string NameString { get; }

        public int[] Hues { get; }
    }

    public class CustomHuePicker
    {
        public static readonly CustomHuePicker SpecialDyeTub = new(
            new[]
            {
                /* Violet */
                new CustomHueGroup(1018345, new[] { 1230, 1231, 1232, 1233, 1234, 1235 }),
                /* Tan */
                new CustomHueGroup(1018346, new[] { 1501, 1502, 1503, 1504, 1505, 1506, 1507, 1508 }),
                /* Brown */
                new CustomHueGroup(1018347, new[] { 2012, 2013, 2014, 2015, 2016, 2017 }),
                /* Dark Blue */
                new CustomHueGroup(1018348, new[] { 1303, 1304, 1305, 1306, 1307, 1308 }),
                /* Forest Green */
                new CustomHueGroup(1018349, new[] { 1420, 1421, 1422, 1423, 1424, 1425, 1426 }),
                /* Pink */
                new CustomHueGroup(1018350, new[] { 1619, 1620, 1621, 1622, 1623, 1624, 1625, 1626 }),
                /* Red */
                new CustomHueGroup(1018351, new[] { 1640, 1641, 1642, 1643, 1644 }),
                /* Olive */
                new CustomHueGroup(1018352, new[] { 2001, 2002, 2003, 2004, 2005 })
            },
            false,
            1018344
        );

        public static readonly CustomHuePicker LeatherDyeTub = new(
            new[]
            {
                /* Dull Copper */
                new CustomHueGroup(1018332, new[] { 2419, 2420, 2421, 2422, 2423, 2424 }),
                /* Shadow Iron */
                new CustomHueGroup(1018333, new[] { 2406, 2407, 2408, 2409, 2410, 2411, 2412 }),
                /* Copper */
                new CustomHueGroup(1018334, new[] { 2413, 2414, 2415, 2416, 2417, 2418 }),
                /* Bronze */
                new CustomHueGroup(1018335, new[] { 2414, 2415, 2416, 2417, 2418 }),
                /* Glden */
                new CustomHueGroup(1018336, new[] { 2213, 2214, 2215, 2216, 2217, 2218 }),
                /* Agapite */
                new CustomHueGroup(1018337, new[] { 2425, 2426, 2427, 2428, 2429, 2430 }),
                /* Verite */
                new CustomHueGroup(1018338, new[] { 2207, 2208, 2209, 2210, 2211, 2212 }),
                /* Valorite */
                new CustomHueGroup(1018339, new[] { 2219, 2220, 2221, 2222, 2223, 2224 }),
                /* Reds */
                new CustomHueGroup(1018340, new[] { 2113, 2114, 2115, 2116, 2117, 2118 }),
                /* Blues */
                new CustomHueGroup(1018341, new[] { 2119, 2120, 2121, 2122, 2123, 2124 }),
                /* Greens */
                new CustomHueGroup(1018342, new[] { 2126, 2127, 2128, 2129, 2130 }),
                /* Yellows */
                new CustomHueGroup(1018343, new[] { 2213, 2214, 2215, 2216, 2217, 2218 })
            },
            true
        );

        public CustomHuePicker(CustomHueGroup[] groups, bool defaultSupported)
        {
            Groups = groups;
            DefaultSupported = defaultSupported;
        }

        public CustomHuePicker(CustomHueGroup[] groups, bool defaultSupported, int title)
        {
            Groups = groups;
            DefaultSupported = defaultSupported;
            Title = title;
        }

        public CustomHuePicker(CustomHueGroup[] groups, bool defaultSupported, string title)
        {
            Groups = groups;
            DefaultSupported = defaultSupported;
            TitleString = title;
        }

        public bool DefaultSupported { get; }

        public CustomHueGroup[] Groups { get; }

        public int Title { get; }

        public string TitleString { get; }
    }

    public delegate void CustomHuePickerCallback<in T>(Mobile from, T state, int hue);

    public class CustomHuePickerGump<T> : Gump
    {
        private readonly CustomHuePickerCallback<T> m_Callback;
        private readonly CustomHuePicker m_Definition;
        private readonly Mobile m_From;
        private readonly T m_State;

        public CustomHuePickerGump(
            Mobile from, CustomHuePicker definition, CustomHuePickerCallback<T> callback, T state
        ) : base(50, 50)
        {
            m_From = from;
            m_Definition = definition;
            m_Callback = callback;
            m_State = state;

            RenderBackground();
            RenderCategories();
        }

        private int GetRadioID(int group, int index) => index * m_Definition.Groups.Length + group;

        private void RenderBackground()
        {
            AddPage(0);

            AddBackground(0, 0, 450, 450, 5054);
            AddBackground(10, 10, 430, 430, 3000);

            if (m_Definition.TitleString != null)
            {
                AddHtml(20, 30, 400, 25, m_Definition.TitleString);
            }
            else if (m_Definition.Title > 0)
            {
                AddHtmlLocalized(20, 30, 400, 25, m_Definition.Title);
            }

            AddButton(20, 400, 4005, 4007, 1);
            AddHtmlLocalized(55, 400, 200, 25, 1011036); // OKAY

            if (m_Definition.DefaultSupported)
            {
                AddButton(200, 400, 4005, 4007, 2);
                AddLabel(235, 400, 0, "DEFAULT");
            }
        }

        private void RenderCategories()
        {
            var groups = m_Definition.Groups;

            for (var i = 0; i < groups.Length; ++i)
            {
                AddButton(30, 85 + i * 25, 5224, 5224, 0, GumpButtonType.Page, 1 + i);

                if (groups[i].NameString != null)
                {
                    AddHtml(55, 85 + i * 25, 200, 25, groups[i].NameString);
                }
                else
                {
                    AddHtmlLocalized(55, 85 + i * 25, 200, 25, groups[i].Name);
                }
            }

            for (var i = 0; i < groups.Length; ++i)
            {
                AddPage(1 + i);

                var hues = groups[i].Hues;

                for (var j = 0; j < hues.Length; ++j)
                {
                    AddRadio(260, 90 + j * 25, 210, 211, false, GetRadioID(i, j));
                    AddLabel(278, 90 + j * 25, hues[j] - 1, "*****");
                }
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            switch (info.ButtonID)
            {
                case 1: // Okay
                    {
                        var switches = info.Switches;

                        if (switches.Length > 0)
                        {
                            var index = switches[0];

                            var group = index % m_Definition.Groups.Length;
                            index /= m_Definition.Groups.Length;

                            if (group >= 0 && group < m_Definition.Groups.Length)
                            {
                                var hues = m_Definition.Groups[group].Hues;

                                if (index >= 0 && index < hues.Length)
                                {
                                    m_Callback(m_From, m_State, hues[index]);
                                }
                            }
                        }

                        break;
                    }
                case 2: // Default
                    {
                        if (m_Definition.DefaultSupported)
                        {
                            m_Callback(m_From, m_State, 0);
                        }

                        break;
                    }
            }
        }
    }
}
