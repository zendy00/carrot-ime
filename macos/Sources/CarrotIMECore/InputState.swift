/// 인디케이터가 구분해 보여주는 현재 입력 상태.
public enum InputState {
    /// 한글 IME.
    case hangul
    /// 영문 모드·라틴 레이아웃.
    case english
    /// 일본어·중국어 등 다른 입력기.
    case otherIme
}
