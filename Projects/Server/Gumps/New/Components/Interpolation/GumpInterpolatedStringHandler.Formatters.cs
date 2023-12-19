namespace Server.Gumps.Components.Interpolation
{
    public static class GumpInterpolatedStringHandler
    {
        public interface IGumpInterpolationTextFormatter<TSelf>
            where TSelf : IGumpInterpolationTextFormatter<TSelf>
        {
            public string? Begin { get; }
            public string? End { get; }

            public static abstract TSelf Create();
            public static abstract TSelf Create(int color);
        }

        public readonly struct None : IGumpInterpolationTextFormatter<None>
        {
            public string? Begin => null;
            public string? End => null;

            public static None Create() => default;
            public static None Create(int color) => default;
        }

        public readonly struct Centered : IGumpInterpolationTextFormatter<Centered>
        {
            public string? Begin { get; }
            public string? End { get; }

            public Centered()
            {
                Begin = $"<CENTER>";
                End = "</CENTER>";
            }

            public Centered(int color)
            {
                Begin = $"<BASEFONT COLOR=#{color:X6}><CENTER>";
                End = "</CENTER></BASEFONT>";
            }

            public static Centered Create() => new();
            public static Centered Create(int color) => new(color);
        }

        public readonly struct Colored : IGumpInterpolationTextFormatter<Colored>
        {
            public string? Begin { get; }
            public string? End { get; }

            public Colored(int color)
            {
                Begin = $"<BASEFONT COLOR=#{color:X6}>";
                End = "</BASEFONT>";
            }

            public static Colored Create() => default;
            public static Colored Create(int color) => new(color);
        }
    }
}
