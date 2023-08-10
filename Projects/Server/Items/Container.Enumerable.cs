using Server.Collections;
using Server.Common.Filters;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Server.Items;

public partial class Container
{
    private static readonly Queue<Container> containersForGetItem = new();

    public AllItemsEnumerable<IdentityFilter<Item>, Item> GetAllItems() => new(this);

    public AllItemsEnumerable<TFilter, TResult> GetAllItems<TFilter, TResult>(TFilter filter = default)
        where TFilter : struct, IFilter<Item, TResult>
        => new(this, filter);

    public AllItemsEnumerable<PredicateFilter<Item>, Item> GetAllItems(Predicate<Item> predicate)
        => new(this, new(predicate));

    public AllItemsEnumerable<TypeFilter<Item, TResult>, TResult> GetAllItems<TResult>()
        => new(this, default);

    public AllItemsEnumerable<TypeFilter<Item>, Item> GetAllItems(Type type)
        => new(this, new(type));

    public readonly ref struct AllItemsEnumerable<TFilter, TResult>
        where TFilter : struct, IFilter<Item, TResult>
    {
        private readonly Container container;
        private readonly TFilter filter;

        public AllItemsEnumerable(Container container, TFilter filter = default)
        {
            this.container = container;
            this.filter = filter;
        }

        public AllItemsEnumerable<ChainFilter<Item, TResult, TTResult, TFilter, TTFilter>, TTResult> Where<TTFilter, TTResult>(TTFilter filter = default)
            where TTFilter : struct, IFilter<TResult, TTResult>
        {
            return new(container, new(this.filter, filter));
        }

        public AllItemsEnumerable<ChainFilter<Item, TResult, TTResult, TFilter, TypeFilter<TResult, TTResult>>, TTResult> OfType<TTResult>()
        {
            return new(container, new(filter, default));
        }

        public AllItemsEnumerable<ChainFilter<Item, TResult, TResult, TFilter, TakeFilter<TResult>>, TResult> Take(int amount)
        {
            return new(container, new(filter, new(amount)));
        }

        public TResult? FirstOrDefault(TResult? @default = default)
        {
            if (container.m_Items is not { Count: > 0 })
            {
                return @default;
            }

            Container? current = container;

            do
            {
                Span<Item> itemsSpan = CollectionsMarshal.AsSpan(current.m_Items);

                for (int i = 0; i < itemsSpan.Length; i++)
                {
                    Item item = itemsSpan[i];

                    if (item is TResult tItem)
                    {
                        containersForGetItem.Clear();
                        return tItem;
                    }

                    if (item is Container { m_Items.Count: > 0 } c)
                    {
                        containersForGetItem.Enqueue(c);
                    }
                }
            }
            while (containersForGetItem.TryDequeue(out current));

            containersForGetItem.Clear();

            return @default;
        }

        public bool Any() => FirstOrDefault() != null;

        public PooledRefQueue<TResult> ToPooledRefQueue()
        {
            PooledRefQueue<TResult> toRet = PooledRefQueue<TResult>.Create();

            foreach (TResult item in this)
            {
                toRet.Enqueue(item);
            }

            return toRet;
        }

        public PooledRefList<TResult> ToPooledRefList()
        {
            PooledRefList<TResult> toRet = PooledRefList<TResult>.Create();

            foreach (TResult item in this)
            {
                toRet.Add(item);
            }

            return toRet;
        }

        public List<TResult> ToList()
        {
            List<TResult> toRet = new();

            foreach (TResult item in this)
            {
                toRet.Add(item);
            }

            return toRet;
        }

        public AllItemsEnumerator<TFilter, TResult> GetEnumerator() => new(container, filter);
    }

    public ref struct AllItemsEnumerator<TFilter, TResult>
        where TFilter : IFilter<Item, TResult>
    {
        private readonly PooledRefQueue<Container> containers;
        private readonly TFilter filter;
        private Span<Item> items;
        private int index;

        public TResult Current { get; private set; }

        public AllItemsEnumerator(Container container, TFilter filter)
        {
            this.filter = filter;

            containers = PooledRefQueue<Container>.Create();

            if (container.m_Items is not null)
            {
                items = CollectionsMarshal.AsSpan(container.m_Items);
            }

            Current = default!;
        }

        public bool MoveNext()
        {
            return SetNextItem() || SetNextContainer() && SetNextItem();
        }

        private bool SetNextContainer()
        {
            if (!containers.TryDequeue(out Container? c))
            {
                return false;
            }

            items = CollectionsMarshal.AsSpan(c.m_Items);
            index = 0;
            return true;
        }

        private bool SetNextItem()
        {
            while (index < items.Length)
            {
                Item item = items[index++];
                if (item is Container { m_Items.Count: > 0 } c)
                {
                    containers.Enqueue(c);
                }

                TResult? convertedItem = filter.Invoke(item);
                if (convertedItem is not null)
                {
                    Current = convertedItem;
                    return true;
                }
            }

            return false;
        }

        public readonly void Dispose()
        {
            containers.Dispose();
        }
    }

    public ref struct TestItemsEnumerator<TFilter, TResult>
        where TFilter : IFilter<Item, TResult>
    {
        private readonly TFilter filter;
        private readonly Span<Item> items;
        private int index;

        public TResult Current { get; private set; }

        public TestItemsEnumerator(Container container, TFilter filter)
        {
            this.filter = filter;

            if (container.m_Items is not null)
            {
                items = CollectionsMarshal.AsSpan(container.m_Items);
            }

            Current = default!;
        }

        public bool MoveNext()
        {
            while (index < items.Length)
            {
                TResult? convertedItem = filter.Invoke(items[index++]);

                if (convertedItem is not null)
                {
                    Current = convertedItem;
                    return true;
                }
            }

            return false;
        }
    }
}
