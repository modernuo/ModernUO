using System;
using ModernUO.Serialization;
using Server.Engines.Craft;

namespace Server.Items;

[SerializationGenerator( 0 )]
public partial class ScrappersCompendium : Spellbook
{
    [Constructible]
    public ScrappersCompendium()
    {
        Hue = 0x494;
        Attributes.SpellDamage = 25;
        Attributes.LowerManaCost = 10;
        Attributes.CastSpeed = 1;
        Attributes.CastRecovery = 1;
    }

    public ScrappersCompendium( Serial serial )
        : base( serial )
    {
    }

    public override int LabelNumber => 1072940; // scrappers compendium

    public override int OnCraft(
        int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem,
        int resHue
    )
    {
        if ( Utility.RandomDouble() < 0.5 )
        {
            var magery = from.Skills.Magery.Value - 100;

            if ( magery < 0 )
            {
                magery = 0;
            }

            var count = Math.Min( 3, ( int )Math.Round( magery * Utility.RandomDouble() / 5 ) );

            BaseRunicTool.ApplyAttributesTo( this, true, 0, count, 70, 80 );
        }

        Attributes.SpellDamage = 25;
        Attributes.LowerManaCost = 10;
        Attributes.CastSpeed = 1;
        Attributes.CastRecovery = 1;

        if ( makersMark )
        {
            Crafter = from.Name;
        }

        return quality;
    }
}
