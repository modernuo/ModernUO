using Server.Mobiles;
using Server.Network;

namespace Server.Gumps
{
    public class CharacterStatueGump : Gump
    {
        private readonly Item m_Maker;
        private readonly Mobile m_Owner;
        private readonly CharacterStatue m_Statue;

        public CharacterStatueGump(Item maker, CharacterStatue statue, Mobile owner) : base(60, 36)
        {
            m_Maker = maker;
            m_Statue = statue;
            m_Owner = owner;

            if (m_Statue == null)
            {
                return;
            }

            Closable = true;
            Disposable = true;
            Draggable = true;
            Resizable = false;

            AddPage(0);

            AddBackground(0, 0, 327, 324, 0x13BE);
            AddImageTiled(10, 10, 307, 20, 0xA40);
            AddImageTiled(10, 40, 307, 244, 0xA40);
            AddImageTiled(10, 294, 307, 20, 0xA40);
            AddAlphaRegion(10, 10, 307, 304);
            AddHtmlLocalized(14, 12, 327, 20, 1076156, 0x7FFF); // Character Statue Maker

            // pose
            AddHtmlLocalized(133, 41, 120, 20, 1076168, 0x7FFF); // Choose Pose
            AddHtmlLocalized(133, 61, 120, 20, 1076208 + (int)m_Statue.Pose, 0x77E);
            AddButton(163, 81, 0xFA5, 0xFA7, (int)Buttons.PoseNext);
            AddButton(133, 81, 0xFAE, 0xFB0, (int)Buttons.PosePrev);

            // direction
            AddHtmlLocalized(133, 126, 120, 20, 1076170, 0x7FFF); // Choose Direction
            AddHtmlLocalized(133, 146, 120, 20, GetDirectionNumber(m_Statue.Direction), 0x77E);
            AddButton(163, 167, 0xFA5, 0xFA7, (int)Buttons.DirNext);
            AddButton(133, 167, 0xFAE, 0xFB0, (int)Buttons.DirPrev);

            // material
            AddHtmlLocalized(133, 211, 120, 20, 1076171, 0x7FFF); // Choose Material
            AddHtmlLocalized(133, 231, 120, 20, GetMaterialNumber(m_Statue.StatueType, m_Statue.Material), 0x77E);
            AddButton(163, 253, 0xFA5, 0xFA7, (int)Buttons.MatNext);
            AddButton(133, 253, 0xFAE, 0xFB0, (int)Buttons.MatPrev);

            // cancel
            AddButton(10, 294, 0xFB1, 0xFB2, (int)Buttons.Close);
            AddHtmlLocalized(45, 294, 80, 20, 1006045, 0x7FFF); // Cancel

            // sculpt
            AddButton(234, 294, 0xFB7, 0xFB9, (int)Buttons.Sculpt);
            AddHtmlLocalized(269, 294, 80, 20, 1076174, 0x7FFF); // Sculpt

            // restore
            if (m_Maker is CharacterStatueDeed)
            {
                AddButton(107, 294, 0xFAB, 0xFAD, (int)Buttons.Restore);
                AddHtmlLocalized(142, 294, 80, 20, 1076193, 0x7FFF); // Restore
            }
        }

        private int GetMaterialNumber(StatueType type, StatueMaterial material)
        {
            switch (material)
            {
                case StatueMaterial.Antique:

                    return type switch
                    {
                        StatueType.Bronze => 1076187,
                        StatueType.Jade   => 1076186,
                        StatueType.Marble => 1076182,
                        _                 => 1076187
                    };

                case StatueMaterial.Dark:

                    if (type == StatueType.Marble)
                    {
                        return 1076183;
                    }

                    return 1076182;
                case StatueMaterial.Medium: return 1076184;
                case StatueMaterial.Light:  return 1076185;
                default:                    return 1076187;
            }
        }

        private int GetDirectionNumber(Direction direction)
        {
            return direction switch
            {
                Direction.North => 1075389,
                Direction.Right => 1075388,
                Direction.East  => 1075387,
                Direction.Down  => 1076204,
                Direction.South => 1075386,
                Direction.Left  => 1075391,
                Direction.West  => 1075390,
                Direction.Up    => 1076205,
                _               => 1075386
            };
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (m_Statue?.Deleted != false)
            {
                return;
            }

            var sendGump = false;

            if (info.ButtonID == (int)Buttons.Sculpt)
            {
                if (m_Maker is CharacterStatueDeed deed)
                {
                    var backup = deed.Statue;

                    backup?.Delete();
                }

                m_Maker?.Delete();

                m_Statue.Sculpt(state.Mobile);
            }
            else if (info.ButtonID == (int)Buttons.PosePrev)
            {
                m_Statue.Pose = (StatuePose)(((int)m_Statue.Pose + 5) % 6);
                sendGump = true;
            }
            else if (info.ButtonID == (int)Buttons.PoseNext)
            {
                m_Statue.Pose = (StatuePose)(((int)m_Statue.Pose + 1) % 6);
                sendGump = true;
            }
            else if (info.ButtonID == (int)Buttons.DirPrev)
            {
                m_Statue.Direction = (Direction)(((int)m_Statue.Direction + 7) % 8);
                m_Statue.InvalidatePose();
                sendGump = true;
            }
            else if (info.ButtonID == (int)Buttons.DirNext)
            {
                m_Statue.Direction = (Direction)(((int)m_Statue.Direction + 1) % 8);
                m_Statue.InvalidatePose();
                sendGump = true;
            }
            else if (info.ButtonID == (int)Buttons.MatPrev)
            {
                m_Statue.Material = (StatueMaterial)(((int)m_Statue.Material + 3) % 4);
                sendGump = true;
            }
            else if (info.ButtonID == (int)Buttons.MatNext)
            {
                m_Statue.Material = (StatueMaterial)(((int)m_Statue.Material + 1) % 4);
                sendGump = true;
            }
            else if (info.ButtonID == (int)Buttons.Restore)
            {
                if (m_Maker is CharacterStatueDeed deed)
                {
                    var backup = deed.Statue;

                    if (backup != null)
                    {
                        m_Statue.Restore(backup);
                    }
                }

                sendGump = true;
            }
            else // Close
            {
                sendGump = !m_Statue.Demolish(state.Mobile);
            }

            if (sendGump)
            {
                state.Mobile.SendGump(new CharacterStatueGump(m_Maker, m_Statue, m_Owner));
            }
        }

        private enum Buttons
        {
            Close,
            Sculpt,
            PosePrev,
            PoseNext,
            DirPrev,
            DirNext,
            MatPrev,
            MatNext,
            Restore
        }
    }
}
