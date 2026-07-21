using System.Drawing;

namespace CarrotIME.Core;

/// <summary>
/// 여러 캐럿 소스를 정해진 순서(네이티브 → UIA → MSAA)로 시도하고
/// 처음으로 위치를 준 소스의 결과를 쓴다. 모두 실패하면 null.
/// 순수 오케스트레이션 — 실제 OS 소스는 어댑터가 델리게이트로 주입한다.
/// </summary>
public sealed class CaretResolver
{
    private readonly IReadOnlyList<Func<uint, Rectangle?>> _sources;

    public CaretResolver(params Func<uint, Rectangle?>[] sources) => _sources = sources;

    public Rectangle? Resolve(uint threadId)
    {
        foreach (var source in _sources)
        {
            var rect = source(threadId);
            if (rect is not null)
                return rect;
        }
        return null;
    }
}
