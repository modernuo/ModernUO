using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Maps
{
    public static class ExpansionMapSelectionFlags
    {
        public static readonly MapSelectionFlags[] PreT2A = {
            MapSelectionFlags.Felucca
        };

        public static readonly MapSelectionFlags[] T2A = {
            MapSelectionFlags.Trammel, MapSelectionFlags.Felucca
        };

        public static readonly MapSelectionFlags[] LBR = {
            MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar
        };

        public static readonly MapSelectionFlags[] AOS = {
            MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar,
            MapSelectionFlags.Malas
        };

        public static readonly MapSelectionFlags[] SE = {
            MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar,
            MapSelectionFlags.Malas, MapSelectionFlags.Tokuno
        };

        public static readonly MapSelectionFlags[] SA = {
            MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar,
            MapSelectionFlags.Malas, MapSelectionFlags.Tokuno, MapSelectionFlags.TerMur
        };

        public static readonly MapSelectionFlags[] TOL = {
            MapSelectionFlags.Trammel, MapSelectionFlags.Felucca, MapSelectionFlags.Ilshenar,
            MapSelectionFlags.Malas, MapSelectionFlags.Tokuno, MapSelectionFlags.TerMur
        };

        public static MapSelectionFlags[] FromExpansion(Expansion expansion)
        {
            MapSelectionFlags[] mapOptionsForExpansion =
                expansion switch
                {
                    >= Expansion.TOL => ExpansionMapSelectionFlags.TOL,
                    >= Expansion.SA => ExpansionMapSelectionFlags.SA,
                    >= Expansion.SE => ExpansionMapSelectionFlags.SE,
                    >= Expansion.AOS => ExpansionMapSelectionFlags.AOS,
                    >= Expansion.LBR => ExpansionMapSelectionFlags.LBR,
                    >= Expansion.T2A => ExpansionMapSelectionFlags.T2A,
                    _ => ExpansionMapSelectionFlags.PreT2A
                };
            return mapOptionsForExpansion;
        }
    }
}
