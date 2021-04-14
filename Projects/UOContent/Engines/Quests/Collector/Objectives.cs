using System;

namespace Server.Engines.Quests.Collector
{
    public class FishPearlsObjective : QuestObjective
    {
        public override object Message => 1055084;

        public override int MaxProgress => 6;

        public override void RenderProgress(BaseQuestGump gump)
        {
            if (!Completed)
            {
                // Rainbow pearls collected:
                gump.AddHtmlObject(70, 260, 270, 100, 1055085, BaseQuestGump.Blue, false, false);

                gump.AddLabel(70, 280, 0x64, CurProgress.ToString());
                gump.AddLabel(100, 280, 0x64, "/");
                gump.AddLabel(130, 280, 0x64, MaxProgress.ToString());
            }
            else
            {
                base.RenderProgress(gump);
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new ReturnPearlsObjective());
        }
    }

    public class ReturnPearlsObjective : QuestObjective
    {
        public override object Message => 1055088;

        public override void OnComplete()
        {
            System.AddConversation(new ReturnPearlsConversation());
        }
    }

    public class FindAlbertaObjective : QuestObjective
    {
        public override object Message => 1055091;

        public override void OnComplete()
        {
            System.AddConversation(new AlbertaPaintingConversation());
        }
    }

    public class SitOnTheStoolObjective : QuestObjective
    {
        private static readonly Point3D m_StoolLocation = new(2899, 706, 0);
        private static readonly Map m_StoolMap = Map.Trammel;

        private DateTime m_Begin;

        public SitOnTheStoolObjective() => m_Begin = DateTime.MaxValue;

        public override object Message => 1055093;

        public override void CheckProgress()
        {
            var pm = System.From;

            if (pm.Map == m_StoolMap && pm.Location == m_StoolLocation)
            {
                if (m_Begin == DateTime.MaxValue)
                {
                    m_Begin = Core.Now;
                }
                else if (Core.Now - m_Begin > TimeSpan.FromSeconds(30.0))
                {
                    Complete();
                }
            }
            else if (m_Begin != DateTime.MaxValue)
            {
                m_Begin = DateTime.MaxValue;
                pm.SendLocalizedMessage(
                    1055095,
                    "",
                    0x26
                ); // You must remain seated on the stool until the portrait is complete. Alberta will now have to start again with a fresh canvas.
            }
        }

        public override void OnComplete()
        {
            System.AddConversation(new AlbertaEndPaintingConversation());
        }
    }

    public class ReturnPaintingObjective : QuestObjective
    {
        public override object Message => 1055099;

        public override void OnComplete()
        {
            System.AddConversation(new ReturnPaintingConversation());
        }
    }

    public class FindGabrielObjective : QuestObjective
    {
        public override object Message => 1055101;

        public override void OnComplete()
        {
            System.AddConversation(new GabrielAutographConversation());
        }
    }

    public enum Theater
    {
        Britain,
        Nujelm,
        Jhelom
    }

    public class FindSheetMusicObjective : QuestObjective
    {
        private Theater m_Theater;

        public FindSheetMusicObjective(bool init)
        {
            if (init)
            {
                InitTheater();
            }
        }

        public FindSheetMusicObjective()
        {
        }

        public override object Message => 1055104;

        public void InitTheater()
        {
            m_Theater = Utility.Random(3) switch
            {
                1 => Theater.Britain,
                2 => Theater.Nujelm,
                _ => Theater.Jhelom
            };
        }

        public bool IsInRightTheater()
        {
            var player = System.From;

            var region = Region.Find(player.Location, player.Map);

            if (region == null)
            {
                return false;
            }

            return m_Theater switch
            {
                Theater.Britain => region.IsPartOf("Britain"),
                Theater.Nujelm  => region.IsPartOf("Nujel'm"),
                Theater.Jhelom  => region.IsPartOf("Jhelom"),
                _               => false
            };
        }

        public override void OnComplete()
        {
            System.AddConversation(new GetSheetMusicConversation());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            m_Theater = (Theater)reader.ReadEncodedInt();
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt((int)m_Theater);
        }
    }

    public class ReturnSheetMusicObjective : QuestObjective
    {
        public override object Message => 1055110;

        public override void OnComplete()
        {
            System.AddConversation(new GabrielSheetMusicConversation());
        }
    }

    public class ReturnAutographObjective : QuestObjective
    {
        public override object Message => 1055114;

        public override void OnComplete()
        {
            System.AddConversation(new ReturnAutographConversation());
        }
    }

    public class FindTomasObjective : QuestObjective
    {
        public override object Message => 1055117;

        public override void OnComplete()
        {
            System.AddConversation(new TomasToysConversation());
        }
    }

    public enum CaptureResponse
    {
        Valid,
        AlreadyDone,
        Invalid
    }

    public class CaptureImagesObjective : QuestObjective
    {
        private bool[] m_Done;
        private ImageType[] m_Images;

        public CaptureImagesObjective(bool init)
        {
            if (init)
            {
                m_Images = ImageTypeInfo.RandomList(4);
                m_Done = new bool[4];
            }
        }

        public CaptureImagesObjective()
        {
        }

        public override object Message => 1055120;

        public override bool Completed
        {
            get
            {
                for (var i = 0; i < m_Done.Length; i++)
                {
                    if (!m_Done[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public override bool IgnoreYoungProtection(Mobile from)
        {
            if (Completed)
            {
                return false;
            }

            var fromType = from.GetType();

            for (var i = 0; i < m_Images.Length; i++)
            {
                var info = ImageTypeInfo.Get(m_Images[i]);

                if (info.Type == fromType)
                {
                    return true;
                }
            }

            return false;
        }

        public CaptureResponse CaptureImage(Type type, out ImageType image)
        {
            for (var i = 0; i < m_Images.Length; i++)
            {
                var info = ImageTypeInfo.Get(m_Images[i]);

                if (info.Type == type)
                {
                    image = m_Images[i];

                    if (m_Done[i])
                    {
                        return CaptureResponse.AlreadyDone;
                    }

                    m_Done[i] = true;

                    CheckCompletionStatus();

                    return CaptureResponse.Valid;
                }
            }

            image = 0;
            return CaptureResponse.Invalid;
        }

        public override void RenderProgress(BaseQuestGump gump)
        {
            if (!Completed)
            {
                for (var i = 0; i < m_Images.Length; i++)
                {
                    var info = ImageTypeInfo.Get(m_Images[i]);

                    gump.AddHtmlObject(70, 260 + 20 * i, 200, 100, info.Name, BaseQuestGump.Blue, false, false);
                    gump.AddLabel(200, 260 + 20 * i, 0x64, " : ");
                    gump.AddHtmlObject(
                        220,
                        260 + 20 * i,
                        100,
                        100,
                        m_Done[i] ? 1055121 : 1055122,
                        BaseQuestGump.Blue,
                        false,
                        false
                    );
                }
            }
            else
            {
                base.RenderProgress(gump);
            }
        }

        public override void OnComplete()
        {
            System.AddObjective(new ReturnImagesObjective());
        }

        public override void ChildDeserialize(IGenericReader reader)
        {
            var version = reader.ReadEncodedInt();

            var count = reader.ReadEncodedInt();

            m_Images = new ImageType[count];
            m_Done = new bool[count];

            for (var i = 0; i < count; i++)
            {
                m_Images[i] = (ImageType)reader.ReadEncodedInt();
                m_Done[i] = reader.ReadBool();
            }
        }

        public override void ChildSerialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(m_Images.Length);

            for (var i = 0; i < m_Images.Length; i++)
            {
                writer.WriteEncodedInt((int)m_Images[i]);
                writer.Write(m_Done[i]);
            }
        }
    }

    public class ReturnImagesObjective : QuestObjective
    {
        public override object Message => 1055128;

        public override void OnComplete()
        {
            System.AddConversation(new ReturnImagesConversation());
        }
    }

    public class ReturnToysObjective : QuestObjective
    {
        public override object Message => 1055132;
    }

    public class MakeRoomObjective : QuestObjective
    {
        public override object Message => 1055136;
    }
}
