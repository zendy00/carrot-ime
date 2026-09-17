import ServiceManagement

/// 로그인 시 자동 시작 — SMAppService(macOS 13+). Windows판 작업 스케줄러 등록의 대응물.
/// 주의: 번들된 CarrotIME.app에서 실행해야 등록된다. 언번들 dev 바이너리에선 register가 실패한다.
enum AutoStart {
    static var isEnabled: Bool { SMAppService.mainApp.status == .enabled }

    static func setEnabled(_ on: Bool) -> Result<Void, Error> {
        do {
            if on {
                try SMAppService.mainApp.register()
            } else {
                try SMAppService.mainApp.unregister()
            }
            return .success(())
        } catch {
            return .failure(error)
        }
    }
}
