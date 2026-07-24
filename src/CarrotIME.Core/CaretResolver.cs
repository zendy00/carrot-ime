using System.Drawing;

namespace CarrotIME.Core;

/// <summary>
/// 여러 캐럿 소스를 정해진 순서(네이티브 → UIA → MSAA)로 시도하고
/// 처음으로 위치를 준 소스의 결과를 쓴다. 모두 실패하면 null.
/// 소스마다 "캐럿 존재 = 편집 필드 확정" 신뢰 여부가 다르다 — 네이티브·MSAA 캐럿은
/// 편집 필드에서만 존재하지만, UIA selection 캐럿은 읽기 전용 텍스트(브라우저 본문)에서도
/// 잡히므로 편집 확정 근거가 못 된다.
/// 순수 오케스트레이션 — 실제 OS 소스는 어댑터가 델리게이트로 주입한다.
/// </summary>
public sealed class CaretResolver
{
    /// <param name="TryGet">캐럿 rect를 시도하는 어댑터.</param>
    /// <param name="ImpliesEditable">이 소스의 캐럿 존재가 편집 필드를 확정하는지.</param>
    public readonly record struct Source(Func<uint, Rectangle?> TryGet, bool ImpliesEditable);

    private readonly IReadOnlyList<Source> _sources;

    public CaretResolver(params Source[] sources) => _sources = sources;

    public (Rectangle Rect, bool ImpliesEditable)? Resolve(uint threadId)
    {
        foreach (var source in _sources)
        {
            var rect = source.TryGet(threadId);
            if (rect is Rectangle r)
                return (r, source.ImpliesEditable);
        }
        return null;
    }
}
