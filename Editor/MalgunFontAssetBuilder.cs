// 맑은 고딕(malgun.ttf)으로부터 한글 글리프를 동적(Dynamic) 생성하는 TMP 폰트 에셋을 만든다.
// 한글 음절은 11,172자라 정적 아틀라스로는 부적합 → Dynamic 모드로 런타임에 필요한 글자만 굽는다.

#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ShareLens.Keyboard.EditorTools
{
    public static class MalgunFontAssetBuilder
    {
        private const string FontPath = "Assets/ShareLens/Keyboard/Fonts/malgun.ttf";
        private const string OutputPath = "Assets/ShareLens/Keyboard/Fonts/Malgun SDF.asset";

        [MenuItem("Tools/ShareLens/Create Malgun TMP Font")]
        public static TMP_FontAsset Create()
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (source == null)
            {
                Debug.LogError($"[Malgun] 소스 폰트를 찾을 수 없음: {FontPath}");
                return null;
            }

            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
            if (existing != null)
            {
                Debug.Log($"[Malgun] 이미 존재합니다: {OutputPath}");
                Selection.activeObject = existing;
                return existing;
            }

            // 90pt 샘플링, padding 9, SDFAA, 1024x1024 아틀라스, Dynamic
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);

            if (fontAsset == null)
            {
                Debug.LogError("[Malgun] TMP_FontAsset 생성에 실패했습니다.");
                return null;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(OutputPath);
            AssetDatabase.CreateAsset(fontAsset, OutputPath);

            // 아틀라스 텍스처/머티리얼을 폰트 에셋의 서브에셋으로 포함
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
            {
                fontAsset.atlasTextures[0].name = "Malgun Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = "Malgun Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(OutputPath);

            Debug.Log($"[Malgun] 생성 완료: {OutputPath}");
            Selection.activeObject = fontAsset;
            return fontAsset;
        }
    }
}
#endif
