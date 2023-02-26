using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[Flippable(0x1053, 0x1054)]
[SerializationGenerator(2)]
public partial class DawnsMusicGear : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private MusicName _music;

    [Constructible]
    public DawnsMusicGear() : this(DawnsMusicBox.RandomTrack(DawnsMusicRarity.Common))
    {
    }

    [Constructible]
    public DawnsMusicGear(MusicName music) : base(0x1053)
    {
        _music = music;
        Weight = 1.0;
    }

    public static DawnsMusicGear RandomCommon => new(DawnsMusicBox.RandomTrack(DawnsMusicRarity.Common));

    public static DawnsMusicGear RandomUncommon => new(DawnsMusicBox.RandomTrack(DawnsMusicRarity.Uncommon));

    public static DawnsMusicGear RandomRare => new(DawnsMusicBox.RandomTrack(DawnsMusicRarity.Rare));

    public override void AddNameProperty(IPropertyList list)
    {
        var info = DawnsMusicBox.GetInfo(Music);

        if (info == null)
        {
            base.AddNameProperty(list);
            return;
        }

        if (info.Rarity == DawnsMusicRarity.Common)
        {
            list.Add(1075204); // Gear for Dawn's Music Box (Common)
        }
        else if (info.Rarity == DawnsMusicRarity.Uncommon)
        {
            list.Add(1075205); // Gear for Dawn's Music Box (Uncommon)
        }
        else if (info.Rarity == DawnsMusicRarity.Rare)
        {
            list.Add(1075206); // Gear for Dawn's Music Box (Rare)
        }

        list.Add(info.Name);
    }

    public override void OnDoubleClick(Mobile from)
    {
        from.Target = new InternalTarget(this);
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        Music = (MusicName)reader.ReadInt();
    }

    public class InternalTarget : Target
    {
        private readonly DawnsMusicGear _gear;

        public InternalTarget(DawnsMusicGear gear) : base(2, false, TargetFlags.None) => _gear = gear;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (_gear?.Deleted != false)
            {
                return;
            }

            if (targeted is not DawnsMusicBox box)
            {
                from.SendLocalizedMessage(1071964); // Gears can only be put into a musicbox.
                return;
            }

            if (!box.Tracks.Contains(_gear.Music))
            {
                box.Tracks.Add(_gear.Music);
                from.SendLocalizedMessage(1071962); // This song track is already in the musicbox.
                return;
            }

            box.InvalidateProperties();
            _gear.Delete();
            from.SendLocalizedMessage(1071961); // This song has been added to the musicbox.
        }
    }
}
