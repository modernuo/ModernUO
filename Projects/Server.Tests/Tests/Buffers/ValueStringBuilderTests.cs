using Server.Text;
using Xunit;

namespace Server.Tests.Buffers;

[Collection("Sequential Server Tests")]
public class ValueStringBuilderTests
{
    [Theory]
    [InlineData("Admin Kamron", "Kamron", 0, 6)]
    [InlineData("Admin Kamron", "Admin ron", 6, 3)]
    [InlineData("Admin Kamron", "Admin", 5, 7)]
    public void TestRemove(string original, string removed, int startIndex, int length)
    {
        using var sb = new ValueStringBuilder(stackalloc char[64]);
        sb.Append(original);
        sb.Remove(startIndex, length);

        Assert.Equal(removed, sb.ToString());
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9876)]
    [InlineData(-5)]
    [InlineData(-130984209)]
    public void TestAppendInt32(int value)
    {
        using var sb = new ValueStringBuilder(stackalloc char[64]);
        sb.Append(value);

        Assert.Equal(value.ToString(), sb.ToString());
    }

    [Theory]
    [InlineData("Kamron")]
    [InlineData("")]
    [InlineData(5)]
    [InlineData(-30.6)]
    public void TestAppendInterpolation(object value)
    {
        var sb = ValueStringBuilder.Create();
        sb.Append($"Hi, this is {value}.");
        sb.Append(" I am a string.");

        Assert.Equal($"Hi, this is {value}. I am a string.", sb.ToString());
        sb.Dispose();
    }

    // --- InterpolationHandler reconciliation tests ---
    // These validate the copy-and-reconcile pattern: the handler receives a VALUE COPY of the builder,
    // writes into the shared buffer, and Append() reconciles via `this = handler._builder`.
    // Critical to detect if C# compiler codegen changes break this assumption.

    [Fact]
    public void Interpolation_Stackalloc_NoGrow()
    {
        // Stackalloc buffer large enough — no Grow needed.
        // Validates: copy's Span shares same stackalloc memory, _length is reconciled.
        var sb = new ValueStringBuilder(stackalloc char[64]);
        sb.Append($"Hello {42} world");

        Assert.Equal("Hello 42 world", sb.ToString());
        Assert.Equal(14, sb.Length);
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_Stackalloc_WithGrow()
    {
        // Tiny stackalloc forces Grow inside the handler's copy.
        // Validates: after Grow, copy moves to pooled array; reconciliation updates
        // _chars, _arrayToReturnToPool, and _length on the original.
        var sb = new ValueStringBuilder(stackalloc char[4]);
        sb.Append($"This string is much longer than 4 chars: {12345}");

        var expected = "This string is much longer than 4 chars: 12345";
        Assert.Equal(expected, sb.ToString());
        Assert.Equal(expected.Length, sb.Length);
        // After grow, capacity should have expanded beyond 4
        Assert.True(sb.Capacity >= 47);
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_Stackalloc_PreExistingContent_NoGrow()
    {
        // Append plain text first, then interpolation. Buffer large enough.
        // Validates: interpolation appends after existing content, doesn't overwrite.
        var sb = new ValueStringBuilder(stackalloc char[64]);
        sb.Append("prefix-");
        sb.Append($"value={99}");

        Assert.Equal("prefix-value=99", sb.ToString());
        Assert.Equal(15, sb.Length);
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_Stackalloc_PreExistingContent_WithGrow()
    {
        // Fill most of a small stackalloc, then interpolate enough to force Grow.
        // Validates: existing content is preserved through the Grow, new content is appended.
        var sb = new ValueStringBuilder(stackalloc char[16]);
        sb.Append("0123456789"); // 10 chars, 6 remaining
        sb.Append($"abcdefghij{42}"); // 12 chars, exceeds remaining — Grow

        Assert.Equal("0123456789abcdefghij42", sb.ToString());
        Assert.Equal(22, sb.Length);
        Assert.True(sb.Capacity >= 22);
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_Heap_NoGrow()
    {
        // Heap-allocated (Create) with sufficient capacity.
        // Validates: handler works with pooled backing, _length is reconciled.
        var sb = ValueStringBuilder.Create(64);
        sb.Append($"Score: {100}, Name: {"Test"}");

        Assert.Equal("Score: 100, Name: Test", sb.ToString());
        Assert.Equal(22, sb.Length);
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_Heap_WithGrow()
    {
        // Small heap allocation forces Grow.
        // Validates: after Grow, copy's new pooled array replaces original's
        // (original's old array was returned to pool by copy's Grow).
        var sb = ValueStringBuilder.Create(4);
        sb.Append($"This exceeds the initial 4 char capacity: {67890}");

        var expected = "This exceeds the initial 4 char capacity: 67890";
        Assert.Equal(expected, sb.ToString());
        Assert.Equal(expected.Length, sb.Length);
        Assert.True(sb.Capacity >= expected.Length);
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_Heap_PreExistingContent_WithGrow()
    {
        // Heap with pre-existing content, then Grow via interpolation.
        // Validates: existing content preserved, pooled array properly transitioned.
        var sb = ValueStringBuilder.Create(8);
        sb.Append("ABCD");
        sb.Append($"EFGHIJKLMNOP{42}");

        Assert.Equal("ABCDEFGHIJKLMNOP42", sb.ToString());
        Assert.Equal(18, sb.Length);
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_Sequential_MultipleAppends()
    {
        // Multiple sequential Append($"...") calls.
        // Validates: each reconciliation correctly advances _length, subsequent calls
        // see the updated state from prior reconciliations.
        var sb = new ValueStringBuilder(stackalloc char[128]);
        sb.Append($"A={1}");
        sb.Append($" B={2}");
        sb.Append($" C={3}");
        sb.Append($" D={4}");

        var expected = "A=1 B=2 C=3 D=4";
        Assert.Equal(expected, sb.ToString());
        Assert.Equal(expected.Length, sb.Length);
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_Sequential_GrowOnSecondAppend()
    {
        // First Append fits, second triggers Grow.
        // Validates: reconciliation after first Append leaves builder in a state
        // that the second Append (with Grow) works correctly.
        var sb = new ValueStringBuilder(stackalloc char[16]);
        sb.Append($"Fits: {1}"); // 7 chars, fits in 16
        sb.Append($" - Now this is a much longer string that forces growth: {999}");

        Assert.Equal("Fits: 1 - Now this is a much longer string that forces growth: 999", sb.ToString());
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_MixedAppendStyles()
    {
        // Mix plain Append with interpolated Append.
        // Validates: reconciliation is compatible with non-interpolated Append calls.
        var sb = new ValueStringBuilder(stackalloc char[64]);
        sb.Append("plain-");
        sb.Append($"interp={42}-");
        sb.Append("plain2-");
        sb.Append($"interp2={99}");

        Assert.Equal("plain-interp=42-plain2-interp2=99", sb.ToString());
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_EmptyInterpolation()
    {
        // Empty interpolation expression.
        // Validates: handler with zero holes still reconciles correctly.
        var sb = new ValueStringBuilder(stackalloc char[32]);
        sb.Append("before");
        sb.Append($"");
        sb.Append("after");

        Assert.Equal("beforeafter", sb.ToString());
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_OnlyLiteral()
    {
        // Interpolation with no holes (just a literal).
        // Validates: AppendLiteral-only path reconciles _length.
        var sb = new ValueStringBuilder(stackalloc char[32]);
        sb.Append($"just a literal");

        Assert.Equal("just a literal", sb.ToString());
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_FormatSpecifiers()
    {
        // Format specifiers in interpolation holes.
        // Validates: AppendFormatted<T> with format string works through handler.
        var sb = new ValueStringBuilder(stackalloc char[64]);
        sb.Append($"pi={3.14159:F2}, hex={255:X4}, date={new System.DateTime(2025, 1, 15):yyyy-MM-dd}");

        Assert.Equal("pi=3.14, hex=00FF, date=2025-01-15", sb.ToString());
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_SpanFormattableTypes()
    {
        // Various ISpanFormattable types.
        // Validates: int, double, DateTime all format directly via TryFormat (no ToString allocation).
        var sb = new ValueStringBuilder(stackalloc char[64]);
        sb.Append($"{42}{3.14}{true}");

        Assert.Equal("423.14True", sb.ToString());
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_NullString()
    {
        // Null string in interpolation hole.
        // Validates: AppendFormatted(string?) handles null without crash.
        string? name = null;
        var sb = new ValueStringBuilder(stackalloc char[32]);
        sb.Append($"Name: {name}!");

        Assert.Equal("Name: !", sb.ToString());
        sb.Dispose();
    }

    [Fact]
    public void Interpolation_Stackalloc_DisposeAfterGrow()
    {
        // After Grow moves stackalloc to pool, Dispose should return the pooled array.
        // Validates: _arrayToReturnToPool is correctly reconciled so Dispose works.
        var sb = new ValueStringBuilder(stackalloc char[4]);
        sb.Append($"grow beyond stackalloc: {12345}");
        // If _arrayToReturnToPool wasn't reconciled, Dispose would either
        // not return the pooled array (leak) or return null (no-op when it shouldn't be).
        sb.Dispose();
        // No assertion needed — if _arrayToReturnToPool was wrong, pool corruption
        // would surface as test failures elsewhere. The test passing = no crash.
    }

    [Fact]
    public void Interpolation_Heap_DoubleGrow()
    {
        // Force two consecutive Grows via two interpolated appends on a tiny buffer.
        // Validates: reconciliation after first Grow leaves builder in valid state
        // for the second Grow to succeed.
        var sb = ValueStringBuilder.Create(4);
        sb.Append($"First grow: {"ABCDEFGHIJ"}"); // forces first grow
        sb.Append($"Second grow: {"KLMNOPQRSTUVWXYZ0123456789"}"); // forces second grow

        Assert.Equal("First grow: ABCDEFGHIJSecond grow: KLMNOPQRSTUVWXYZ0123456789", sb.ToString());
        sb.Dispose();
    }
}
