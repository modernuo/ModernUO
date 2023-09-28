namespace Server.Common.Filters;

public interface IFilter<in TIn, out TOut>
{
    TOut? Invoke(TIn instance);
}
