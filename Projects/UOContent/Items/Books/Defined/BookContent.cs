namespace Server.Items
{
    public class BookContent
    {
        public BookContent(string title, string author, params BookPageInfo[] pages)
        {
            Title = title;
            Author = author;
            Pages = pages;
        }

        public string Title { get; }

        public string Author { get; }

        public BookPageInfo[] Pages { get; }

        public BookPageInfo[] Copy()
        {
            BookPageInfo[] copy = new BookPageInfo[Pages.Length];

            for (int i = 0; i < copy.Length; ++i)
                copy[i] = new BookPageInfo(Pages[i].Lines);

            return copy;
        }

        public bool IsMatch(BookPageInfo[] cmp)
        {
            if (cmp.Length != Pages.Length)
                return false;

            for (int i = 0; i < cmp.Length; ++i)
            {
                string[] a = Pages[i].Lines;
                string[] b = cmp[i].Lines;

                if (a.Length != b.Length) return false;

                if (a != b)
                    for (int j = 0; j < a.Length; ++j)
                        if (a[j] != b[j])
                            return false;
            }

            return true;
        }
    }
}
