using System;
using Server.Mobiles;

namespace Server;

public class OppositionGroup
{
    private readonly Type[][] _types;

    public OppositionGroup(Type[][] types) => _types = types;

    public static OppositionGroup TerathansAndOphidians { get; } = new(
        [
            [
                typeof(TerathanAvenger),
                typeof(TerathanDrone),
                typeof(TerathanMatriarch),
                typeof(TerathanWarrior)
            ],
            [
                typeof(OphidianArchmage),
                typeof(OphidianKnight),
                typeof(OphidianMage),
                typeof(OphidianMatriarch),
                typeof(OphidianWarrior)
            ]
        ]
    );

    public static OppositionGroup SavagesAndOrcs { get; } = new(
        [
            [
                typeof(Orc),
                typeof(OrcBomber),
                typeof(OrcBrute),
                typeof(OrcCaptain),
                typeof(OrcishLord),
                typeof(OrcishMage),
                typeof(SpawnedOrcishLord)
            ],
            [
                typeof(Savage),
                typeof(SavageRider),
                typeof(SavageRidgeback),
                typeof(SavageShaman)
            ]
        ]
    );

    public static OppositionGroup FeyAndUndead { get; } = new(
        [
            [
                typeof(Centaur),
                typeof(EtherealWarrior),
                typeof(Kirin),
                typeof(LordOaks),
                typeof(Pixie),
                typeof(Silvani),
                typeof(Unicorn),
                typeof(Wisp),
                typeof(Treefellow),
                typeof(MLDryad),
                typeof(Satyr)
            ],
            [
                typeof(AncientLich),
                typeof(Bogle),
                typeof(LichLord),
                typeof(Shade),
                typeof(Spectre),
                typeof(Wraith),
                typeof(BoneKnight),
                typeof(Ghoul),
                typeof(Mummy),
                typeof(SkeletalKnight),
                typeof(Skeleton),
                typeof(Zombie),
                typeof(ShadowKnight),
                typeof(DarknightCreeper),
                typeof(RevenantLion),
                typeof(LadyOfTheSnow),
                typeof(RottingCorpse),
                typeof(SkeletalDragon),
                typeof(Lich)
            ]
        ]
    );

    public bool IsEnemy(object from, object target)
    {
        var fromGroup = IndexOf(from);
        var targGroup = IndexOf(target);

        return fromGroup != -1 && targGroup != -1 && fromGroup != targGroup;
    }

    public int IndexOf(object obj)
    {
        if (obj == null)
        {
            return -1;
        }

        var type = obj.GetType();

        for (var i = 0; i < _types.Length; ++i)
        {
            var group = _types[i];

            var contains = false;

            for (var j = 0; !contains && j < group.Length; ++j)
            {
                contains = group[j].IsAssignableFrom(type);
            }

            if (contains)
            {
                return i;
            }
        }

        return -1;
    }
}
