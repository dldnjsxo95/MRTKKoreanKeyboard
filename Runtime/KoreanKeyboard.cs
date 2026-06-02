// 독립형 한글(두벌식) MRTK 키보드 컨트롤러.
// MRTK PressableButton 키를 레이아웃 테이블로부터 런타임 생성하고,
// HangulComposer 로 한글을 조합하며, 한/영·Shift·Space·Backspace·Enter 를 처리한다.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShareLens.Keyboard
{
    /// <summary>두벌식 한글 입력을 지원하는 커스텀 가상 키보드.</summary>
    public sealed class KoreanKeyboard : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("키 1개를 나타내는 프리팹(KoreanKey + PressableButton + TMP 라벨).")]
        [SerializeField] private GameObject _keyPrefab;

        [Tooltip("행(Row)들이 생성될 부모. VerticalLayoutGroup 권장.")]
        [SerializeField] private RectTransform _keysRoot;

        [Tooltip("키 라벨에 적용할 한글 TMP 폰트.")]
        [SerializeField] private TMP_FontAsset _koreanFont;

        [Header("Layout")]
        [Tooltip("키 한 변의 크기(캔버스 단위).")]
        [SerializeField] private float _keySize = 32f;

        [Tooltip("키 사이 간격(캔버스 단위).")]
        [SerializeField] private float _spacing = 2f;

        [Header("Behaviour")]
        [Tooltip("시작할 때 한글 모드로 둘지 여부. 기본은 영문.")]
        [SerializeField] private bool _startInHangul = false;

        [Header("Placement (MR)")]
        [Tooltip("숨김→표시(소환) 시 사용자 정면에 배치할지 여부. 끄면 현재 위치 유지.")]
        [SerializeField] private bool _placeInFrontOnShow = true;

        [Tooltip("카메라 정면 기준 배치 거리(m). poke 면 팔 닿는 0.45~0.6 권장.")]
        [SerializeField] private float _placeDistance = 0.5f;

        [Tooltip("눈높이 대비 수직 오프셋(m). 음수면 아래.")]
        [SerializeField] private float _placeVerticalOffset = -0.15f;

        [Tooltip("사용자 쪽으로 눕히는 틸트 각(도). 양수면 윗변이 뒤로.")]
        [SerializeField] private float _placeTiltAngle = 15f;

        private readonly HangulComposer _composer = new HangulComposer();
        private readonly List<KoreanKey> _keys = new List<KoreanKey>();
        private TMP_InputField _targetField; // AttachTo() 로 런타임 지정(인스펙터 노출 안 함)
        private string _committed = "";
        private bool _hangul = true;
        private bool _shift = false;
        private bool _symbol = false; // 기호 페이지 표시 중 여부
        private bool _built = false;

        /// <summary>현재 한글 모드 여부.</summary>
        public bool IsHangul => _hangul;

        /// <summary>현재 Shift 상태.</summary>
        public bool IsShifted => _shift;

        /// <summary>확정 텍스트 + 조합 중 글자.</summary>
        public string Text => _committed + _composer.Composing;

        private void Awake()
        {
            // 시작 모드는 최초 1회만 적용. 첫 Build 가 영문(_startInHangul=false)·문자 페이지(_symbol=false)로 생성됨.
            _hangul = _startInHangul;
        }

        private void OnEnable()
        {
            // 최초 1회만 생성(첫 열림 = 영문 문자 페이지). 이후엔 직전 상태(모드/페이지/내용)를 그대로 유지.
            if (!_built)
            {
                Build();
            }
            RefreshKeyLabels();
            UpdatePreview();
        }

        private void OnDisable()
        {
            // 닫힐 때(SetActive false 포함) 조합 중인 글자를 확정해 입력 손실을 막는다.
            if (_composer.HasComposition)
            {
                _committed += _composer.Flush();
                UpdatePreview();
            }
        }

        // ── 입력 처리 ────────────────────────────────────────────────
        /// <summary>키가 눌렸을 때 KoreanKey 가 호출한다.</summary>
        public void HandleKey(KoreanKey key)
        {
            switch (key.Def.Kind)
            {
                case KeyKind.Character:
                    InputCharacter(key.Def.ValueFor(_hangul, _shift));
                    // 한글 모드만 입력 후 Shift 자동 해제. 영문(Caps)·기호(2번째 세트)는 유지.
                    if (_shift && _hangul && !_symbol)
                    {
                        SetShift(false);
                    }
                    break;

                case KeyKind.Backspace:
                    Backspace();
                    break;

                case KeyKind.Clear:
                    Clear();
                    break;

                case KeyKind.Space:
                    _committed += _composer.Flush() + " ";
                    break;

                case KeyKind.Enter:
                    Submit();
                    break;

                case KeyKind.Shift:
                    if (_symbol)
                    {
                        // 기호 페이지: Shift 로 2번째 기호 세트로 전환(키 재생성).
                        _shift = !_shift;
                        Build();
                    }
                    else
                    {
                        SetShift(!_shift);
                    }
                    break;

                case KeyKind.Lang:
                    _committed += _composer.Flush();
                    _hangul = !_hangul;
                    SetShift(false);
                    RefreshKeyLabels();
                    break;

                case KeyKind.Symbol:
                    _committed += _composer.Flush();
                    _symbol = !_symbol;
                    _shift = false;
                    Build(); // 페이지가 바뀌므로 키를 다시 생성
                    break;

                case KeyKind.Close:
                    _committed += _composer.Flush();
                    gameObject.SetActive(false);
                    break;
            }

            UpdatePreview();
        }

        private void InputCharacter(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (_hangul && value.Length == 1 && HangulComposer.IsJamo(value[0]))
            {
                _committed += _composer.Input(value[0]);
            }
            else
            {
                // 영문/숫자/기호: 조합 중 글자를 먼저 확정한 뒤 그대로 덧붙인다.
                _committed += _composer.Flush();
                _committed += value;
            }
        }

        private void Backspace()
        {
            // 조합 중 글자가 있으면 한 단계 분해, 없으면 확정 텍스트의 마지막 글자 삭제.
            if (!_composer.Backspace() && _committed.Length > 0)
            {
                _committed = _committed.Substring(0, _committed.Length - 1);
            }
        }

        private void Submit()
        {
            // Enter: 조합 중인 글자를 확정한 뒤, 현재 포커스된 필드의 onSubmit 으로 라우팅한다.
            // (키보드에 전역 이벤트 없음 — 앱 로직은 각 InputField 의 onSubmit 에 직접 연결한다.)
            _committed += _composer.Flush();
            UpdatePreview();
            if (_targetField != null && _targetField.isActiveAndEnabled)
            {
                _targetField.onSubmit.Invoke(_targetField.text);
            }
        }

        /// <summary>입력 내용을 모두 비운다.</summary>
        public void Clear()
        {
            _composer.Reset();
            _committed = "";
            SetShift(false);
            UpdatePreview();
        }

        private void SetShift(bool value)
        {
            if (_shift == value)
            {
                return;
            }
            _shift = value;
            RefreshKeyLabels();
        }

        private void RefreshKeyLabels()
        {
            foreach (KoreanKey key in _keys)
            {
                key.Refresh(_hangul, _shift);
            }
        }

        private void UpdatePreview()
        {
            // 현재 포커스된 필드에 직접 write-back. 텍스트 변경 통지는 필드의 onValueChanged 가 담당한다.
            if (_targetField != null && _targetField.isActiveAndEnabled)
            {
                string text = Text;
                _targetField.text = text;
                _targetField.caretPosition = text.Length;
            }
        }

        // ── 외부 연동 (InputField) ──────────────────────────────────
        /// <summary>
        /// 지정한 InputField 에 입력을 연결하고 키보드를 표시한다.
        /// (보통 <see cref="KoreanKeyboardManager"/> 가 InputField 선택 시 호출)
        /// </summary>
        public void AttachTo(TMP_InputField field)
        {
            bool wasHidden = !gameObject.activeSelf;

            _targetField = field;
            gameObject.SetActive(true); // 비활성 상태였다면 OnEnable 에서 Build 수행

            // 숨김→표시(소환)되는 순간에만 사용자 정면에 재배치. 이미 떠 있으면(필드만 전환)
            // 위치 유지 → grab 으로 옮겨둔 자리를 존중하고 타이핑 중 흔들림 없음.
            if (wasHidden)
            {
                PlaceInFrontOfUser();
            }

            // 기존 필드 내용을 이어서 편집.
            _composer.Reset();
            _committed = field != null ? field.text : "";
            SetShift(false);
            UpdatePreview();
        }

        /// <summary>키보드를 사용자(메인 카메라) 정면 편한 위치·각도에 배치한다.</summary>
        public void PlaceInFrontOfUser()
        {
            if (!_placeInFrontOnShow)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                return; // 카메라가 없으면 현재 위치 유지
            }

            Transform camTr = cam.transform;

            // 카메라 정면을 수평으로 투영(피치 제거) → 천장/바닥을 봐도 키보드는 수평 정면에 배치.
            Vector3 flatForward = Vector3.ProjectOnPlane(camTr.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 1e-4f)
            {
                flatForward = Vector3.ProjectOnPlane(camTr.up, Vector3.up); // 정수리/발끝 응시 예외
            }
            flatForward.Normalize();

            transform.position = camTr.position
                                 + flatForward * _placeDistance
                                 + Vector3.up * _placeVerticalOffset;

            // 캔버스 정면(-Z)이 사용자를 향하도록: +Z 가 사용자 반대(바라보는 방향)를 보게 한 뒤
            // 사용자 쪽으로 살짝 눕히는 틸트를 더한다. (180° 회전 금지 — 텍스트 좌우 반전됨)
            Quaternion faceUser = Quaternion.LookRotation(flatForward, Vector3.up);
            transform.rotation = faceUser * Quaternion.Euler(_placeTiltAngle, 0f, 0f);
        }

        // ── 키보드 생성 ──────────────────────────────────────────────
        /// <summary>레이아웃 테이블로부터 키 버튼들을 생성한다.</summary>
        [ContextMenu("Rebuild Keyboard")]
        public void Build()
        {
            if (_keyPrefab == null || _keysRoot == null)
            {
                Debug.LogError("[KoreanKeyboard] _keyPrefab / _keysRoot 가 할당되지 않았습니다.");
                return;
            }

            // 기존 자식 제거(에디터/런타임 재생성 대응).
            for (int i = _keysRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _keysRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            _keys.Clear();

            EnsureVerticalLayout(_keysRoot.gameObject);

            foreach (KeyDef[] row in BuildLayout())
            {
                GameObject rowGo = new GameObject("Row", typeof(RectTransform));
                rowGo.layer = _keysRoot.gameObject.layer;
                RectTransform rowRect = rowGo.GetComponent<RectTransform>();
                rowRect.SetParent(_keysRoot, false);
                EnsureHorizontalLayout(rowGo);

                foreach (KeyDef def in row)
                {
                    GameObject keyGo = Instantiate(_keyPrefab, rowRect);
                    keyGo.name = $"Key_{(def.Kind == KeyKind.Character ? def.English : def.Kind.ToString())}";

                    KoreanKey key = keyGo.GetComponent<KoreanKey>();
                    if (key == null)
                    {
                        key = keyGo.AddComponent<KoreanKey>();
                    }
                    key.Bind(this, def, _koreanFont, _keySize, _spacing);
                    _keys.Add(key);
                }
            }

            _built = true;
            RefreshKeyLabels();
        }

        private void EnsureVerticalLayout(GameObject go)
        {
            VerticalLayoutGroup v = go.GetComponent<VerticalLayoutGroup>();
            if (v == null)
            {
                v = go.AddComponent<VerticalLayoutGroup>();
            }
            v.spacing = _spacing;
            v.childAlignment = TextAnchor.MiddleCenter;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            // 끝단 키와 가장자리 사이 여백 = 키 간격 + 2 정도.
            int pad = Mathf.RoundToInt(_spacing) + 2;
            v.padding = new RectOffset(pad, pad, pad, pad);

            ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = go.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // 배경 Image 는 코드에서 생성하지 않음(직접 추가). KeysRoot 크기는 ContentSizeFitter 가 자동 계산.
        }

        private void EnsureHorizontalLayout(GameObject go)
        {
            HorizontalLayoutGroup h = go.GetComponent<HorizontalLayoutGroup>();
            if (h == null)
            {
                h = go.AddComponent<HorizontalLayoutGroup>();
            }
            h.spacing = _spacing;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = go.AddComponent<ContentSizeFitter>();
            }
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // ── 레이아웃 정의 ────────────────────────────────────────────
        // 현재 페이지(문자/기호)에 맞는 행 목록을 돌려준다.
        private IEnumerable<KeyDef[]> BuildLayout()
            => _symbol ? BuildSymbolLayout() : BuildLetterLayout();

        // 기호 한 글자를 모드와 무관하게 입력하는 키.
        private static KeyDef Sym(string s) => KeyDef.Char(s, s, s, s);


        // 두벌식 문자 페이지 (숫자는 기호 페이지로 통합 → 3 자모행 + 기능행)
        private static IEnumerable<KeyDef[]> BuildLetterLayout()
        {
            // 1행: ㅂㅈㄷㄱㅅ ㅛㅕㅑㅐㅔ  (q~p)
            yield return new[]
            {
                KeyDef.Char("ㅂ","ㅃ","q","Q"), KeyDef.Char("ㅈ","ㅉ","w","W"), KeyDef.Char("ㄷ","ㄸ","e","E"),
                KeyDef.Char("ㄱ","ㄲ","r","R"), KeyDef.Char("ㅅ","ㅆ","t","T"), KeyDef.Char("ㅛ","ㅛ","y","Y"),
                KeyDef.Char("ㅕ","ㅕ","u","U"), KeyDef.Char("ㅑ","ㅑ","i","I"), KeyDef.Char("ㅐ","ㅒ","o","O"),
                KeyDef.Char("ㅔ","ㅖ","p","P")
            };

            // 2행: ㅁㄴㅇㄹㅎ ㅗㅓㅏㅣ (a~l) + [Clear](Back 위)
            yield return new[]
            {
                KeyDef.Char("ㅁ","ㅁ","a","A"), KeyDef.Char("ㄴ","ㄴ","s","S"), KeyDef.Char("ㅇ","ㅇ","d","D"),
                KeyDef.Char("ㄹ","ㄹ","f","F"), KeyDef.Char("ㅎ","ㅎ","g","G"), KeyDef.Char("ㅗ","ㅗ","h","H"),
                KeyDef.Char("ㅓ","ㅓ","j","J"), KeyDef.Char("ㅏ","ㅏ","k","K"), KeyDef.Char("ㅣ","ㅣ","l","L"),
                KeyDef.Func(KeyKind.Clear, "Clr", 1f)
            };

            // 3행: [Shift] ㅋㅌㅊㅍ ㅠㅜㅡ [Back] — Backspace 는 표준 위치(우측 하단)
            yield return new[]
            {
                KeyDef.Func(KeyKind.Shift, "Shift", 1.5f),
                KeyDef.Char("ㅋ","ㅋ","z","Z"), KeyDef.Char("ㅌ","ㅌ","x","X"), KeyDef.Char("ㅊ","ㅊ","c","C"),
                KeyDef.Char("ㅍ","ㅍ","v","V"), KeyDef.Char("ㅠ","ㅠ","b","B"), KeyDef.Char("ㅜ","ㅜ","n","N"),
                KeyDef.Char("ㅡ","ㅡ","m","M"),
                KeyDef.Func(KeyKind.Backspace, "Back", 1.5f)
            };

            // 하단 기능 행: [Sym] [KR/EN] [,] [Space] [.] [Enter] — Space 가 채워 1·3·4행 좌우 정렬
            yield return new[]
            {
                KeyDef.Func(KeyKind.Symbol, "Sym", 1.5f),
                KeyDef.Func(KeyKind.Lang, "KR/EN", 1f),
                Sym(","),
                KeyDef.Func(KeyKind.Space, "Space", 3.5f),
                Sym("."),
                KeyDef.Func(KeyKind.Enter, "Enter", 2f)
            };
        }

        // 기호 페이지. Shift 로 1/2 ↔ 2/2 세트 전환(_shift).
        private IEnumerable<KeyDef[]> BuildSymbolLayout()
            => _shift ? BuildSymbolPage2() : BuildSymbolPage1();

        // 기호 페이지 하단 기능 행. KR/EN 없음(언어 전환은 문자 페이지에서) → 그 폭만큼 ABC 를 키워 정렬 유지.
        // [ABC(=Sym+Lang 폭)][,][Space][.][Enter]
        private static KeyDef[] SymbolBottomRow()
            => new[]
            {
                KeyDef.Func(KeyKind.Symbol, "ABC", 2.5f),
                Sym(","),
                KeyDef.Func(KeyKind.Space, "Space", 3.5f),
                Sym("."),
                KeyDef.Func(KeyKind.Enter, "Enter", 2f)
            };

        // 기호 1/2 — 문자 페이지와 동일 형태: 10 / (9+Clr) / (Shift+7+Back) / 기능행.
        private static IEnumerable<KeyDef[]> BuildSymbolPage1()
        {
            yield return new[]
            {
                Sym("1"), Sym("2"), Sym("3"), Sym("4"), Sym("5"),
                Sym("6"), Sym("7"), Sym("8"), Sym("9"), Sym("0")
            };
            yield return new[]
            {
                Sym("+"), Sym("-"), Sym("*"), Sym("/"), Sym("="),
                Sym("("), Sym(")"), Sym("["), Sym("]"),
                KeyDef.Func(KeyKind.Clear, "Clr", 1f)
            };
            yield return new[]
            {
                KeyDef.Func(KeyKind.Shift, "1/2", 1.5f),
                Sym("@"), Sym("#"), Sym("$"), Sym("%"), Sym("&"), Sym("_"), Sym("\\"),
                KeyDef.Func(KeyKind.Backspace, "Back", 1.5f)
            };
            yield return SymbolBottomRow();
        }

        // 기호 2/2 — 동일 형태. Shift 로 전환되는 보조 기호 세트.
        private static IEnumerable<KeyDef[]> BuildSymbolPage2()
        {
            yield return new[]
            {
                Sym("!"), Sym("?"), Sym("~"), Sym("`"), Sym("^"),
                Sym("|"), Sym("<"), Sym(">"), Sym("{"), Sym("}")
            };
            yield return new[]
            {
                Sym(":"), Sym(";"), Sym("'"), Sym("\""), Sym("₩"),
                Sym("€"), Sym("£"), Sym("¥"), Sym("°"),
                KeyDef.Func(KeyKind.Clear, "Clr", 1f)
            };
            yield return new[]
            {
                KeyDef.Func(KeyKind.Shift, "2/2", 1.5f),
                Sym("※"), Sym("★"), Sym("●"), Sym("→"), Sym("±"), Sym("×"), Sym("÷"),
                KeyDef.Func(KeyKind.Backspace, "Back", 1.5f)
            };
            yield return SymbolBottomRow();
        }
    }
}
