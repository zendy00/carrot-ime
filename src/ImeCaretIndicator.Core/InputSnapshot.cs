using System.Drawing;

namespace ImeCaretIndicator.Core;

/// <summary>
/// Decide가 필요로 하는 모든 것을 담은 순수 값 스냅샷. OS 어댑터가 만들어 넘긴다.
/// (유일한 seam의 입력 — 여기엔 OS 타입이 아니라 평범한 값만 들어간다.)
/// </summary>
public readonly record struct InputSnapshot(
    bool EditableFocus,
    Rectangle? Caret,
    Rectangle ActiveWindowBounds,
    Rectangle ScreenBounds,
    Size IndicatorSize,
    ushort KeyboardLangId,
    uint ConversionMode,
    // 마지막 입력 활동(캐럿 이동) 이후 경과 밀리초. 활동을 알 수 없으면 큰 값(유휴로 간주).
    long MillisSinceInputActivity);
