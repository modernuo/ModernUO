using System;
using System.Runtime.CompilerServices;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.Virtues;

public enum JusticeProtectorStatus : byte
{
    None,
    Protector,
    Protected
}

#pragma warning disable CA1052 // Cannot be static because its used as a generic for CanBeginAction.
public class JusticeVirtue
{
    private const int LossAmount = 950;
    private static readonly TimeSpan LossDelay = TimeSpan.FromDays(7.0);

    public static void AddProtection(PlayerMobile protector, PlayerMobile protectee)
    {
        var virtues = protectee.Virtues;
        virtues.JusticeStatus = JusticeProtectorStatus.Protected;
        virtues.JusticeProtection = protector;

        virtues = protector.Virtues;
        virtues.JusticeStatus = JusticeProtectorStatus.Protector;
        virtues.JusticeProtection = protectee;
    }

    public static bool IsProtected(PlayerMobile pm) =>
        VirtueSystem.GetVirtues(pm) is { JusticeStatus: JusticeProtectorStatus.Protected, JusticeProtection: not null };

    public static PlayerMobile GetProtector(PlayerMobile pm) =>
        VirtueSystem.GetVirtues(pm) is { JusticeStatus: JusticeProtectorStatus.Protected } virtues ? virtues.JusticeProtection : null;

    public static PlayerMobile GetProtected(PlayerMobile pm) =>
        VirtueSystem.GetVirtues(pm) is { JusticeStatus: JusticeProtectorStatus.Protector } virtues ? virtues.JusticeProtection : null;

    public static void CancelProtection(PlayerMobile pm)
    {
        if (VirtueSystem.GetVirtues(pm) is { JusticeStatus: not JusticeProtectorStatus.None } virtues)
        {
            var protector = virtues.JusticeProtection;
            virtues.JusticeProtection = null;
            virtues.JusticeStatus = JusticeProtectorStatus.None;

            virtues = VirtueSystem.GetVirtues(protector);
            if (virtues != null)
            {
                virtues.JusticeProtection = null;
                virtues.JusticeStatus = JusticeProtectorStatus.None;
            }
        }
    }

    public static bool CancelProtection(PlayerMobile pm, out PlayerMobile protector)
    {
        if (VirtueSystem.GetVirtues(pm) is { JusticeStatus: not JusticeProtectorStatus.None } virtues)
        {
            protector = virtues.JusticeProtection;
            virtues.JusticeProtection = null;
            virtues.JusticeStatus = JusticeProtectorStatus.None;

            virtues = VirtueSystem.GetVirtues(protector);
            if (virtues != null)
            {
                virtues.JusticeProtection = null;
                virtues.JusticeStatus = JusticeProtectorStatus.None;
            }

            return true;
        }

        protector = null;
        return false;
    }

    public static void Initialize()
    {
        VirtueGump.Register(109, OnVirtueUsed);
        EventSink.PlayerDeleted += OnPlayerDeleted;
    }

    private static void OnPlayerDeleted(Mobile m)
    {
        if (m is PlayerMobile pm)
        {
            CancelProtection(pm);
        }
    }

    public static bool CheckMapRegion(Mobile first, Mobile second)
    {
        var map = first.Map;

        if (second.Map != map)
        {
            return false;
        }

        return GetMapRegion(map, first.Location) == GetMapRegion(map, second.Location);
    }

    public static int GetMapRegion(Map map, Point3D loc) =>
        map is not { MapID: < 2 } ? 0 :
        loc.X < 5120 ? 0 :
        loc.Y < 2304 ? 1 : 2;

    public static void OnVirtueUsed(Mobile from)
    {
        if (!from.CheckAlive())
        {
            return;
        }

        if (from is not PlayerMobile protector)
        {
            return;
        }

        if (!VirtueSystem.IsSeeker(protector, VirtueName.Justice))
        {
            protector.SendLocalizedMessage(1049610); // You must reach the first path in this virtue to invoke it.
        }
        else if (!protector.CanBeginAction<JusticeVirtue>())
        {
            protector.SendLocalizedMessage(1049370); // You must wait a while before offering your protection again.
        }
        else if (IsProtected(protector))
        {
            protector.SendLocalizedMessage(1049542); // You cannot protect someone while being protected.
        }
        else if (protector.Map != Map.Felucca)
        {
            protector.SendLocalizedMessage(1049372); // You cannot use this ability here.
        }
        else
        {
            protector.BeginTarget(14, false, TargetFlags.None, OnVirtueTargeted);
            protector.SendLocalizedMessage(1049366); // Choose the player you wish to protect.
        }
    }

    public static void OnVirtueTargeted(Mobile from, object obj)
    {
        if (from is not PlayerMobile protector)
        {
            return;
        }

        var pm = obj as PlayerMobile;

        if (!VirtueSystem.IsSeeker(protector, VirtueName.Justice))
        {
            protector.SendLocalizedMessage(1049610); // You must reach the first path in this virtue to invoke it.
        }
        else if (!protector.CanBeginAction<JusticeVirtue>())
        {
            protector.SendLocalizedMessage(1049370); // You must wait a while before offering your protection again.
        }
        else if (IsProtected(protector))
        {
            protector.SendLocalizedMessage(1049542); // You cannot protect someone while being protected.
        }
        else if (protector.Map != Map.Felucca)
        {
            protector.SendLocalizedMessage(1049372); // You cannot use this ability here.
        }
        else if (pm == null)
        {
            protector.SendLocalizedMessage(1049678); // Only players can be protected.
        }
        else if (pm.Map != Map.Felucca)
        {
            protector.SendLocalizedMessage(1049372); // You cannot use this ability here.
        }
        else if (pm == protector || pm.Criminal || pm.Kills >= 5)
        {
            protector.SendLocalizedMessage(1049436); // That player cannot be protected.
        }
        else if (IsProtected(pm))
        {
            protector.SendLocalizedMessage(1049369); // You cannot protect that player right now.
        }
        else if (pm.HasGump<AcceptProtectorGump>())
        {
            protector.SendLocalizedMessage(1049369); // You cannot protect that player right now.
        }
        else
        {
            pm.SendGump(new AcceptProtectorGump(protector, pm));
        }
    }

    public static void OnVirtueAccepted(PlayerMobile protector, PlayerMobile protectee)
    {
        if (!VirtueSystem.IsSeeker(protector, VirtueName.Justice))
        {
            protector.SendLocalizedMessage(1049610); // You must reach the first path in this virtue to invoke it.
        }
        else if (!protector.CanBeginAction<JusticeVirtue>())
        {
            protector.SendLocalizedMessage(1049370); // You must wait a while before offering your protection again.
        }
        else if (IsProtected(protector))
        {
            protector.SendLocalizedMessage(1049542); // You cannot protect someone while being protected.
        }
        else if (protector.Map != Map.Felucca)
        {
            protector.SendLocalizedMessage(1049372); // You cannot use this ability here.
        }
        else if (protectee.Map != Map.Felucca)
        {
            protector.SendLocalizedMessage(1049372); // You cannot use this ability here.
        }
        else if (protectee == protector || protectee.Criminal || protectee.Kills >= 5)
        {
            protector.SendLocalizedMessage(1049436); // That player cannot be protected.
        }
        else if (IsProtected(protectee))
        {
            protector.SendLocalizedMessage(1049369); // You cannot protect that player right now.
        }
        else
        {
            AddProtection(protectee, protector);

            var args = $"{protector.Name}\t{protectee.Name}";

            protectee.SendLocalizedMessage(1049451, args); // You are now being protected by ~1_NAME~.
            protector.SendLocalizedMessage(1049452, args); // You are now protecting ~2_NAME~.
        }
    }

    public static void OnVirtueRejected(PlayerMobile protector, PlayerMobile protectee)
    {
        var args = $"{protector.Name}\t{protectee.Name}";

        protectee.SendLocalizedMessage(1049453, args); // You have declined protection from ~1_NAME~.
        protector.SendLocalizedMessage(1049454, args); // ~2_NAME~ has declined your protection.

        if (protector.BeginAction<JusticeVirtue>())
        {
            Timer.StartTimer(TimeSpan.FromMinutes(15.0), protector.EndAction<JusticeVirtue>);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanAtrophy(VirtueContext context) => context.LastJusticeLoss + LossDelay < Core.Now;

    public static void CheckAtrophy(PlayerMobile pm)
    {
        var virtues = VirtueSystem.GetVirtues(pm);
        if (virtues?.Justice > 0 && CanAtrophy(virtues))
        {
            if (VirtueSystem.Atrophy(pm, VirtueName.Justice, LossAmount))
            {
                pm.SendLocalizedMessage(1049373); // You have lost some Justice.
            }

            virtues.LastJusticeLoss = Core.Now;
        }
    }
}

public class AcceptProtectorGump : Gump
{
    private readonly PlayerMobile _protectee;
    private readonly PlayerMobile _protector;

    public AcceptProtectorGump(PlayerMobile protector, PlayerMobile protectee) : base(150, 50)
    {
        _protector = protector;
        _protectee = protectee;

        Closable = false;

        AddPage(0);

        AddBackground(0, 0, 396, 218, 3600);

        AddImageTiled(15, 15, 365, 190, 2624);
        AddAlphaRegion(15, 15, 365, 190);

        // Another player is offering you their <a href="?ForceTopic88">protection</a>:
        AddHtmlLocalized(30, 20, 360, 25, 1049365, 0x7FFF);
        AddLabel(90, 55, 1153, protector.Name);

        AddImage(50, 45, 9005);
        AddImageTiled(80, 80, 200, 1, 9107);
        AddImageTiled(95, 82, 200, 1, 9157);

        AddRadio(30, 110, 9727, 9730, true, 1);
        AddHtmlLocalized(65, 115, 300, 25, 1049444, 0x7FFF); // Yes, I would like their protection.

        AddRadio(30, 145, 9727, 9730, false, 0);
        AddHtmlLocalized(65, 148, 300, 25, 1049445, 0x7FFF); // No thanks, I can take care of myself.

        AddButton(160, 175, 247, 248, 2);

        AddImage(215, 0, 50581);

        AddImageTiled(15, 14, 365, 1, 9107);
        AddImageTiled(380, 14, 1, 190, 9105);
        AddImageTiled(15, 205, 365, 1, 9107);
        AddImageTiled(15, 14, 1, 190, 9105);
        AddImageTiled(0, 0, 395, 1, 9157);
        AddImageTiled(394, 0, 1, 217, 9155);
        AddImageTiled(0, 216, 395, 1, 9157);
        AddImageTiled(0, 0, 1, 217, 9155);
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
        if (info.ButtonID == 2)
        {
            var okay = info.IsSwitched(1);

            if (okay)
            {
                JusticeVirtue.OnVirtueAccepted(_protector, _protectee);
            }
            else
            {
                JusticeVirtue.OnVirtueRejected(_protector, _protectee);
            }
        }
    }
}
#pragma warning restore CA1052
