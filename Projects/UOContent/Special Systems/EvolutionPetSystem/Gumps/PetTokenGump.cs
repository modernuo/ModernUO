using EvolutionPetSystem;
using Server.Items;
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
        private PetToken m_PetToken;

        public Mobile User { get => m_User; set => m_User = value; }

        public BaseEvo EvoPet { get => m_EvoPet; set => m_EvoPet = value; }

        public PetToken PetToken { get => m_PetToken; set => m_PetToken = value; }

        public PetTokenGump(Mobile user, BaseEvo evopet, PetToken pettoken)
            : base(0, 0)
        {
            m_User = user;
            m_EvoPet = evopet;
            m_PetToken = pettoken;
            Draggable = true;
            Closable = true;
            Resizable = false;
            Disposable = false;


            AddPage(0);

            //Backgrounds
            AddBackground(177, 77, 439, 22, 9200);
            AddBackground(173, 99, 450, 473, 83);
            AddImage(195, 320, 11008);

            //Dragon Frame
            AddImage(125, 41, 10400);
            AddImage(126, 199, 10401);
            AddImage(127, 358, 10402);

            //Headline
            AddLabel(284, 79, 1160, "Evolution Pet Summoner Information");

            
            AddImage(228, 129, 2329); //Preview image
            AddItem(230, 137, 9668); //Background for evo type




            //******************************************
            AddLabel(317, 133, 1152, "Type:");
            AddLabel(350, 133, 1152, "Hell Spider");
            //******************************************
            AddLabel(317, 150, 1152, "Ability Slots:");
            if (m_EvoPet.Abilities != null)
            {
                AddLabel(391, 150, 1152, $"{m_EvoPet.Abilities.Count}");
            }
            else
            {
                AddLabel(411, 150, 1152, $"0");
            }
            //******************************************
            AddLabel(317, 168, 1152, "Experience:");
            AddLabel(385, 168, 1152, $"{m_EvoPet.Ep}/80000000");
            //******************************************
            AddLabel(238, 200, 1152, "Stage:");
            AddLabel(275, 200, 1152, $"{m_EvoPet.Stage}");
            //******************************************
            AddLabel(237, 223, 1152, "Bound to:");
            AddLabel(298, 223, 1152, m_EvoPet.ControlMaster.Name);
            //******************************************
            AddImage(175, 206, 40101); //Str image
            AddLabel(264, 259, 1152, "Strength");
            AddLabel(264, 275, 1152, m_EvoPet.HitsMax.ToString());
            //******************************************
            AddImage(300, 206, 40110); //Int image
            AddLabel(391, 259, 1152, "Intelligence");
            AddLabel(392, 275, 1152, m_EvoPet.ManaMax.ToString());
            //******************************************
            AddImage(421, 206, 40095); //Dex image
            AddLabel(510, 259, 1152, "Dexterity");
            AddLabel(511, 275, 1152, m_EvoPet.StamMax.ToString());
            //******************************************
            AddButton(527, 203, 4014, 4016, (int)Buttons.BondButton, GumpButtonType.Reply, 0);
            AddLabel(443, 203, 1152, "Bond/Rebond");
            //******************************************
            AddButton(527, 228, 4002, 4003, (int)Buttons.UnbondButton, GumpButtonType.Reply, 0);
            AddLabel(443, 228, 1152, "Unbond");
            //******************************************

            GenerateAbilitySlot_1();
            GenerateAbilitySlot_2();
            GenerateAbilitySlot_3();
            GenerateAbilitySlot_4();

            //AddImage(413, 440, 2262); // Schloss
        }



        public void GenerateAbilitySlot_1()
        {
            

            if (m_EvoPet.Abilities != null && m_EvoPet.Abilities.Count > 0) 
            {
                if (m_EvoPet.Abilities[0] != null)
                {
                    AddImage(252, 340, m_EvoPet.Abilities[0].Icon);
                    AddLabel(299, 339, 1152, Enum.GetName(m_EvoPet.Abilities[0].AbilityType));
                    AddLabel(299, 354, 1152, "");
                    AddLabel(299, 372, 1152, $"{m_EvoPet.Abilities[0].XP}/{m_EvoPet.Abilities[0].MaxXP}");
                    AddImageTiled(256, 394, 118, 16, 30008);
                    AddImageTiled(256, 394, 0 + m_EvoPet.Abilities[0].Stage * 12, 16, 30009);
                }
                

            }
            else
            {
                AddImage(252, 340, 2262);
                AddLabel(299, 339, 1152, "n/a");
                AddLabel(299, 354, 1152, "");
                AddLabel(299, 372, 1152, "0/0");
                //AddHtml(299, 372, 90, 16, "<BASEFONT SIZE=2 FACE=2 COLOR=#FFFFFF>1050/750000</BASEFONT>", false, false);
                AddImageTiled(256, 394, 118, 16, 30008);
                //AddImageTiled(256, 394, 73, 16, 30009);

            }


        }

        public void GenerateAbilitySlot_2()
        {
            AddImage(252, 440, 2262);
            AddLabel(299, 439, 1152, "n/a");
            AddLabel(299, 454, 1152, "");
            AddLabel(299, 472, 1152, "0/0");
            AddImageTiled(256, 494, 118, 16, 30008);
            //AddImageTiled(256, 494, 118, 16, 30009);

            //AddImage(252, 440, 39841);
            //AddLabel(299, 439, 1152, "Armor");
            //AddLabel(299, 454, 1152, "Penetration");
            //AddLabel(299, 472, 1152, "750000/750000");
            //AddImageTiled(256, 494, 118, 16, 30008);
            //AddImageTiled(256, 494, 118, 16, 30009);


        }
        public void GenerateAbilitySlot_3()
        {
            AddImage(412, 340, 2262);
            AddLabel(461, 339, 1152, "n/a");
            AddLabel(461, 354, 1152, "");
            AddLabel(461, 372, 1152, "0/0");
            AddImageTiled(416, 394, 118, 16, 30008);
            //AddImageTiled(416, 394, 28, 16, 30009);

            //AddImage(412, 340, 39829);
            //AddLabel(461, 339, 1152, "Life Leech");
            //AddLabel(461, 354, 1152, "");
            //AddLabel(461, 372, 1152, "11000/30000");
            //AddImageTiled(416, 394, 118, 16, 30008);
            //AddImageTiled(416, 394, 28, 16, 30009);

        }
        public void GenerateAbilitySlot_4()
        {
            AddImage(412, 440, 2262);
            AddLabel(461, 439, 1152, "n/a");
            AddLabel(461, 454, 1152, "");
            AddLabel(461, 472, 1152, "0/0");
            AddImageTiled(416, 494, 118, 16, 30008);
            //AddImageTiled(416, 494, 118, 16, 30009);

            //AddImage(412, 440, 39852);
            //AddLabel(461, 439, 1152, "Rage");
            //AddLabel(461, 454, 1152, "");
            //AddLabel(461, 472, 1152, "750000/750000");
            //AddImageTiled(416, 494, 118, 16, 30008);
            //AddImageTiled(416, 494, 118, 16, 30009);

        }

        public enum Buttons
        {
            BondButton = 1,
            UnbondButton = 2,
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            switch (info.ButtonID)
            {
                case (int)Buttons.BondButton:
                    m_User.Target = new PetTokenTarget(m_PetToken);
                    break;
                case (int)Buttons.UnbondButton:
                    m_PetToken.BoundEvoPet = null;
                    m_User.SendMessage("Your pet was successfully removed from the token.");
                    break;
                default:
                    break;
            }
        }

        public override void OnServerClose(NetState owner)
        {
        }
    }
}
