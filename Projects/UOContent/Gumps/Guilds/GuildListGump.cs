using System.Collections.Generic;
using Server.Guilds;

namespace Server.Gumps
{
    public abstract class GuildListGump : DynamicGump
    {
        protected Guild _guild;
        protected List<Guild> _list;
        protected Mobile _mobile;
        private readonly bool _radio;

        public override bool Singleton => true;

        protected GuildListGump(Mobile from, Guild guild, bool radio, List<Guild> list) : base(20, 30)
        {
            _mobile = from;
            _guild = guild;
            _radio = radio;
            _list = list;
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.SetNoMove();

            builder.AddPage();
            builder.AddBackground(0, 0, 550, 440, 5054);
            builder.AddBackground(10, 10, 530, 420, 3000);

            BuildHeader(ref builder);

            for (var i = 0; i < _list.Count; ++i)
            {
                if (i % 11 == 0)
                {
                    if (i != 0)
                    {
                        builder.AddButton(300, 370, 4005, 4007, 0, GumpButtonType.Page, i / 11 + 1);
                        builder.AddHtmlLocalized(335, 370, 300, 35, 1011066); // Next page
                    }

                    builder.AddPage(i / 11 + 1);

                    if (i != 0)
                    {
                        builder.AddButton(20, 370, 4014, 4016, 0, GumpButtonType.Page, i / 11);
                        builder.AddHtmlLocalized(55, 370, 300, 35, 1011067); // Previous page
                    }
                }

                if (_radio)
                {
                    builder.AddRadio(20, 35 + i % 11 * 30, 208, 209, false, i);
                }

                var g = _list[i];

                var name = g.Name?.Trim().DefaultIfNullOrEmpty("(empty)");
                builder.AddLabel(_radio ? 55 : 20, 35 + i % 11 * 30, 0, name);
            }
        }

        protected abstract void BuildHeader(ref DynamicGumpBuilder builder);
    }
}
