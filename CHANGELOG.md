# Changelog

이 프로젝트의 모든 주요 변경 사항을 기록합니다. 형식은 [Keep a Changelog](https://keepachangelog.com/) 를, 버전은 [SemVer](https://semver.org/) 를 따릅니다.

## [1.0.1]

### Changed
- 기본 폰트를 맑은 고딕(© Microsoft, 재배포 제약) → **Pretendard(SIL OFL)** 로 교체. 외부 배포 가능. `Fonts/LICENSE.txt`(OFL) 동봉.
- 폰트 빌더 메뉴: `Create Malgun TMP Font` → `Create Pretendard TMP Font`.

## [1.0.0]

### Added
- 두벌식 한글 오토마타(`HangulComposer`) — 초/중/종성, 겹받침, 복합모음, 연음, 백스페이스 분해.
- 런타임 키 생성 키보드(`KoreanKeyboard`) — 문자/기호 페이지, 한/영 토글, Shift(영문 Caps 유지), Clear, Backspace, Enter(줄바꿈 없이 `onSubmit` 라우팅).
- 전역 `KoreanKeyboardManager` — `TMP_InputField` 선택 자동 감지·연결, 프리팹 지연 생성.
- MR 배치 — 소환 시 사용자 정면 배치 후 고정.
- Grab 이동 핸들(프리팹 내장 `GrabBar` + `ObjectManipulator`).
- 에디터 도구 — 데모 InputField 생성, 맑은 고딕 TMP 폰트 빌더, 스모크 테스트.
- UPM 패키지화(asmdef, package.json).
