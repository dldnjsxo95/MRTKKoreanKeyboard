// 대상 RectTransform 의 크기(필요 시 위치)를 따라가게 한다.
// KeysRoot(ContentSizeFitter 로 자동 크기) 와 동일 크기를 유지해야 하는 형제 오브젝트(아웃라인 등)에 사용.

using UnityEngine;

namespace ShareLens.Keyboard
{
    [ExecuteAlways]
    public sealed class MatchRectSize : MonoBehaviour
    {
        [Tooltip("크기를 따라갈 대상(예: KeysRoot).")]
        [SerializeField] private RectTransform _target;

        [Tooltip("대상 크기에 더할 여백(상하/좌우).")]
        [SerializeField] private Vector2 _padding = Vector2.zero;

        [Tooltip("위치(anchoredPosition)도 대상과 동일하게 맞출지 여부.")]
        [SerializeField] private bool _matchPosition = false;

        private RectTransform _rt;

        private void OnEnable()
        {
            _rt = transform as RectTransform;
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (_target == null)
            {
                return;
            }
            if (_rt == null)
            {
                _rt = transform as RectTransform;
                if (_rt == null) return;
            }

            Rect r = _target.rect;
            _rt.sizeDelta = new Vector2(r.width, r.height) + _padding;

            if (_matchPosition)
            {
                _rt.anchorMin = _target.anchorMin;
                _rt.anchorMax = _target.anchorMax;
                _rt.pivot = _target.pivot;
                _rt.anchoredPosition = _target.anchoredPosition;
            }
        }
    }
}
