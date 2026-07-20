using System.Drawing;

namespace ImeCaretIndicator.Core;

/// <summary>
/// Decide가 필요로 하는 모든 것을 담은 순수 값 스냅샷. OS 어댑터가 만들어 넘긴다.
/// (유일한 seam의 입력 — 여기엔 OS 타입이 아니라 평범한 값만 들어간다.)
/// </summary>
public readonly record struct InputSnapshot(
    bool EditableFocus,
    Rectangle? Caret,
    Rectangle ScreenBounds,
    Size IndicatorSize,
    ushort KeyboardLangId,
    uint ConversionMode);
