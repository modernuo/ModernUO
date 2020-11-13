using System;
using Server.Misc;
using Server.Network;

namespace Server.Gumps
{
    public class ToTAdminGump : Gump
    {
        private static readonly string[] m_ToTInfo =
        {
            // Opening Message
            "<center>Treasures of Tokuno Admin</center><br>" +
            "-Use the gems to switch eras<br>" +
            "-Drop era and Reward era can be changed seperately<br>" +
            "-Drop era can be deactivated, Reward era is always activated",
            // Treasures of Tokuno 1 message
            "<center>Treasures of Tokuno 1</center><br>" +
            "-10 charge Bleach Pigment can drop as a Lesser Artifact<br>" +
            "-50 charge Neon Pigments available as a reward<br>",
            // Treasures of Tokuno 2 message
            "<center>Treasures of Tokuno 2</center><br>" +
            "-30 types of 1 charge Metallic Pigments drop as Lesser Artifacts<br>" +
            "-1 charge Bleach Pigment can drop as a Lesser Artifact<br>" +
            "-10 charge Greater Metallic Pigments available as a reward",
            // Treasures of Tokuno 3 message
            "<center>Treasures of Tokuno 3</center><br>" +
            "-10 types of 1 charge Fresh Pigments drop as Lesser Artifacts<br>" +
            "-1 charge Bleach Pigment can drop as a Lesser Artifact<br>" +
            "-Leurocian's Mempo Of Fortune can drop as a Lesser Artifact"
        };

        private readonly int m_ToTEras;

        public ToTAdminGump() : base(30, 50)
        {
            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            m_ToTEras = Enum.GetValues(typeof(TreasuresOfTokunoEra)).Length - 1;

            AddPage(0);
            AddBackground(0, 0, 320, 75 + m_ToTEras * 25, 9200);
            AddImageTiled(25, 18, 270, 10, 9267);
            AddLabel(75, 5, 54, "Treasures of Tokuno Admin");
            AddLabel(10, 25, 54, "ToT Era");
            AddLabel(90, 25, 54, "Drop Era");
            AddLabel(195, 25, 54, "Reward Era");
            AddLabel(287, 25, 54, "Info");

            AddBackground(320, 0, 200, 150, 9200);
            AddImageTiled(325, 5, 190, 140, 2624);
            AddAlphaRegion(325, 5, 190, 140);

            SetupToTEras();
        }

        public void SetupToTEras()
        {
            var isActivated = TreasuresOfTokuno.DropEra != TreasuresOfTokunoEra.None;
            AddButton(75, 50, isActivated ? 2361 : 2360, isActivated ? 2361 : 2360, 1);
            AddLabel(90, 45, isActivated ? 167 : 137, isActivated ? "Activated" : "Deactivated");

            for (var i = 0; i < m_ToTEras; i++)
            {
                var yoffset = i * 25;

                var isThisDropEra = (int)TreasuresOfTokuno.DropEra - 1 == i;
                var isThisRewardEra = (int)TreasuresOfTokuno.RewardEra - 1 == i;
                var dropButtonID = isThisDropEra ? 2361 : 2360;
                var rewardButtonID = isThisRewardEra ? 2361 : 2360;

                AddLabel(10, 70 + yoffset, 2100, $"ToT {i + 1}");
                AddButton(75, 75 + yoffset, dropButtonID, dropButtonID, 2 + i * 2);
                AddLabel(90, 70 + yoffset, isThisDropEra ? 167 : 137, isThisDropEra ? "Active" : "Inactive");
                AddButton(180, 75 + yoffset, rewardButtonID, rewardButtonID, 2 + i * 2 + 1);
                AddLabel(195, 70 + yoffset, isThisRewardEra ? 167 : 137, isThisRewardEra ? "Active" : "Inactive");

                AddButton(285, 70 + yoffset, 4005, 4006, i, GumpButtonType.Page, 2 + i);
            }

            for (var i = 0; i < m_ToTInfo.Length; i++)
            {
                AddPage(1 + i);
                AddHtml(330, 10, 180, 130, m_ToTInfo[i], false, true);
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            var button = info.ButtonID;
            var from = sender.Mobile;

            if (button == 1)
            {
                TreasuresOfTokuno.DropEra = TreasuresOfTokunoEra.None;
                from.SendMessage("Treasures of Tokuno Drops have been deactivated");
            }
            else if (button >= 2)
            {
                int selectedToT;
                if (button % 2 == 0)
                {
                    selectedToT = button / 2;
                    TreasuresOfTokuno.DropEra = (TreasuresOfTokunoEra)selectedToT;
                    from.SendMessage($"Treasures of Tokuno {selectedToT} Drops have been enabled");
                }
                else
                {
                    selectedToT = (button - 1) / 2;
                    TreasuresOfTokuno.RewardEra = (TreasuresOfTokunoEra)selectedToT;
                    from.SendMessage($"Treasures of Tokuno {selectedToT} Rewards have been enabled");
                }
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("ToTAdmin", AccessLevel.Administrator, ToTAdmin_OnCommand);
        }

        [Usage("ToTAdmin"), Description("Displays a menu to configure Treasures of Tokuno.")]
        public static void ToTAdmin_OnCommand(CommandEventArgs e)
        {
            ToTAdminGump tg;

            tg = new ToTAdminGump();
            e.Mobile.CloseGump<ToTAdminGump>();
            e.Mobile.SendGump(tg);
        }
    }
}
