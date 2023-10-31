using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Server.Accounting;
using Server.Collections;
using Server.Commands;
using Server.Maps;
using Server.Misc;
using Server.Multis;
using Server.Network;
using Server.Prompts;
using Server.Text;

namespace Server.Gumps
{
    public enum AdminGumpPage
    {
        Information_General,
        Information_Perf,
        Administer,
        Clients,
        Accounts,
        Accounts_Shared,
        Firewall,
        Administer_WorldBuilding,
        Administer_Server,
        Administer_Access,
        Administer_Access_Lockdown,
        Administer_Commands,
        ClientInfo,
        AccountDetails,
        AccountDetails_Information,
        AccountDetails_Characters,
        AccountDetails_Access,
        AccountDetails_Access_ClientIPs,
        AccountDetails_Access_Restrictions,
        AccountDetails_Comments,
        AccountDetails_Tags,
        AccountDetails_ChangePassword,
        AccountDetails_ChangeAccess,
        FirewallInfo
    }

    public class AdminGump : Gump
    {
        private const int LabelColor = 0x7FFF;
        private const int SelectedColor = 0x421F;
        private const int DisabledColor = 0x4210;

        private const int LabelColor32 = 0xFFFFFF;
        private const int SelectedColor32 = 0x8080FF;
        private const int DisabledColor32 = 0x808080;

        private const int LabelHue = 0x480;
        private const int GreenHue = 0x40;
        private const int RedHue = 0x20;

        private static readonly string[] m_AccessLevelStrings =
        {
            "Player",
            "Counselor",
            "Game Master",
            "Seer",
            "Administrator",
            "Developer",
            "Owner"
        };

        private readonly Mobile m_From;
        private readonly List<object> m_List;
        private readonly int m_ListPage;
        private readonly AdminGumpPage m_PageType;
        private readonly object m_State;

        public AdminGump(
            Mobile from, AdminGumpPage pageType, int listPage = 0, List<object> list = null, string notice = null,
            object state = null
        ) : base(50, 40)
        {
            from.CloseGump<AdminGump>();

            m_From = from;
            m_PageType = pageType;
            m_ListPage = listPage;
            m_State = state;
            m_List = list;

            FilterAccess(m_List, from);

            AddPage(0);

            AddBackground(0, 0, 420, 440, 5054);

            AddBlackAlpha(10, 10, 170, 100);
            AddBlackAlpha(190, 10, 220, 100);
            AddBlackAlpha(10, 120, 400, 260);
            AddBlackAlpha(10, 390, 400, 40);

            AddPageButton(
                10,
                10,
                GetButtonID(0, 0),
                "INFORMATION",
                AdminGumpPage.Information_General,
                AdminGumpPage.Information_Perf
            );
            AddPageButton(
                10,
                30,
                GetButtonID(0, 1),
                "ADMINISTER",
                AdminGumpPage.Administer,
                AdminGumpPage.Administer_Access,
                AdminGumpPage.Administer_Commands,
                AdminGumpPage.Administer_Server,
                AdminGumpPage.Administer_WorldBuilding,
                AdminGumpPage.Administer_Access_Lockdown
            );
            AddPageButton(10, 50, GetButtonID(0, 2), "CLIENT LIST", AdminGumpPage.Clients, AdminGumpPage.ClientInfo);
            AddPageButton(
                10,
                70,
                GetButtonID(0, 3),
                "ACCOUNT LIST",
                AdminGumpPage.Accounts,
                AdminGumpPage.Accounts_Shared,
                AdminGumpPage.AccountDetails,
                AdminGumpPage.AccountDetails_Information,
                AdminGumpPage.AccountDetails_Characters,
                AdminGumpPage.AccountDetails_Access,
                AdminGumpPage.AccountDetails_Access_ClientIPs,
                AdminGumpPage.AccountDetails_Access_Restrictions,
                AdminGumpPage.AccountDetails_Comments,
                AdminGumpPage.AccountDetails_Tags,
                AdminGumpPage.AccountDetails_ChangeAccess,
                AdminGumpPage.AccountDetails_ChangePassword
            );
            AddPageButton(10, 90, GetButtonID(0, 4), "FIREWALL", AdminGumpPage.Firewall, AdminGumpPage.FirewallInfo);

            if (notice != null)
            {
                AddHtml(12, 392, 396, 36, Color(notice, LabelColor32));
            }

            switch (pageType)
            {
                case AdminGumpPage.Information_General:
                    {
                        var banned = 0;
                        var active = 0;

                        foreach (Account acct in Accounts.GetAccounts())
                        {
                            if (acct.Banned)
                            {
                                ++banned;
                            }
                            else
                            {
                                ++active;
                            }
                        }

                        AddLabel(20, 130, LabelHue, "Active Accounts:");
                        AddLabel(150, 130, LabelHue, active.ToString());

                        AddLabel(20, 150, LabelHue, "Banned Accounts:");
                        AddLabel(150, 150, LabelHue, banned.ToString());

                        AddLabel(20, 170, LabelHue, "Firewalled:");
                        AddLabel(150, 170, LabelHue, Firewall.Set.Count.ToString());

                        AddLabel(20, 190, LabelHue, "Clients:");
                        AddLabel(150, 190, LabelHue, TcpServer.Instances.Count.ToString());

                        AddLabel(20, 210, LabelHue, "Mobiles:");
                        AddLabel(150, 210, LabelHue, World.Mobiles.Count.ToString());

                        AddLabel(20, 230, LabelHue, "Mobile Scripts:");
                        AddLabel(150, 230, LabelHue, Core.ScriptMobiles.ToString());

                        AddLabel(20, 250, LabelHue, "Items:");
                        AddLabel(150, 250, LabelHue, World.Items.Count.ToString());

                        AddLabel(20, 270, LabelHue, "Item Scripts:");
                        AddLabel(150, 270, LabelHue, Core.ScriptItems.ToString());

                        AddLabel(20, 290, LabelHue, "Uptime:");
                        AddLabel(150, 290, LabelHue, FormatTimeSpan(TimeSpan.FromMilliseconds(Core.Uptime)));

                        AddLabel(20, 310, LabelHue, "Memory:");
                        AddLabel(150, 310, LabelHue, FormatByteAmount(GC.GetTotalMemory(false)));

                        AddLabel(20, 330, LabelHue, "Framework:");
                        AddLabel(150, 330, LabelHue, Environment.Version.ToString());

                        AddLabel(20, 350, LabelHue, "Operating System: ");
                        var os = Environment.OSVersion.ToString();

                        os = os.ReplaceOrdinal("Microsoft", "MSFT");
                        os = os.ReplaceOrdinal("Service Pack", "SP");

                        AddLabel(150, 350, LabelHue, os);

                        /*string str;

                        try{ str = FormatTimeSpan( Core.Process.TotalProcessorTime ); }
                        catch{ str = "(unable to retrieve)"; }

                        AddLabel( 20, 330, LabelHue, "Process Time:" );
                        AddLabel( 250, 330, LabelHue, str );*/

                        /*try{ str = Core.Process.PriorityClass.ToString(); }
                        catch{ str = "(unable to retrieve)"; }

                        AddLabel( 20, 350, LabelHue, "Process Priority:" );
                        AddLabel( 250, 350, LabelHue, str );*/

                        AddPageButton(200, 20, GetButtonID(0, 0), "General", AdminGumpPage.Information_General);
                        AddPageButton(200, 40, GetButtonID(0, 5), "Performance", AdminGumpPage.Information_Perf);

                        break;
                    }
                case AdminGumpPage.Information_Perf:
                    {
                        AddLabel(20, 130, LabelHue, "Cycles Per Second:");
                        AddLabel(40, 150, LabelHue, $"Current: {Core.CyclesPerSecond:N2}");
                        AddLabel(40, 170, LabelHue, $"Average: {Core.AverageCPS:N2}");

                        using var sb = ValueStringBuilder.Create();

                        ThreadPool.GetAvailableThreads(out var curUser, out var curIOCP);
                        ThreadPool.GetMaxThreads(out var maxUser, out var maxIOCP);

                        sb.Append("Worker Threads:<br>Capacity: ");
                        sb.Append(maxUser);
                        sb.Append("<br>Available: ");
                        sb.Append(curUser);
                        sb.Append("<br>Usage: ");
                        sb.Append((maxUser - curUser) * 100 / maxUser);
                        sb.Append("%<br><br>IOCP Threads:<br>Capacity: ");
                        sb.Append(maxIOCP);
                        sb.Append("<br>Available: ");
                        sb.Append(curIOCP);
                        sb.Append("<br>Usage: ");
                        sb.Append((maxIOCP - curIOCP) * 100 / maxIOCP);
                        sb.Append('%');

                        AddLabel(20, 200, LabelHue, "Pooling:");
                        AddHtml(20, 220, 380, 150, sb.ToString(), true, true);

                        AddPageButton(200, 20, GetButtonID(0, 0), "General", AdminGumpPage.Information_General);
                        AddPageButton(200, 40, GetButtonID(0, 5), "Performance", AdminGumpPage.Information_Perf);

                        break;
                    }
                case AdminGumpPage.Administer_WorldBuilding:
                    {
                        AddHtml(10, 125, 400, 20, Color(Center("Generating"), LabelColor32));

                        AddButtonLabeled(20, 175, GetButtonID(3, 101), "Teleporters");
                        AddButtonLabeled(220, 175, GetButtonID(3, 102), "Moongates");

                        AddButtonLabeled(20, 200, GetButtonID(3, 103), "Generate Spawns");
                        AddButtonLabeled(220, 200, GetButtonID(3, 106), "Decoration");

                        AddButtonLabeled(20, 225, GetButtonID(3, 104), "Doors");
                        AddButtonLabeled(220, 225, GetButtonID(3, 105), "Signs");

                        goto case AdminGumpPage.Administer;
                    }
                case AdminGumpPage.Administer_Server:
                    {
                        AddHtml(10, 125, 400, 20, Color(Center("Server"), LabelColor32));

                        AddButtonLabeled(20, 150, GetButtonID(3, 200), "Save");

                        /*if (!Core.Service)
                        {*/
                        AddButtonLabeled(20, 180, GetButtonID(3, 201), "Shutdown (With Save)");
                        AddButtonLabeled(20, 200, GetButtonID(3, 202), "Shutdown (Without Save)");

                        AddButtonLabeled(20, 230, GetButtonID(3, 203), "Shutdown & Restart (With Save)");
                        AddButtonLabeled(20, 250, GetButtonID(3, 204), "Shutdown & Restart (Without Save)");
                        /*}
                        else
                        {
                          AddLabel( 20, 215, LabelHue, "Shutdown/Restart not available." );
                        }*/

                        AddHtml(10, 295, 400, 20, Color(Center("Broadcast"), LabelColor32));

                        AddTextField(20, 320, 380, 20, 0);
                        AddButtonLabeled(20, 350, GetButtonID(3, 210), "To Everyone");
                        AddButtonLabeled(220, 350, GetButtonID(3, 211), "To Staff");

                        goto case AdminGumpPage.Administer;
                    }
                case AdminGumpPage.Administer_Access_Lockdown:
                    {
                        AddHtml(10, 125, 400, 20, Color(Center("Server Lockdown"), LabelColor32));

                        AddHtml(
                            20,
                            150,
                            380,
                            80,
                            Color(
                                "When enabled, only clients with an access level equal to or greater than the specified lockdown level may access the server. After setting a lockdown level, use the <em>Purge Invalid Clients</em> button to disconnect those clients without access.",
                                LabelColor32
                            )
                        );

                        var level = AccountHandler.LockdownLevel;
                        var isLockedDown = level > AccessLevel.Player;

                        AddSelectedButton(20, 230, GetButtonID(3, 500), "Not Locked Down", !isLockedDown);
                        AddSelectedButton(
                            20,
                            260,
                            GetButtonID(3, 504),
                            "Administrators",
                            isLockedDown && level <= AccessLevel.Administrator
                        );
                        AddSelectedButton(20, 280, GetButtonID(3, 503), "Seers", isLockedDown && level <= AccessLevel.Seer);
                        AddSelectedButton(
                            20,
                            300,
                            GetButtonID(3, 502),
                            "Game Masters",
                            isLockedDown && level <= AccessLevel.GameMaster
                        );
                        AddSelectedButton(
                            20,
                            320,
                            GetButtonID(3, 501),
                            "Counselors",
                            isLockedDown && level <= AccessLevel.Counselor
                        );

                        AddButtonLabeled(20, 350, GetButtonID(3, 510), "Purge Invalid Clients");

                        goto case AdminGumpPage.Administer;
                    }
                case AdminGumpPage.Administer_Access:
                    {
                        AddHtml(10, 125, 400, 20, Color(Center("Access"), LabelColor32));

                        AddHtml(10, 155, 400, 20, Color(Center("Connectivity"), LabelColor32));

                        AddButtonLabeled(20, 180, GetButtonID(3, 300), "Kick");
                        AddButtonLabeled(220, 180, GetButtonID(3, 301), "Ban");

                        AddButtonLabeled(20, 210, GetButtonID(3, 302), "Firewall");
                        AddButtonLabeled(220, 210, GetButtonID(3, 303), "Lockdown");

                        AddHtml(10, 245, 400, 20, Color(Center("Staff"), LabelColor32));

                        AddButtonLabeled(20, 270, GetButtonID(3, 310), "Make Player");
                        AddButtonLabeled(20, 290, GetButtonID(3, 311), "Make Counselor");
                        AddButtonLabeled(20, 310, GetButtonID(3, 312), "Make Game Master");
                        AddButtonLabeled(20, 330, GetButtonID(3, 313), "Make Seer");

                        if (from.AccessLevel > AccessLevel.Administrator)
                        {
                            AddButtonLabeled(220, 270, GetButtonID(3, 314), "Make Administrator");

                            if (from.AccessLevel > AccessLevel.Developer)
                            {
                                AddButtonLabeled(220, 290, GetButtonID(3, 315), "Make Developer");

                                if (from.AccessLevel >= AccessLevel.Owner)
                                {
                                    AddButtonLabeled(220, 310, GetButtonID(3, 316), "Make Owner");
                                }
                            }
                        }

                        goto case AdminGumpPage.Administer;
                    }
                case AdminGumpPage.Administer_Commands:
                    {
                        AddHtml(10, 125, 400, 20, Color(Center("Commands"), LabelColor32));

                        AddButtonLabeled(20, 150, GetButtonID(3, 400), "Add");
                        AddButtonLabeled(220, 150, GetButtonID(3, 401), "Remove");

                        AddButtonLabeled(20, 170, GetButtonID(3, 402), "Dupe");
                        AddButtonLabeled(220, 170, GetButtonID(3, 403), "Dupe in bag");

                        AddButtonLabeled(20, 200, GetButtonID(3, 404), "Properties");
                        AddButtonLabeled(220, 200, GetButtonID(3, 405), "Skills");

                        AddButtonLabeled(20, 230, GetButtonID(3, 406), "Mortal");
                        AddButtonLabeled(220, 230, GetButtonID(3, 407), "Immortal");

                        AddButtonLabeled(20, 250, GetButtonID(3, 408), "Squelch");
                        AddButtonLabeled(220, 250, GetButtonID(3, 409), "Unsquelch");

                        AddButtonLabeled(20, 270, GetButtonID(3, 410), "Freeze");
                        AddButtonLabeled(220, 270, GetButtonID(3, 411), "Unfreeze");

                        AddButtonLabeled(20, 290, GetButtonID(3, 412), "Hide");
                        AddButtonLabeled(220, 290, GetButtonID(3, 413), "Unhide");

                        AddButtonLabeled(20, 310, GetButtonID(3, 414), "Kill");
                        AddButtonLabeled(220, 310, GetButtonID(3, 415), "Resurrect");

                        AddButtonLabeled(20, 330, GetButtonID(3, 416), "Move");
                        AddButtonLabeled(220, 330, GetButtonID(3, 417), "Wipe");

                        AddButtonLabeled(20, 350, GetButtonID(3, 418), "Teleport");
                        AddButtonLabeled(220, 350, GetButtonID(3, 419), "Teleport (Multiple)");

                        goto case AdminGumpPage.Administer;
                    }
                case AdminGumpPage.Administer:
                    {
                        AddPageButton(200, 20, GetButtonID(3, 0), "World Building", AdminGumpPage.Administer_WorldBuilding);
                        AddPageButton(200, 40, GetButtonID(3, 1), "Server", AdminGumpPage.Administer_Server);
                        AddPageButton(
                            200,
                            60,
                            GetButtonID(3, 2),
                            "Access",
                            AdminGumpPage.Administer_Access,
                            AdminGumpPage.Administer_Access_Lockdown
                        );
                        AddPageButton(200, 80, GetButtonID(3, 3), "Commands", AdminGumpPage.Administer_Commands);

                        break;
                    }
                case AdminGumpPage.Clients:
                    {
                        if (m_List == null)
                        {
                            var states = TcpServer.Instances.ToList();
                            states.Sort(NetStateComparer.Instance);

                            m_List = states.ToList<object>();
                            FilterAccess(m_List, from);
                        }

                        AddClientHeader();

                        AddLabelCropped(12, 120, 81, 20, LabelHue, "Name");
                        AddLabelCropped(95, 120, 81, 20, LabelHue, "Account");
                        AddLabelCropped(178, 120, 81, 20, LabelHue, "Access Level");
                        AddLabelCropped(273, 120, 109, 20, LabelHue, "IP Address");

                        if (listPage > 0)
                        {
                            AddButton(375, 122, 0x15E3, 0x15E7, GetButtonID(1, 0));
                        }
                        else
                        {
                            AddImage(375, 122, 0x25EA);
                        }

                        if ((listPage + 1) * 12 < m_List.Count)
                        {
                            AddButton(392, 122, 0x15E1, 0x15E5, GetButtonID(1, 1));
                        }
                        else
                        {
                            AddImage(392, 122, 0x25E6);
                        }

                        if (m_List.Count == 0)
                        {
                            AddLabel(12, 140, LabelHue, "There are no clients to display.");
                        }

                        for (int i = 0, index = listPage * 12; i < 12 && index >= 0 && index < m_List.Count; ++i, ++index)
                        {
                            if (m_List[index] is not NetState ns)
                            {
                                continue;
                            }

                            var m = ns.Mobile;
                            var a = ns.Account as Account;
                            var offset = 140 + i * 20;

                            if (m == null)
                            {
                                AddLabelCropped(12, offset, 81, 20, LabelHue, "(logging in)");
                            }
                            else
                            {
                                AddLabelCropped(12, offset, 81, 20, GetHueFor(m), m.Name);
                            }

                            AddLabelCropped(95, offset, 81, 20, LabelHue, a == null ? "(no account)" : a.Username);
                            AddLabelCropped(
                                178,
                                offset,
                                81,
                                20,
                                LabelHue,
                                m == null
                                    ? a != null ? FormatAccessLevel(a.AccessLevel) : ""
                                    : FormatAccessLevel(m.AccessLevel)
                            );
                            AddLabelCropped(273, offset, 109, 20, LabelHue, ns.ToString());

                            if (a != null || m != null)
                            {
                                AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(4, index + 2));
                            }
                        }

                        break;
                    }
                case AdminGumpPage.ClientInfo:
                    {
                        if (state is not Mobile m)
                        {
                            break;
                        }

                        AddClientHeader();

                        AddHtml(10, 125, 400, 20, Color(Center("Information"), LabelColor32));

                        var y = 146;

                        AddLabel(20, y, LabelHue, "Name:");
                        AddLabel(200, y, GetHueFor(m), m.Name);
                        y += 20;

                        var a = m.Account as Account;

                        AddLabel(20, y, LabelHue, "Account:");
                        AddLabel(200, y, a?.Banned == true ? RedHue : LabelHue, a == null ? "(no account)" : a.Username);

                        if (m.AccessLevel > from.AccessLevel || a?.AccessLevel > from.AccessLevel)
                        {
                            AddLabel(20, y + 20, LabelHue, "You do not have permission to view this client's information.");
                            break;
                        }

                        AddButton(380, y, 0xFA5, 0xFA7, GetButtonID(7, 14));
                        y += 20;

                        var ns = m.NetState;

                        if (ns == null)
                        {
                            AddLabel(20, y, LabelHue, "Address:");
                            AddLabel(200, y, RedHue, "Offline");
                            y += 20;

                            AddLabel(20, y, LabelHue, "Location:");
                            AddLabel(200, y, LabelHue, $"{m.Location} [{m.Map}]");
                            y += 44;
                        }
                        else
                        {
                            AddLabel(20, y, LabelHue, "Address:");
                            AddLabel(200, y, GreenHue, ns.ToString());
                            y += 20;

                            var v = ns.Version;

                            AddLabel(20, y, LabelHue, "Version:");
                            if (ns.Assistant != null)
                            {
                                AddLabel(200, y, LabelHue, $"{v.SourceString ?? "(null)"} ({ns.Assistant})");
                            }
                            else
                            {
                                AddLabel(200, y, LabelHue, v.SourceString ?? "(null)");
                            }

                            y += 20;

                            AddLabel(20, y, LabelHue, "Location:");
                            AddLabel(200, y, LabelHue, $"{m.Location} [{m.Map}]");
                            y += 24;
                        }

                        AddButtonLabeled(20, y, GetButtonID(7, 0), "Go to");
                        AddButtonLabeled(200, y, GetButtonID(7, 1), "Get");
                        y += 20;

                        AddButtonLabeled(20, y, GetButtonID(7, 2), "Kick");
                        AddButtonLabeled(200, y, GetButtonID(7, 3), "Ban");
                        y += 20;

                        AddButtonLabeled(20, y, GetButtonID(7, 4), "Properties");
                        AddButtonLabeled(200, y, GetButtonID(7, 5), "Skills");
                        y += 20;

                        AddButtonLabeled(20, y, GetButtonID(7, 6), "Mortal");
                        AddButtonLabeled(200, y, GetButtonID(7, 7), "Immortal");
                        y += 20;

                        AddButtonLabeled(20, y, GetButtonID(7, 8), "Squelch");
                        AddButtonLabeled(200, y, GetButtonID(7, 9), "Unsquelch");
                        y += 20;

                        /*AddButtonLabeled(  20, y, GetButtonID( 7, 10 ), "Hide" );
                        AddButtonLabeled( 200, y, GetButtonID( 7, 11 ), "Unhide" );
                        y += 20;*/

                        AddButtonLabeled(20, y, GetButtonID(7, 12), "Kill");
                        AddButtonLabeled(200, y, GetButtonID(7, 13), "Resurrect");

                        break;
                    }
                case AdminGumpPage.Accounts_Shared:
                    {
                        m_List ??= GetAllSharedAccounts();

                        AddLabelCropped(12, 120, 60, 20, LabelHue, "Count");
                        AddLabelCropped(72, 120, 120, 20, LabelHue, "Address");
                        AddLabelCropped(192, 120, 180, 20, LabelHue, "Accounts");

                        if (listPage > 0)
                        {
                            AddButton(375, 122, 0x15E3, 0x15E7, GetButtonID(1, 0));
                        }
                        else
                        {
                            AddImage(375, 122, 0x25EA);
                        }

                        if ((listPage + 1) * 12 < m_List.Count)
                        {
                            AddButton(392, 122, 0x15E1, 0x15E5, GetButtonID(1, 1));
                        }
                        else
                        {
                            AddImage(392, 122, 0x25E6);
                        }

                        if (m_List.Count == 0)
                        {
                            AddLabel(12, 140, LabelHue, "There are no accounts to display.");
                        }

                        using var sb = ValueStringBuilder.Create();

                        for (int i = 0, index = listPage * 12; i < 12 && index >= 0 && index < m_List.Count; ++i, ++index)
                        {
                            var (ipAddr, accts) = (KeyValuePair<IPAddress, List<Account>>)m_List[index];

                            var offset = 140 + i * 20;

                            AddLabelCropped(12, offset, 60, 20, LabelHue, accts.Count.ToString());
                            AddLabelCropped(72, offset, 120, 20, LabelHue, ipAddr.ToString());

                            sb.Reset();

                            for (var j = 0; j < accts.Count; ++j)
                            {
                                if (j > 0)
                                {
                                    sb.Append(", ");
                                }

                                if (j < 4)
                                {
                                    var acct = accts[j];

                                    sb.Append(acct.Username);
                                }
                                else
                                {
                                    sb.Append("...");
                                    break;
                                }
                            }

                            AddLabelCropped(192, offset, 180, 20, LabelHue, sb.ToString());

                            AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(5, index + 56));
                        }

                        break;
                    }
                case AdminGumpPage.Accounts:
                    {
                        m_List ??= new List<object>();

                        var rads = state as List<Account>;

                        AddAccountHeader();

                        if (rads == null)
                        {
                            AddLabelCropped(12, 120, 120, 20, LabelHue, "Name");
                        }
                        else
                        {
                            AddLabelCropped(32, 120, 100, 20, LabelHue, "Name");
                        }

                        AddLabelCropped(132, 120, 120, 20, LabelHue, "Access Level");
                        AddLabelCropped(252, 120, 120, 20, LabelHue, "Status");

                        if (listPage > 0)
                        {
                            AddButton(375, 122, 0x15E3, 0x15E7, GetButtonID(1, 0));
                        }
                        else
                        {
                            AddImage(375, 122, 0x25EA);
                        }

                        if ((listPage + 1) * 12 < m_List.Count)
                        {
                            AddButton(392, 122, 0x15E1, 0x15E5, GetButtonID(1, 1));
                        }
                        else
                        {
                            AddImage(392, 122, 0x25E6);
                        }

                        if (m_List.Count == 0)
                        {
                            AddLabel(12, 140, LabelHue, "There are no accounts to display.");
                        }

                        if (rads != null && notice == null)
                        {
                            AddButtonLabeled(10, 390, GetButtonID(5, 27), "Ban marked");
                            AddButtonLabeled(10, 410, GetButtonID(5, 28), "Delete marked");

                            AddButtonLabeled(210, 390, GetButtonID(5, 29), "Mark all");
                            AddButtonLabeled(210, 410, GetButtonID(5, 35), "Unmark house owners");
                        }

                        for (int i = 0, index = listPage * 12; i < 12 && index >= 0 && index < m_List.Count; ++i, ++index)
                        {
                            if (m_List[index] is not Account a)
                            {
                                continue;
                            }

                            var offset = 140 + i * 20;

                            GetAccountInfo(a, out var accessLevel, out var online);

                            if (rads == null)
                            {
                                AddLabelCropped(12, offset, 120, 20, LabelHue, a.Username);
                            }
                            else
                            {
                                AddCheck(10, offset, 0xD2, 0xD3, rads.Contains(a), index);
                                AddLabelCropped(32, offset, 100, 20, LabelHue, a.Username);
                            }

                            AddLabelCropped(132, offset, 120, 20, LabelHue, FormatAccessLevel(accessLevel));

                            if (online)
                            {
                                AddLabelCropped(252, offset, 120, 20, GreenHue, "Online");
                            }
                            else if (a.Banned)
                            {
                                AddLabelCropped(252, offset, 120, 20, RedHue, "Banned");
                            }
                            else
                            {
                                AddLabelCropped(252, offset, 120, 20, RedHue, "Offline");
                            }

                            AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(5, index + 56));
                        }

                        break;
                    }
                case AdminGumpPage.AccountDetails:
                    {
                        AddPageButton(
                            190,
                            10,
                            GetButtonID(5, 0),
                            "Information",
                            AdminGumpPage.AccountDetails_Information,
                            AdminGumpPage.AccountDetails_ChangeAccess,
                            AdminGumpPage.AccountDetails_ChangePassword
                        );
                        AddPageButton(190, 30, GetButtonID(5, 1), "Characters", AdminGumpPage.AccountDetails_Characters);
                        AddPageButton(
                            190,
                            50,
                            GetButtonID(5, 13),
                            "Access",
                            AdminGumpPage.AccountDetails_Access,
                            AdminGumpPage.AccountDetails_Access_ClientIPs,
                            AdminGumpPage.AccountDetails_Access_Restrictions
                        );
                        AddPageButton(190, 70, GetButtonID(5, 2), "Comments", AdminGumpPage.AccountDetails_Comments);
                        AddPageButton(190, 90, GetButtonID(5, 3), "Tags", AdminGumpPage.AccountDetails_Tags);
                        break;
                    }
                case AdminGumpPage.AccountDetails_ChangePassword:
                    {
                        if (state is not Account a)
                        {
                            break;
                        }

                        AddHtml(10, 125, 400, 20, Color(Center("Change Password"), LabelColor32));

                        AddLabel(20, 150, LabelHue, "Username:");
                        AddLabel(200, 150, LabelHue, a.Username);

                        AddLabel(20, 180, LabelHue, "Password:");
                        AddTextField(200, 180, 160, 20, 0);

                        AddLabel(20, 210, LabelHue, "Confirm:");
                        AddTextField(200, 210, 160, 20, 1);

                        AddButtonLabeled(20, 240, GetButtonID(5, 12), "Submit Change");

                        goto case AdminGumpPage.AccountDetails;
                    }
                case AdminGumpPage.AccountDetails_ChangeAccess:
                    {
                        if (state is not Account a)
                        {
                            break;
                        }

                        AddHtml(10, 125, 400, 20, Color(Center("Change Access Level"), LabelColor32));

                        AddLabel(20, 150, LabelHue, "Username:");
                        AddLabel(200, 150, LabelHue, a.Username);

                        AddLabel(20, 170, LabelHue, "Current Level:");
                        AddLabel(200, 170, LabelHue, FormatAccessLevel(a.AccessLevel));

                        AddButtonLabeled(20, 200, GetButtonID(5, 20), "Player");
                        AddButtonLabeled(20, 220, GetButtonID(5, 21), "Counselor");
                        AddButtonLabeled(20, 240, GetButtonID(5, 22), "Game Master");
                        AddButtonLabeled(20, 260, GetButtonID(5, 23), "Seer");

                        if (from.AccessLevel > AccessLevel.Administrator)
                        {
                            AddButtonLabeled(20, 280, GetButtonID(5, 24), "Administrator");

                            if (from.AccessLevel > AccessLevel.Developer)
                            {
                                AddButtonLabeled(20, 300, GetButtonID(5, 33), "Developer");

                                if (from.AccessLevel >= AccessLevel.Owner)
                                {
                                    AddButtonLabeled(20, 320, GetButtonID(5, 34), "Owner");
                                }
                            }
                        }

                        goto case AdminGumpPage.AccountDetails;
                    }
                case AdminGumpPage.AccountDetails_Information:
                    {
                        if (state is not Account a)
                        {
                            break;
                        }

                        var charCount = 0;

                        for (var i = 0; i < a.Length; ++i)
                        {
                            if (a[i] != null)
                            {
                                ++charCount;
                            }
                        }

                        AddHtml(10, 125, 400, 20, Color(Center("Information"), LabelColor32));

                        AddLabel(20, 150, LabelHue, "Username:");
                        AddLabel(200, 150, LabelHue, a.Username);

                        AddLabel(20, 170, LabelHue, "Access Level:");
                        AddLabel(200, 170, LabelHue, FormatAccessLevel(a.AccessLevel));

                        AddLabel(20, 190, LabelHue, "Status:");
                        AddLabel(200, 190, a.Banned ? RedHue : GreenHue, a.Banned ? "Banned" : "Active");

                        if (a.Banned && a.GetBanTags(out var banTime, out var banDuration))
                        {
                            if (banDuration == TimeSpan.MaxValue)
                            {
                                AddLabel(250, 190, LabelHue, "(Infinite)");
                            }
                            else if (banDuration == TimeSpan.Zero)
                            {
                                AddLabel(250, 190, LabelHue, "(Zero)");
                            }
                            else
                            {
                                var remaining = (Core.Now - banTime).Clamp(TimeSpan.Zero, banDuration);
                                var remMinutes = remaining.TotalMinutes;
                                var totMinutes = banDuration.TotalMinutes;

                                var perc = remMinutes / totMinutes;

                                AddLabel(250, 190, LabelHue, $"{FormatTimeSpan(banDuration)} [{perc * 100:F0}%]");
                            }
                        }
                        else if (a.Banned)
                        {
                            AddLabel(250, 190, LabelHue, "(Unspecified)");
                        }

                        AddLabel(20, 210, LabelHue, "Created:");
                        AddLabel(200, 210, LabelHue, a.Created.ToString());

                        AddLabel(20, 230, LabelHue, "Last Login:");
                        AddLabel(200, 230, LabelHue, a.LastLogin.ToString());

                        AddLabel(20, 250, LabelHue, "Character Count:");
                        AddLabel(200, 250, LabelHue, charCount.ToString());

                        AddLabel(20, 270, LabelHue, "Comment Count:");
                        AddLabel(200, 270, LabelHue, a.Comments.Count.ToString());

                        AddLabel(20, 290, LabelHue, "Tag Count:");
                        AddLabel(200, 290, LabelHue, a.Tags.Count.ToString());

                        AddButtonLabeled(20, 320, GetButtonID(5, 8), "Change Password");
                        AddButtonLabeled(200, 320, GetButtonID(5, 9), "Change Access Level");

                        if (!a.Banned)
                        {
                            AddButtonLabeled(20, 350, GetButtonID(5, 10), "Ban Account");
                        }
                        else
                        {
                            AddButtonLabeled(20, 350, GetButtonID(5, 11), "Unban Account");
                        }

                        AddButtonLabeled(200, 350, GetButtonID(5, 25), "Delete Account");

                        goto case AdminGumpPage.AccountDetails;
                    }
                case AdminGumpPage.AccountDetails_Access:
                    {
                        if (state is not Account a)
                        {
                            break;
                        }

                        AddHtml(10, 125, 400, 20, Color(Center("Access"), LabelColor32));

                        AddPageButton(
                            20,
                            150,
                            GetButtonID(5, 14),
                            "View client addresses",
                            AdminGumpPage.AccountDetails_Access_ClientIPs
                        );
                        AddPageButton(
                            20,
                            170,
                            GetButtonID(5, 15),
                            "Manage restrictions",
                            AdminGumpPage.AccountDetails_Access_Restrictions
                        );

                        goto case AdminGumpPage.AccountDetails;
                    }
                case AdminGumpPage.AccountDetails_Access_ClientIPs:
                    {
                        if (state is not Account a)
                        {
                            break;
                        }

                        m_List ??= a.LoginIPs.ToList<object>();

                        AddHtml(10, 195, 400, 20, Color(Center("Client Addresses"), LabelColor32));

                        AddButtonLabeled(227, 225, GetButtonID(5, 16), "View all shared accounts");
                        AddButtonLabeled(227, 245, GetButtonID(5, 17), "Ban all shared accounts");
                        AddButtonLabeled(227, 265, GetButtonID(5, 18), "Firewall all addresses");
                        AddButtonLabeled(227, 285, GetButtonID(5, 36), "Clear all addresses");

                        AddHtml(
                            225,
                            315,
                            180,
                            80,
                            Color("List of IP addresses which have accessed this account.", LabelColor32)
                        );

                        AddImageTiled(15, 219, 206, 156, 0xBBC);
                        AddBlackAlpha(16, 220, 204, 154);

                        AddHtml(18, 221, 114, 20, Color("IP Address", LabelColor32));

                        if (listPage > 0)
                        {
                            AddButton(184, 223, 0x15E3, 0x15E7, GetButtonID(1, 0));
                        }
                        else
                        {
                            AddImage(184, 223, 0x25EA);
                        }

                        if ((listPage + 1) * 6 < m_List.Count)
                        {
                            AddButton(201, 223, 0x15E1, 0x15E5, GetButtonID(1, 1));
                        }
                        else
                        {
                            AddImage(201, 223, 0x25E6);
                        }

                        if (m_List.Count == 0)
                        {
                            AddHtml(18, 243, 200, 60, Color("This account has not yet been accessed.", LabelColor32));
                        }

                        for (int i = 0, index = listPage * 6; i < 6 && index >= 0 && index < m_List.Count; ++i, ++index)
                        {
                            AddHtml(18, 243 + i * 22, 114, 20, Color(m_List[index].ToString(), LabelColor32));
                            AddButton(130, 242 + i * 22, 0xFA2, 0xFA4, GetButtonID(8, index));
                            AddButton(160, 242 + i * 22, 0xFA8, 0xFAA, GetButtonID(9, index));
                            AddButton(190, 242 + i * 22, 0xFB1, 0xFB3, GetButtonID(10, index));
                        }

                        goto case AdminGumpPage.AccountDetails_Access;
                    }
                case AdminGumpPage.AccountDetails_Access_Restrictions:
                    {
                        if (state is not Account a)
                        {
                            break;
                        }

                        m_List ??= a.IpRestrictions.ToList<object>();

                        AddHtml(10, 195, 400, 20, Color(Center("Address Restrictions"), LabelColor32));

                        AddTextField(227, 225, 120, 20, 0);

                        AddButtonLabeled(352, 225, GetButtonID(5, 19), "Add");

                        AddHtml(
                            225,
                            255,
                            180,
                            120,
                            Color(
                                "Any clients connecting from an address not in this list will be rejected. Or, if the list is empty, any client may connect.",
                                LabelColor32
                            )
                        );

                        AddImageTiled(15, 219, 206, 156, 0xBBC);
                        AddBlackAlpha(16, 220, 204, 154);

                        AddHtml(18, 221, 114, 20, Color("IP Address", LabelColor32));

                        if (listPage > 0)
                        {
                            AddButton(184, 223, 0x15E3, 0x15E7, GetButtonID(1, 0));
                        }
                        else
                        {
                            AddImage(184, 223, 0x25EA);
                        }

                        if ((listPage + 1) * 6 < m_List.Count)
                        {
                            AddButton(201, 223, 0x15E1, 0x15E5, GetButtonID(1, 1));
                        }
                        else
                        {
                            AddImage(201, 223, 0x25E6);
                        }

                        if (m_List.Count == 0)
                        {
                            AddHtml(18, 243, 200, 60, Color("There are no addresses in this list.", LabelColor32));
                        }

                        for (int i = 0, index = listPage * 6; i < 6 && index >= 0 && index < m_List.Count; ++i, ++index)
                        {
                            AddHtml(18, 243 + i * 22, 114, 20, Color((string)m_List[index], LabelColor32));
                            AddButton(190, 242 + i * 22, 0xFB1, 0xFB3, GetButtonID(8, index));
                        }

                        goto case AdminGumpPage.AccountDetails_Access;
                    }
                case AdminGumpPage.AccountDetails_Characters:
                    {
                        if (state is not Account a)
                        {
                            break;
                        }

                        AddHtml(10, 125, 400, 20, Color(Center("Characters"), LabelColor32));

                        AddLabelCropped(12, 150, 120, 20, LabelHue, "Name");
                        AddLabelCropped(132, 150, 120, 20, LabelHue, "Access Level");
                        AddLabelCropped(252, 150, 120, 20, LabelHue, "Status");

                        var index = 0;

                        for (var i = 0; i < a.Length; ++i)
                        {
                            var m = a[i];

                            if (m == null)
                            {
                                continue;
                            }

                            var offset = 170 + index * 20;

                            AddLabelCropped(12, offset, 120, 20, GetHueFor(m), m.Name);
                            AddLabelCropped(132, offset, 120, 20, LabelHue, FormatAccessLevel(m.AccessLevel));

                            if (m.NetState != null)
                            {
                                AddLabelCropped(252, offset, 120, 20, GreenHue, "Online");
                            }
                            else
                            {
                                AddLabelCropped(252, offset, 120, 20, RedHue, "Offline");
                            }

                            AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(5, i + 50));

                            ++index;
                        }

                        if (index == 0)
                        {
                            AddLabel(12, 170, LabelHue, "The character list is empty.");
                        }

                        goto case AdminGumpPage.AccountDetails;
                    }
                case AdminGumpPage.AccountDetails_Comments:
                    {
                        if (state is not Account a)
                        {
                            break;
                        }

                        AddHtml(10, 125, 400, 20, Color(Center("Comments"), LabelColor32));

                        AddButtonLabeled(20, 150, GetButtonID(5, 4), "Add Comment");

                        var sb = ValueStringBuilder.Create();

                        if (a.Comments.Count == 0)
                        {
                            sb.Append("There are no comments for this account.");
                        }

                        for (var i = 0; i < a.Comments.Count; ++i)
                        {
                            if (i > 0)
                            {
                                sb.Append("<BR><BR>");
                            }

                            var c = a.Comments[i];
                            sb.Append('[');
                            sb.Append(c.AddedBy);
                            sb.Append(" on ");
                            sb.Append(c.LastModified.ToString());
                            sb.Append("]<BR>");
                            sb.Append(c.Content);
                        }

                        AddHtml(20, 180, 380, 190, sb.ToString(), true, true);

                        sb.Dispose(); // Due to an analyzer bug, we can't use a using
                        goto case AdminGumpPage.AccountDetails;
                    }
                case AdminGumpPage.AccountDetails_Tags:
                    {
                        if (state is not Account a)
                        {
                            break;
                        }

                        AddHtml(10, 125, 400, 20, Color(Center("Tags"), LabelColor32));

                        AddButtonLabeled(20, 150, GetButtonID(5, 5), "Add Tag");

                        var sb = ValueStringBuilder.Create();

                        if (a.Tags.Count == 0)
                        {
                            sb.Append("There are no tags for this account.");
                        }

                        for (var i = 0; i < a.Tags.Count; ++i)
                        {
                            if (i > 0)
                            {
                                sb.Append("<BR>");
                            }

                            var tag = a.Tags[i];

                            sb.Append(tag.Name);
                            sb.Append(" = ");
                            sb.Append(tag.Value);
                        }

                        AddHtml(20, 180, 380, 190, sb.ToString(), true, true);

                        sb.Dispose();
                        goto case AdminGumpPage.AccountDetails;
                    }
                case AdminGumpPage.Firewall:
                    {
                        AddFirewallHeader();

                        m_List ??= Firewall.Set.ToList<object>();

                        AddLabelCropped(12, 120, 358, 20, LabelHue, "IP Address");

                        if (listPage > 0)
                        {
                            AddButton(375, 122, 0x15E3, 0x15E7, GetButtonID(1, 0));
                        }
                        else
                        {
                            AddImage(375, 122, 0x25EA);
                        }

                        if ((listPage + 1) * 12 < m_List.Count)
                        {
                            AddButton(392, 122, 0x15E1, 0x15E5, GetButtonID(1, 1));
                        }
                        else
                        {
                            AddImage(392, 122, 0x25E6);
                        }

                        if (m_List.Count == 0)
                        {
                            AddLabel(12, 140, LabelHue, "The firewall list is empty.");
                        }
                        else
                        {
                            var i = 0;
                            var index = listPage * 12;
                            foreach (var firewallEntry in m_List)
                            {
                                if (i >= 12)
                                {
                                    break;
                                }

                                var offset = 140 + i++ * 20;

                                AddLabelCropped(12, offset, 358, 20, LabelHue, firewallEntry.ToString());
                                AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(6, index++ + 4));
                            }
                        }

                        break;
                    }
                case AdminGumpPage.FirewallInfo:
                    {
                        AddFirewallHeader();

                        if (state is not Firewall.IFirewallEntry firewallEntry)
                        {
                            break;
                        }

                        AddHtml(10, 125, 400, 20, Color(Center(firewallEntry.ToString()), LabelColor32));

                        AddButtonLabeled(20, 150, GetButtonID(6, 3), "Remove");

                        AddHtml(10, 175, 400, 20, Color(Center("Potentially Affected Accounts"), LabelColor32));

                        if (m_List == null)
                        {
                            using var blockedEntriesList = PooledRefList<Account>.Create();

                            foreach (var ia in Accounts.GetAccounts())
                            {
                                var acct = (Account)ia;

                                var loginList = acct.LoginIPs;

                                for (var i = 0; i < loginList.Length; ++i)
                                {
                                    if (firewallEntry.IsBlocked(loginList[i]))
                                    {
                                        blockedEntriesList.Add(acct);
                                        break;
                                    }
                                }
                            }

                            blockedEntriesList.Sort(AccountComparer.Instance);

                            m_List = blockedEntriesList.ToList<Account, object>();
                        }

                        if (listPage > 0)
                        {
                            AddButton(375, 177, 0x15E3, 0x15E7, GetButtonID(1, 0));
                        }
                        else
                        {
                            AddImage(375, 177, 0x25EA);
                        }

                        if ((listPage + 1) * 12 < m_List.Count)
                        {
                            AddButton(392, 177, 0x15E1, 0x15E5, GetButtonID(1, 1));
                        }
                        else
                        {
                            AddImage(392, 177, 0x25E6);
                        }

                        if (m_List.Count == 0)
                        {
                            AddLabelCropped(12, 200, 398, 20, LabelHue, "No accounts found.");
                        }

                        for (int i = 0, index = listPage * 9;
                             i < 9 && index >= 0 && index < m_List.Count;
                             ++i, ++index)
                        {
                            var a = (Account)m_List[index];

                            var offset = 200 + i * 20;

                            GetAccountInfo(a, out var accessLevel, out var online);

                            AddLabelCropped(12, offset, 120, 20, LabelHue, a.Username);
                            AddLabelCropped(132, offset, 120, 20, LabelHue, FormatAccessLevel(accessLevel));

                            if (online)
                            {
                                AddLabelCropped(252, offset, 120, 20, GreenHue, "Online");
                            }
                            else if (a.Banned)
                            {
                                AddLabelCropped(252, offset, 120, 20, RedHue, "Banned");
                            }
                            else
                            {
                                AddLabelCropped(252, offset, 120, 20, RedHue, "Offline");
                            }

                            AddButton(380, offset - 1, 0xFA5, 0xFA7, GetButtonID(5, index + 56));
                        }

                        break;
                    }
            }
        }

        public void AddPageButton(
            int x, int y, int buttonID, string text, AdminGumpPage page,
            params AdminGumpPage[] subPages
        )
        {
            var isSelection = m_PageType == page;

            for (var i = 0; !isSelection && i < subPages.Length; ++i)
            {
                isSelection = m_PageType == subPages[i];
            }

            AddSelectedButton(x, y, buttonID, text, isSelection);
        }

        public void AddSelectedButton(int x, int y, int buttonID, string text, bool isSelection)
        {
            AddButton(x, y - 1, isSelection ? 4006 : 4005, 4007, buttonID);
            AddHtml(x + 35, y, 200, 20, Color(text, isSelection ? SelectedColor32 : LabelColor32));
        }

        public void AddButtonLabeled(int x, int y, int buttonID, string text)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID);
            AddHtml(x + 35, y, 240, 20, Color(text, LabelColor32));
        }

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

        public void AddBlackAlpha(int x, int y, int width, int height)
        {
            AddImageTiled(x, y, width, height, 2624);
            AddAlphaRegion(x, y, width, height);
        }

        public int GetButtonID(int type, int index) => 1 + index * 11 + type;

        public static string FormatTimeSpan(TimeSpan ts) =>
            $"{ts.Days:D2}:{ts.Hours % 24:D2}:{ts.Minutes % 60:D2}:{ts.Seconds % 60:D2}";

        public static string FormatByteAmount(long totalBytes)
        {
            return totalBytes switch
            {
                > 1000000000 => $"{(double)totalBytes / 1073741824:F1} GB",
                > 1000000    => $"{(double)totalBytes / 1048576:F1} MB",
                > 1000       => $"{(double)totalBytes / 1024:F1} KB",
                _            => $"{totalBytes} Bytes"
            };
        }

        public static void Initialize()
        {
            CommandSystem.Register("Admin", AccessLevel.Administrator, Admin_OnCommand);
        }

        [Usage("Admin"), Description(
             "Opens an interface providing server information and administration features including client, account, and firewall management."
         )]
        public static void Admin_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new AdminGump(e.Mobile, AdminGumpPage.Clients));
        }

        public static int GetHueFor(Mobile m)
        {
            if (m == null)
            {
                return LabelHue;
            }

            switch (m.AccessLevel)
            {
                case AccessLevel.Owner:
                case AccessLevel.Developer:
                case AccessLevel.Administrator:
                    {
                        return 0x516;
                    }
                case AccessLevel.Seer:
                    {
                        return 0x144;
                    }
                case AccessLevel.GameMaster:
                    {
                        return 0x21;
                    }
                case AccessLevel.Counselor:
                    {
                        return 0x2;
                    }
                default:
                    {
                        if (m.Kills >= 5)
                        {
                            return 0x21;
                        }

                        return m.Criminal ? 0x3B1 : 0x58;
                    }
            }
        }

        public static string FormatAccessLevel(AccessLevel level)
        {
            var v = (int)level;

            if (v >= 0 && v < m_AccessLevelStrings.Length)
            {
                return m_AccessLevelStrings[v];
            }

            return "Unknown";
        }

        public void AddTextField(int x, int y, int width, int height, int index)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
        }

        public void AddClientHeader()
        {
            AddTextField(200, 20, 200, 20, 0);
            AddButtonLabeled(200, 50, GetButtonID(4, 0), "Search For Name");
            AddButtonLabeled(200, 80, GetButtonID(4, 1), "Search For IP Address");
        }

        public void AddAccountHeader()
        {
            AddPage(1);

            AddLabel(200, 20, LabelHue, "Name:");
            AddTextField(250, 20, 150, 20, 0);

            AddLabel(200, 50, LabelHue, "Pass:");
            AddTextField(250, 50, 150, 20, 1);

            AddButtonLabeled(200, 80, GetButtonID(5, 6), "Add");
            AddButtonLabeled(290, 80, GetButtonID(5, 7), "Search");

            AddButton(384, 84, 0x15E1, 0x15E5, 0, GumpButtonType.Page, 2);

            AddPage(2);

            AddButtonLabeled(200, 20, GetButtonID(5, 31), "View All: Inactive");
            AddButtonLabeled(200, 40, GetButtonID(5, 32), "View All: Banned");
            AddButtonLabeled(200, 60, GetButtonID(5, 26), "View All: Shared");
            AddButtonLabeled(200, 80, GetButtonID(5, 30), "View All: Empty");

            AddButton(384, 84, 0x15E1, 0x15E5, 0, GumpButtonType.Page, 1);

            AddPage(0);
        }

        public void AddFirewallHeader()
        {
            AddTextField(200, 20, 200, 20, 0);
            AddButtonLabeled(320, 50, GetButtonID(6, 0), "Search");
            AddButtonLabeled(200, 50, GetButtonID(6, 1), "Add (Input)");
            AddButtonLabeled(200, 80, GetButtonID(6, 2), "Add (Target)");
        }

        private static List<object> GetAllSharedAccounts()
        {
            var table = new Dictionary<IPAddress, List<Account>>();

            foreach (Account acct in Accounts.GetAccounts())
            {
                var theirAddresses = acct.LoginIPs;

                for (var i = 0; i < theirAddresses.Length; ++i)
                {
                    var theirAddress = theirAddresses[i];

                    // This path is heavy for larger shards, so use optimized code
                    ref var accts = ref CollectionsMarshal.GetValueRefOrAddDefault(table, theirAddress, out var acctExists);

                    // If we don't have a list, create one
                    if (!acctExists)
                    {
                        accts = new List<Account>();
                    }

                    accts.Add(acct);
                }
            }

            var list = new List<object>();

            // Lets find all the entries that have only one account
            foreach (var kvp in table)
            {
                if (kvp.Value.Count > 1)
                {
                    // Sort the accounts alphabetically
                    kvp.Value.Sort(AccountComparer.Instance);

                    // Can't avoid boxing because `m_List` in AdminGump is expecting List<object>
                    list.Add(kvp);
                }
            }

            // Sort by highest accounts per IP first
            list.Sort(SharedAccountDescendingComparer.Instance);

            return list;
        }

        private static List<Account> GetSharedAccounts(IPAddress ipAddress)
        {
            var list = new List<Account>();

            foreach (var account in Accounts.GetAccounts())
            {
                var acct = (Account)account;

                var theirAddresses = acct.LoginIPs;
                var contains = false;

                for (var i = 0; !contains && i < theirAddresses.Length; ++i)
                {
                    contains = ipAddress.Equals(theirAddresses[i]);
                }

                if (contains)
                {
                    list.Add(acct);
                }
            }

            list.Sort(AccountComparer.Instance);
            return list;
        }

        private static List<Account> GetSharedAccounts(IPAddress[] ipAddresses)
        {
            var list = new List<Account>();

            foreach (Account acct in Accounts.GetAccounts())
            {
                var theirAddresses = acct.LoginIPs;
                var contains = false;

                for (var i = 0; !contains && i < theirAddresses.Length; ++i)
                {
                    var check = theirAddresses[i];

                    for (var j = 0; !contains && j < ipAddresses.Length; ++j)
                    {
                        contains = check.Equals(ipAddresses[j]);
                    }
                }

                if (contains)
                {
                    list.Add(acct);
                }
            }

            list.Sort(AccountComparer.Instance);
            return list;
        }

        public static void BanShared_Callback(Mobile from, bool okay, Account a)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            string notice;
            List<Account> list = null;

            if (okay)
            {
                list = GetSharedAccounts(a.LoginIPs);

                for (var i = 0; i < list.Count; ++i)
                {
                    list[i].SetUnspecifiedBan(from);
                    list[i].Banned = true;
                }

                notice = "All addresses in the list have been banned.";
            }
            else
            {
                notice = "You have chosen not to ban all shared accounts.";
            }

            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, notice, a));

            if (okay)
            {
                from.SendGump(new BanDurationGump(list));
            }
        }

        public static void AccountDelete_Callback(Mobile from, bool okay, Account a)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            if (okay)
            {
                CommandLogging.WriteLine(
                    from,
                    $"{from.AccessLevel} {CommandLogging.Format(from)} deleting account {a.Username}"
                );
                a.Delete();

                from.SendGump(
                    new AdminGump(
                        from,
                        AdminGumpPage.Accounts,
                        0,
                        null,
                        $"{a.Username} : The account has been deleted."
                    )
                );
            }
            else
            {
                from.SendGump(
                    new AdminGump(
                        from,
                        AdminGumpPage.AccountDetails_Information,
                        0,
                        null,
                        "You have chosen not to delete the account.",
                        a
                    )
                );
            }
        }

        public static void ResendGump_Callback(Mobile from, List<object> list, List<Account> rads, int page)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            from.SendGump(new AdminGump(from, AdminGumpPage.Accounts, page, list, null, rads));
        }

        public static void Marked_Callback(Mobile from, bool okay, bool ban, List<object> list, List<Account> rads, int page)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            if (okay)
            {
                if (!ban)
                {
                    NetState.FlushAll();
                }

                for (var i = 0; i < rads.Count; ++i)
                {
                    var acct = rads[i];

                    if (ban)
                    {
                        CommandLogging.WriteLine(
                            from,
                            $"{from.AccessLevel} {CommandLogging.Format(from)} banning account {acct.Username}"
                        );
                        acct.SetUnspecifiedBan(from);
                        acct.Banned = true;
                    }
                    else
                    {
                        CommandLogging.WriteLine(
                            from,
                            $"{from.AccessLevel} {CommandLogging.Format(from)} deleting account {acct.Username}"
                        );
                        acct.Delete();
                        rads.RemoveAt(i--);
                        list.Remove(acct);
                    }
                }

                from.SendGump(
                    new NoticeGump(
                        1060637,
                        30720,
                        $"You have {(ban ? "banned" : "deleted")} the account{(rads.Count == 1 ? "" : "s")}.",
                        0xFFC000,
                        420,
                        280,
                        () => ResendGump_Callback(from, list, rads, ban ? page : 0)
                    )
                );

                if (ban)
                {
                    from.SendGump(new BanDurationGump(rads));
                }
            }
            else
            {
                from.SendGump(
                    new NoticeGump(
                        1060637,
                        30720,
                        $"You have chosen not to {(ban ? "ban" : "delete")} the account{(rads.Count == 1 ? "" : "s")}.",
                        0xFFC000,
                        420,
                        280,
                        () => ResendGump_Callback(from, list, rads, page)
                    )
                );
            }
        }

        public static void FirewallShared_Callback(Mobile from, bool okay, Account a)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            string notice;

            if (okay)
            {
                for (var i = 0; i < a.LoginIPs.Length; ++i)
                {
                    Firewall.Add(a.LoginIPs[i]);
                }

                notice = "All addresses in the list have been firewalled.";
            }
            else
            {
                notice = "You have chosen not to firewall all addresses.";
            }

            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, notice, a));
        }

        public static void Firewall_Callback(Mobile from, bool okay, Account a, object toFirewall)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            string notice;

            if (okay)
            {
                Firewall.Add(toFirewall);

                notice = $"{toFirewall} : Added to firewall.";
            }
            else
            {
                notice = "You have chosen not to firewall the address.";
            }

            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, notice, a));
        }

        public static void RemoveLoginIP_Callback(Mobile from, bool okay, Account a, IPAddress ip)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            string notice;

            if (okay)
            {
                var ips = a.LoginIPs;

                if (ips.Length != 0 && Equals(ip, ips[0]) && AccountHandler.IPTable.ContainsKey(ips[0]))
                {
                    --AccountHandler.IPTable[ip];
                }

                var newList = new List<IPAddress>(ips);
                newList.Remove(ip);
                a.LoginIPs = newList.ToArray();

                notice = $"{ip} : Removed address.";
            }
            else
            {
                notice = "You have chosen not to remove the address.";
            }

            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, notice, a));
        }

        public static void RemoveLoginIPs_Callback(Mobile from, bool okay, Account a)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            string notice;

            if (okay)
            {
                var ips = a.LoginIPs;

                if (ips.Length != 0 && AccountHandler.IPTable.ContainsKey(ips[0]))
                {
                    --AccountHandler.IPTable[ips[0]];
                }

                a.LoginIPs = Array.Empty<IPAddress>();

                notice = "All addresses in the list have been removed.";
            }
            else
            {
                notice = "You have chosen not to clear all addresses.";
            }

            from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Access_ClientIPs, 0, null, notice, a));
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var val = info.ButtonID - 1;

            if (val < 0)
            {
                return;
            }

            var from = m_From;

            if (from.AccessLevel < AccessLevel.Administrator)
            {
                return;
            }

            if (m_PageType == AdminGumpPage.Accounts)
            {
                if (m_State is List<Account> rads)
                {
                    for (int i = 0, v = m_ListPage * 12; i < 12 && v < m_List.Count; ++i, ++v)
                    {
                        var acct = (Account)m_List[v];

                        if (info.IsSwitched(v))
                        {
                            if (!rads.Contains(acct))
                            {
                                rads.Add(acct);
                            }
                        }
                        else if (rads.Contains(acct))
                        {
                            rads.Remove(acct);
                        }
                    }
                }
            }

            var type = val % 11;
            var index = val / 11;

            switch (type)
            {
                case 0:
                    {
                        AdminGumpPage page;

                        switch (index)
                        {
                            case 0:
                                {
                                    page = AdminGumpPage.Information_General;
                                    break;
                                }
                            case 1:
                                {
                                    page = AdminGumpPage.Administer;
                                    break;
                                }
                            case 2:
                                {
                                    page = AdminGumpPage.Clients;
                                    break;
                                }
                            case 3:
                                {
                                    page = AdminGumpPage.Accounts;
                                    break;
                                }
                            case 4:
                                {
                                    page = AdminGumpPage.Firewall;
                                    break;
                                }
                            case 5:
                                {
                                    page = AdminGumpPage.Information_Perf;
                                    break;
                                }
                            default:
                                {
                                    return;
                                }
                        }

                        from.SendGump(new AdminGump(from, page));
                        break;
                    }
                case 1:
                    {
                        switch (index)
                        {
                            case 0:
                                {
                                    if (m_List != null && m_ListPage > 0)
                                    {
                                        from.SendGump(
                                            new AdminGump(from, m_PageType, m_ListPage - 1, m_List, null, m_State)
                                        );
                                    }

                                    break;
                                }
                            case 1:
                                {
                                    if (m_List != null /*&& (m_ListPage + 1) * 12 < m_List.Count*/)
                                    {
                                        from.SendGump(
                                            new AdminGump(from, m_PageType, m_ListPage + 1, m_List, null, m_State)
                                        );
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 3:
                    {
                        string notice = null;
                        var page = AdminGumpPage.Administer;

                        if (index >= 500)
                        {
                            page = AdminGumpPage.Administer_Access_Lockdown;
                        }
                        else if (index >= 400)
                        {
                            page = AdminGumpPage.Administer_Commands;
                        }
                        else if (index >= 300)
                        {
                            page = AdminGumpPage.Administer_Access;
                        }
                        else if (index >= 200)
                        {
                            page = AdminGumpPage.Administer_Server;
                        }
                        else if (index >= 100)
                        {
                            page = AdminGumpPage.Administer_WorldBuilding;
                        }

                        switch (index)
                        {
                            case 0:
                                {
                                    page = AdminGumpPage.Administer_WorldBuilding;
                                    break;
                                }
                            case 1:
                                {
                                    page = AdminGumpPage.Administer_Server;
                                    break;
                                }
                            case 2:
                                {
                                    page = AdminGumpPage.Administer_Access;
                                    break;
                                }
                            case 3:
                                {
                                    page = AdminGumpPage.Administer_Commands;
                                    break;
                                }

                            case 101:
                                {
                                    InvokeCommand("TelGen");
                                    notice = "Teleporters have been generated.";
                                    break;
                                }
                            case 102:
                                {
                                    InvokeCommand("MoonGen");
                                    notice = "Moongates have been generated.";
                                    break;
                                }
                            case 104:
                                {
                                    InvokeCommand("DoorGen");
                                    notice = "Doors have been generated.";
                                    break;
                                }
                            case 103:
                                {
                                    var folder = Core.SA ? "post-uoml" : "uoml";

                                    var availableMaps = ExpansionInfo.CoreExpansion.MapSelectionFlags;
                                    if (Core.SA && availableMaps.Includes(MapSelectionFlags.TerMur))
                                    {
                                        InvokeCommand($"GenerateSpawners Data/Spawns/post-uoml/termur/*.json");
                                    }

                                    if (availableMaps.Includes(MapSelectionFlags.Malas))
                                    {
                                        InvokeCommand($"GenerateSpawners Data/Spawns/{folder}/malas/*.json");
                                    }

                                    if (availableMaps.Includes(MapSelectionFlags.Tokuno))
                                    {
                                        InvokeCommand($"GenerateSpawners Data/Spawns/{folder}/tokuno/*.json");
                                    }

                                    if (availableMaps.Includes(MapSelectionFlags.Ilshenar))
                                    {
                                        InvokeCommand($"GenerateSpawners Data/Spawns/{folder}/ilshenar/*.json");
                                    }

                                    if (availableMaps.Includes(MapSelectionFlags.Trammel))
                                    {
                                        InvokeCommand($"GenerateSpawners Data/Spawns/{folder}/trammel/*.json");
                                    }

                                    if (availableMaps.Includes(MapSelectionFlags.Felucca))
                                    {
                                        InvokeCommand($"GenerateSpawners Data/Spawns/{folder}/felucca/*.json");
                                    }

                                    notice = "Spawners have been generated.";
                                    break;
                                }
                            case 105:
                                {
                                    InvokeCommand("SignGen");
                                    notice = "Signs have been generated.";
                                    break;
                                }
                            case 106:
                                {
                                    InvokeCommand("Decorate");
                                    notice = "Decoration has been generated.";
                                    break;
                                }

                            case 110:
                                {
                                    InvokeCommand("Freeze");
                                    notice = "Target bounding points.";
                                    break;
                                }
                            case 120:
                                {
                                    InvokeCommand("Unfreeze");
                                    notice = "Target bounding points.";
                                    break;
                                }

                            case 200:
                                {
                                    InvokeCommand("Save");
                                    notice = "The world has been saved.";
                                    break;
                                }
                            case 201:
                                {
                                    Shutdown(false, true);
                                    break;
                                }
                            case 202:
                                {
                                    Shutdown(false, false);
                                    break;
                                }
                            case 203:
                                {
                                    Shutdown(true, true);
                                    break;
                                }
                            case 204:
                                {
                                    Shutdown(true, false);
                                    break;
                                }
                            case 210:
                            case 211:
                                {
                                    var text = info.GetTextEntry(0)?.Text.Trim();

                                    if (string.IsNullOrEmpty(text))
                                    {
                                        notice = "You must enter text to broadcast it.";
                                    }
                                    else
                                    {
                                        notice = "Your message has been broadcasted.";
                                        InvokeCommand($"{(index == 210 ? "BC" : "SM")} {text}");
                                    }

                                    break;
                                }

                            case 300:
                                {
                                    InvokeCommand("Kick");
                                    notice = "Target the player to kick.";
                                    break;
                                }
                            case 301:
                                {
                                    InvokeCommand("Ban");
                                    notice = "Target the player to ban.";
                                    break;
                                }
                            case 302:
                                {
                                    InvokeCommand("Firewall");
                                    notice = "Target the player to firewall.";
                                    break;
                                }

                            case 303:
                                {
                                    page = AdminGumpPage.Administer_Access_Lockdown;
                                    break;
                                }

                            case 310:
                                {
                                    InvokeCommand("Set AccessLevel Player");
                                    notice = "Target the player to change their access level. (Player)";
                                    break;
                                }
                            case 311:
                                {
                                    InvokeCommand("Set AccessLevel Counselor");
                                    notice = "Target the player to change their access level. (Counselor)";
                                    break;
                                }
                            case 312:
                                {
                                    InvokeCommand("Set AccessLevel GameMaster");
                                    notice = "Target the player to change their access level. (Game Master)";
                                    break;
                                }
                            case 313:
                                {
                                    InvokeCommand("Set AccessLevel Seer");
                                    notice = "Target the player to change their access level. (Seer)";
                                    break;
                                }

                            case 314:
                                {
                                    if (from.AccessLevel > AccessLevel.Administrator)
                                    {
                                        InvokeCommand("Set AccessLevel Administrator");
                                        notice = "Target the player to change their access level. (Administrator)";
                                    }

                                    break;
                                }

                            case 315:
                                {
                                    if (from.AccessLevel > AccessLevel.Developer)
                                    {
                                        InvokeCommand("Set AccessLevel Developer");
                                        notice = "Target the player to change their access level. (Developer)";
                                    }

                                    break;
                                }

                            case 316:
                                {
                                    if (from.AccessLevel >= AccessLevel.Owner)
                                    {
                                        InvokeCommand("Set AccessLevel Owner");
                                        notice = "Target the player to change their access level. (Owner)";
                                    }

                                    break;
                                }

                            case 400:
                                {
                                    notice = "Enter search terms to add objects.";
                                    break;
                                }
                            case 401:
                                {
                                    InvokeCommand("Remove");
                                    notice = "Target the item or mobile to remove.";
                                    break;
                                }
                            case 402:
                                {
                                    InvokeCommand("Dupe");
                                    notice = "Target the item to dupe.";
                                    break;
                                }
                            case 403:
                                {
                                    InvokeCommand("DupeInBag");
                                    notice = "Target the item to dupe. The item will be duped at it's current location.";
                                    break;
                                }
                            case 404:
                                {
                                    InvokeCommand("Props");
                                    notice = "Target the item or mobile to inspect.";
                                    break;
                                }
                            case 405:
                                {
                                    InvokeCommand("Skills");
                                    notice = "Target a mobile to view their skills.";
                                    break;
                                }
                            case 406:
                                {
                                    InvokeCommand("Set Blessed False");
                                    notice = "Target the mobile to make mortal.";
                                    break;
                                }
                            case 407:
                                {
                                    InvokeCommand("Set Blessed True");
                                    notice = "Target the mobile to make immortal.";
                                    break;
                                }
                            case 408:
                                {
                                    InvokeCommand("Set Squelched True");
                                    notice = "Target the mobile to squelch.";
                                    break;
                                }
                            case 409:
                                {
                                    InvokeCommand("Set Squelched False");
                                    notice = "Target the mobile to unsquelch.";
                                    break;
                                }
                            case 410:
                                {
                                    InvokeCommand("Set Frozen True");
                                    notice = "Target the mobile to freeze.";
                                    break;
                                }
                            case 411:
                                {
                                    InvokeCommand("Set Frozen False");
                                    notice = "Target the mobile to unfreeze.";
                                    break;
                                }
                            case 412:
                                {
                                    InvokeCommand("Set Hidden True");
                                    notice = "Target the mobile to hide.";
                                    break;
                                }
                            case 413:
                                {
                                    InvokeCommand("Set Hidden False");
                                    notice = "Target the mobile to unhide.";
                                    break;
                                }
                            case 414:
                                {
                                    InvokeCommand("Kill");
                                    notice = "Target the mobile to kill.";
                                    break;
                                }
                            case 415:
                                {
                                    InvokeCommand("Resurrect");
                                    notice = "Target the mobile to resurrect.";
                                    break;
                                }
                            case 416:
                                {
                                    InvokeCommand("Move");
                                    notice = "Target the item or mobile to move.";
                                    break;
                                }
                            case 417:
                                {
                                    InvokeCommand("Wipe");
                                    notice = "Target bounding points.";
                                    break;
                                }
                            case 418:
                                {
                                    InvokeCommand("Tele");
                                    notice = "Choose your destination.";
                                    break;
                                }
                            case 419:
                                {
                                    InvokeCommand("Multi Tele");
                                    notice = "Choose your destination.";
                                    break;
                                }

                            case 500:
                            case 501:
                            case 502:
                            case 503:
                            case 504:
                                {
                                    AccountHandler.LockdownLevel = (AccessLevel)(index - 500);

                                    if (AccountHandler.LockdownLevel > AccessLevel.Player)
                                    {
                                        notice = "The lockdown level has been changed.";
                                    }
                                    else
                                    {
                                        notice = "The server is now accessible to everyone.";
                                    }

                                    break;
                                }

                            case 510:
                                {
                                    var level = AccountHandler.LockdownLevel;

                                    if (level > AccessLevel.Player)
                                    {
                                        var count = 0;

                                        foreach (var ns in TcpServer.Instances)
                                        {
                                            var a = ns.Account;

                                            if (a == null)
                                            {
                                                continue;
                                            }

                                            var hasAccess = false;

                                            if (a.AccessLevel >= level)
                                            {
                                                hasAccess = true;
                                            }
                                            else
                                            {
                                                for (var j = 0; !hasAccess && j < a.Length; ++j)
                                                {
                                                    var m = a[j];

                                                    if (m?.AccessLevel >= level)
                                                    {
                                                        hasAccess = true;
                                                    }
                                                }
                                            }

                                            if (!hasAccess)
                                            {
                                                ns.Disconnect("Server has been locked down.");
                                                ++count;
                                            }
                                        }

                                        if (count == 0)
                                        {
                                            notice = "Nobody without access was found to disconnect.";
                                        }
                                        else
                                        {
                                            notice = $"Number of players disconnected: {count}";
                                        }
                                    }
                                    else
                                    {
                                        notice = "The server is not currently locked down.";
                                    }

                                    break;
                                }
                        }

                        from.SendGump(new AdminGump(from, page, 0, null, notice));

                        switch (index)
                        {
                            case 400:
                                {
                                    InvokeCommand("Add");
                                    break;
                                }
                            case 111:
                                {
                                    InvokeCommand("FreezeWorld");
                                    break;
                                }
                            case 112:
                                {
                                    InvokeCommand("FreezeMap");
                                    break;
                                }
                            case 121:
                                {
                                    InvokeCommand("UnfreezeWorld");
                                    break;
                                }
                            case 122:
                                {
                                    InvokeCommand("UnfreezeMap");
                                    break;
                                }
                        }

                        break;
                    }
                case 4:
                    {
                        switch (index)
                        {
                            case 0:
                            case 1:
                                {
                                    var forName = index == 0;

                                    var results = new List<NetState>();

                                    var match = info.GetTextEntry(0)?.Text.Trim().ToLower();
                                    string notice = null;

                                    if (string.IsNullOrEmpty(match))
                                    {
                                        notice = $"You must enter {(forName ? "a name" : "an ip address")} to search.";
                                    }
                                    else
                                    {
                                        foreach (var ns in TcpServer.Instances)
                                        {
                                            bool isMatch;

                                            if (forName)
                                            {
                                                var m = ns.Mobile;
                                                var a = ns.Account;

                                                isMatch = m?.Name.InsensitiveContains(match) == true
                                                          || a?.Username.InsensitiveContains(match) == true;
                                            }
                                            else
                                            {
                                                isMatch = ns.ToString().ContainsOrdinal(match);
                                            }

                                            if (isMatch)
                                            {
                                                results.Add(ns);
                                            }
                                        }

                                        results.Sort(NetStateComparer.Instance);
                                    }

                                    if (results.Count == 1)
                                    {
                                        var ns = results[0];
                                        var state = ns.Mobile ?? (object)ns.Account;

                                        if (state is Mobile)
                                        {
                                            from.SendGump(
                                                new AdminGump(
                                                    from,
                                                    AdminGumpPage.ClientInfo,
                                                    0,
                                                    null,
                                                    "One match found.",
                                                    state
                                                )
                                            );
                                        }
                                        else if (state is Account)
                                        {
                                            from.SendGump(
                                                new AdminGump(
                                                    from,
                                                    AdminGumpPage.AccountDetails_Information,
                                                    0,
                                                    null,
                                                    "One match found.",
                                                    state
                                                )
                                            );
                                        }
                                        else
                                        {
                                            from.SendGump(
                                                new AdminGump(
                                                    from,
                                                    AdminGumpPage.Clients,
                                                    0,
                                                    results.ToList<object>(),
                                                    "One match found."
                                                )
                                            );
                                        }
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.Clients,
                                                0,
                                                results.ToList<object>(),
                                                notice ?? (results.Count == 0 ? "Nothing matched your search terms." : null)
                                            )
                                        );
                                    }

                                    break;
                                }
                            default:
                                {
                                    index -= 2;

                                    if (index < m_List?.Count)
                                    {
                                        if (m_List[index] is not NetState ns)
                                        {
                                            break;
                                        }

                                        var m = ns.Mobile;
                                        var a = ns.Account as Account;

                                        if (m != null)
                                        {
                                            from.SendGump(new AdminGump(from, AdminGumpPage.ClientInfo, 0, null, null, m));
                                        }
                                        else if (a != null)
                                        {
                                            from.SendGump(
                                                new AdminGump(
                                                    from,
                                                    AdminGumpPage.AccountDetails_Information,
                                                    0,
                                                    null,
                                                    null,
                                                    a
                                                )
                                            );
                                        }
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 5:
                    {
                        switch (index)
                        {
                            case 0:
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_Information,
                                            0,
                                            null,
                                            null,
                                            m_State
                                        )
                                    );
                                    break;
                                }
                            case 1:
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_Characters,
                                            0,
                                            null,
                                            null,
                                            m_State
                                        )
                                    );
                                    break;
                                }
                            case 2:
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_Comments,
                                            0,
                                            null,
                                            null,
                                            m_State
                                        )
                                    );
                                    break;
                                }
                            case 3:
                                {
                                    from.SendGump(
                                        new AdminGump(from, AdminGumpPage.AccountDetails_Tags, 0, null, null, m_State)
                                    );
                                    break;
                                }
                            case 13:
                                {
                                    from.SendGump(
                                        new AdminGump(from, AdminGumpPage.AccountDetails_Access, 0, null, null, m_State)
                                    );
                                    break;
                                }
                            case 14:
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_Access_ClientIPs,
                                            0,
                                            null,
                                            null,
                                            m_State
                                        )
                                    );
                                    break;
                                }
                            case 15:
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_Access_Restrictions,
                                            0,
                                            null,
                                            null,
                                            m_State
                                        )
                                    );
                                    break;
                                }
                            case 4:
                                {
                                    from.Prompt = new AddCommentPrompt(m_State as Account);
                                    from.SendMessage("Enter the new account comment.");
                                    break;
                                }
                            case 5:
                                {
                                    from.Prompt = new AddTagNamePrompt(m_State as Account);
                                    from.SendMessage("Enter the new tag name.");
                                    break;
                                }
                            case 6:
                                {
                                    var un = info.GetTextEntry(0)?.Text.Trim();
                                    var pw = info.GetTextEntry(1)?.Text.Trim();

                                    Account dispAccount = null;
                                    string notice;

                                    if (string.IsNullOrEmpty(un))
                                    {
                                        notice = "You must enter a username to add an account.";
                                    }
                                    else if (string.IsNullOrEmpty(pw))
                                    {
                                        notice = "You must enter a password to add an account.";
                                    }
                                    else
                                    {
                                        var account = Accounts.GetAccount(un);

                                        if (account != null)
                                        {
                                            notice = "There is already an account with that username.";
                                        }
                                        else
                                        {
                                            dispAccount = new Account(un, pw);
                                            notice = $"{un} : Account added.";
                                            CommandLogging.WriteLine(
                                                from,
                                                $"{from.AccessLevel} {CommandLogging.Format(from)} adding new account: {un}"
                                            );
                                        }
                                    }

                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            dispAccount != null ? AdminGumpPage.AccountDetails_Information : m_PageType,
                                            m_ListPage,
                                            m_List,
                                            notice,
                                            dispAccount ?? m_State
                                        )
                                    );
                                    break;
                                }
                            case 7:
                                {
                                    List<IAccount> results;

                                    var matchEntry = info.GetTextEntry(0);
                                    var match = matchEntry?.Text.Trim().ToLower();

                                    if (string.IsNullOrEmpty(match))
                                    {
                                        results = Accounts.GetAccounts().ToList();
                                        results.Sort(AccountComparer.Instance);
                                    }
                                    else
                                    {
                                        results = new List<IAccount>();
                                        foreach (var acct in Accounts.GetAccounts())
                                        {
                                            if (acct.Username.InsensitiveContains(match))
                                            {
                                                results.Add(acct);
                                            }
                                        }

                                        results.Sort(AccountComparer.Instance);
                                    }

                                    if (results.Count == 1)
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Information,
                                                0,
                                                null,
                                                "One match found.",
                                                results[0]
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.Accounts,
                                                0,
                                                results.ToList<object>(),
                                                results.Count == 0 ? "Nothing matched your search terms." : null,
                                                new List<object>()
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 8:
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_ChangePassword,
                                            0,
                                            null,
                                            null,
                                            m_State
                                        )
                                    );
                                    break;
                                }
                            case 9:
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_ChangeAccess,
                                            0,
                                            null,
                                            null,
                                            m_State
                                        )
                                    );
                                    break;
                                }
                            case 10:
                            case 11:
                                {
                                    if (m_State is not Account a)
                                    {
                                        break;
                                    }

                                    a.SetUnspecifiedBan(from);
                                    a.Banned = index == 10;
                                    CommandLogging.WriteLine(
                                        from,
                                        $"{from.AccessLevel} {CommandLogging.Format(from)} {a.Username} account {(a.Banned ? "banning" : "unbanning")}"
                                    );
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            m_PageType,
                                            m_ListPage,
                                            m_List,
                                            $"The account has been {(a.Banned ? "banned" : "unbanned")}.",
                                            m_State
                                        )
                                    );

                                    if (index == 10)
                                    {
                                        from.SendGump(new BanDurationGump(a));
                                    }

                                    break;
                                }
                            case 12:
                                {
                                    if (m_State is not Account a)
                                    {
                                        break;
                                    }

                                    var passwordEntry = info.GetTextEntry(0);
                                    var confirmEntry = info.GetTextEntry(1);

                                    var password = passwordEntry?.Text.Trim();
                                    var confirm = confirmEntry?.Text.Trim();

                                    string notice;
                                    var page = AdminGumpPage.AccountDetails_ChangePassword;

                                    if (string.IsNullOrEmpty(password))
                                    {
                                        notice = "You must enter the password.";
                                    }
                                    else if (confirm != password)
                                    {
                                        notice =
                                            "You must confirm the password. That field must precisely match the password field.";
                                    }
                                    else
                                    {
                                        notice = "The password has been changed.";
                                        a.SetPassword(password);
                                        page = AdminGumpPage.AccountDetails_Information;
                                        CommandLogging.WriteLine(
                                            from,
                                            $"{from.AccessLevel} {CommandLogging.Format(from)} changing password of account {a.Username}"
                                        );
                                    }

                                    from.SendGump(new AdminGump(from, page, 0, null, notice, m_State));

                                    break;
                                }
                            case 16: // view shared
                                {
                                    if (m_State is not Account a)
                                    {
                                        break;
                                    }

                                    var list = GetSharedAccounts(a.LoginIPs);

                                    if (list.Count > 1 || list.Count == 1 && !list.Contains(a))
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.Accounts,
                                                0,
                                                list.ToList<object>(),
                                                null,
                                                new List<object>()
                                            )
                                        );
                                    }
                                    else if (a.LoginIPs.Length > 0)
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Access_ClientIPs,
                                                0,
                                                null,
                                                "There are no other accounts which share an address with this one.",
                                                m_State
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Access_ClientIPs,
                                                0,
                                                null,
                                                "This account has not yet been accessed.",
                                                m_State
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 17: // ban shared
                                {
                                    if (m_State is not Account a)
                                    {
                                        break;
                                    }

                                    var list = GetSharedAccounts(a.LoginIPs);

                                    if (list.Count > 0)
                                    {
                                        using var sb = ValueStringBuilder.Create();
                                        sb.Append("You are about to ban ");
                                        sb.Append(list.Count);
                                        sb.Append(list.Count != 1 ? "accounts." : "account.");
                                        sb.Append(" Do you wish to continue?");

                                        for (var i = 0; i < list.Count; ++i)
                                        {
                                            sb.Append("<br>- ");
                                            sb.Append(list[i].Username);
                                        }

                                        from.SendGump(
                                            new WarningGump(
                                                1060635,
                                                30720,
                                                sb.ToString(),
                                                0xFFC000,
                                                420,
                                                400,
                                                okay => BanShared_Callback(from, okay, a)
                                            )
                                        );
                                    }
                                    else if (a.LoginIPs.Length > 0)
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Access_ClientIPs,
                                                0,
                                                null,
                                                "There are no accounts which share an address with this one.",
                                                m_State
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Access_ClientIPs,
                                                0,
                                                null,
                                                "This account has not yet been accessed.",
                                                m_State
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 18: // firewall all
                                {
                                    if (m_State is not Account a)
                                    {
                                        break;
                                    }

                                    if (a.LoginIPs.Length > 0)
                                    {
                                        from.SendGump(
                                            new WarningGump(
                                                1060635,
                                                30720,
                                                $"You are about to firewall {a.LoginIPs.Length} address{(a.LoginIPs.Length != 1 ? "s" : "")}. Do you wish to continue?",
                                                0xFFC000,
                                                420,
                                                400,
                                                okay => FirewallShared_Callback(from, okay, a)
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Access_ClientIPs,
                                                0,
                                                null,
                                                "This account has not yet been accessed.",
                                                m_State
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 19: // add
                                {
                                    if (m_State is not Account a)
                                    {
                                        break;
                                    }

                                    var entry = info.GetTextEntry(0);
                                    var ip = entry?.Text.Trim();

                                    string notice;

                                    if (string.IsNullOrEmpty(ip))
                                    {
                                        notice = "You must enter an address to add.";
                                    }
                                    else
                                    {
                                        var list = a.IpRestrictions;

                                        var contains = false;
                                        for (var i = 0; !contains && i < list.Length; ++i)
                                        {
                                            contains = list[i] == ip;
                                        }

                                        if (contains)
                                        {
                                            notice = "That address is already contained in the list.";
                                        }
                                        else
                                        {
                                            var newList = new string[list.Length + 1];

                                            for (var i = 0; i < list.Length; ++i)
                                            {
                                                newList[i] = list[i];
                                            }

                                            newList[list.Length] = ip;

                                            a.IpRestrictions = newList;

                                            notice = $"{ip} : Added to restriction list.";
                                        }
                                    }

                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_Access_Restrictions,
                                            0,
                                            null,
                                            notice,
                                            m_State
                                        )
                                    );

                                    break;
                                }
                            case 20: // Change access level
                            case 21:
                            case 22:
                            case 23:
                            case 24:
                                {
                                    if (m_State is not Account a)
                                    {
                                        break;
                                    }

                                    var newLevel = index switch
                                    {
                                        21 => AccessLevel.Counselor,
                                        22 => AccessLevel.GameMaster,
                                        23 => AccessLevel.Seer,
                                        24 => AccessLevel.Administrator,
                                        33 => AccessLevel.Developer,
                                        34 => AccessLevel.Owner,
                                        _  => AccessLevel.Player // 20
                                    };

                                    if (newLevel < from.AccessLevel || from.AccessLevel == AccessLevel.Owner)
                                    {
                                        a.AccessLevel = newLevel;

                                        CommandLogging.WriteLine(
                                            from,
                                            $"{from.AccessLevel} {CommandLogging.Format(from)} changing access level of account {a.Username} to {a.AccessLevel}"
                                        );
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Information,
                                                0,
                                                null,
                                                "The access level has been changed.",
                                                m_State
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 25:
                                {
                                    if (m_State is not Account a)
                                    {
                                        break;
                                    }

                                    from.SendGump(
                                        new WarningGump(
                                            1060635,
                                            30720,
                                            $"<center>Account of {a.Username}</center><br>You are about to <em><basefont color=red>permanently delete</basefont></em> the account. Likewise, all characters on the account will be deleted, including equipped, inventory, and banked items. Any houses tied to the account will be demolished.<br><br>Do you wish to continue?",
                                            0xFFC000,
                                            420,
                                            280,
                                            okay => AccountDelete_Callback(from, okay, a)
                                        )
                                    );
                                    break;
                                }
                            case 26: // View all shared accounts
                                {
                                    from.SendGump(new AdminGump(from, AdminGumpPage.Accounts_Shared));
                                    break;
                                }
                            case 27: // Ban marked
                                {
                                    var list = m_List;

                                    if (list == null || m_State is not List<Account> rads)
                                    {
                                        break;
                                    }

                                    if (rads.Count > 0)
                                    {
                                        from.SendGump(
                                            new WarningGump(
                                                1060635,
                                                30720,
                                                $"You are about to ban {rads.Count} marked account{(rads.Count == 1 ? "" : "s")}. Be cautioned, the only way to reverse this is by hand--manually unbanning each account.<br><br>Do you wish to continue?",
                                                0xFFC000,
                                                420,
                                                280,
                                                okay => Marked_Callback(from, okay, true, list, rads, m_ListPage)
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new NoticeGump(
                                                1060637,
                                                30720,
                                                "You have not yet marked any accounts. Place a check mark next to the accounts you wish to ban and then try again.",
                                                0xFFC000,
                                                420,
                                                280,
                                                () => ResendGump_Callback(from, list, rads, m_ListPage)
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 28: // Delete marked
                                {
                                    var list = m_List;

                                    if (list == null || m_State is not List<Account> rads)
                                    {
                                        break;
                                    }

                                    if (rads.Count > 0)
                                    {
                                        from.SendGump(
                                            new WarningGump(
                                                1060635,
                                                30720,
                                                string.Format(
                                                    "You are about to <em><basefont color=red>permanently delete</basefont></em> {0} marked account{1}. Likewise, all characters on the account{1} will be deleted, including equipped, inventory, and banked items. Any houses tied to the account{1} will be demolished.<br><br>Do you wish to continue?",
                                                    rads.Count,
                                                    rads.Count == 1 ? "" : "s"
                                                ),
                                                0xFFC000,
                                                420,
                                                280,
                                                okay => Marked_Callback(from, okay, false, list, rads, m_ListPage)
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new NoticeGump(
                                                1060637,
                                                30720,
                                                "You have not yet marked any accounts. Place a check mark next to the accounts you wish to ban and then try again.",
                                                0xFFC000,
                                                420,
                                                280,
                                                () => ResendGump_Callback(from, list, rads, m_ListPage)
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 29: // Mark all
                                {
                                    if (m_List == null || m_State is not List<object>)
                                    {
                                        break;
                                    }

                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.Accounts,
                                            m_ListPage,
                                            m_List,
                                            null,
                                            m_List.ToList()
                                        )
                                    );

                                    break;
                                }
                            case 30: // View all empty accounts
                                {
                                    var results = new List<object>();

                                    foreach (Account acct in Accounts.GetAccounts())
                                    {
                                        var empty = true;

                                        for (var i = 0; empty && i < acct.Length; ++i)
                                        {
                                            empty = acct[i] == null;
                                        }

                                        if (empty)
                                        {
                                            results.Add(acct);
                                        }
                                    }

                                    if (results.Count == 1)
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Information,
                                                0,
                                                null,
                                                "One match found.",
                                                results[0]
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.Accounts,
                                                0,
                                                results,
                                                results.Count == 0 ? "Nothing matched your search terms." : null,
                                                new List<object>()
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 31: // View all inactive accounts
                                {
                                    var results = new List<object>();

                                    foreach (Account acct in Accounts.GetAccounts())
                                    {
                                        if (acct.Inactive)
                                        {
                                            results.Add(acct);
                                        }
                                    }

                                    if (results.Count == 1)
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Information,
                                                0,
                                                null,
                                                "One match found.",
                                                results[0]
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.Accounts,
                                                0,
                                                results,
                                                results.Count == 0 ? "Nothing matched your search terms." : null,
                                                new List<object>()
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 32: // View all banned accounts
                                {
                                    var results = new List<object>();

                                    foreach (Account acct in Accounts.GetAccounts())
                                    {
                                        if (acct.Banned)
                                        {
                                            results.Add(acct);
                                        }
                                    }

                                    if (results.Count == 1)
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Information,
                                                0,
                                                null,
                                                "One match found.",
                                                results[0]
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.Accounts,
                                                0,
                                                results,
                                                results.Count == 0 ? "Nothing matched your search terms." : null,
                                                new List<object>()
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 33: // Change access level (extended)
                            case 34:
                                {
                                    goto case 20;
                                }
                            case 35: // Unmark house owners
                                {
                                    var list = m_List;
                                    var rads = m_State as List<Account>;

                                    if (list == null || rads == null)
                                    {
                                        break;
                                    }

                                    var newRads = new List<Account>();

                                    foreach (var acct in rads)
                                    {
                                        var hasHouse = false;

                                        for (var i = 0; i < acct.Length && !hasHouse; ++i)
                                        {
                                            if (acct[i] != null && BaseHouse.HasHouse(acct[i]))
                                            {
                                                hasHouse = true;
                                            }
                                        }

                                        if (!hasHouse)
                                        {
                                            newRads.Add(acct);
                                        }
                                    }

                                    from.SendGump(
                                        new AdminGump(from, AdminGumpPage.Accounts, m_ListPage, m_List, null, newRads)
                                    );

                                    break;
                                }
                            case 36: // Clear login addresses
                                {
                                    if (m_State is not Account a)
                                    {
                                        break;
                                    }

                                    var ips = a.LoginIPs;

                                    if (ips.Length == 0)
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.AccountDetails_Access_ClientIPs,
                                                0,
                                                null,
                                                "This account has not yet been accessed.",
                                                m_State
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new WarningGump(
                                                1060635,
                                                30720,
                                                $"You are about to clear the address list for account {a} containing {ips.Length} {(ips.Length == 1 ? "entry" : "entries")}. Do you wish to continue?",
                                                0xFFC000,
                                                420,
                                                280,
                                                okay => RemoveLoginIPs_Callback(from, okay, a)
                                            )
                                        );
                                    }

                                    break;
                                }
                            default:
                                {
                                    index -= 50;

                                    if (m_State is Account a && index >= 0 && index < a.Length)
                                    {
                                        var m = a[index];

                                        if (m != null)
                                        {
                                            from.SendGump(new AdminGump(from, AdminGumpPage.ClientInfo, 0, null, null, m));
                                        }
                                    }
                                    else
                                    {
                                        index -= 6;

                                        if (m_List != null && index >= 0 && index < m_List.Count)
                                        {
                                            if (m_List[index] is Account)
                                            {
                                                from.SendGump(
                                                    new AdminGump(
                                                        from,
                                                        AdminGumpPage.AccountDetails_Information,
                                                        0,
                                                        null,
                                                        null,
                                                        m_List[index]
                                                    )
                                                );
                                            }
                                            else if (m_List[index] is KeyValuePair<IPAddress, List<Account>> kvp)
                                            {
                                                from.SendGump(
                                                    new AdminGump(
                                                        from,
                                                        AdminGumpPage.Accounts,
                                                        0,
                                                        kvp.Value.ToList<object>(),
                                                        null,
                                                        new List<object>()
                                                    )
                                                );
                                            }
                                        }
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 6:
                    {
                        switch (index)
                        {
                            case 0:
                                {
                                    var matchEntry = info.GetTextEntry(0);
                                    var match = matchEntry?.Text.Trim();

                                    string notice = null;
                                    var results = new List<object>();

                                    if (string.IsNullOrEmpty(match))
                                    {
                                        notice = "You must enter a username to search.";
                                    }
                                    else
                                    {
                                        foreach (var check in Firewall.Set)
                                        {
                                            var checkStr = check.ToString();

                                            if (checkStr.ContainsOrdinal(match))
                                            {
                                                results.Add(check);
                                            }
                                        }
                                    }

                                    if (results.Count == 1)
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.FirewallInfo,
                                                0,
                                                null,
                                                "One match found.",
                                                results[0]
                                            )
                                        );
                                    }
                                    else if (results.Count > 1)
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.Firewall,
                                                0,
                                                results,
                                                $"Search results for : {match}",
                                                m_State
                                            )
                                        );
                                    }
                                    else
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                m_PageType,
                                                m_ListPage,
                                                m_List,
                                                notice ?? "Nothing matched your search terms.",
                                                m_State
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 1:
                                {
                                    var relay = info.GetTextEntry(0);
                                    var text = relay?.Text.Trim();

                                    if (string.IsNullOrEmpty(text))
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                m_PageType,
                                                m_ListPage,
                                                m_List,
                                                "You must enter an address or pattern to add.",
                                                m_State
                                            )
                                        );
                                    }
                                    else if (!Utility.IsValidIP(text))
                                    {
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                m_PageType,
                                                m_ListPage,
                                                m_List,
                                                "That is not a valid address or pattern.",
                                                m_State
                                            )
                                        );
                                    }
                                    else
                                    {
                                        object toAdd = Firewall.ToFirewallEntry(text);

                                        CommandLogging.WriteLine(
                                            from,
                                            $"{from.AccessLevel} {CommandLogging.Format(from)} firewalling {toAdd}"
                                        );

                                        Firewall.Add(toAdd);
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.FirewallInfo,
                                                0,
                                                null,
                                                $"{toAdd} : Added to firewall.",
                                                toAdd
                                            )
                                        );
                                    }

                                    break;
                                }
                            case 2:
                                {
                                    InvokeCommand("Firewall");
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            m_PageType,
                                            m_ListPage,
                                            m_List,
                                            "Target the player to firewall.",
                                            m_State
                                        )
                                    );
                                    break;
                                }
                            case 3:
                                {
                                    if (m_State is Firewall.IFirewallEntry)
                                    {
                                        CommandLogging.WriteLine(
                                            from,
                                            $"{from.AccessLevel} {CommandLogging.Format(from)} removing {m_State} from firewall list"
                                        );

                                        Firewall.Remove(m_State);
                                        from.SendGump(
                                            new AdminGump(
                                                from,
                                                AdminGumpPage.Firewall,
                                                0,
                                                null,
                                                $"{m_State} : Removed from firewall."
                                            )
                                        );
                                    }

                                    break;
                                }
                            default:
                                {
                                    index -= 4;

                                    if (index < m_List?.Count)
                                    {
                                        from.SendGump(
                                            new AdminGump(from, AdminGumpPage.FirewallInfo, 0, null, null, m_List[index])
                                        );
                                    }

                                    break;
                                }
                        }

                        break;
                    }
                case 7:
                    {
                        if (m_State is not Mobile m ||
                            m.AccessLevel > from.AccessLevel || m.Account?.AccessLevel > from.AccessLevel)
                        {
                            break;
                        }

                        string notice = null;
                        var sendGump = true;

                        switch (index)
                        {
                            case 0:
                                {
                                    var map = m.Map;
                                    var loc = m.Location;

                                    if (map == null || map == Map.Internal)
                                    {
                                        map = m.LogoutMap;
                                        loc = m.LogoutLocation;
                                    }

                                    if (map != null && map != Map.Internal)
                                    {
                                        from.MoveToWorld(loc, map);
                                        notice = "You have been teleported to their location.";
                                    }

                                    break;
                                }
                            case 1:
                                {
                                    m.MoveToWorld(from.Location, from.Map);
                                    notice = "They have been teleported to your location.";
                                    break;
                                }
                            case 2:
                                {
                                    var ns = m.NetState;

                                    if (ns != null)
                                    {
                                        CommandLogging.WriteLine(
                                            from,
                                            $"{from.AccessLevel} {CommandLogging.Format(from)} kicking {CommandLogging.Format(m)}"
                                        );
                                        ns.Disconnect($"Kicked by {from}.");
                                        notice = "They have been kicked.";
                                    }
                                    else
                                    {
                                        notice = "They are already disconnected.";
                                    }

                                    break;
                                }
                            case 3:
                                {
                                    if (m.Account is Account a)
                                    {
                                        CommandLogging.WriteLine(
                                            from,
                                            $"{from.AccessLevel} {CommandLogging.Format(from)} banning {CommandLogging.Format(m)}"
                                        );
                                        a.Banned = true;

                                        var ns = m.NetState;

                                        ns?.Disconnect($"Banned by {from}.");

                                        notice = "They have been banned.";
                                    }

                                    break;
                                }
                            case 6:
                                {
                                    Properties.SetValue(from, m, "Blessed", "False");
                                    notice = "They are now mortal.";
                                    break;
                                }
                            case 7:
                                {
                                    Properties.SetValue(from, m, "Blessed", "True");
                                    notice = "They are now immortal.";
                                    break;
                                }
                            case 8:
                                {
                                    Properties.SetValue(from, m, "Squelched", "True");
                                    notice = "They are now squelched.";
                                    break;
                                }
                            case 9:
                                {
                                    Properties.SetValue(from, m, "Squelched", "False");
                                    notice = "They are now unsquelched.";
                                    break;
                                }
                            case 10:
                                {
                                    Properties.SetValue(from, m, "Hidden", "True");
                                    notice = "They are now hidden.";
                                    break;
                                }
                            case 11:
                                {
                                    Properties.SetValue(from, m, "Hidden", "False");
                                    notice = "They are now unhidden.";
                                    break;
                                }
                            case 12:
                                {
                                    CommandLogging.WriteLine(
                                        from,
                                        $"{from.AccessLevel} {CommandLogging.Format(from)} killing {CommandLogging.Format(m)}"
                                    );
                                    m.Kill();
                                    notice = "They have been killed.";
                                    break;
                                }
                            case 13:
                                {
                                    CommandLogging.WriteLine(
                                        from,
                                        $"{from.AccessLevel} {CommandLogging.Format(from)} resurrecting {CommandLogging.Format(m)}"
                                    );
                                    m.Resurrect();
                                    notice = "They have been resurrected.";
                                    break;
                                }
                            case 14:
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_Information,
                                            0,
                                            null,
                                            null,
                                            m.Account
                                        )
                                    );
                                    sendGump = false;
                                    break;
                                }
                        }

                        if (sendGump)
                        {
                            from.SendGump(new AdminGump(from, AdminGumpPage.ClientInfo, 0, null, notice, m_State));
                        }

                        switch (index)
                        {
                            case 3:
                                {
                                    if (m.Account is Account a)
                                    {
                                        from.SendGump(new BanDurationGump(a));
                                    }

                                    break;
                                }
                            case 4:
                                {
                                    from.SendGump(new PropertiesGump(from, m));
                                    break;
                                }
                            case 5:
                                {
                                    from.SendGump(new SkillsGump(from, m));
                                    break;
                                }
                        }

                        break;
                    }
                case 8:
                    {
                        if (index < m_List?.Count)
                        {
                            if (m_State is not Account a)
                            {
                                break;
                            }

                            if (m_PageType == AdminGumpPage.AccountDetails_Access_ClientIPs)
                            {
                                from.SendGump(
                                    new WarningGump(
                                        1060635,
                                        30720,
                                        $"You are about to firewall {m_List[index]}. All connection attempts from a matching IP will be refused. Are you sure?",
                                        0xFFC000,
                                        420,
                                        280,
                                        okay => Firewall_Callback(from, okay, a, m_List[index])
                                    )
                                );
                            }
                            else if (m_PageType == AdminGumpPage.AccountDetails_Access_Restrictions)
                            {
                                var list = a.IpRestrictions.ToList();
                                list.Remove(m_List[index] as string);
                                a.IpRestrictions = list.ToArray();

                                from.SendGump(
                                    new AdminGump(
                                        from,
                                        AdminGumpPage.AccountDetails_Access_Restrictions,
                                        0,
                                        null,
                                        $"{m_List[index]} : Removed from list.",
                                        a
                                    )
                                );
                            }
                        }

                        break;
                    }
                case 9:
                    {
                        if (index < m_List?.Count)
                        {
                            if (m_PageType == AdminGumpPage.AccountDetails_Access_ClientIPs)
                            {
                                var obj = m_List[index];

                                if (obj is not IPAddress ip)
                                {
                                    break;
                                }

                                if (m_State is not Account a)
                                {
                                    break;
                                }

                                var list = GetSharedAccounts(ip);

                                if (list.Count > 1 || list.Count == 1 && !list.Contains(a))
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.Accounts,
                                            0,
                                            list.ToList<object>(),
                                            null,
                                            new List<object>()
                                        )
                                    );
                                }
                                else
                                {
                                    from.SendGump(
                                        new AdminGump(
                                            from,
                                            AdminGumpPage.AccountDetails_Access_ClientIPs,
                                            0,
                                            null,
                                            "There are no other accounts which share that address.",
                                            a
                                        )
                                    );
                                }
                            }
                        }

                        break;
                    }
                case 10:
                    {
                        if (index < m_List?.Count)
                        {
                            if (m_PageType == AdminGumpPage.AccountDetails_Access_ClientIPs)
                            {
                                var ip = m_List[index] as IPAddress;

                                if (ip == null)
                                {
                                    break;
                                }

                                if (m_State is not Account a)
                                {
                                    break;
                                }

                                from.SendGump(
                                    new WarningGump(
                                        1060635,
                                        30720,
                                        $"You are about to remove address {ip} from account {a}. Do you wish to continue?",
                                        0xFFC000,
                                        420,
                                        280,
                                        okay => RemoveLoginIP_Callback(from, okay, a, ip)
                                    )
                                );
                            }
                        }

                        break;
                    }
            }
        }

        private void Shutdown(bool restart, bool save)
        {
            CommandLogging.WriteLine(
                m_From,
                $"{m_From.AccessLevel} {CommandLogging.Format(m_From)} shutting down server (Restart: {restart}) (Save: {save})"
            );

            if (save)
            {
                InvokeCommand("Save");
            }

            Core.Kill(restart);
        }

        private void InvokeCommand(string c)
        {
            CommandSystem.Handle(m_From, $"{CommandSystem.Prefix}{c}");
        }

        public static void GetAccountInfo(IAccount a, out AccessLevel accessLevel, out bool online)
        {
            accessLevel = a.AccessLevel;
            online = false;

            for (var j = 0; j < a.Length; ++j)
            {
                var check = a[j];

                if (check == null)
                {
                    continue;
                }

                if (check.AccessLevel > accessLevel)
                {
                    accessLevel = check.AccessLevel;
                }

                if (check.NetState != null)
                {
                    online = true;
                }
            }
        }

        private static void FilterAccess(List<object> list, Mobile from)
        {
            if (list == null || list.Count == 0)
            {
                return;
            }

            for (var i = list.Count - 1; i >= 0; i--)
            {
                var obj = list[i];

                if (obj is Account acc && acc.AccessLevel > from.AccessLevel ||
                    obj is Mobile mob && mob.AccessLevel > from.AccessLevel ||
                    obj is NetState ns &&
                    (ns.Mobile?.AccessLevel > from.AccessLevel || ns.Account?.AccessLevel > from.AccessLevel))
                {
                    list.RemoveAt(i);
                }
            }
        }

        private class SharedAccountDescendingComparer : IComparer<object>
        {
            public static readonly IComparer<object> Instance = new SharedAccountDescendingComparer();

            public int Compare(object x, object y)
            {
                if (x is not KeyValuePair<IPAddress, List<Account>> a)
                {
                    return -1;
                }

                if (y is not KeyValuePair<IPAddress, List<Account>> b)
                {
                    return 1;
                }

                return a.Value.Count - b.Value.Count;
            }
        }

        private class AddCommentPrompt : Prompt
        {
            private readonly Account m_Account;

            public AddCommentPrompt(Account acct) => m_Account = acct;

            public override void OnCancel(Mobile from)
            {
                from.SendGump(
                    new AdminGump(
                        from,
                        AdminGumpPage.AccountDetails_Comments,
                        0,
                        null,
                        "Request to add comment was canceled.",
                        m_Account
                    )
                );
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Account != null)
                {
                    m_Account.Comments.Add(new AccountComment(from.RawName, text));
                    from.SendGump(
                        new AdminGump(
                            from,
                            AdminGumpPage.AccountDetails_Comments,
                            0,
                            null,
                            "Comment added.",
                            m_Account
                        )
                    );
                }
            }
        }

        private class AddTagNamePrompt : Prompt
        {
            private readonly Account m_Account;

            public AddTagNamePrompt(Account acct) => m_Account = acct;

            public override void OnCancel(Mobile from)
            {
                from.SendGump(
                    new AdminGump(
                        from,
                        AdminGumpPage.AccountDetails_Tags,
                        0,
                        null,
                        "Request to add tag was canceled.",
                        m_Account
                    )
                );
            }

            public override void OnResponse(Mobile from, string text)
            {
                from.Prompt = new AddTagValuePrompt(m_Account, text);
                from.SendMessage("Enter the new tag value.");
            }
        }

        private class AddTagValuePrompt : Prompt
        {
            private readonly Account m_Account;
            private readonly string m_Name;

            public AddTagValuePrompt(Account acct, string name)
            {
                m_Account = acct;
                m_Name = name;
            }

            public override void OnCancel(Mobile from)
            {
                from.SendGump(
                    new AdminGump(
                        from,
                        AdminGumpPage.AccountDetails_Tags,
                        0,
                        null,
                        "Request to add tag was canceled.",
                        m_Account
                    )
                );
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Account != null)
                {
                    m_Account.AddTag(m_Name, text);
                    from.SendGump(new AdminGump(from, AdminGumpPage.AccountDetails_Tags, 0, null, "Tag added.", m_Account));
                }
            }
        }

        private class NetStateComparer : IComparer<NetState>
        {
            public static readonly IComparer<NetState> Instance = new NetStateComparer();

            public int Compare(NetState x, NetState y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                var aMob = x.Mobile;
                var bMob = y.Mobile;

                if (aMob == null && bMob == null)
                {
                    return 0;
                }

                if (aMob == null)
                {
                    return 1;
                }

                if (bMob == null)
                {
                    return -1;
                }

                if (aMob.AccessLevel > bMob.AccessLevel)
                {
                    return -1;
                }

                return aMob.AccessLevel < bMob.AccessLevel ? 1 : aMob.Name.InsensitiveCompare(bMob.Name);
            }
        }

        private class AccountComparer : IComparer<IAccount>
        {
            public static readonly IComparer<IAccount> Instance = new AccountComparer();

            public int Compare(IAccount x, IAccount y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return -1;
                }

                if (y == null)
                {
                    return 1;
                }

                GetAccountInfo(x, out var aLevel, out var aOnline);
                GetAccountInfo(y, out var bLevel, out var bOnline);

                if (aOnline && !bOnline)
                {
                    return -1;
                }

                if (bOnline && !aOnline)
                {
                    return 1;
                }

                if (aLevel > bLevel)
                {
                    return -1;
                }

                return aLevel < bLevel ? 1 : x.Username.InsensitiveCompare(y.Username);
            }
        }
    }
}
