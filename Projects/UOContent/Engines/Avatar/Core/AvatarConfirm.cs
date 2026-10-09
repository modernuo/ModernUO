using System;
using Server.Gumps;

namespace Server.Engines.Avatar;

public static class AvatarConfirm
{
    public const string Red = "#FF4040";

    public static void Send(Mobile from, string title, string body, Action onConfirmed)
    {
        from.SendGump(
            new WarningGump(
                $"<BASEFONT COLOR=#FFA500>{title}</BASEFONT><BR><BR>{body}",
                420,
                240,
                confirmed =>
                {
                    if (confirmed)
                    {
                        onConfirmed();
                    }
                }
            )
        );
    }
}
