using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items
{
    [SerializationGenerator(2, false)]
    public partial class BulletinMessage : Item
    {
        public BulletinMessage(Mobile poster, BulletinMessage thread, string subject, string[] lines) : base(0xEB0)
        {
            Movable = false;

            Poster = poster;
            Subject = subject;
            Time = Core.Now;
            LastPostTime = Time;
            Thread = thread;
            PostedName = Poster.Name;
            PostedBody = Poster.Body;
            PostedHue = Poster.Hue;
            Lines = lines;

            var list = new List<BulletinEquip>();

            for (var i = 0; i < poster.Items.Count; ++i)
            {
                var item = poster.Items[i];

                if (item.Layer >= Layer.OneHanded && item.Layer <= Layer.Mount)
                {
                    list.Add(new BulletinEquip(item.ItemID, item.Hue));
                }
            }

            PostedEquip = list.ToArray();
        }

        [SerializableField(0)]
        private Mobile _poster;

        [SerializableField(1)]
        private string _subject;

        [SerializableField(2)]
        private DateTime _time;

        [SerializableField(3)]
        private DateTime _lastPostTime;

        [SerializableField(4)]
        private BulletinMessage _thread;

        [SerializableField(5)]
        private string _postedName;

        [SerializableField(6)]
        private int _postedBody;

        [SerializableField(7)]
        private int _postedHue;

        [SerializableField(8)]
        private BulletinEquip[] _postedEquip;

        [SerializableField(9)]
        private string[] _lines;

        // TODO: Memoize
        public string GetTimeAsString() => Time.ToString("MMM dd, yyyy");

        public override bool CheckTarget(Mobile from, Target targ, object targeted) => false;

        public override bool IsAccessibleTo(Mobile check) => false;

        private void Deserialize(IGenericReader reader, int version)
        {
            Poster = reader.ReadEntity<Mobile>();
            Subject = reader.ReadString();
            Time = reader.ReadDateTime();
            LastPostTime = reader.ReadDateTime();
            reader.ReadBool(); // Has thread
            Thread = reader.ReadEntity<BulletinMessage>();
            PostedName = reader.ReadString();
            PostedBody = reader.ReadInt();
            PostedHue = reader.ReadInt();

            PostedEquip = new BulletinEquip[reader.ReadInt()];

            for (var i = 0; i < PostedEquip.Length; ++i)
            {
                PostedEquip[i]._itemID = reader.ReadInt();
                PostedEquip[i]._hue = reader.ReadInt();
            }

            Lines = new string[reader.ReadInt()];

            for (var i = 0; i < Lines.Length; ++i)
            {
                Lines[i] = reader.ReadString();
            }

            // Moved validation/cleanup to the BB itself
        }
    }
}
