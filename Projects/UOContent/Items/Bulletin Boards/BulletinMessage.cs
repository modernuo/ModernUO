using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Items
{
    public class BulletinMessage : Item
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

        public BulletinMessage(Serial serial) : base(serial)
        {
        }

        public Mobile Poster { get; private set; }

        public BulletinMessage Thread { get; private set; }

        public string Subject { get; private set; }

        public DateTime Time { get; private set; }

        public DateTime LastPostTime { get; set; }

        public string PostedName { get; private set; }

        public int PostedBody { get; private set; }

        public int PostedHue { get; private set; }

        public BulletinEquip[] PostedEquip { get; private set; }

        public string[] Lines { get; private set; }

        // TODO: Memoize
        public string GetTimeAsString() => Time.ToString("MMM dd, yyyy");

        public override bool CheckTarget(Mobile from, Target targ, object targeted) => false;

        public override bool IsAccessibleTo(Mobile check) => false;

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(Poster);
            writer.Write(Subject);
            writer.Write(Time);
            writer.Write(LastPostTime);
            writer.Write(Thread != null);
            writer.Write(Thread);
            writer.Write(PostedName);
            writer.Write(PostedBody);
            writer.Write(PostedHue);

            writer.Write(PostedEquip.Length);

            for (var i = 0; i < PostedEquip.Length; ++i)
            {
                writer.Write(PostedEquip[i].itemID);
                writer.Write(PostedEquip[i].hue);
            }

            writer.Write(Lines.Length);

            for (var i = 0; i < Lines.Length; ++i)
            {
                writer.Write(Lines[i]);
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        Poster = reader.ReadEntity<Mobile>();
                        Subject = reader.ReadString();
                        Time = reader.ReadDateTime();
                        LastPostTime = reader.ReadDateTime();
                        var hasThread = reader.ReadBool();
                        Thread = reader.ReadEntity<BulletinMessage>();
                        PostedName = reader.ReadString();
                        PostedBody = reader.ReadInt();
                        PostedHue = reader.ReadInt();

                        PostedEquip = new BulletinEquip[reader.ReadInt()];

                        for (var i = 0; i < PostedEquip.Length; ++i)
                        {
                            PostedEquip[i].itemID = reader.ReadInt();
                            PostedEquip[i].hue = reader.ReadInt();
                        }

                        Lines = new string[reader.ReadInt()];

                        for (var i = 0; i < Lines.Length; ++i)
                        {
                            Lines[i] = reader.ReadString();
                        }

                        if (hasThread && Thread == null)
                        {
                            Delete();
                        }

                        if (version == 0)
                        {
                            ValidationQueue<BulletinMessage>.Add(this);
                        }

                        break;
                    }
            }
        }

        public void Validate()
        {
            if ((Parent as BulletinBoard)?.Items.Contains(this) == false)
            {
                Delete();
            }
        }
    }
}
