using System.Drawing;
using CarrotIME.Core;
using Xunit;

namespace CarrotIME.Core.Tests;

public class CaretResolverTests
{
    private static CaretResolver.Source Source(
        Rectangle? result, List<string> calls, string name, bool impliesEditable = true)
        => new(_ => { calls.Add(name); return result; }, impliesEditable);

    [Fact]
    public void First_source_that_hits_wins_and_later_sources_are_not_called()
    {
        var calls = new List<string>();
        var resolver = new CaretResolver(
            Source(new Rectangle(10, 20, 2, 16), calls, "native"),
            Source(new Rectangle(99, 99, 2, 16), calls, "uia"),
            Source(new Rectangle(77, 77, 2, 16), calls, "msaa"));

        var result = resolver.Resolve(threadId: 1234);

        Assert.Equal(new Rectangle(10, 20, 2, 16), result?.Rect);
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

        Assert.Equal(new Rectangle(5, 5, 2, 16), result?.Rect);
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

    [Fact]
    public void Trusted_source_hit_implies_editable_focus()
    {
        var calls = new List<string>();
        var resolver = new CaretResolver(
            Source(new Rectangle(10, 20, 2, 16), calls, "native", impliesEditable: true));

        Assert.True(resolver.Resolve(threadId: 1)?.ImpliesEditable);
    }

    [Fact]
    public void Untrusted_source_hit_does_not_imply_editable_focus()
    {
        // UIA selection 기반 캐럿은 읽기 전용 텍스트(브라우저 본문)에서도 잡히므로
        // 캐럿 위치는 쓰되 편집 포커스 확정 근거로는 쓰지 않는다.
        var calls = new List<string>();
        var resolver = new CaretResolver(
            Source(null, calls, "native"),
            Source(new Rectangle(99, 99, 2, 16), calls, "uia", impliesEditable: false));

        var result = resolver.Resolve(threadId: 1);

        Assert.Equal(new Rectangle(99, 99, 2, 16), result?.Rect);
        Assert.False(result?.ImpliesEditable);
    }
}
