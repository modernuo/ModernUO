using System;
using System.Text;
using Microsoft.Toolkit.HighPerformance;
using Server.Commands;
using Server.Gumps;
using Server.Network;

namespace Server.Misc
{
    public static class TestCenter
    {
        public static bool Enabled { get; private set; }

        public static void Configure()
        {
            Enabled = ServerConfiguration.GetOrUpdateSetting("testCenter.enable", false);
        }

        public static void Initialize()
        {
            // Register our speech handler
            if (Enabled)
            {
                EventSink.Speech += EventSink_Speech;
            }
        }

        private static void EventSink_Speech(SpeechEventArgs args)
        {
            if (args.Handled)
            {
                return;
            }

            if (args.Speech.InsensitiveStartsWith("set"))
            {
                var from = args.Mobile;

                var tokenizer = args.Speech.Tokenize(' ');
                if (!tokenizer.MoveNext())
                {
                    return;
                }

                var name = tokenizer.MoveNext() ? tokenizer.Current : null;
                var valueStr = tokenizer.MoveNext() ? tokenizer.Current : null;
                if (valueStr == null)
                {
                    return;
                }

                var value = double.Parse(valueStr);

                try
                {
                    if (name.InsensitiveEquals("str"))
                    {
                        ChangeStrength(from, (int)value);
                    }
                    else if (name.InsensitiveEquals("dex"))
                    {
                        ChangeDexterity(from, (int)value);
                    }
                    else if (name.InsensitiveEquals("int"))
                    {
                        ChangeIntelligence(from, (int)value);
                    }
                    else
                    {
                        ChangeSkill(from, name.ToString(), value);
                    }
                }
                catch
                {
                    // ignored
                }
            }
            else if (args.Speech.InsensitiveEquals("help"))
            {
                args.Mobile.SendGump(new TCHelpGump());
                args.Handled = true;
            }
        }

        private static void ChangeStrength(Mobile from, int value)
        {
            if (value < 10 || value > 125)
            {
                from.SendLocalizedMessage(1005628); // Stats range between 10 and 125.
            }
            else
            {
                if (value + from.RawDex + from.RawInt > from.StatCap)
                {
                    from.SendLocalizedMessage(
                        1005629
                    ); // You can not exceed the stat cap.  Try setting another stat lower first.
                }
                else
                {
                    from.RawStr = value;
                    from.SendLocalizedMessage(1005630); // Your stats have been adjusted.
                }
            }
        }

        private static void ChangeDexterity(Mobile from, int value)
        {
            if (value < 10 || value > 125)
            {
                from.SendLocalizedMessage(1005628); // Stats range between 10 and 125.
            }
            else
            {
                if (from.RawStr + value + from.RawInt > from.StatCap)
                {
                    from.SendLocalizedMessage(
                        1005629
                    ); // You can not exceed the stat cap.  Try setting another stat lower first.
                }
                else
                {
                    from.RawDex = value;
                    from.SendLocalizedMessage(1005630); // Your stats have been adjusted.
                }
            }
        }

        private static void ChangeIntelligence(Mobile from, int value)
        {
            if (value < 10 || value > 125)
            {
                from.SendLocalizedMessage(1005628); // Stats range between 10 and 125.
            }
            else
            {
                if (from.RawStr + from.RawDex + value > from.StatCap)
                {
                    from.SendLocalizedMessage(
                        1005629
                    ); // You can not exceed the stat cap.  Try setting another stat lower first.
                }
                else
                {
                    from.RawInt = value;
                    from.SendLocalizedMessage(1005630); // Your stats have been adjusted.
                }
            }
        }

        private static void ChangeSkill(Mobile from, string name, double value)
        {
            if (!Enum.TryParse(name, true, out SkillName index) || !Core.SE && (int)index > 51 ||
                !Core.AOS && (int)index > 48)
            {
                from.SendLocalizedMessage(1005631); // You have specified an invalid skill to set.
                return;
            }

            var skill = from.Skills[index];

            if (skill != null)
            {
                if (value < 0 || value > skill.Cap)
                {
                    from.SendMessage($"Your skill in {skill.Info.Name} is capped at {skill.Cap:F1}.");
                }
                else
                {
                    var newFixedPoint = (int)(value * 10.0);
                    var oldFixedPoint = skill.BaseFixedPoint;

                    if (skill.Owner.Total - oldFixedPoint + newFixedPoint > skill.Owner.Cap)
                    {
                        from.SendMessage("You can not exceed the skill cap.  Try setting another skill lower first.");
                    }
                    else
                    {
                        skill.BaseFixedPoint = newFixedPoint;
                    }
                }
            }
            else
            {
                from.SendLocalizedMessage(1005631); // You have specified an invalid skill to set.
            }
        }

        public class TCHelpGump : Gump
        {
            public TCHelpGump() : base(40, 40)
            {
                AddPage(0);
                AddBackground(0, 0, 160, 120, 5054);

                AddButton(10, 10, 0xFB7, 0xFB9, 1);
                AddLabel(45, 10, 0x34, "ModernUO");

                AddButton(10, 35, 0xFB7, 0xFB9, 2);
                AddLabel(45, 35, 0x34, "List of skills");

                AddButton(10, 60, 0xFB7, 0xFB9, 3);
                AddLabel(45, 60, 0x34, "Command list");

                AddButton(10, 85, 0xFB1, 0xFB3, 0);
                AddLabel(45, 85, 0x34, "Close");
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                switch (info.ButtonID)
                {
                    case 1:
                        {
                            sender.LaunchBrowser("https://www.modernuo.com");
                            break;
                        }
                    case 2: // List of skills
                        {
                            var strings = Enum.GetNames(typeof(SkillName));

                            Array.Sort(strings);

                            var sb = new StringBuilder();

                            if (strings.Length > 0)
                            {
                                sb.Append(strings[0]);
                            }

                            for (var i = 1; i < strings.Length; ++i)
                            {
                                var v = strings[i];

                                if (sb.Length + 1 + v.Length >= 256)
                                {
                                    sender.SendMessage(
                                        Serial.MinusOne,
                                        -1,
                                        MessageType.Label,
                                        0x35,
                                        3,
                                        true,
                                        null,
                                        "System",
                                        sb.ToString()
                                    );

                                    sb = new StringBuilder();
                                    sb.Append(v);
                                }
                                else
                                {
                                    sb.Append(' ');
                                    sb.Append(v);
                                }
                            }

                            if (sb.Length > 0)
                            {
                                sender.SendMessage(
                                    Serial.MinusOne,
                                    -1,
                                    MessageType.Label,
                                    0x35,
                                    3,
                                    true,
                                    null,
                                    "System",
                                    sb.ToString()
                                );
                            }

                            break;
                        }
                    case 3: // Command list
                        {
                            sender.Mobile.SendAsciiMessage(0x482, "The command prefix is \"{0}\"", CommandSystem.Prefix);
                            CommandHandlers.Help_OnCommand(new CommandEventArgs(sender.Mobile, "help", "", Array.Empty<string>()));

                            break;
                        }
                }
            }
        }
    }
}
