import Foundation

/// 진단용 로그 — 환경변수 CARROTIME_DEBUG=1 일 때만 /tmp/carrotime.log 에 남긴다.
/// 평소엔 완전히 비활성(파일 I/O 없음).
enum Log {
    static let enabled = ProcessInfo.processInfo.environment["CARROTIME_DEBUG"] == "1"
    private static let url = URL(fileURLWithPath: "/tmp/carrotime.log")

    static func line(_ s: String) {
        guard enabled, let data = (s + "\n").data(using: .utf8) else { return }
        if let h = try? FileHandle(forWritingTo: url) {
            h.seekToEndOfFile()
            h.write(data)
            try? h.close()
        } else {
            try? data.write(to: url)
        }
    }
}
