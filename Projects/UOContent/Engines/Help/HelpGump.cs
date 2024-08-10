using System;
using System.Runtime.CompilerServices;
using Server.Engines.ConPVP;
using Server.Factions;
using Server.Gumps;
using Server.Menus.Questions;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;

namespace Server.Engines.Help;

public class ContainedMenu : QuestionMenu
{
    private static readonly string[] _options = [
        "Leave my old help request like it is.",
        "Remove my help request from the queue."
    ];

    private readonly Mobile _from;

    public ContainedMenu(Mobile from) : base(
        "You already have an open help request. We will have someone assist you as soon as possible.  What would you like to do?",
        _options
    ) => _from = from;

    public override void OnCancel(NetState state)
    {
        _from.SendLocalizedMessage(1005306, "", 0x35); // Help request unchanged.
    }

    public override void OnResponse(NetState state, int index)
    {
        if (index == 0)
        {
            _from.SendLocalizedMessage(1005306, "", 0x35); // Help request unchanged.
        }
        else if (index == 1)
        {
            var entry = PageQueue.GetEntry(_from);

            if (entry != null && entry.Handler == null)
            {
                _from.SendLocalizedMessage(1005307, "", 0x35); // Removed help request.
                // entry.AddResponse(entry.Sender, "[Canceled]");
                PageQueue.Remove(entry);
            }
            else
            {
                _from.SendLocalizedMessage(1005306, "", 0x35); // Help request unchanged.
            }
        }
    }
}

public sealed class HelpGump : DynamicGump
{
    private readonly Mobile _from;

    public override bool Singleton => true;

    public HelpGump(Mobile from) : base(0, 0) => _from = from;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        var isYoung = IsYoung(_from);
        var totalHeight = 110 + (isYoung ? 64 : 0) + 4 * 80;

        builder.AddBackground(50, 25, 540, totalHeight, 2600);

        builder.AddPage();

        builder.AddHtmlLocalized(150, 50, 360, 40, 1001002);     // <CENTER><U>Ultima Online Help Menu</U></CENTER>
        builder.AddButton(425, totalHeight - 15, 2073, 2072, 0); // Close

        builder.AddPage(1);

        var delta = 80;
        int y;
        const int height = 74;

        if (isYoung)
        {
            // <U>Young player haven transport</U>: Select this option if you want to be transported to Haven.
            AddOption(ref builder, 90, 48, 9, 1041525, page: 2);
            y = 90 + 54;
        }
        else
        {
            y = 90;
        }

        /* <U>General question about Ultima Online</U>:
         * Select this option if you are having difficulties learning to use a skill,
         * if you have a general gameplay question, or you would like to search the UO Knowledge Base.
         */
        AddOption(ref builder, y, height, 1, 1001003, page: 2);

        /*
         * <U>My character is physically stuck</U>:
         * This choice only covers cases where your character is physically stuck in a location they cannot move out of.
         */
        AddOption(ref builder, y += delta, height, 2, 1001004);

        /*
         * <U>Another player is harassing me</U>:
         * Another player is harassing me verbally or physically, or is breaking the Terms of Service Agreement.
         * To see what constitutes harassment please visit
         * <A HREF="https://help.ea.com/article/how-do-i-report-someone-for-harassment-in-uo">
         * - How do I report someone for Harassment in UO? -</A>.
         */
        AddOption(ref builder, y += delta, height, 0, 1001005, GumpButtonType.Page, 3);

        /*
         * <U>Other</U>: If you are experiencing a problem in the game that does not fall into one of the other categories
         * or is not addressed on the Support web page
         * (located at <A HREF="https://help.ea.com/en/ultima-online/">https://help.ea.com/en/ultima-online/</A>) and
         * requires in-game assistance please use this option.
         */
        AddOption(ref builder, y + delta, height, 0, 1001006, GumpButtonType.Page, 2);

        builder.AddPage(2);

        y = 90;

        /*
         * <U>Report a bug</U>:
         * Use this option to launch your web browser submit a bug report.
         * Your report will be read by our Quality Assurance staff
         * We apologize for not being able to reply to individual bug reports.
         */
        AddOption(ref builder, y, 3, 1001009);

        /*
         * <U>Suggestion for the Game</U>:
         * If you'd like to make a suggestion for the game it should be directed to the Development Team Members who
         * participate in the discussion forums.
         * Choosing this option will take you to the discussion forums.
         */
        AddOption(ref builder, y += delta, 4, 1074795);

        /*
         * <U>Visit the Ultima Online Knowledge Base</U>:
         * You can find detailed answers to many of the most frequently asked questions in our Knowledge Base.
         * This selection will launch your web browser and take you to those answers.
         */
        AddOption(ref builder, y += delta, 5, 1074796);

        /*
         * <U>Other</U>: If you are experiencing a problem in the game that does not fall into one of the other categories
         * or is not addressed on the Support web page
         * (located at <A HREF="https://help.ea.com/en/ultima-online/">https://help.ea.com/en/ultima-online/</A>) and
         * requires in-game assistance please use this option.
         */
        AddOption(ref builder, y + delta, 6, 1001006);

        builder.AddPage(3);

        y = 90;
        delta = 150;

        /* <U><CENTER>Another player is harassing me (or Exploiting).</CENTER></U><BR>
         * VERBAL HARASSMENT<BR>
         * Use this option when another player is verbally harassing your character.
         * Verbal harassment behaviors include but are not limited to, using bad language, threats etc..
         * Before you submit a complaint be sure you understand what constitutes harassment
         * <A HREF="http://uo.custhelp.com/cgi-bin/uo.cfg/php/enduser/std_adp.php?p_faqid=40">- what is verbal harassment? -</A>
         * and that you have followed these steps:<BR>
         * 1. You have asked the player to stop and they have continued.<BR>
         * 2. You have tried to remove yourself from the situation.<BR>
         * 3. You have done nothing to instigate or further encourage the harassment.<BR>
         * 4. You have added the player to your ignore list.
         * <A HREF="http://uo.custhelp.com/cgi-bin/uo.cfg/php/enduser/std_adp.php?p_faqid=138">- How do I ignore a player?</A><BR>
         * 5. You have read and understand Origin's definition of harassment.<BR>
         * 6. Your account information is up to date. (Including a current email address)<BR>
         * *If these steps have not been taken, GMs may be unable to take action against the offending player.<BR>
         * **A chat log will be review by a GM to assess the validity of this complaint.
         * Abuse of this system is a violation of the Rules of Conduct.<BR>
         * EXPLOITING<BR>
         * Use this option to report someone who may be exploiting or cheating.
         * <A HREF="http://uo.custhelp.com/cgi-bin/uo.cfg/php/enduser/std_adp.php?p_faqid=41">- What constitutes an exploit?</a>
         */
        AddOption(ref builder, y, 145, 7, 1062572);

        /* <U><CENTER>Another player is harassing me using game mechanics.</CENTER></U><BR>
         * <BR>
         * PHYSICAL HARASSMENT<BR>
         * Use this option when another player is harassing your character using game mechanics.
         * Physical harassment includes but is not limited to luring, Kill Stealing, and any act that causes a players death in Trammel.
         * Before you submit a complaint be sure you understand what constitutes harassment
         * <A HREF="http://uo.custhelp.com/cgi-bin/uo.cfg/php/enduser/std_adp.php?p_faqid=59"> - what is physical harassment?</A>
         * and that you have followed these steps:<BR>
         * 1. You have asked the player to stop and they have continued.<BR>
         * 2. You have tried to remove yourself from the situation.<BR>
         * 3. You have done nothing to instigate or further encourage the harassment.<BR>
         * 4. You have added the player to your ignore list.
         * <A HREF="http://uo.custhelp.com/cgi-bin/uo.cfg/php/enduser/std_adp.php?p_faqid=138"> - how do I ignore a player?</A><BR>
         * 5. You have read and understand Origin's definition of harassment.<BR>
         * 6. Your account information is up to date. (Including a current email address)<BR>
         * *If these steps have not been taken, GMs may be unable to take action against the offending player.<BR>
         * **This issue will be reviewed by a GM to assess the validity of this complaint.
         * Abuse of this system is a violation of the Rules of Conduct.
         */
        AddOption(ref builder, y += delta, 145, 8, 1062573);

        builder.AddButton(150, y + 150, 5540, 5541, 0, GumpButtonType.Page, 1);
        builder.AddHtmlLocalized(180, y + 150, 335, 40, 1001015); // NO  - I meant to ask for help with another matter.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddOption(
        ref DynamicGumpBuilder builder, int y, int buttonId, int localizedName, GumpButtonType type = GumpButtonType.Reply,
        int page = 0
    ) => AddOption(ref builder, y, 74, buttonId, localizedName, type, page);

    private static void AddOption(
        ref DynamicGumpBuilder builder, int y, int height, int buttonId, int localizedName,
        GumpButtonType type = GumpButtonType.Reply, int page = 0
    )
    {
        builder.AddButton(80, y, 5540, 5541, buttonId, type, page);
        builder.AddHtmlLocalized(
            110,
            y,
            450,
            height,
            localizedName,
            true,
            true
        );
    }

    public static void HelpRequest(Mobile m)
    {
        if (m.HasGump<HelpGump>())
        {
            return;
        }

        if (!PageQueue.CheckAllowedToPage(m))
        {
            return;
        }

        if (PageQueue.Contains(m))
        {
            m.SendMenu(new ContainedMenu(m));
        }
        else
        {
            m.SendGump(new HelpGump(m));
        }
    }

    private static bool IsYoung(Mobile m) => m is PlayerMobile mobile && mobile.Young;

    public static bool CheckCombat(Mobile m)
    {
        for (var i = 0; i < m.Aggressed.Count; ++i)
        {
            var info = m.Aggressed[i];

            if (Core.Now - info.LastCombatTime < TimeSpan.FromSeconds(30.0))
            {
                return true;
            }
        }

        return false;
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        var type = (PageType)(-1);

        switch (info.ButtonID)
        {
            case 0: // Close/Cancel
                {
                    from.SendLocalizedMessage(501235, "", 0x35); // Help request aborted.

                    break;
                }
            case 1: // General question
                {
                    type = PageType.Question;
                    break;
                }
            case 2: // Stuck
                {
                    var house = BaseHouse.FindHouseAt(from);

                    if (house?.IsAosRules == true && !from.Region.IsPartOf<SafeZone>()) // Dueling
                    {
                        from.Location = house.BanLocation;
                    }
                    else if (from.Region.IsPartOf<JailRegion>())
                    {
                        from.SendLocalizedMessage(1114345, "", 0x35); // You'll need a better jailbreak plan than that!
                    }
                    else if (Sigil.ExistsOn(from))
                    {
                        from.SendLocalizedMessage(1061632); // You can't do that while carrying the sigil.
                    }
                    else if (from is PlayerMobile mobile && mobile.CanUseStuckMenu() &&
                             mobile.Region.CanUseStuckMenu(mobile) && !CheckCombat(mobile) && !mobile.Frozen &&
                             !mobile.Criminal && (Core.AOS || mobile.Kills < 5))
                    {
                        var menu = new StuckMenu(mobile, mobile, true);

                        menu.BeginClose();

                        mobile.SendGump(menu);
                    }
                    else
                    {
                        type = PageType.Stuck;
                    }

                    break;
                }
            case 3: // Report bug
                {
                    type = PageType.Bug;
                    break;
                }
            case 4: // Game suggestion
                {
                    type = PageType.Suggestion;
                    break;
                }
            case 5: // Account management
                {
                    type = PageType.Account;
                    break;
                }
            case 6: // Other
                {
                    type = PageType.Other;
                    break;
                }
            case 7: // Harassment: verbal/exploit
                {
                    type = PageType.VerbalHarassment;
                    break;
                }
            case 8: // Harassment: physical
                {
                    type = PageType.PhysicalHarassment;
                    break;
                }
            case 9: // Young player transport
                {
                    if (IsYoung(from))
                    {
                        if (from.Region.IsPartOf<JailRegion>())
                        {
                            // You'll need a better jailbreak plan than that!
                            from.SendLocalizedMessage(1114345, "", 0x35);
                        }
                        else if (from.Region.IsPartOf("Haven Island"))
                        {
                            from.SendLocalizedMessage(1041529); // You're already in Haven
                        }
                        else
                        {
                            from.MoveToWorld(new Point3D(3503, 2574, 14), Map.Trammel);
                        }
                    }

                    break;
                }
        }

        if (type != (PageType)(-1) && PageQueue.CheckAllowedToPage(from))
        {
            from.SendGump(new PagePromptGump(from, type));
        }
    }
}
