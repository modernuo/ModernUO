using System.Collections.Generic;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Quests.Haven
{
    public class SchmendrickApprenticeCorpse : Corpse
    {
        private static int m_HairHue;

        private Lantern m_Lantern;

        [Constructible]
        public SchmendrickApprenticeCorpse() : base(GetOwner(), GetHair(), GetFacialHair(), GetEquipment())
        {
            Direction = Direction.West;

            foreach (var item in EquipItems)
            {
                DropItem(item);
            }

            m_Lantern = new Lantern { Movable = false, Protected = true };
            m_Lantern.Ignite();
        }

        public SchmendrickApprenticeCorpse(Serial serial) : base(serial)
        {
        }

        // TODO: What is this? Why are we creating and deleting a mobile?
        private static Mobile GetOwner()
        {
            var apprentice = new Mobile();

            apprentice.Hue = Race.Human.RandomSkinHue();
            apprentice.Female = false;
            apprentice.Body = 0x190;
            apprentice.Name = NameList.RandomName("male");

            apprentice.Delete();

            return apprentice;
        }

        private static List<Item> GetEquipment()
        {
            var list = new List<Item>();

            list.Add(new Robe(QuestSystem.RandomBrightHue()));
            list.Add(new WizardsHat(Utility.RandomNeutralHue()));
            list.Add(new Shoes(Utility.RandomNeutralHue()));
            list.Add(new Spellbook());

            return list;
        }

        private static HairInfo GetHair()
        {
            m_HairHue = Race.Human.RandomHairHue();
            return new HairInfo(Race.Human.RandomHair(false), m_HairHue);
        }

        private static FacialHairInfo GetFacialHair()
        {
            m_HairHue = Race.Human.RandomHairHue();

            return new FacialHairInfo(Race.Human.RandomFacialHair(false), m_HairHue);
        }

        public override void AddNameProperty(IPropertyList list)
        {
            if (ItemID == 0x2006) // Corpse form
            {
                list.Add("a human corpse");
                list.Add(1049144, Name); // the remains of ~1_NAME~ the apprentice
            }
            else
            {
                list.Add(1049145); // the remains of a wizard's apprentice
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            var hue = Notoriety.GetHue(NotorietyHandlers.CorpseNotoriety(from, this));

            if (ItemID == 0x2006) // Corpse form
            {
                from.NetState.SendMessageLocalized(
                    Serial,
                    ItemID,
                    MessageType.Label,
                    hue,
                    3,
                    1049144,
                    "",
                    Name
                ); // the remains of ~1_NAME~ the apprentice
            }
            else
            {
                from.NetState.SendMessageLocalized(
                    Serial,
                    ItemID,
                    MessageType.Label,
                    hue,
                    3,
                    1049145
                ); // the remains of a wizard's apprentice
            }
        }

        public override void Open(Mobile from, bool checkSelfLoot)
        {
            if (!from.InRange(GetWorldLocation(), 2))
            {
                return;
            }

            if (from is PlayerMobile player)
            {
                var qs = player.Quest;

                if (qs is UzeraanTurmoilQuest)
                {
                    QuestObjective obj = qs.FindObjective<FindApprenticeObjective>();

                    if (obj?.Completed == false)
                    {
                        Item scroll = new SchmendrickScrollOfPower();

                        if (player.PlaceInBackpack(scroll))
                        {
                            player.SendLocalizedMessage(1049147, "", 0x22); // You find the scroll and put it in your pack.
                            obj.Complete();
                        }
                        else
                        {
                            player.SendLocalizedMessage(
                                1049146,
                                "",
                                0x22
                            ); // You find the scroll, but can't pick it up because your pack is too full.  Come back when you have more room in your pack.
                            scroll.Delete();
                        }

                        return;
                    }
                }
            }

            from.SendLocalizedMessage(
                1049143,
                "",
                0x22
            ); // This is the corpse of a wizard's apprentice.  You can't bring yourself to search it without a good reason.
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            if (m_Lantern?.Deleted == false)
            {
                m_Lantern.Location = new Point3D(X, Y + 1, Z);
            }
        }

        public override void OnMapChange()
        {
            if (m_Lantern?.Deleted == false)
            {
                m_Lantern.Map = Map;
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Lantern?.Deleted == false)
            {
                m_Lantern.Delete();
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            if (m_Lantern?.Deleted == true)
            {
                m_Lantern = null;
            }

            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write(m_Lantern);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_Lantern = (Lantern)reader.ReadEntity<Item>();
        }
    }
}
