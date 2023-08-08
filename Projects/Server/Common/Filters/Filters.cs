using System;

namespace Server.Common.Filters;

public readonly struct DummyFilter<T> : IFilter<T, T>
{
    public T? Invoke(T instance)
    {
        return instance;
    }
}

public readonly struct ChainFilter<TIn, TMiddle, TOut, TFirst, TSecond> : IFilter<TIn, TOut>
        where TFirst : IFilter<TIn, TMiddle>
        where TSecond : IFilter<TMiddle, TOut>
{
    private readonly TFirst first;
    private readonly TSecond second;

    public ChainFilter(TFirst first, TSecond second)
    {
        this.first = first;
        this.second = second;
    }

    public TOut? Invoke(TIn instance)
    {
        return first.Invoke(instance) is not { } middle ? default : second.Invoke(middle);
    }
}

public readonly struct PredicateFilter<T> : IFilter<T, T>
{
    private readonly Predicate<T> predicate;

    public PredicateFilter(Predicate<T> predicate)
    {
        this.predicate = predicate;
    }

    public T? Invoke(T instance)
    {
        return predicate(instance) ? instance : default;
    }
}

public struct TakeFilter<T> : IFilter<T, T>
{
    private readonly int toTake;
    private int taken;

    public TakeFilter(int toTake)
    {
        this.toTake = toTake;
    }

    public T? Invoke(T instance)
    {
        return taken++ < toTake ? instance : default;
    }
}

public readonly struct TypeFilter<T> : IFilter<T, T>
{
    private readonly Type type;

    public TypeFilter(Type type)
    {
        this.type = type;
    }

    public T? Invoke(T instance)
    {
        return type.IsInstanceOfType(instance) ? instance : default;
    }
}

public readonly struct TypeFilter<T, TResult> : IFilter<T, TResult>
{
    public TResult? Invoke(T instance)
    {
        return instance is TResult tInstance ? tInstance : default;
    }
}
