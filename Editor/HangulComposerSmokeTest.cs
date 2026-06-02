// HangulComposer 두벌식 오토마타 스모크 테스트.
// 메뉴 Tools/ShareLens/Test Hangul Composer 로 실행하면 콘솔에 PASS/FAIL 을 출력한다.

#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ShareLens.Keyboard.EditorTools
{
    public static class HangulComposerSmokeTest
    {
        // 자모 시퀀스를 새 컴포저로 입력하고, 확정 + 조합중 글자를 합친 전체 문자열을 반환.
        private static string Type(string jamos)
        {
            var composer = new HangulComposer();
            var sb = new StringBuilder();
            foreach (char j in jamos)
            {
                sb.Append(composer.Input(j));
            }
            sb.Append(composer.Composing);
            return sb.ToString();
        }

        // 자모 입력 후 백스페이스 n회 적용한 전체 문자열.
        private static string TypeThenBackspace(string jamos, int n)
        {
            var composer = new HangulComposer();
            var committed = new StringBuilder();
            foreach (char j in jamos)
            {
                committed.Append(composer.Input(j));
            }
            for (int i = 0; i < n; i++)
            {
                if (!composer.Backspace() && committed.Length > 0)
                {
                    committed.Remove(committed.Length - 1, 1);
                }
            }
            return committed.ToString() + composer.Composing;
        }

        [MenuItem("Tools/ShareLens/Test Hangul Composer")]
        public static void Run()
        {
            int pass = 0, fail = 0;

            void Check(string label, string actual, string expected)
            {
                if (actual == expected)
                {
                    pass++;
                    Debug.Log($"[HangulTest] PASS {label}: \"{actual}\"");
                }
                else
                {
                    fail++;
                    Debug.LogError($"[HangulTest] FAIL {label}: expected \"{expected}\" but got \"{actual}\"");
                }
            }

            // 기본 조합
            Check("한글", Type("ㅎㅏㄴㄱㅡㄹ"), "한글");
            Check("안녕", Type("ㅇㅏㄴㄴㅕㅇ"), "안녕");
            Check("값", Type("ㄱㅏㅂㅅ"), "값");            // 겹받침 ㅄ
            Check("닭", Type("ㄷㅏㄹㄱ"), "닭");            // 겹받침 ㄺ
            Check("과", Type("ㄱㅗㅏ"), "과");              // 복합모음 ㅘ
            Check("의", Type("ㅇㅡㅣ"), "의");              // 복합모음 ㅢ

            // 연음 (받침이 다음 글자 초성으로 이동)
            Check("연음 마나", Type("ㅁㅏㄴㅏ"), "마나");
            Check("연음 아가", Type("ㅇㅏㄱㅏ"), "아가");
            Check("겹받침 연음 외곬→…", Type("ㄱㅏㄹㄱㅣ"), "갈기"); // 갉+ㅣ → 갈+기

            // 두 자음 연속(받침 불가 조합) → 분리
            Check("ㄱㄴ 분리", Type("ㄱㄴ"), "ㄱㄴ");

            // 백스페이스 분해
            Check("닭→달", TypeThenBackspace("ㄷㅏㄹㄱ", 1), "달");   // 겹받침 한 겹 제거
            Check("값→갑", TypeThenBackspace("ㄱㅏㅂㅅ", 1), "갑");
            Check("과→고", TypeThenBackspace("ㄱㅗㅏ", 1), "고");     // 복합모음 분해
            Check("한→하", TypeThenBackspace("ㅎㅏㄴ", 1), "하");
            Check("하→ㅎ", TypeThenBackspace("ㅎㅏㄴ", 2), "ㅎ");

            Debug.Log($"[HangulTest] ===== 결과: PASS {pass} / FAIL {fail} =====");
        }
    }
}
#endif
