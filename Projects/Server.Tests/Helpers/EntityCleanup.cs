using System;
using System.Collections.Generic;

namespace Server.Tests;

/// <summary>
/// Deletes tracked entities on dispose, so a test that fails mid-way does not leave them in the shared World.
/// Null and already-deleted entities are skipped.
/// </summary>
internal sealed class EntityCleanup : IDisposable
{
    private readonly List<IEntity> _entities = [];

    public T Track<T>(T entity) where T : IEntity
    {
        _entities.Add(entity);
        return entity;
    }

    public void Dispose()
    {
        // Reverse order deletes children before the containers or owners that hold them.
        for (var i = _entities.Count - 1; i >= 0; i--)
        {
            if (_entities[i] is { Deleted: false } entity)
            {
                entity.Delete();
            }
        }

        _entities.Clear();
    }
}
