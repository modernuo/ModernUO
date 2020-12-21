using System.Collections.Generic;
using Server.Guilds;

namespace Server.Gumps
{
    public abstract class GuildListGump : Gump
    {
        protected Guild m_Guild;
        protected List<Guild> m_List;
        protected Mobile m_Mobile;

        public GuildListGump(Mobile from, Guild guild, bool radio, List<Guild> list) : base(20, 30)
        {
            m_Mobile = from;
            m_Guild = guild;

            Draggable = false;

            AddPage(0);
            AddBackground(0, 0, 550, 440, 5054);
            AddBackground(10, 10, 530, 420, 3000);

            Design();

            m_List = list;

            for (var i = 0; i < m_List.Count; ++i)
            {
                if (i % 11 == 0)
                {
                    if (i != 0)
                    {
                        AddButton(300, 370, 4005, 4007, 0, GumpButtonType.Page, i / 11 + 1);
                        AddHtmlLocalized(335, 370, 300, 35, 1011066); // Next page
                    }

                    AddPage(i / 11 + 1);

                    if (i != 0)
                    {
                        AddButton(20, 370, 4014, 4016, 0, GumpButtonType.Page, i / 11);
                        AddHtmlLocalized(55, 370, 300, 35, 1011067); // Previous page
                    }
                }

                if (radio)
                {
                    AddRadio(20, 35 + i % 11 * 30, 208, 209, false, i);
                }

                var g = m_List[i];

                string name;

                if ((name = g.Name) != null && (name = name.Trim()).Length <= 0)
                {
                    name = "(empty)";
                }

                AddLabel(radio ? 55 : 20, 35 + i % 11 * 30, 0, name);
            }
        }

        protected virtual void Design()
        {
        }
    }
}
