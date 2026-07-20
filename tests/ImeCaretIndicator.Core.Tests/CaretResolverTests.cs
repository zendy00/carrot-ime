using System.Drawing;
using ImeCaretIndicator.Core;
using Xunit;

namespace ImeCaretIndicator.Core.Tests;

public class CaretResolverTests
{
    private static Func<uint, Rectangle?> Source(Rectangle? result, List<string> calls, string name)
        => _ => { calls.Add(name); return result; };

    [Fact]
    public void First_source_that_hits_wins_and_later_sources_are_not_called()
    {
        var calls = new List<string>();
        var resolver = new CaretResolver(
            Source(new Rectangle(10, 20, 2, 16), calls, "native"),
            Source(new Rectangle(99, 99, 2, 16), calls, "uia"),
            Source(new Rectangle(77, 77, 2, 16), calls, "msaa"));

        var result = resolver.Resolve(threadId: 1234);

        Assert.Equal(new Rectangle(10, 20, 2, 16), result);
        Assert.Equal(new[] { "native" }, calls); // short-circuits after the first hit
    }

    [Fact]
    public void Falls_through_in_order_until_a_source_hits()
    {
        var calls = new List<string>();
        var resolver = new CaretResolver(
            Source(null, calls, "native"),
            Source(null, calls, "uia"),
            Source(new Rectangle(5, 5, 2, 16), calls, "msaa"));

        var result = resolver.Resolve(threadId: 1);

        Assert.Equal(new Rectangle(5, 5, 2, 16), result);
        Assert.Equal(new[] { "native", "uia", "msaa" }, calls); // order preserved
    }

    [Fact]
    public void Returns_null_when_every_source_misses()
    {
        var calls = new List<string>();
        var resolver = new CaretResolver(
            Source(null, calls, "native"),
            Source(null, calls, "uia"),
            Source(null, calls, "msaa"));

        Assert.Null(resolver.Resolve(threadId: 1));
        Assert.Equal(new[] { "native", "uia", "msaa" }, calls);
    }
}
