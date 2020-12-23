using Server.Gumps;
using Server.Mobiles;
using Server.Multis;

namespace Server.Items
{
    public class CharacterStatuePlinth : Static, IAddon
    {
        private CharacterStatue m_Statue;

        public CharacterStatuePlinth(CharacterStatue statue) : base(0x32F2)
        {
            m_Statue = statue;

            InvalidateHue();
        }

        public CharacterStatuePlinth(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1076201; // Character Statue
        public Item Deed => new CharacterStatueDeed(m_Statue);

        public virtual bool CouldFit(IPoint3D p, Map map)
        {
            var point = new Point3D(p.X, p.Y, p.Z);

            if (map?.CanFit(point, 20) != true)
            {
                return false;
            }

            var house = BaseHouse.FindHouseAt(point, map, 20);

            if (house == null)
            {
                return false;
            }

            var result = CharacterStatueTarget.CheckDoors(point, 20, house);

            if (result == AddonFitResult.Valid)
            {
                return true;
            }

            return false;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Statue?.Deleted == false)
            {
                m_Statue.Delete();
            }
        }

        public override void OnMapChange()
        {
            if (m_Statue != null)
            {
                m_Statue.Map = Map;
            }
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_Statue != null)
            {
                m_Statue.Location = new Point3D(X, Y, Z + 5);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Statue != null)
            {
                from.SendGump(new CharacterPlinthGump(m_Statue));
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write(m_Statue);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadEncodedInt();

            m_Statue = reader.ReadEntity<CharacterStatue>();

            if (m_Statue?.SculptedBy == null || Map == Map.Internal)
            {
                Timer.DelayCall(Delete);
            }
        }

        public void InvalidateHue()
        {
            if (m_Statue != null)
            {
                Hue = 0xB8F + (int)m_Statue.StatueType * 4 + (int)m_Statue.Material;
            }
        }

        private class CharacterPlinthGump : Gump
        {
            public CharacterPlinthGump(CharacterStatue statue) : base(60, 30)
            {
                Closable = true;
                Disposable = true;
                Draggable = true;
                Resizable = false;

                AddPage(0);
                AddImage(0, 0, 0x24F4);
                AddHtml(55, 50, 150, 20, statue.Name);
                AddHtml(55, 75, 150, 20, statue.SculptedOn.ToString());
                AddHtmlLocalized(55, 100, 150, 20, GetTypeNumber(statue.StatueType), 0);
            }

            public int GetTypeNumber(StatueType type)
            {
                return type switch
                {
                    StatueType.Marble => 1076181,
                    StatueType.Jade   => 1076180,
                    StatueType.Bronze => 1076230,
                    _                 => 1076181
                };
            }
        }
    }
}
