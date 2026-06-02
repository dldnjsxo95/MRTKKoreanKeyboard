// 키보드 데모용 TMP_InputField(월드 캔버스)를 씬에 생성한다.
// 씬의 KoreanKeyboardManager 가 InputField 선택을 감지해 키보드를 자동 표시하므로,
// 필드 자체에는 별도 트리거 컴포넌트가 필요 없다.

#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ShareLens.Keyboard.EditorTools
{
    public static class KeyboardDemoSetup
    {
        // 폰트는 설치 위치(Assets/ vs Packages/)에 무관하게 GUID 검색으로 찾는다(패키지 배포 대응).
        private static TMP_FontAsset FindKoreanFont()
        {
            string[] guids = AssetDatabase.FindAssets("Malgun SDF t:TMP_FontAsset");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }

        [MenuItem("Tools/ShareLens/Create Demo InputField")]
        public static void CreateDemoInputField()
        {
            // 키보드 표시는 씬의 KoreanKeyboardManager 가 담당. 없으면 경고만 하고 필드는 그대로 생성.
#if UNITY_2023_1_OR_NEWER
            var manager = Object.FindFirstObjectByType<KoreanKeyboardManager>(FindObjectsInactive.Include);
#else
            var manager = Object.FindObjectOfType<KoreanKeyboardManager>(true);
#endif
            if (manager == null)
            {
                Debug.LogWarning("[KeyboardDemo] 씬에 KoreanKeyboardManager 가 없습니다. 필드를 선택해도 키보드가 자동 표시되지 않습니다.");
            }

            var font = FindKoreanFont();

            // 월드 스페이스 캔버스 (키보드 위쪽)
            var canvasGo = new GameObject("DemoInputCanvas",
                typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasRt = (RectTransform)canvasGo.transform;
            canvasRt.sizeDelta = new Vector2(460f, 70f);
            canvasRt.position = new Vector3(0f, 1.56f, 0.6f);
            canvasRt.localScale = Vector3.one * 0.001f;

            // 배경
            var bg = new GameObject("Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bg.layer = canvasGo.layer;
            var bgRt = (RectTransform)bg.transform;
            bgRt.SetParent(canvasRt, false);
            Stretch(bgRt);
            bg.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.95f);

            // TMP InputField (표준 구조 생성)
            GameObject fieldGo = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            fieldGo.name = "DemoInputField";
            var fieldRt = (RectTransform)fieldGo.transform;
            fieldRt.SetParent(canvasRt, false);
            Stretch(fieldRt);
            SetLayerRecursively(fieldGo, canvasGo.layer);

            var inputField = fieldGo.GetComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.pointSize = 28f;
            if (inputField.textComponent != null)
            {
                if (font != null) inputField.textComponent.font = font;
                inputField.textComponent.fontSize = 28f;
                inputField.textComponent.color = Color.white;
            }
            if (inputField.placeholder is TMP_Text placeholder)
            {
                if (font != null) placeholder.font = font;
                placeholder.fontSize = 28f;
                placeholder.text = "여기를 눌러 입력하세요";
                placeholder.color = new Color(1f, 1f, 1f, 0.4f);
            }

            Selection.activeGameObject = fieldGo;
            EditorUtility.SetDirty(canvasGo);
            Debug.Log("[KeyboardDemo] DemoInputField 생성 완료. 필드 선택 시 KoreanKeyboardManager 가 키보드를 표시합니다.");
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
#endif
