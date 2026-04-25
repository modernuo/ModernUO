using System.Collections.Generic;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Virtues;

public delegate void OnVirtueUsed(PlayerMobile from);

public class VirtueGump : DynamicGump
{
    private static readonly Dictionary<int, OnVirtueUsed> _callbacks = new();

    private static readonly int[] _table =
    {
        0x0481, 0x0963, 0x0965,
        0x060A, 0x060F, 0x002A,
        0x08A4, 0x08A7, 0x0034,
        0x0965, 0x08FD, 0x0480,
        0x00EA, 0x0845, 0x0020,
        0x0011, 0x0269, 0x013D,
        0x08A1, 0x08A3, 0x0042,
        0x0543, 0x0547, 0x0061
    };

    private readonly PlayerMobile _beheld;
    private readonly PlayerMobile _beholder;

    public override bool Singleton => true;

    private VirtueGump(PlayerMobile beholder, PlayerMobile beheld) : base(0, 0)
    {
        _beholder = beholder;
        _beheld = beheld;

        Serial = beheld.Serial;
    }

    public static void Register(int gumpID, OnVirtueUsed callback)
    {
        _callbacks[gumpID] = callback;
    }

    public static void RequestVirtueGump(PlayerMobile beholder, PlayerMobile beheld)
    {
        if (beholder == beheld && beholder.Murderer)
        {
            beholder.SendLocalizedMessage(1049609); // Murderers cannot invoke this virtue.
        }
        else if (beholder.Map == beheld.Map && beholder.InRange(beheld, 12))
        {
            beholder.SendGump(new VirtueGump(beholder, beheld));
        }
    }

    public static void RequestVirtueItem(PlayerMobile beholder, Mobile beheld, int gumpID)
    {
        if (beholder != beheld)
        {
            return;
        }

        beholder.CloseGump<VirtueGump>();

        if (beholder.Murderer)
        {
            beholder.SendLocalizedMessage(1049609); // Murderers cannot invoke this virtue.
            return;
        }

        if (_callbacks.TryGetValue(gumpID, out var callback))
        {
            callback(beholder);
        }
        else
        {
            beholder.SendLocalizedMessage(1052066); // That virtue is not active yet.
        }
    }

    public static void RequestVirtueMacro(PlayerMobile beholder, int virtue)
    {
        var virtueID = virtue switch
        {
            0 => 107, // Honor
            1 => 110, // Sacrifice
            2 => 112, // Valor;
            _ => 0
        };

        RequestVirtueItem(beholder, beholder, virtueID);
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddImage(30, 40, 104);

        builder.AddPage(1);

        builder.AddImage(61, 71, 108, GetHueFor(0), "VirtueGumpItem");   // Humility
        builder.AddImage(123, 46, 112, GetHueFor(4), "VirtueGumpItem");  // Valor
        builder.AddImage(187, 70, 107, GetHueFor(5), "VirtueGumpItem");  // Honor
        builder.AddImage(35, 135, 110, GetHueFor(1), "VirtueGumpItem");  // Sacrifice
        builder.AddImage(211, 133, 105, GetHueFor(2), "VirtueGumpItem"); // Compassion
        builder.AddImage(61, 195, 111, GetHueFor(3), "VirtueGumpItem");  // Spirituality
        builder.AddImage(186, 195, 109, GetHueFor(6), "VirtueGumpItem"); // Justice
        builder.AddImage(121, 221, 106, GetHueFor(7), "VirtueGumpItem"); // Honesty

        if (_beholder == _beheld)
        {
            builder.AddButton(57, 269, 2027, 2027, 1);
            builder.AddButton(186, 269, 2071, 2071, 2);
        }
    }

    private int GetHueFor(int index)
    {
        var value = VirtueSystem.GetVirtues(_beheld)?.GetValue(index) ?? 0;

        if (value < 4000)
        {
            return 2402;
        }

        if (value >= 30000)
        {
            value = 20000;
        }

        var vl = value switch
        {
            < 10000                  => 0,
            >= 20000 when index == 5 => 2,
            >= 22000 when index == 1 => 2,
            >= 21000 when index != 1 => 2,
            _                        => 1
        };

        return _table[index * 3 + vl];
    }

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 1 && _beholder == _beheld)
        {
            VirtueStatusGump.DisplayTo(_beholder);
        }
    }
}
