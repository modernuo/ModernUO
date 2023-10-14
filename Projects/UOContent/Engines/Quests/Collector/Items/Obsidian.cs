using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Network;
using Server.Targeting;

namespace Server.Engines.Quests.Collector;

[SerializationGenerator(0, false)]
public partial class Obsidian : Item
{
    private const int Partial = 2;
    private const int Completed = 10;

    private static readonly string[] _names =
    {
        null,
        "an aggressive cavalier",
        "a beguiling rogue",
        "a benevolent physician",
        "a brilliant artisan",
        "a capricious adventurer",
        "a clever beggar",
        "a convincing charlatan",
        "a creative inventor",
        "a creative tinker",
        "a cunning knave",
        "a dauntless explorer",
        "a despicable ruffian",
        "an earnest malcontent",
        "an exultant animal tamer",
        "a famed adventurer",
        "a fanatical crusader",
        "a fastidious clerk",
        "a fearless hunter",
        "a festive harlequin",
        "a fidgety assassin",
        "a fierce soldier",
        "a fierce warrior",
        "a frugal magnate",
        "a glib pundit",
        "a gnomic shaman",
        "a graceful noblewoman",
        "a idiotic madman",
        "a imaginative designer",
        "an inept conjurer",
        "an innovative architect",
        "an inventive blacksmith",
        "a judicious mayor",
        "a masterful chef",
        "a masterful woodworker",
        "a melancholy clown",
        "a melodic bard",
        "a merciful guard",
        "a mirthful jester",
        "a nervous surgeon",
        "a peaceful scholar",
        "a prolific gardener",
        "a quixotic knight",
        "a regal aristocrat",
        "a resourceful smith",
        "a reticent alchemist",
        "a sanctified priest",
        "a scheming patrician",
        "a shrewd mage",
        "a singing minstrel",
        "a skilled tailor",
        "a squeamish assassin",
        "a stoic swordsman",
        "a studious scribe",
        "a thought provoking writer",
        "a treacherous scoundrel",
        "a troubled poet",
        "an unflappable wizard",
        "a valiant warrior",
        "a wayward fool"
    };

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _statueName;

    [Constructible]
    public Obsidian() : base(0x1EA7)
    {
        Hue = 0x497;

        _quantity = 1;
        _statueName = "";
    }

    [EncodedInt]
    [SerializableProperty(0)]
    [CommandProperty(AccessLevel.GameMaster)]
    public int Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value switch
            {
                <= 1           => 1,
                >= Completed => Completed,
                _              => value
            };

            ItemID = _quantity switch
            {
                < Partial   => 0x1EA7,
                < Completed => 0x1F13,
                _             => 0x12CB
            };

            InvalidateProperties();
            this.MarkDirty();
        }
    }

    public override bool ForceShowProperties => ObjectPropertyList.Enabled;

    public static string RandomName(Mobile from) => _names.RandomElement() ?? from.Name;

    public override void AddNameProperty(IPropertyList list)
    {
        if (_quantity < Partial)
        {
            list.Add(1055137); // a section of an obsidian statue
        }
        else if (_quantity < Completed)
        {
            list.Add(1055138); // a partially reconstructed obsidian statue
        }
        else
        {
            list.Add(1055139, _statueName); // an obsidian statue of ~1_STATUE_NAME~
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        if (_quantity < Partial)
        {
            LabelTo(from, 1055137); // a section of an obsidian statue
        }
        else if (_quantity < Completed)
        {
            LabelTo(from, 1055138); // a partially reconstructed obsidian statue
        }
        else
        {
            LabelTo(from, 1055139, _statueName); // an obsidian statue of ~1_STATUE_NAME~
        }
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (from.Alive && _quantity is >= Partial and < Completed && IsChildOf(from.Backpack))
        {
            list.Add(new DisassembleEntry(this));
        }
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (_quantity < Completed)
        {
            if (!IsChildOf(from.Backpack))
            {
                // Nothing Happens.
                from.NetState.SendMessageLocalized(Serial, ItemID, MessageType.Regular, 0x2C, 3, 500309);
            }
            else
            {
                from.Target = new InternalTarget(this);
            }
        }
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        _statueName?.Intern();
    }

    private class DisassembleEntry : ContextMenuEntry
    {
        private readonly Obsidian _obsidian;

        public DisassembleEntry(Obsidian obsidian) : base(6142) => _obsidian = obsidian;

        public override void OnClick()
        {
            var from = Owner.From;
            if (!_obsidian.Deleted && _obsidian.Quantity >= Partial && _obsidian.Quantity < Completed &&
                _obsidian.IsChildOf(from.Backpack) && from.CheckAlive())
            {
                for (var i = 0; i < _obsidian.Quantity - 1; i++)
                {
                    from.AddToBackpack(new Obsidian());
                }

                _obsidian.Quantity = 1;
            }
        }
    }

    private class InternalTarget : Target
    {
        private readonly Obsidian _obsidian;

        public InternalTarget(Obsidian obsidian) : base(-1, false, TargetFlags.None) => _obsidian = obsidian;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_obsidian.Deleted || _obsidian.Quantity >= Completed || targeted is not Item targ)
            {
                return;
            }

            if (_obsidian.IsChildOf(from.Backpack) && targ.IsChildOf(from.Backpack) && targ is Obsidian targObsidian &&
                targ != _obsidian)
            {
                if (targObsidian.Quantity < Completed)
                {
                    if (targObsidian.Quantity + _obsidian.Quantity <= Completed)
                    {
                        targObsidian.Quantity += _obsidian.Quantity;
                        _obsidian.Delete();
                    }
                    else
                    {
                        var delta = Completed - targObsidian.Quantity;
                        targObsidian.Quantity += delta;
                        _obsidian.Quantity -= delta;
                    }

                    if (targObsidian.Quantity >= Completed)
                    {
                        targObsidian.StatueName = RandomName(from);
                    }

                    from.NetState.SendMessage(
                        targObsidian.Serial,
                        targObsidian.ItemID,
                        MessageType.Regular,
                        0x59,
                        3,
                        true,
                        null,
                        _obsidian.Name,
                        "Something Happened."
                    );

                    return;
                }
            }

            from.NetState.SendMessageLocalized(_obsidian.Serial,
                _obsidian.ItemID,
                MessageType.Regular,
                0x2C,
                3,
                500309, // Nothing Happens.
                _obsidian.Name
            );
        }
    }
}
