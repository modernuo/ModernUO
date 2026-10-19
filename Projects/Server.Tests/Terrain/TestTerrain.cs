using System;
using System.Collections.Generic;
using Server.Engines.Pathing.Cache;

namespace Server.Tests.Terrain;

/// <summary>
/// Stamps land and statics into the synthetic terrain. Felucca and Trammel share one terrain, so every write
/// goes to both. Once <see cref="Prefabs.StampAll"/> has run, writes are only allowed inside a
/// <see cref="Scope"/>, which puts back every block it touched when disposed.
/// </summary>
public static class TestTerrain
{
    // Blocks this class allocated, keyed by matrix and block. Anything else is the matrix's shared default
    // block, which must never be written to.
    private static readonly Dictionary<(TileMatrix, int, int), LandTile[]> _land = [];
    private static readonly Dictionary<(TileMatrix, int, int), StaticTile[][][]> _statics = [];

    private static TerrainScope _scope;

    internal static bool Sealed { get; set; }

    public static TerrainScope Scope()
    {
        if (_scope != null)
        {
            throw new InvalidOperationException("A terrain scope is already open; dispose it first.");
        }

        // Cached step masks describe the terrain as it was; drop them so nothing serves the old cells.
        StepCache.Instance.Clear();
        return _scope = new TerrainScope();
    }

    public static void SetLand(int x, int y, short id, sbyte z)
    {
        var facets = Facets();
        for (var i = 0; i < facets.Length; i++)
        {
            LandBlock(facets[i].Tiles, x >> 3, y >> 3)[((y & 7) << 3) + (x & 7)] = new LandTile(id, z);
        }
    }

    public static void FillLand(Rectangle2D area, short id, sbyte z)
    {
        for (var x = area.Start.X; x < area.End.X; x++)
        {
            for (var y = area.Start.Y; y < area.End.Y; y++)
            {
                SetLand(x, y, id, z);
            }
        }
    }

    public static void AddStatic(int x, int y, ushort id, sbyte z)
    {
        var facets = Facets();
        for (var i = 0; i < facets.Length; i++)
        {
            var column = StaticBlock(facets[i].Tiles, x >> 3, y >> 3)[x & 7];
            column[y & 7] = [..column[y & 7], new StaticTile(id, z)];
        }
    }

    public static void ClearStatics(int x, int y)
    {
        var facets = Facets();
        for (var i = 0; i < facets.Length; i++)
        {
            StaticBlock(facets[i].Tiles, x >> 3, y >> 3)[x & 7][y & 7] = [];
        }
    }

    internal static void EndScope(TerrainScope scope)
    {
        foreach (var ((tiles, bx, by), original) in scope.Land)
        {
            if (original == null)
            {
                _land.Remove((tiles, bx, by));
            }
            else
            {
                _land[(tiles, bx, by)] = original;
            }

            tiles.SetLandBlock(bx, by, original);
        }

        foreach (var ((tiles, bx, by), original) in scope.Statics)
        {
            if (original == null)
            {
                _statics.Remove((tiles, bx, by));
            }
            else
            {
                _statics[(tiles, bx, by)] = original;
            }

            tiles.SetStaticBlock(bx, by, original);
        }

        _scope = null;
        StepCache.Instance.Clear();
    }

    private static Map[] Facets()
    {
        if (Sealed && _scope == null)
        {
            throw new InvalidOperationException("Stamp terrain inside TestTerrain.Scope() so it is put back afterwards.");
        }

        return [Map.Felucca, Map.Trammel];
    }

    private static LandTile[] LandBlock(TileMatrix tiles, int bx, int by)
    {
        var key = (tiles, bx, by);
        _land.TryGetValue(key, out var owned);

        // The scope keeps the block as it was and the stamp goes into a copy, so disposing is a swap back.
        if (_scope != null && !_scope.Land.ContainsKey(key))
        {
            _scope.Land[key] = owned;
            owned = (LandTile[])owned?.Clone();
        }
        else if (owned != null)
        {
            return owned;
        }

        var block = owned ?? new LandTile[64];
        _land[key] = block;
        tiles.SetLandBlock(bx, by, block);
        return block;
    }

    private static StaticTile[][][] StaticBlock(TileMatrix tiles, int bx, int by)
    {
        var key = (tiles, bx, by);
        _statics.TryGetValue(key, out var owned);

        if (_scope != null && !_scope.Statics.ContainsKey(key))
        {
            _scope.Statics[key] = owned;
            owned = owned == null ? null : CopyStatics(owned);
        }
        else if (owned != null)
        {
            return owned;
        }

        var block = owned ?? CopyStatics(tiles.EmptyStaticBlock);
        _statics[key] = block;
        tiles.SetStaticBlock(bx, by, block);
        return block;
    }

    private static StaticTile[][][] CopyStatics(StaticTile[][][] source)
    {
        var copy = new StaticTile[8][][];
        for (var x = 0; x < 8; x++)
        {
            copy[x] = new StaticTile[8][];
            for (var y = 0; y < 8; y++)
            {
                copy[x][y] = source[x][y];
            }
        }

        return copy;
    }
}

/// <summary>
/// Bespoke terrain for one test. Dispose it (a <c>using</c>) to put back every block it touched.
/// </summary>
public sealed class TerrainScope : IDisposable
{
    // The block each key held before this scope first wrote to it; null means it was the default.
    internal Dictionary<(TileMatrix, int, int), LandTile[]> Land { get; } = [];
    internal Dictionary<(TileMatrix, int, int), StaticTile[][][]> Statics { get; } = [];

    private bool _disposed;

    internal TerrainScope()
    {
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        TestTerrain.EndScope(this);
    }
}
