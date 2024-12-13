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

    private static int _maxCount;

    private Type _type;
    private int _baseId;

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
            builder.AddButton(26, 37, 0x5782, 0x5782, 1);

            builder.AddItem(47, 45, _baseId + 2);
            builder.AddButton(43, 57, 0x5783, 0x5783, 2);

            builder.AddItem(87, 22, _baseId + 10);
            builder.AddButton(116, 35, 0x5785, 0x5785, 6);

            builder.AddItem(65, 45, _baseId + 8);
            builder.AddButton(96, 55, 0x5784, 0x5784, 5);

            builder.AddButton(73, 36, 0x2716, 0x2716, 9);
        }
        else
        {
            var pages = _types.Length;
            if (_maxCount == 0)
            {
                for (var i = 0; i < pages; i++)
                {
                    _maxCount = Math.Max(_maxCount, _types[i].Length);
                }
            }

            AddBlueBack(ref builder, 20 + (_maxCount + 1) * 50, 165);

            builder.AddHtmlLocalized(30, 45, 60, 20, 1043353, 0x7FFF); // Next
            builder.AddHtmlLocalized(30, 85, 60, 20, 1011393, 0x7FFF); // Back

            for (var i = 0; i < pages; ++i)
            {
                var page = i + 1;

                builder.AddPage(page);

                if (page < pages)
                {
                    builder.AddButton(30, 60, 0xFA5, 0xFA7, 0, GumpButtonType.Page, page + 1);
                }
                else
                {
                    builder.AddButton(30, 60, 0xFA5, 0xFA7, 0, GumpButtonType.Page, 1);
                }

                if (page > 1)
                {
                    builder.AddButton(30, 100, 0xFAE, 0xFB0, 0, GumpButtonType.Page, page - 1);
                }
                else
                {
                    builder.AddButton(30, 100, 0xFAE, 0xFB0, 0, GumpButtonType.Page, pages);
                }

                for (var j = 0; j < _types[i].Length; j++)
                {
                    var x = (j + 1) * 50;

                    builder.AddButton(30 + x, 20, 0x2624, 0x2625, i * _maxCount + j + 1);
                    builder.AddItem(15 + x, 30, _types[i][j].BaseID);
                }
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
        var button = info.ButtonID - 1;

        if (_type == null)
        {
            if (button < 0 || button >= _types.Length)
            {
                return;
            }

            var page = Math.DivRem(button, _maxCount, out var pos);
            var door = _types[page][pos];
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
        else
        {
            _type = null;
            _baseId = 0;
        }

        from.SendGump(this);
    }
}
