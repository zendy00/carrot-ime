namespace CarrotIME.Core;

/// <summary>인디케이터가 구분해 보여주는 현재 입력 상태.</summary>
public enum InputState
{
    /// <summary>한글 IME가 한글 조합 모드.</summary>
    Hangul,

    /// <summary>영문 모드·영어 레이아웃 등 그 외 알파벳 입력.</summary>
    English,

    /// <summary>일본어·중국어 등 다른 입력기. (Ticket 05에서 채워짐)</summary>
    OtherIme
}
