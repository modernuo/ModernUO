using System;

namespace Server.Items
{
    public class BookPageInfo
    {
        public BookPageInfo() => Lines = Array.Empty<string>();

        public BookPageInfo(params string[] lines) => Lines = lines;

        public BookPageInfo(IGenericReader reader)
        {
            var length = reader.ReadInt();

            Lines = new string[length];

            for (var i = 0; i < Lines.Length; ++i)
            {
                Lines[i] = reader.ReadString().Intern();
            }
        }

        public string[] Lines { get; set; }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(Lines.Length);

            for (var i = 0; i < Lines.Length; ++i)
            {
                writer.Write(Lines[i]);
            }
        }
    }
}
