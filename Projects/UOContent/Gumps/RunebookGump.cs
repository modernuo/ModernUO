using System;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Prompts;
using Server.Spells.Chivalry;
using Server.Spells.Fourth;
using Server.Spells.Seventh;

namespace Server.Gumps;

public class RunebookGump : DynamicGump
{
    private readonly Runebook _book;
    public Runebook Book => _book;

    public override bool Singleton => true;

    public RunebookGump(Runebook book) : base(150, 200) => _book = book;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        AddBackground(ref builder);
        AddIndex(ref builder);

        for (var page = 0; page < 8; ++page)
        {
            builder.AddPage(2 + page);

            builder.AddButton(125, 14, 2205, 2205, 0, GumpButtonType.Page, 1 + page);

            if (page < 7)
            {
                builder.AddButton(393, 14, 2206, 2206, 0, GumpButtonType.Page, 3 + page);
            }

            for (var half = 0; half < 2; ++half)
            {
                AddDetails(ref builder, page * 2 + half, half);
            }
        }
    }

    public static int GetMapHue(Map map)
    {
        if (map == Map.Trammel)
        {
            return 10;
        }

        if (map == Map.Felucca)
        {
            return 81;
        }

        if (map == Map.Ilshenar)
        {
            return 1102;
        }

        if (map == Map.Malas)
        {
            return 1102;
        }

        if (map == Map.Tokuno)
        {
            return 1154;
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetName(ReadOnlySpan<char> name) => name.IsEmpty ? "(indescript)" : name.Trim();

    private void AddBackground(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        // Background image
        builder.AddImage(100, 10, 2200);

        // Two separators
        for (var i = 0; i < 2; ++i)
        {
            var xOffset = 125 + i * 165;

            builder.AddImage(xOffset, 50, 57);
            xOffset += 20;

            for (var j = 0; j < 6; ++j, xOffset += 15)
            {
                builder.AddImage(xOffset, 50, 58);
            }

            builder.AddImage(xOffset - 5, 50, 59);
        }

        // First four page buttons
        for (int i = 0, xOffset = 130, gumpID = 2225; i < 4; ++i, xOffset += 35, ++gumpID)
        {
            builder.AddButton(xOffset, 187, gumpID, gumpID, 0, GumpButtonType.Page, 2 + i);
        }

        // Next four page buttons
        for (int i = 0, xOffset = 300, gumpID = 2229; i < 4; ++i, xOffset += 35, ++gumpID)
        {
            builder.AddButton(xOffset, 187, gumpID, gumpID, 0, GumpButtonType.Page, 6 + i);
        }

        // Charges
        builder.AddHtmlLocalized(140, 40, 80, 18, 1011296); // Charges:
        builder.AddHtml(220, 40, 30, 18, $"{_book.CurCharges}");

        // Max charges
        builder.AddHtmlLocalized(300, 40, 100, 18, 1011297); // Max Charges:
        builder.AddHtml(400, 40, 30, 18, $"{_book.MaxCharges}");
    }

    private void AddIndex(ref DynamicGumpBuilder builder)
    {
        // Index
        builder.AddPage(1);

        // Rename button
        builder.AddButton(125, 15, 2472, 2473, 1);
        builder.AddHtmlLocalized(158, 22, 100, 18, 1011299); // Rename book

        // List of entries
        var entries = _book.Entries;

        for (var i = 0; i < 16; ++i)
        {
            // Use charge button
            builder.AddButton(130 + i / 8 * 160, 65 + i % 8 * 15, 2103, 2104, 2 + i * 6 + 0);

            // Description label
            if (i < entries.Count)
            {
                var entry = entries[i];

                builder.AddLabelCropped(
                    145 + i / 8 * 160,
                    60 + i % 8 * 15,
                    115,
                    17,
                    GetMapHue(entry.Map),
                    GetName(entry.Description)
                );
            }
            else
            {
                builder.AddLabelCropped(145 + i / 8 * 160, 60 + i % 8 * 15, 115, 17, 0, "Empty");
            }
        }

        // Turn page button
        builder.AddButton(393, 14, 2206, 2206, 0, GumpButtonType.Page, 2);
    }

    private void AddDetails(ref DynamicGumpBuilder builder, int index, int half)
    {
        // Use charge button
        builder.AddButton(130 + half * 160, 65, 2103, 2104, 2 + index * 6 + 0);

        if (index < _book.Entries.Count)
        {
            var e = _book.Entries[index];

            // Location labels
            var xLong = 0;
            var yLat = 0;
            var xMins = 0;
            var yMins = 0;
            var xEast = false;
            var ySouth = false;

            if (Sextant.Format(e.Location, e.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
            {
                builder.AddLabel(135 + half * 160, 80, 0, $"{yLat}° {yMins}'{(ySouth ? "S" : "N")}");
                builder.AddLabel(135 + half * 160, 95, 0, $"{xLong}° {xMins}'{(xEast ? "E" : "W")}");
            }

            // Drop rune button
            builder.AddButton(135 + half * 160, 115, 2437, 2438, 2 + index * 6 + 1);
            builder.AddHtmlLocalized(150 + half * 160, 115, 100, 18, 1011298); // Drop rune

            // Set as default button
            var defButtonID = e != _book.Default ? 2361 : 2360;

            builder.AddButton(160 + half * 140, 20, defButtonID, defButtonID, 2 + index * 6 + 2);
            builder.AddHtmlLocalized(175 + half * 140, 15, 100, 18, 1011300); // Set default

            if (Core.AOS)
            {
                builder.AddButton(135 + half * 160, 140, 2103, 2104, 2 + index * 6 + 3);
                builder.AddHtmlLocalized(150 + half * 160, 136, 110, 20, 1062722); // Recall

                builder.AddButton(135 + half * 160, 158, 2103, 2104, 2 + index * 6 + 4);
                builder.AddHtmlLocalized(150 + half * 160, 154, 110, 20, 1062723); // Gate Travel

                builder.AddButton(135 + half * 160, 176, 2103, 2104, 2 + index * 6 + 5);
                builder.AddHtmlLocalized(150 + half * 160, 172, 110, 20, 1062724); // Sacred Journey
            }
            else
            {
                // Recall button
                builder.AddButton(135 + half * 160, 140, 2271, 2271, 2 + index * 6 + 3);

                // Gate button
                builder.AddButton(205 + half * 160, 140, 2291, 2291, 2 + index * 6 + 4);
            }

            // Description label
            builder.AddLabelCropped(145 + half * 160, 60, 115, 17, GetMapHue(e.Map), GetName(e.Description));
        }
        else
        {
            // Description label
            builder.AddLabelCropped(145 + half * 160, 60, 115, 17, 0, "Empty");
        }
    }

    public static bool HasSpell(Mobile from, int spellID) => Spellbook.Find(from, spellID)?.HasSpell(spellID) == true;

    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;

        _book.Openers.Remove(from);

        if (_book.Deleted || !from.InRange(_book.GetWorldLocation(), Core.ML ? 3 : 1) || !DesignContext.Check(from))
        {
            return;
        }

        var buttonID = info.ButtonID;

        if (buttonID == 1) // Rename book
        {
            if (!_book.IsLockedDown || from.AccessLevel >= AccessLevel.GameMaster)
            {
                from.SendLocalizedMessage(502414); // Please enter a title for the runebook:
                from.Prompt = new InternalPrompt(_book);
            }
            else
            {
                from.SendLocalizedMessage(502413, null, 0x35); // That cannot be done while the book is locked down.
            }

            return;
        }

        buttonID -= 2;

        var index = Math.DivRem(buttonID, 6, out var type);

        if (index < 0 || index >= _book.Entries.Count)
        {
            return;
        }

        var e = _book.Entries[index];

        switch (type)
        {
            case 0: // Use charges
                {
                    if (_book.CurCharges <= 0)
                    {
                        from.SendLocalizedMessage(502412); // There are no charges left on that item.
                    }
                    else
                    {
                        SendTargetCoordinates(from, e.Location, e.Map);

                        _book.OnTravel();
                        new RecallSpell(from, e, _book, _book).Cast();
                    }

                    break;
                }
            case 1: // Drop rune
                {
                    if (!_book.IsLockedDown || from.AccessLevel >= AccessLevel.GameMaster)
                    {
                        _book.DropRune(from, e, index);

                        if (!Core.ML)
                        {
                            _book.SendGumpTo(from);
                        }
                    }
                    else
                    {
                        // That cannot be done while the book is locked down.
                        from.SendLocalizedMessage(502413, null, 0x35);
                    }

                    break;
                }
            case 2: // Set default
                {
                    if (_book.CheckAccess(from))
                    {
                        _book.Default = e;

                        // Resend the runebook gump
                        _book.SendGumpTo(from);

                        from.SendLocalizedMessage(502417); // New default location set.
                    }

                    break;
                }
            case 3: // Recall
                {
                    if (!HasSpell(from, 31))
                    {
                        from.SendLocalizedMessage(500015); // You do not have that spell!
                        break;
                    }

                    SendTargetCoordinates(from, e.Location, e.Map);

                    _book.OnTravel();
                    new RecallSpell(from, e).Cast();
                    break;
                }
            case 4: // Gate
                {
                    if (!HasSpell(from, 51))
                    {
                        from.SendLocalizedMessage(500015); // You do not have that spell!
                        break;
                    }

                    SendTargetCoordinates(from, e.Location, e.Map);

                    _book.OnTravel();
                    new GateTravelSpell(from, e).Cast();
                    break;
                }
            case 5: // Sacred Journey
                {
                    if (Core.AOS)
                    {
                        if (!HasSpell(from, 209))
                        {
                            from.SendLocalizedMessage(500015); // You do not have that spell!
                            _book.Openers.Remove(from);
                            break;
                        }

                        SendTargetCoordinates(from, e.Location, e.Map);

                        _book.OnTravel();
                        new SacredJourneySpell(from, e).Cast();
                    }
                    break;
                }
        }
    }

    private static void SendTargetCoordinates(Mobile to, Point3D location, Map map)
    {
        var xLong = 0;
        var yLat = 0;
        var xMins = 0;
        var yMins = 0;
        bool xEast = false, ySouth = false;

        if (Sextant.Format(
                location,
                map,
                ref xLong,
                ref yLat,
                ref xMins,
                ref yMins,
                ref xEast,
                ref ySouth
            ))
        {
            var locationString =
                $"{yLat}° {yMins}'{(ySouth ? "S" : "N")}, {xLong}' {xMins}'{(xEast ? "E" : "W")}";
            to.SendMessage(locationString);
        }
    }

    private class InternalPrompt : Prompt
    {
        private readonly Runebook _book;

        public InternalPrompt(Runebook book) => _book = book;

        public override void OnResponse(Mobile from, string text)
        {
            if (_book.Deleted || !from.InRange(_book.GetWorldLocation(), Core.ML ? 3 : 1))
            {
                return;
            }

            if (_book.CheckAccess(from))
            {
                _book.Description = text.AsSpan().Trim().FixHtml();
                _book.SendGumpTo(from);

                from.SendMessage("The book's title has been changed.");
            }
            else
            {
                from.SendLocalizedMessage(502416); // That cannot be done while the book is locked down.
            }
        }

        public override void OnCancel(Mobile from)
        {
            from.SendLocalizedMessage(502415); // Request cancelled.

            if (!_book.Deleted && from.InRange(_book.GetWorldLocation(), Core.ML ? 3 : 1))
            {
                _book.SendGumpTo(from);
            }
        }
    }
}
