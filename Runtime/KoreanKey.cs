// 키보드의 개별 키. 런타임에 KoreanKeyboard 가 레이아웃 테이블로부터 생성/바인딩한다.
// MRTK PressableButton 의 OnClicked 를 받아 컨트롤러로 전달하고, 한/영·Shift 상태에 따라 라벨을 갱신한다.

using MixedReality.Toolkit.UX;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShareLens.Keyboard
{
    /// <summary>키 종류.</summary>
    public enum KeyKind
    {
        Character,  // 일반 문자/자모 키
        Backspace,
        Clear,      // 전체 지우기
        Space,
        Enter,
        Shift,
        Lang,       // 한/영 전환
        Symbol,     // 기호 ⇄ 문자 페이지 전환
        Close
    }

    /// <summary>한 키의 정의(런타임 데이터). 프리팹에 직렬화되지 않고 컨트롤러가 채워준다.</summary>
    public sealed class KeyDef
    {
        public KeyKind Kind = KeyKind.Character;

        public string Hangul = "";       // 한글 모드 기본값
        public string HangulShift = "";  // 한글 모드 Shift 값(겹자음/ㅒㅖ 등)
        public string English = "";      // 영문 모드 기본값
        public string EnglishShift = ""; // 영문 모드 Shift 값(대문자/기호)
        public string FunctionLabel = ""; // 기능 키 표시 라벨

        public float Weight = 1f;        // 가로 폭 가중치(Space 등은 넓게)

        public static KeyDef Char(string hangul, string hangulShift, string english, string englishShift)
            => new KeyDef
            {
                Kind = KeyKind.Character,
                Hangul = hangul,
                HangulShift = string.IsNullOrEmpty(hangulShift) ? hangul : hangulShift,
                English = english,
                EnglishShift = string.IsNullOrEmpty(englishShift) ? english : englishShift
            };

        public static KeyDef Func(KeyKind kind, string label, float weight = 1f)
            => new KeyDef { Kind = kind, FunctionLabel = label, Weight = weight };

        /// <summary>현재 모드/Shift 상태에서 표시할 라벨.</summary>
        public string LabelFor(bool hangul, bool shifted)
        {
            if (Kind == KeyKind.Lang)
            {
                return hangul ? "EN" : "KR"; // 현재 입력 모드 표시(토글)
            }
            if (Kind != KeyKind.Character)
            {
                return FunctionLabel;
            }
            if (hangul)
            {
                return shifted ? HangulShift : Hangul;
            }
            return shifted ? EnglishShift : English;
        }

        /// <summary>현재 모드/Shift 상태에서 입력될 값.</summary>
        public string ValueFor(bool hangul, bool shifted) => LabelFor(hangul, shifted);
    }

    /// <summary>개별 키 동작 컴포넌트.</summary>
    public sealed class KoreanKey : MonoBehaviour
    {
        [Tooltip("키 라벨 TMP. 비워두면 자식에서 자동 생성.")]
        [SerializeField] private TMP_Text _label;

        private PressableButton _button;
        private LayoutElement _layoutElement;
        private KoreanKeyboard _keyboard;
        private KeyDef _def;

        /// <summary>키 정의.</summary>
        public KeyDef Def => _def;

        /// <summary>컨트롤러가 키를 생성한 직후 호출. 이벤트 연결 및 초기 라벨/폰트 설정.</summary>
        public void Bind(KoreanKeyboard keyboard, KeyDef def, TMP_FontAsset font, float keySize, float spacing)
        {
            _keyboard = keyboard;
            _def = def;

            EnsureLabel(font);
            EnsureLayout(keySize, spacing);
            EnsureHook();
            Refresh(keyboard.IsHangul, keyboard.IsShifted);
        }

        private const string LabelObjectName = "KeyLabel";

        // MRTK Action Button 의 기본 라벨("Text")은 프론트플레이트/애니메이터/컬링에 가려 보이지 않으므로
        // (z 를 앞으로 띄워도 안 보임), 키 루트에 사용자 쪽(-Z)으로 띄운 전용 KeyLabel 을 만들어 사용한다.
        private void EnsureLabel(TMP_FontAsset font)
        {
            // Action Button 기본 캡션 라벨이 있으면 비워서 간섭을 막는다.
            foreach (TMP_Text existing in GetComponentsInChildren<TMP_Text>(true))
            {
                if (existing.gameObject.name == "Text")
                {
                    existing.text = string.Empty;
                }
            }

            if (_label == null)
            {
                Transform found = transform.Find(LabelObjectName);
                _label = found != null ? found.GetComponent<TMP_Text>() : null;
            }

            if (_label == null)
            {
                GameObject go = new GameObject(LabelObjectName, typeof(RectTransform));
                go.layer = gameObject.layer;

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.SetParent(transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
                // 사용자 쪽(-Z, 프론트플레이트 앞)으로 띄워 깊이 가림을 방지.
                rt.anchoredPosition3D = new Vector3(0f, 0f, -6f);

                _label = go.AddComponent<TextMeshProUGUI>();
            }

            if (font != null)
            {
                _label.font = font;
            }
            _label.color = Color.white;
            _label.alignment = TextAlignmentOptions.MidlineGeoAligned; // 가로 geometry 중앙 + 세로 Midline
            _label.enableAutoSizing = false;
            _label.fontSize = 14f;
            _label.enableWordWrapping = false;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.raycastTarget = false;
        }

        private void EnsureLayout(float keySize, float spacing)
        {
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
            {
                _layoutElement = gameObject.AddComponent<LayoutElement>();
            }
            // 가로 폭은 Weight 로 결정(행 좌우 정렬을 예측 가능하게). 라벨이 길면 Weight 를 키워 맞춘다.
            _layoutElement.preferredWidth = keySize * _def.Weight + spacing * (_def.Weight - 1f);
            _layoutElement.preferredHeight = keySize;
            _layoutElement.flexibleWidth = 0f;
            _layoutElement.flexibleHeight = 0f;
        }

        private void EnsureHook()
        {
            if (_button == null)
            {
                _button = GetComponent<PressableButton>();
            }
            if (_button != null)
            {
                _button.OnClicked.RemoveListener(OnClicked);
                _button.OnClicked.AddListener(OnClicked);
            }
        }

        private void OnClicked()
        {
            if (_keyboard != null)
            {
                _keyboard.HandleKey(this);
            }
        }

        /// <summary>한/영·Shift 상태에 맞춰 라벨을 다시 그린다.</summary>
        public void Refresh(bool hangul, bool shifted)
        {
            if (_label != null)
            {
                _label.text = _def.LabelFor(hangul, shifted);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.OnClicked.RemoveListener(OnClicked);
            }
        }
    }
}
