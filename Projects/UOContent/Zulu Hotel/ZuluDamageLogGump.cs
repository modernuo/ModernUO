using Server.Engines.MLQuests.Definitions;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Zulu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Zulu_Hotel
{
    public class TargetClasseGump : Gump
    {
        public static void Initialize()
        {
            CommandSystem.Register("TargetClasse", AccessLevel.Player, TargetClassOnCommand);
        }
        public static void TargetClassOnCommand(CommandEventArgs e)
        {
            if (!(e.Mobile is PlayerMobile pm))
                return;

            pm.CloseGump<TargetClasseGump>();
            pm.SendGump(new TargetClasseGump(pm));
        }

        private void AddBackground()
        {
            AddPage(0);

            // Background must be added first
            AddBackground(0, 0, 1300, 700, 9270);
            AddAlphaRegion(10, 10, 1280, 680); // Transparent background
        }

        public TargetClasseGump(PlayerMobile pm) : base(250, 200)
        {
            AddBackground();

            var x1 = 50;
            var y1 = 50;
            AddLabel(x1, y1, 400, "Combatent"); x1 += 200;


            foreach (var type in Enum.GetValues<ZuluDamageType>())
            {
                AddLabelHtml(x1, y1, 100, 30, type.ToString(), "white", 5, true);
                x1 += 100;
            }

            AddLabelHtml(x1, y1, 100, 30, "Final Damage", "white", 1, true);
            x1 += 100;

            var y = 90;

            
            foreach (var item in pm.ZuluDamageSystem.CombatList)
            {
                if (item is not null)
                {
                    var x = 50;
                    AddLabel(x, y, 400, item.combatent.Name);
                    x += 200;

                    foreach (var type in Enum.GetValues<ZuluDamageType>())
                    {
                        AddLabelHtml(x, y, 45, 30, item.damageDealt[type] != 0 ? item.damageDealt[type].ToString("0.#") : "-", item.damageDealt[type] > 0 ? "green" : "white", 4, true);
                        //AddLabel(x, y, 400, item.damageDealt[type].ToString("0.##"));
                        x += 48;
                        AddLabel(x, y, 999, "/");
                        //AddLabelHtml(x, y, 4, 30, "/", "white", 4, true);
                        x += 4;
                        //AddLabel(x, y, 400, item.damageTaken[type].ToString("0.##"));
                        AddLabelHtml(x, y, 45, 30, item.damageTaken[type] != 0 ? item.damageTaken[type].ToString("0.#") : "-", item.damageTaken[type] > 0 ? "red" : item.damageTaken[type] < 0 ? "green" : "white", 4, true);
                        x += 48;
                    }

                    AddLabelHtml(x, y, 45, 30, item.totalDamageDealt != 0 ? item.totalDamageDealt.ToString("0.#") : "-", item.totalDamageDealt > 0 ? "green" : "white", 4, true);
                    //AddLabel(x, y, 400, item.damageDealt[type].ToString("0.##"));
                    x += 48;
                    AddLabel(x, y, 999, "/");
                    //AddLabelHtml(x, y, 4, 30, "/", "white", 4, true);
                    x += 4;
                    //AddLabel(x, y, 400, item.damageTaken[type].ToString("0.##"));
                    AddLabelHtml(x, y, 45, 30, item.totalDamageTaken != 0 ? item.totalDamageTaken.ToString("0.#") : "-", item.totalDamageTaken > 0 ? "red" : item.totalDamageTaken < 0 ? "green" : "white", 4, true);
                    x += 48;
                    AddButton(x, y, 4005, 4007, 1);

                    y += 30;
                }

            }




        }
    }
}
