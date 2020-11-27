using System;
using Server.Items;
using Server.Network;

namespace Server.Gumps
{
    public class AddDoorGump : Gump
    {
        public static DoorInfo[] m_Types =
        {
            new(typeof(MetalDoor), 0x675),
            new(typeof(RattanDoor), 0x695),
            new(typeof(DarkWoodDoor), 0x6A5),
            new(typeof(LightWoodDoor), 0x6D5),
            new(typeof(StrongWoodDoor), 0x6E5)
        };

        private readonly int m_Type;

        public AddDoorGump(int type = -1) : base(50, 40)
        {
            m_Type = type;

            AddPage(0);

            if (m_Type >= 0 && m_Type < m_Types.Length)
            {
                AddBlueBack(155, 174);

                var baseID = m_Types[m_Type].m_BaseID;

                AddItem(25, 24, baseID);
                AddButton(26, 37, 0x5782, 0x5782, 1);

                AddItem(47, 45, baseID + 2);
                AddButton(43, 57, 0x5783, 0x5783, 2);

                AddItem(87, 22, baseID + 10);
                AddButton(116, 35, 0x5785, 0x5785, 6);

                AddItem(65, 45, baseID + 8);
                AddButton(96, 55, 0x5784, 0x5784, 5);

                AddButton(73, 36, 0x2716, 0x2716, 9);
            }
            else
            {
                AddBlueBack(265, 145);

                for (var i = 0; i < m_Types.Length; ++i)
                {
                    AddButton(30 + i * 49, 13, 0x2624, 0x2625, i + 1);
                    AddItem(22 + i * 49, 20, m_Types[i].m_BaseID);
                }
            }
        }

        public void AddBlueBack(int width, int height)
        {
            AddBackground(0, 0, width - 00, height - 00, 0xE10);
            AddBackground(8, 5, width - 16, height - 11, 0x053);
            AddImageTiled(15, 14, width - 29, height - 29, 0xE14);
            AddAlphaRegion(15, 14, width - 29, height - 29);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var from = sender.Mobile;
            var button = info.ButtonID - 1;

            if (m_Type == -1)
            {
                if (button >= 0 && button < m_Types.Length)
                {
                    from.SendGump(new AddDoorGump(button));
                }
            }
            else
            {
                if (button >= 0 && button < 8)
                {
                    from.SendGump(new AddDoorGump(m_Type));
                    CommandSystem.Handle(
                        from,
                        $"{CommandSystem.Prefix}Add {m_Types[m_Type].m_Type.Name} {(DoorFacing)button}"
                    );
                }
                else if (button == 8)
                {
                    from.SendGump(new AddDoorGump(m_Type));
                    CommandSystem.Handle(from, $"{CommandSystem.Prefix}Link");
                }
                else
                {
                    from.SendGump(new AddDoorGump());
                }
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("AddDoor", AccessLevel.GameMaster, AddDoor_OnCommand);
        }

        [Usage("AddDoor"), Description("Displays a menu from which you can interactively add doors.")]
        public static void AddDoor_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendGump(new AddDoorGump());
        }
    }

    public class DoorInfo
    {
        public int m_BaseID;
        public Type m_Type;

        public DoorInfo(Type type, int baseID)
        {
            m_Type = type;
            m_BaseID = baseID;
        }
    }
}
