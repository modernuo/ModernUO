using System;
using System.Runtime.CompilerServices;

namespace Server.Text;

public interface ISelfInterpolatedStringHandler<T>
{
    public void Add([InterpolatedStringHandlerArgument("")] ref InterpolatedStringHandler handler);
    public void Add(int number, [InterpolatedStringHandlerArgument("")] ref InterpolatedStringHandler handler);
    public void InitializeInterpolation(int literalLength, int formattedCount);
    public void AppendLiteral(string value);
    public void AppendFormatted<T>(T value);
    public void AppendFormatted<T>(T value, string? format);
    public void AppendFormatted<T>(T value, int alignment);
    public void AppendFormatted<T>(T value, int alignment, string? format);
    public void AppendFormatted(ReadOnlySpan<char> value);
    public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string? format = null);
    public void AppendFormatted(object? value, int alignment = 0, string? format = null);
    public void AppendFormatted(string? value);
    public void AppendFormatted(string? value, int alignment, string? format = null);

    [InterpolatedStringHandler]
    public ref struct InterpolatedStringHandler
    {
        private ISelfInterpolatedStringHandler<T> _parent;

        public InterpolatedStringHandler(int literalLength, int formattedCount, ISelfInterpolatedStringHandler<T> parent)
        {
            _parent = parent;
            _parent.InitializeInterpolation(literalLength, formattedCount);
        }

        public void AppendLiteral(string value) => _parent.AppendLiteral(value);

        public void AppendFormatted<T>(T value) => _parent.AppendFormatted(value);

        public void AppendFormatted<T>(T value, string? format) => _parent.AppendFormatted(value, format);

        public void AppendFormatted<T>(T value, int alignment) => _parent.AppendFormatted(value, alignment);

        public void AppendFormatted<T>(T value, int alignment, string? format) =>
            _parent.AppendFormatted(value, alignment, format);

        public void AppendFormatted(ReadOnlySpan<char> value) => _parent.AppendFormatted(value);

        public void AppendFormatted(ReadOnlySpan<char> value, int alignment, string? format = null) =>
            _parent.AppendFormatted(value, alignment, format);

        public void AppendFormatted(object? value, int alignment = 0, string? format = null) =>
            _parent.AppendFormatted(value, alignment, format);

        public void AppendFormatted(string? value) => _parent.AppendFormatted(value);

        public void AppendFormatted(string? value, int alignment, string? format = null) =>
            _parent.AppendFormatted(value, alignment, format);
    }
}
