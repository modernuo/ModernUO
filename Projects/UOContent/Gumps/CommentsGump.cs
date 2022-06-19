using Server.Accounting;
using Server.Network;
using Server.Prompts;
using Server.Targeting;

namespace Server.Gumps
{
    public class CommentsGump : Gump
    {
        private readonly Account m_Acct;

        public CommentsGump(Account acct) : base(30, 30)
        {
            m_Acct = acct;

            AddPage(0);
            AddImageTiled(0, 0, 410, 448, 0xA40);
            AddAlphaRegion(1, 1, 408, 446);

            var title = $"Comments for '{acct.Username}'";
            var x = 205 - title.Length / 2 * 7;
            if (x < 120)
            {
                x = 120;
            }

            AddLabel(x, 12, 2100, title);

            AddPage(1);
            AddButton(12, 12, 0xFA8, 0xFAA, 0x7F);
            AddLabel(48, 12, 2100, "Add Comment");

            var list = acct.Comments;
            if (list.Count > 0)
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    var comment = list[i];

                    if (i >= 5 && i % 5 == 0)
                    {
                        AddButton(368, 12, 0xFA5, 0xFA7, 0, GumpButtonType.Page, i / 5 + 1);
                        AddLabel(298, 12, 2100, "Next Page");
                        AddPage(i / 5 + 1);
                        AddButton(12, 12, 0xFAE, 0xFB0, 0, GumpButtonType.Page, i / 5);
                        AddLabel(48, 12, 2100, "Prev Page");
                    }

                    var html =
                        $"[Added By: {comment.AddedBy} on {comment.LastModified.ToString("H:mm M/d/yy")}]<br>{comment.Content}";
                    AddHtml(12, 44 + i % 5 * 80, 386, 70, html, true, true);
                }
            }
            else
            {
                AddLabel(12, 44, 2100, "There are no comments for this account.");
            }
        }

        public static void Initialize()
        {
            CommandSystem.Register("Comments", AccessLevel.Counselor, Comments_OnCommand);
        }

        [Usage("Comments"), Description("View/Modify/Add account comments.")]
        private static void Comments_OnCommand(CommandEventArgs args)
        {
            args.Mobile.SendMessage("Select the player to view account comments.");
            args.Mobile.BeginTarget(-1, false, TargetFlags.None, OnTarget);
        }

        private static void OnTarget(Mobile from, object target)
        {
            if (target is not Mobile m || !m.Player)
            {
                from.SendMessage("You must target a player.");
                return;
            }

            if (m.Account == null)
            {
                from.SendMessage("That player doesn't have an account loaded... weird.");
            }
            else
            {
                from.SendGump(new CommentsGump((Account)m.Account));
            }
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (info.ButtonID == 0x7F)
            {
                state.Mobile.SendMessage("Enter the text for the account comment (or press [Esc] to cancel):");
                state.Mobile.Prompt = new CommentPrompt(m_Acct);
            }
        }

        public class CommentPrompt : Prompt
        {
            private readonly Account m_Acct;

            public CommentPrompt(Account acct) => m_Acct = acct;

            public override void OnCancel(Mobile from)
            {
                from.CloseGump<CommentsGump>();
                from.SendGump(new CommentsGump(m_Acct));
                base.OnCancel(from);
            }

            public override void OnResponse(Mobile from, string text)
            {
                base.OnResponse(from, text);
                from.SendMessage("Comment added.");
                // m_Acct.AddComment( from.Name, text );
                m_Acct.Comments.Add(new AccountComment(from.Name, text));
                from.CloseGump<CommentsGump>();
                from.SendGump(new CommentsGump(m_Acct));
            }
        }
    }
}
