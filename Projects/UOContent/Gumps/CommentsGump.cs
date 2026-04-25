using Server.Accounting;
using Server.Network;
using Server.Prompts;
using Server.Targeting;

namespace Server.Gumps
{
    public class CommentsGump : DynamicGump
    {
        private readonly Account _acct;

        public override bool Singleton => true;

        private CommentsGump(Account acct) : base(30, 30)
        {
            _acct = acct;
        }

        public static void DisplayTo(Mobile from, Account acct)
        {
            if (from?.NetState == null || acct == null)
            {
                return;
            }

            from.SendGump(new CommentsGump(acct));
        }

        protected override void BuildLayout(ref DynamicGumpBuilder builder)
        {
            builder.AddPage(0);
            builder.AddImageTiled(0, 0, 410, 448, 0xA40);
            builder.AddAlphaRegion(1, 1, 408, 446);

            var title = $"Comments for '{_acct.Username}'";
            var x = 205 - title.Length / 2 * 7;
            if (x < 120)
            {
                x = 120;
            }

            builder.AddLabel(x, 12, 2100, title);

            builder.AddPage(1);
            builder.AddButton(12, 12, 0xFA8, 0xFAA, 0x7F);
            builder.AddLabel(48, 12, 2100, "Add Comment");

            var list = _acct.Comments;
            if (list.Count > 0)
            {
                for (var i = 0; i < list.Count; ++i)
                {
                    var comment = list[i];

                    if (i >= 5 && i % 5 == 0)
                    {
                        builder.AddButton(368, 12, 0xFA5, 0xFA7, 0, GumpButtonType.Page, i / 5 + 1);
                        builder.AddLabel(298, 12, 2100, "Next Page");
                        builder.AddPage(i / 5 + 1);
                        builder.AddButton(12, 12, 0xFAE, 0xFB0, 0, GumpButtonType.Page, i / 5);
                        builder.AddLabel(48, 12, 2100, "Prev Page");
                    }

                    var html =
                        $"[Added By: {comment.AddedBy} on {comment.LastModified.ToString("H:mm M/d/yy")}]<br>{comment.Content}";
                    builder.AddHtml(12, 44 + i % 5 * 80, 386, 70, html, background: true, scrollbar: true);
                }
            }
            else
            {
                builder.AddLabel(12, 44, 2100, "There are no comments for this account.");
            }
        }

        public static void Configure()
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
                DisplayTo(from, (Account)m.Account);
            }
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID == 0x7F)
            {
                state.Mobile.SendMessage("Enter the text for the account comment (or press [Esc] to cancel):");
                state.Mobile.Prompt = new CommentPrompt(_acct);
            }
        }

        public class CommentPrompt : Prompt
        {
            private readonly Account _acct;

            public CommentPrompt(Account acct) => _acct = acct;

            public override void OnCancel(Mobile from)
            {
                DisplayTo(from, _acct);
                base.OnCancel(from);
            }

            public override void OnResponse(Mobile from, string text)
            {
                base.OnResponse(from, text);
                from.SendMessage("Comment added.");
                _acct.Comments.Add(new AccountComment(from.Name, text));
                DisplayTo(from, _acct);
            }
        }
    }
}
