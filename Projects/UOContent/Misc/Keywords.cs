using Server.Guilds;
using Server.Gumps;
using Server.Mobiles;

namespace Server.Misc
{
    public static class Keywords
    {
        public static void Initialize()
        {
            // Register our speech handler
            EventSink.Speech += EventSink_Speech;
        }

        public static void EventSink_Speech(SpeechEventArgs args)
        {
            var from = args.Mobile;
            var keywords = args.Keywords;

            for (var i = 0; i < keywords.Length; ++i)
            {
                switch (keywords[i])
                {
                    case 0x002A: // *i resign from my guild*
                        {
                            ((Guild)from.Guild)?.RemoveMember(from);

                            break;
                        }
                    case 0x0032: // *i must consider my sins*
                        {
                            if (!Core.SE)
                            {
                                from.SendMessage("Short Term Murders : {0}", from.ShortTermMurders);
                                from.SendMessage("Long Term Murders : {0}", from.Kills);
                            }
                            else
                            {
                                from.SendMessage(
                                    0x3B2,
                                    "Short Term Murders: {0} Long Term Murders: {1}",
                                    from.ShortTermMurders,
                                    from.Kills
                                );
                            }

                            break;
                        }
                    case 0x0035: // i renounce my young player status*
                        {
                            if (from is PlayerMobile mobile && mobile.Young && !mobile.HasGump<RenounceYoungGump>())
                            {
                                mobile.SendGump(new RenounceYoungGump());
                            }

                            break;
                        }
                }
            }
        }
    }
}
