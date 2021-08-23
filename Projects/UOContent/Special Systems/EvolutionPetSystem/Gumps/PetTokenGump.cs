using EvolutionPetSystem;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Gumps
{
    public class PetTokenGump : Gump
    {
        private Mobile m_User;
        private BaseEvo m_EvoPet;    

        public Mobile User { get => m_User; set => m_User = value; }

        public BaseEvo EvoPet { get => m_EvoPet; set => m_EvoPet = value; }

        public PetTokenGump(Mobile user, BaseEvo evopet)
            : base(0, 0)
        {
            m_User = user;
            m_EvoPet = evopet;
            Draggable = true;
            Closable = true;
            Resizable = false;
            Disposable = false;
            

            AddPage(0);
            AddBackground(177, 77, 439, 22, 9200);
            AddBackground(173, 99, 450, 473, 83);
            AddImage(195, 320, 11008);
            AddImage(252, 342, 39851);
            AddImage(252, 440, 39841);
            AddImageTiled(416, 395, 118, 16, 30008);
            AddImageTiled(255, 394, 118, 16, 30008);
            AddImage(412, 342, 39829);
            AddImage(412, 440, 39852);
            AddLabel(264, 79, 1160, "Evolution Pet Summoner Information");
            AddLabel(300, 341, 0, "Nox");
            AddLabel(299, 439, 0, "Armor");
            AddImageTiled(254, 394, 73, 16, 30009);
            AddLabel(299, 454, 0, "Penetration");
            AddLabel(461, 341, 0, "Life Leech");
            AddLabel(460, 439, 0, "Rage");
            AddImageTiled(254, 494, 118, 16, 30009);
            AddImageTiled(416, 395, 28, 16, 30009);
            AddImageTiled(417, 494, 118, 16, 30009);
            AddLabel(298, 373, 0, "1050/750000");
            AddLabel(461, 372, 0, "11000/30000");
            AddLabel(299, 472, 0, "750000/750000");
            AddLabel(460, 472, 0, "750000/750000");
            AddLabel(238, 200, 0, "Stage:");
            AddLabel(237, 223, 0, "Bound to:");
            AddImage(125, 41, 10400);
            AddImage(126, 199, 10401);
            AddImage(127, 358, 10402);
            AddImage(228, 129, 2329);
            AddItem(230, 137, 9668);
            AddLabel(316, 133, 0, "Type:");
            AddLabel(350, 133, 0, "Hell Spider");
            AddLabel(317, 150, 0, "Ability Slots:");
            AddLabel(391, 150, 0, $"{m_EvoPet.Stage}");
            //AddLabel(275, 200, 0, m_EvoPet.Abilities.Count.ToString());
            AddLabel(298, 223, 0, m_EvoPet.ControlMaster.Name);
            AddLabel(317, 168, 0, "Experience:");
            AddLabel(385, 168, 0, $"{m_EvoPet.Ep}/80000000");
            AddButton(527, 202, 4014, 4016, (int)Buttons.Button1, GumpButtonType.Reply, 0);
            AddLabel(443, 203, 0, "Bond/Rebond");
            AddLabel(443, 228, 0, "Unbond");
            AddButton(527, 227, 4002, 4003, (int)Buttons.Button2, GumpButtonType.Reply, 0);
            AddImage(175, 206, 40101); //Str image
            AddImage(300, 206, 40110); //Int image
            AddImage(421, 206, 40095); //Dex image
            AddLabel(264, 259, 0, "Strength");
            AddLabel(391, 259, 0, "Intelligence");
            AddLabel(510, 259, 0, "Dexterity");
            AddLabel(264, 275, 0, m_EvoPet.HitsMax.ToString());
            AddLabel(392, 275, 0, m_EvoPet.ManaMax.ToString());
            AddLabel(511, 275, 0, m_EvoPet.StamMax.ToString());
        }

        public enum Buttons
        {
            Button1 = 1,
            Button2 = 2,
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
        }

        public override void OnServerClose(NetState owner)
        {
        }
    }
}
