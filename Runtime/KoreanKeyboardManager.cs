// 전역 키보드 매니저. EventSystem 선택 상태를 감시해, 씬의 어떤 TMP_InputField 가 선택되든
// 자동으로 KoreanKeyboard 를 띄우고 연결한다. (필드마다 트리거를 붙일 필요 없음)
//
// 사용: 씬에 이 컴포넌트(항상 활성) 하나만 두면 끝.
//  - _keyboard 에 씬 인스턴스든 프리팹 에셋이든 넣으면 알아서 처리.
//  - 프리팹 에셋이면 InputField 첫 선택 시점에 1회 인스턴스화해서 사용.
//  - 비워두면 씬에서 자동 탐색(비활성 포함).

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ShareLens.Keyboard
{
    public sealed class KoreanKeyboardManager : MonoBehaviour
    {
        [Tooltip("표시할 키보드. 씬 인스턴스 또는 프리팹 에셋 모두 가능. " +
                 "프리팹이면 첫 선택 시 자동 생성, 비워두면 씬에서 자동 탐색(비활성 포함).")]
        [SerializeField] private KoreanKeyboard _keyboard;

        private GameObject _lastSelected;

        private void Update()
        {
            EventSystem es = EventSystem.current;
            if (es == null)
            {
                return;
            }

            GameObject selected = es.currentSelectedGameObject;
            if (selected == _lastSelected)
            {
                return; // 선택 변화 없음
            }
            _lastSelected = selected;

            if (selected != null && selected.TryGetComponent(out TMP_InputField field))
            {
                KoreanKeyboard keyboard = EnsureKeyboard();
                if (keyboard == null)
                {
                    Debug.LogWarning("[KoreanKeyboardManager] 표시할 키보드가 없습니다. _keyboard 에 인스턴스나 프리팹을 지정하세요.", this);
                    return;
                }

                // InputField 가 선택됨 → 키보드 표시 + 연결. (닫기는 별도 Close 버튼이 담당)
                keyboard.AttachTo(field);
            }
        }

        // 필요한 시점에 사용 가능한 씬 인스턴스를 확보한다.
        private KoreanKeyboard EnsureKeyboard()
        {
            // 1) 지정값이 없으면 씬에서 탐색.
            if (_keyboard == null)
            {
#if UNITY_2023_1_OR_NEWER
                _keyboard = Object.FindFirstObjectByType<KoreanKeyboard>(FindObjectsInactive.Include);
#else
                _keyboard = Object.FindObjectOfType<KoreanKeyboard>(true);
#endif
            }

            // 2) 지정값이 프리팹 에셋이면(씬에 속하지 않음) 씬 루트에 인스턴스화해서 교체.
            //    프리팹 에셋을 그대로 SetActive 하면 디스크 원본을 건드리므로 반드시 복제해야 한다.
            if (_keyboard != null && !_keyboard.gameObject.scene.IsValid())
            {
                _keyboard = Instantiate(_keyboard);
                _keyboard.name = _keyboard.name.Replace("(Clone)", string.Empty);
            }

            return _keyboard;
        }
    }
}
