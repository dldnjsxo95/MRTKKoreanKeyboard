// Pretendard(OFL)로부터 한글 글리프를 동적(Dynamic) 생성하는 TMP 폰트 에셋을 만든다.
// 한글 음절은 11,172자라 정적 아틀라스로는 부적합 → Dynamic 모드로 런타임에 필요한 글자만 굽는다.

#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ShareLens.Keyboard.EditorTools
{
    public static class PretendardFontAssetBuilder
    {
        private const string FontPath = "Assets/ShareLens/Keyboard/Fonts/PretendardVariable.ttf";
        private const string OutputPath = "Assets/ShareLens/Keyboard/Fonts/Pretendard SDF.asset";

        [MenuItem("Tools/ShareLens/Create Pretendard TMP Font")]
        public static TMP_FontAsset Create()
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (source == null)
            {
                Debug.LogError($"[Pretendard] 소스 폰트를 찾을 수 없음: {FontPath}");
                return null;
            }

            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
            if (existing != null)
            {
                Debug.Log($"[Pretendard] 이미 존재합니다: {OutputPath}");
                Selection.activeObject = existing;
                return existing;
            }

            // 90pt 샘플링, padding 9, SDFAA, 1024x1024 아틀라스, Dynamic
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);

            if (fontAsset == null)
            {
                Debug.LogError("[Pretendard] TMP_FontAsset 생성에 실패했습니다.");
                return null;
            }

            fontAsset.name = Path.GetFileNameWithoutExtension(OutputPath);
            AssetDatabase.CreateAsset(fontAsset, OutputPath);

            // 아틀라스 텍스처/머티리얼을 폰트 에셋의 서브에셋으로 포함
            if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
            {
                fontAsset.atlasTextures[0].name = "Pretendard Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            }
            if (fontAsset.material != null)
            {
                fontAsset.material.name = "Pretendard Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(OutputPath);

            Debug.Log($"[Pretendard] 생성 완료: {OutputPath}");
            Selection.activeObject = fontAsset;
            return fontAsset;
        }
    }
}
#endif
