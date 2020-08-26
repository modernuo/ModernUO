/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: RefPool.cs - Created: 2020/02/20 - Updated: 2020/07/30        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;

namespace Server.Utilities
{
    /// <summary> A resource reference object that can be disposed. </summary>
    /// <remarks>
    ///     Disposing the reference is expected to return itself back into the
    ///     original pool that created it.
    /// </remarks>
    public interface IRef : IDisposable
    {
    }

    /// <summary>
    ///     Base implementation of the <see cref="IRef" /> interface.
    /// </summary>
    /// <remarks>
    ///     New implementations of <see cref="IRef" /> should either derive from, or mirror
    ///     the functionality of this base implementation.
    /// </remarks>
    /// <typeparam name="TDerived"></typeparam>
    public abstract class BaseRef<TDerived> : IRef where TDerived : IRef
    {
        private readonly RefPool<TDerived> m_Pool;
        public BaseRef(RefPool<TDerived> pool) => m_Pool = pool;

        public void Dispose()
        {
            OnDispose();
            m_Pool.Return((TDerived)(object)this);
        }

        protected abstract void OnDispose();
    }

    /// <summary>
    ///     A resource reference pool that manages a collection of reusable resources.
    /// </summary>
    /// <typeparam name="TRef">The <see cref="IRef" /> resource type the pool will contain.</typeparam>
    public class RefPool<TRef> where TRef : IRef
    {
        public delegate TRef Generator(RefPool<TRef> targetPool);

        public const int DEFAULT_RESOURCE_RETENTION = 10;
        private readonly Generator m_Generator;

        private readonly Stack<TRef> m_Resources = new Stack<TRef>();
        private int m_MaxRefrenceRetention;

        /// <param name="generator">The generator function for creating new resources.</param>
        /// <param name="preGenerateCount">
        ///     An amount of resources that should be pre-generated during initialization of the resource
        ///     pool.
        /// </param>
        public RefPool(Generator generator, int preGenerateCount = 0, int maxRefrenceRetention = DEFAULT_RESOURCE_RETENTION)
        {
            if (generator == null)
                throw new ArgumentNullException(nameof(generator));
            if (preGenerateCount > maxRefrenceRetention)
                throw new IndexOutOfRangeException(
                    $"{nameof(preGenerateCount)} greater than {nameof(maxRefrenceRetention)}"
                );
            m_Generator = generator;
            m_MaxRefrenceRetention = maxRefrenceRetention;
            while (--preGenerateCount >= 0) m_Resources.Push(generator(this));
        }

        /// <summary>
        ///     The maximum number of unused resources to hold in the pool.
        /// </summary>
        public int MaxRefrenceRetention
        {
            get => m_MaxRefrenceRetention;
            set
            {
                m_MaxRefrenceRetention = value;
                while (m_Resources.Count > value) m_Resources.Pop();
            }
        }

        /// <summary>
        ///     Retrieves a resource reference that is managed by this <see cref="RefPool{TRef}" />. If the pool is has unused
        ///     resources,
        ///     it will remove one from the pool and return it; otherwise, a new resource will be generated.
        /// </summary>
        /// <returns>Unused resource, or a new resource if no unused resources available.</returns>
        public TRef Get() => m_Resources.TryPop(out var item) ? item : m_Generator(this);

        /// <summary>
        ///     Returns a resource reference to the pool of unused resources.
        /// </summary>
        /// <param name="queueRef">Resource to be returned.</param>
        public void Return(TRef queueRef)
        {
            if (m_Resources.Count < MaxRefrenceRetention)
                m_Resources.Push(queueRef);
        }
    }

    public class QueueRef<T> : Queue<T>, IRef
    {
        /// <summary>
        ///     Generator function for creating instances of the <see cref="QueueRef{T}" /> resource.
        /// </summary>
        public static RefPool<QueueRef<T>>.Generator Generate = targetPool => new QueueRef<T>(targetPool);

        private readonly RefPool<QueueRef<T>> m_Pool;
        private QueueRef(RefPool<QueueRef<T>> pool) => m_Pool = pool;

        /// <summary>Clears the queue and returns this resource to its parent resource pool.</summary>
        public void Dispose()
        {
            Clear();
            m_Pool.Return(this);
        }
    }

    public class StackRef<T> : Stack<T>, IRef
    {
        /// <summary>
        ///     Generator function for creating instances of the <see cref="StackRef{T}" /> resource.
        /// </summary>
        public static RefPool<StackRef<T>>.Generator Generate = targetPool => new StackRef<T>(targetPool);

        private readonly RefPool<StackRef<T>> m_Pool;
        private StackRef(RefPool<StackRef<T>> pool) => m_Pool = pool;

        /// <summary>Clears the stack and returns this resource to its parent resource pool.</summary>
        public void Dispose()
        {
            Clear();
            m_Pool.Return(this);
        }
    }
}
