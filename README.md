# MRTK Korean Keyboard

MRTK3 기반 **독립형 두벌식 한글 가상 키보드**. 월드 스페이스 캔버스, Grab 이동 핸들, `TMP_InputField` 자동 연동을 제공합니다.

## 요구 사항 (선행 설치)

이 패키지는 **MRTK3** 에 의존합니다. `package.json` 의 dependencies 로 자동 해결되지 **않으므로**, 먼저 다음을 설치하세요(MRTK Feature Tool 또는 OpenUPM):

- `org.mixedrealitytoolkit.core`
- `org.mixedrealitytoolkit.uxcore` (PressableButton)
- `org.mixedrealitytoolkit.spatialmanipulation` (Grab 이동 핸들 — ObjectManipulator)

그 외 `com.unity.textmeshpro`, `com.unity.xr.interaction.toolkit` 은 dependencies 로 자동 설치됩니다. 권장 Unity: **2022.3 LTS**.

## 설치 (Git URL)

Unity Package Manager → `+` → **Add package from git URL** →

```
http://192.168.50.230:3000/PnCSolution/MRTKKeyboard.git
```

## 사용법

1. 씬에 빈 GameObject 를 만들고 `KoreanKeyboardManager` 를 추가합니다.
2. `KoreanKeyboardManager._keyboard` 슬롯에 `KoreanKeyboard` 프리팹을 지정합니다(또는 씬에 직접 배치).
   - 슬롯에 **프리팹**을 넣으면 InputField 첫 선택 시 자동 생성됩니다.
   - 비워두면 씬에서 자동 탐색합니다.
3. 씬에 `EventSystem`(+ XRI `XRUIInputModule`) 이 있으면, 어떤 `TMP_InputField` 든 선택 시 키보드가 자동으로 떠서 입력이 연결됩니다.

앱 로직은 키보드가 아니라 **각 `TMP_InputField` 의 `onSubmit` / `onValueChanged`** 에 연결하세요(키보드는 입력 장치로서 현재 포커스된 필드로 라우팅만 합니다).

### 에디터 데모

`Tools/ShareLens/Create Demo InputField` 메뉴로 월드 스페이스 InputField 를 즉시 생성해 테스트할 수 있습니다.

## 폰트

한글 렌더링용 `Fonts/Malgun SDF.asset`(맑은 고딕 동적 SDF)이 포함됩니다.

> ⚠️ **라이선스 주의**: 맑은 고딕(malgun.ttf)은 Microsoft 번들 폰트로 **재배포 제약**이 있을 수 있습니다. 공개/외부 배포 시에는 폰트를 제거하고, 오픈 폰트(예: Noto Sans KR, 나눔고딕)로 교체하거나 사용자가 직접 `Tools/ShareLens/Create Malgun TMP Font` 로 생성하도록 안내하세요. 내부 사용 한정입니다.

## 구성

- `Runtime/` — HangulComposer(두벌식 오토마타), KoreanKey, KoreanKeyboard, KoreanKeyboardManager, MatchRectSize
- `Editor/` — 데모 생성, 폰트 빌더, 스모크 테스트
- `Prefabs/` — KoreanKeyboard, HangulKey
- `Materials/` · `Fonts/`
