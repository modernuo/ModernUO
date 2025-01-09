using System;
using Server.Items;
using Server.Network;

namespace Server.Gumps;

public class AddDoorGump : DynamicGump
{
    public static void Configure()
    {
        CommandSystem.Register("AddDoor", AccessLevel.GameMaster, AddDoor_OnCommand);
    }

    [Usage("AddDoor"), Description("Displays a menu from which you can interactively add doors.")]
    public static void AddDoor_OnCommand(CommandEventArgs e)
    {
        e.Mobile.SendGump(new AddDoorGump());
    }

    private static readonly (Type Type, int BaseID)[][] _types =
    [
        // Standard
        [
            (typeof(MetalDoor), 0x675),
            (typeof(RattanDoor), 0x695),
            (typeof(DarkWoodDoor), 0x6A5),
            (typeof(LightWoodDoor), 0x6D5),
            (typeof(StrongWoodDoor), 0x6E5),
            (typeof(BarredMetalDoor2), 0x1FED),
        ],
        // Tall
        [
            (typeof(BarredMetalDoor), 0x685),
            (typeof(MediumWoodDoor), 0x6B5),
            (typeof(MetalDoor2), 0x6C5),
        ],
        // Short
        [
            (typeof(IronGate), 0x824),
            (typeof(IronGateShort), 0x84C),
            (typeof(LightWoodGate), 0x839),
            (typeof(DarkWoodGate), 0x866),
        ],
        // Secret
        [
            (typeof(SecretStoneDoor1), 0xE8),
            (typeof(SecretDungeonDoor), 0x314),
            (typeof(SecretStoneDoor2), 0x324),
            (typeof(SecretWoodenDoor), 0x334),
            (typeof(SecretLightWoodDoor), 0x344),
            (typeof(SecretStoneDoor3), 0x354)
        ]
    ];

    private Type _type;
    private int _baseId;
    private int _page;

    public AddDoorGump() : base(50, 40)
    {
    }

    public override bool Singleton => true;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        if (_type != null)
        {
            AddBlueBack(ref builder, 155, 174);

            builder.AddItem(25, 24, _baseId);
            builder.AddButton(26, 37, 0x5782, 0x5782, 10);

            builder.AddItem(47, 45, _baseId + 2);
            builder.AddButton(43, 57, 0x5783, 0x5783, 11);

            builder.AddItem(87, 22, _baseId + 10);
            builder.AddButton(116, 35, 0x5785, 0x5785, 15);

            builder.AddItem(65, 45, _baseId + 8);
            builder.AddButton(96, 55, 0x5784, 0x5784, 14);

            builder.AddButton(73, 36, 0x2716, 0x2716, 18);
        }
        else
        {
            var pages = _types.Length;
            var types = _types[_page];

            AddBlueBack(ref builder, 20 + (types.Length + 1) * 50, 165);

            builder.AddHtmlLocalized(30, 45, 60, 20, 1043353, 0x7FFF); // Next
            builder.AddHtmlLocalized(30, 85, 60, 20, 1011393, 0x7FFF); // Back

            builder.AddButton(30, 60, 0xFA5, 0xFA7, 1);
            builder.AddButton(30, 100, 0xFAE, 0xFB0, 2);

            for (var i = 0; i < types.Length; i++)
            {
                var x = (i + 1) * 50;

                builder.AddButton(30 + x, 20, 0x2624, 0x2625, _page * types.Length + i + 10);
                builder.AddItem(15 + x, 30, types[i].BaseID);
            }
        }
    }

    private static void AddBlueBack(ref DynamicGumpBuilder builder, int width, int height)
    {
        builder.AddBackground(0, 0, width - 00, height - 00, 0xE10);
        builder.AddBackground(8, 5, width - 16, height - 11, 0x053);
        builder.AddImageTiled(15, 14, width - 29, height - 29, 0xE14);
        builder.AddAlphaRegion(15, 14, width - 29, height - 29);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        var button = info.ButtonID;

        if (button == 0)
        {
            if (_type != null)
            {
                _type = null;
                _baseId = 0;
            }
            else
            {
                return;
            }
        }
        else if (button == 1) // Next
        {
            _page++;
            if (_page >= _types.Length)
            {
                _page = 0;
            }
        }
        else if (button == 2) // Prev
        {
            _page--;
            if (_page < 0)
            {
                _page = _types.Length - 1;
            }
        }
        else
        {
            button -= 10;
            if (_type == null)
            {
                var types = _types[_page];
                var page = Math.DivRem(button, types.Length, out var pos);

                if (page < 0 || pos >= types.Length)
                {
                    return;
                }

                var door = types[pos];
                _type = door.Type;
                _baseId = door.BaseID;
            }
            else if (button is >= 0 and < 8)
            {
                CommandSystem.Handle(
                    from,
                    $"{CommandSystem.Prefix}Add {_type.Name} {(DoorFacing)button}"
                );
            }
            else if (button == 8)
            {
                CommandSystem.Handle(from, $"{CommandSystem.Prefix}Link");
            }
        }

        from.SendGump(this);
    }
}
