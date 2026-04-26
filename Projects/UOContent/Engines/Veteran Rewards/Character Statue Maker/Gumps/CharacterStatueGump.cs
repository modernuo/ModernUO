using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class CharacterStatueGump : DynamicGump
{
    private readonly Item _maker;
    private readonly CharacterStatue _statue;

    public override bool Singleton => true;

    private CharacterStatueGump(Item maker, CharacterStatue statue) : base(60, 36)
    {
        _maker = maker;
        _statue = statue;
    }

    public static void DisplayTo(Mobile from, Item maker, CharacterStatue statue)
    {
        if (from?.NetState == null || statue == null || statue.Deleted)
        {
            return;
        }

        from.SendGump(new CharacterStatueGump(maker, statue));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 327, 324, 0x13BE);
        builder.AddImageTiled(10, 10, 307, 20, 0xA40);
        builder.AddImageTiled(10, 40, 307, 244, 0xA40);
        builder.AddImageTiled(10, 294, 307, 20, 0xA40);
        builder.AddAlphaRegion(10, 10, 307, 304);
        builder.AddHtmlLocalized(14, 12, 327, 20, 1076156, 0x7FFF); // Character Statue Maker

        // pose
        builder.AddHtmlLocalized(133, 41, 120, 20, 1076168, 0x7FFF); // Choose Pose
        builder.AddHtmlLocalized(133, 61, 120, 20, 1076208 + (int)_statue.Pose, 0x77E);
        builder.AddButton(163, 81, 0xFA5, 0xFA7, (int)Buttons.PoseNext);
        builder.AddButton(133, 81, 0xFAE, 0xFB0, (int)Buttons.PosePrev);

        // direction
        builder.AddHtmlLocalized(133, 126, 120, 20, 1076170, 0x7FFF); // Choose Direction
        builder.AddHtmlLocalized(133, 146, 120, 20, GetDirectionNumber(_statue.Direction), 0x77E);
        builder.AddButton(163, 167, 0xFA5, 0xFA7, (int)Buttons.DirNext);
        builder.AddButton(133, 167, 0xFAE, 0xFB0, (int)Buttons.DirPrev);

        // material
        builder.AddHtmlLocalized(133, 211, 120, 20, 1076171, 0x7FFF); // Choose Material
        builder.AddHtmlLocalized(133, 231, 120, 20, GetMaterialNumber(_statue.StatueType, _statue.Material), 0x77E);
        builder.AddButton(163, 253, 0xFA5, 0xFA7, (int)Buttons.MatNext);
        builder.AddButton(133, 253, 0xFAE, 0xFB0, (int)Buttons.MatPrev);

        // cancel
        builder.AddButton(10, 294, 0xFB1, 0xFB2, (int)Buttons.Close);
        builder.AddHtmlLocalized(45, 294, 80, 20, 1006045, 0x7FFF); // Cancel

        // sculpt
        builder.AddButton(234, 294, 0xFB7, 0xFB9, (int)Buttons.Sculpt);
        builder.AddHtmlLocalized(269, 294, 80, 20, 1076174, 0x7FFF); // Sculpt

        // restore
        if (_maker is CharacterStatueDeed)
        {
            builder.AddButton(107, 294, 0xFAB, 0xFAD, (int)Buttons.Restore);
            builder.AddHtmlLocalized(142, 294, 80, 20, 1076193, 0x7FFF); // Restore
        }
    }

    private static int GetMaterialNumber(StatueType type, StatueMaterial material) =>
        material switch
        {
            StatueMaterial.Antique => type switch
            {
                StatueType.Bronze => 1076187,
                StatueType.Jade   => 1076186,
                StatueType.Marble => 1076182,
                _                 => 1076187
            },
            StatueMaterial.Dark   => type == StatueType.Marble ? 1076183 : 1076182,
            StatueMaterial.Medium => 1076184,
            StatueMaterial.Light  => 1076185,
            _                     => 1076187
        };

    private static int GetDirectionNumber(Direction direction) =>
        direction switch
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

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (_statue?.Deleted != false)
        {
            return;
        }

        var sendGump = false;

        if (info.ButtonID == (int)Buttons.Sculpt)
        {
            if (_maker is CharacterStatueDeed deed)
            {
                var backup = deed.Statue;

                backup?.Delete();
            }

            _maker?.Delete();

            _statue.Sculpt(state.Mobile);
        }
        else if (info.ButtonID == (int)Buttons.PosePrev)
        {
            _statue.Pose = (StatuePose)(((int)_statue.Pose + 5) % 6);
            sendGump = true;
        }
        else if (info.ButtonID == (int)Buttons.PoseNext)
        {
            _statue.Pose = (StatuePose)(((int)_statue.Pose + 1) % 6);
            sendGump = true;
        }
        else if (info.ButtonID == (int)Buttons.DirPrev)
        {
            _statue.Direction = (Direction)(((int)_statue.Direction + 7) % 8);
            _statue.InvalidatePose();
            sendGump = true;
        }
        else if (info.ButtonID == (int)Buttons.DirNext)
        {
            _statue.Direction = (Direction)(((int)_statue.Direction + 1) % 8);
            _statue.InvalidatePose();
            sendGump = true;
        }
        else if (info.ButtonID == (int)Buttons.MatPrev)
        {
            _statue.Material = (StatueMaterial)(((int)_statue.Material + 3) % 4);
            sendGump = true;
        }
        else if (info.ButtonID == (int)Buttons.MatNext)
        {
            _statue.Material = (StatueMaterial)(((int)_statue.Material + 1) % 4);
            sendGump = true;
        }
        else if (info.ButtonID == (int)Buttons.Restore)
        {
            if (_maker is CharacterStatueDeed deed)
            {
                var backup = deed.Statue;

                if (backup != null)
                {
                    _statue.Restore(backup);
                }
            }

            sendGump = true;
        }
        else // Close
        {
            sendGump = !_statue.Demolish(state.Mobile);
        }

        if (sendGump)
        {
            state.Mobile.SendGump(this);
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
