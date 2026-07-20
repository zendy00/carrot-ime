# 02 — UIA/MSAA 캐럿 폴백 (커버리지 확대)

**What to build:** 네이티브 캐럿을 노출하지 않는 앱(크롬·워드패드 등)에서도, 접근성 경로(UI Automation, 이어서 MSAA)로 캐럿 위치를 얻어 인디케이터가 입력폼을 따라온다. 캐럿 획득은 네이티브 → UIA → MSAA 순으로 다단 폴백한다.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] 크롬 주소창/웹 입력폼에서 캐럿 옆에 상태가 뜨고 따라온다
- [ ] 워드패드 등 다른 앱에서도 캐럿을 따라온다
- [ ] 네이티브 경로가 실패하면 UIA→MSAA 순으로 시도한다(해당 앱에서 표시되는 외부 행동으로 확인)
- [ ] 메모장(01)의 캐럿 추종 동작이 회귀 없이 유지된다

## 리뷰 노트 (code-review)

- **collapsed 캐럿 위험(수동 검증):** `UiaCaret`는 `GetSelection()[0].GetBoundingRectangles()`를 쓰는데, 선택이 없는(collapsed) 캐럿에선 일부 앱(크롬 주소창·유휴 워드패드)이 빈 배열을 줄 수 있음 → null로 폴백. 크롬은 MSAA `OBJID_CARET`도 잘 안 주므로, 두 소스 모두 놓치면 ticket 04(고정 위치 폴백) 전까지는 인디케이터가 안 뜰 수 있음. **크롬/워드패드 실기 검증 시 반드시 확인.**
  - 후속 개선 후보: `TextPattern2.GetCaretRange`로 collapsed 캐럿 획득, 또는 rects가 비면 포커스 요소의 BoundingRectangle로 근사.
- **multi-line rects[0]:** 여러 줄 선택 시 첫(윗줄) 사각형을 씀 — collapsed 캐럿엔 무관하나, US20("입력 끝점 옆") 관점의 저순위 개선 여지. Decide 계층 사안.
