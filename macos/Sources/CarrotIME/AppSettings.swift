import Foundation

/// 사용자 설정 — UserDefaults 영속. (Windows판 settings.txt 대응.)
/// Windows는 시작 시 1회 로드지만 macOS는 변경 즉시 저장·반영한다.
final class AppSettings {
    static let shared = AppSettings()
    private let d = UserDefaults.standard
    private init() {}

    private enum Key {
        static let idleMs = "idleReappearMs"
        static let opacity = "opacity"
        static let bgHex = "bgColorHex"
        static let fgHex = "fgColorHex"
        static let paused = "paused"
        static let hidePointer = "hideOnPointerMove"
    }

    var idleReappearMs: Int {
        get { d.object(forKey: Key.idleMs) as? Int ?? 3_000 } // 기본 3초(메뉴 프리셋과 일치)
        set { d.set(newValue, forKey: Key.idleMs) }
    }
    var opacity: Double {
        get { (d.object(forKey: Key.opacity) as? Double) ?? 0.75 } // 기본 75%
        set { d.set(min(1.0, max(0.2, newValue)), forKey: Key.opacity) }
    }
    var backgroundHex: String {
        get { d.string(forKey: Key.bgHex) ?? "#3C8CFF" } // 기본 블루
        set { d.set(newValue, forKey: Key.bgHex) }
    }
    var foregroundHex: String {
        get { d.string(forKey: Key.fgHex) ?? "#FFFFFF" } // 기본 흰색
        set { d.set(newValue, forKey: Key.fgHex) }
    }
    var paused: Bool {
        get { d.bool(forKey: Key.paused) }
        set { d.set(newValue, forKey: Key.paused) }
    }
    /// 마우스·트랙패드 움직임도 입력 활동으로 보고 인디케이터를 숨길지. 기본 켜짐.
    var hideOnPointerMove: Bool {
        get { d.object(forKey: Key.hidePointer) as? Bool ?? true }
        set { d.set(newValue, forKey: Key.hidePointer) }
    }
}
