// 두벌식 한글 조합 오토마타 (순수 C#, Unity 비의존)
// 초성/중성/종성 조합, 복합 모음, 겹받침, 연음(받침 이동), 백스페이스 분해를 처리한다.

using System.Collections.Generic;

namespace ShareLens.Keyboard
{
    /// <summary>
    /// 한 글자(음절) 단위의 한글 조합 상태를 들고 있는 두벌식 오토마타.
    /// 컨트롤러는 확정된 텍스트(Committed)와 현재 조합 중인 글자(<see cref="Composing"/>)를
    /// 이어 붙여 화면에 표시한다.
    ///
    /// 사용 패턴:
    ///   string committedDelta = composer.Input(jamo);  // 확정되어 흘러나온 문자열(없으면 "")
    ///   string display = committed + composer.Composing; // 조합 중 글자는 항상 꼬리에 위치
    /// </summary>
    public sealed class HangulComposer
    {
        // ── 자모 테이블 ──────────────────────────────────────────────
        // 초성 19자 (인덱스 0~18)
        private static readonly char[] Cho =
        {
            'ㄱ','ㄲ','ㄴ','ㄷ','ㄸ','ㄹ','ㅁ','ㅂ','ㅃ','ㅅ',
            'ㅆ','ㅇ','ㅈ','ㅉ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
        };

        // 중성 21자 (인덱스 0~20)
        private static readonly char[] Jung =
        {
            'ㅏ','ㅐ','ㅑ','ㅒ','ㅓ','ㅔ','ㅕ','ㅖ','ㅗ','ㅘ',
            'ㅙ','ㅚ','ㅛ','ㅜ','ㅝ','ㅞ','ㅟ','ㅠ','ㅡ','ㅢ','ㅣ'
        };

        // 종성 28자 (인덱스 0 = 받침 없음, 1~27)
        private static readonly char[] Jong =
        {
            '\0','ㄱ','ㄲ','ㄳ','ㄴ','ㄵ','ㄶ','ㄷ','ㄹ','ㄺ',
            'ㄻ','ㄼ','ㄽ','ㄾ','ㄿ','ㅀ','ㅁ','ㅂ','ㅄ','ㅅ',
            'ㅆ','ㅇ','ㅈ','ㅊ','ㅋ','ㅌ','ㅍ','ㅎ'
        };

        // 자모 문자 -> 초성 인덱스
        private static readonly Dictionary<char, int> ChoIndex = BuildIndex(Cho);
        // 자모 문자 -> 중성 인덱스
        private static readonly Dictionary<char, int> JungIndex = BuildIndex(Jung);

        // 단일 받침으로 쓸 수 있는 자음 -> 종성 인덱스 (ㄸ,ㅃ,ㅉ 는 받침이 될 수 없음)
        private static readonly Dictionary<char, int> JongSingleIndex = new Dictionary<char, int>
        {
            { 'ㄱ', 1 }, { 'ㄲ', 2 }, { 'ㄴ', 4 }, { 'ㄷ', 7 }, { 'ㄹ', 8 },
            { 'ㅁ',16 }, { 'ㅂ',17 }, { 'ㅅ',19 }, { 'ㅆ',20 }, { 'ㅇ',21 },
            { 'ㅈ',22 }, { 'ㅊ',23 }, { 'ㅋ',24 }, { 'ㅌ',25 }, { 'ㅍ',26 }, { 'ㅎ',27 }
        };

        // 겹받침 합치기: (현재 종성 인덱스, 추가 자음) -> 새 종성 인덱스
        private static readonly Dictionary<(int, char), int> JongCombine = new Dictionary<(int, char), int>
        {
            { (1,  'ㅅ'),  3 }, // ㄱ+ㅅ = ㄳ
            { (4,  'ㅈ'),  5 }, // ㄴ+ㅈ = ㄵ
            { (4,  'ㅎ'),  6 }, // ㄴ+ㅎ = ㄶ
            { (8,  'ㄱ'),  9 }, // ㄹ+ㄱ = ㄺ
            { (8,  'ㅁ'), 10 }, // ㄹ+ㅁ = ㄻ
            { (8,  'ㅂ'), 11 }, // ㄹ+ㅂ = ㄼ
            { (8,  'ㅅ'), 12 }, // ㄹ+ㅅ = ㄽ
            { (8,  'ㅌ'), 13 }, // ㄹ+ㅌ = ㄾ
            { (8,  'ㅍ'), 14 }, // ㄹ+ㅍ = ㄿ
            { (8,  'ㅎ'), 15 }, // ㄹ+ㅎ = ㅀ
            { (17, 'ㅅ'), 18 }, // ㅂ+ㅅ = ㅄ
        };

        // 겹받침 분해: 종성 인덱스 -> (남는 종성 인덱스, 다음 초성으로 넘어가는 자음)
        private static readonly Dictionary<int, (int rest, char moved)> JongSplit = new Dictionary<int, (int, char)>
        {
            { 3,  (1,  'ㅅ') }, { 5,  (4,  'ㅈ') }, { 6,  (4,  'ㅎ') },
            { 9,  (8,  'ㄱ') }, { 10, (8,  'ㅁ') }, { 11, (8,  'ㅂ') },
            { 12, (8,  'ㅅ') }, { 13, (8,  'ㅌ') }, { 14, (8,  'ㅍ') },
            { 15, (8,  'ㅎ') }, { 18, (17, 'ㅅ') },
        };

        // 복합 모음 합치기: (현재 중성 인덱스, 추가 모음) -> 새 중성 인덱스
        private static readonly Dictionary<(int, char), int> JungCombine = new Dictionary<(int, char), int>
        {
            { (8,  'ㅏ'),  9 }, // ㅗ+ㅏ = ㅘ
            { (8,  'ㅐ'), 10 }, // ㅗ+ㅐ = ㅙ
            { (8,  'ㅣ'), 11 }, // ㅗ+ㅣ = ㅚ
            { (13, 'ㅓ'), 14 }, // ㅜ+ㅓ = ㅝ
            { (13, 'ㅔ'), 15 }, // ㅜ+ㅔ = ㅞ
            { (13, 'ㅣ'), 16 }, // ㅜ+ㅣ = ㅟ
            { (18, 'ㅣ'), 19 }, // ㅡ+ㅣ = ㅢ
        };

        // 복합 모음 분해(백스페이스용): 중성 인덱스 -> 앞 모음 인덱스
        private static readonly Dictionary<int, int> JungSplit = new Dictionary<int, int>
        {
            { 9, 8 }, { 10, 8 }, { 11, 8 }, { 14, 13 }, { 15, 13 }, { 16, 13 }, { 19, 18 },
        };

        private const int SyllableBase = 0xAC00;

        // ── 조합 상태 ────────────────────────────────────────────────
        private int _cho = -1;  // 초성 인덱스 (-1 = 없음)
        private int _jung = -1; // 중성 인덱스 (-1 = 없음)
        private int _jong = 0;  // 종성 인덱스 (0 = 없음)

        /// <summary>현재 조합 중인 글자가 있는지 여부.</summary>
        public bool HasComposition => _cho >= 0 || _jung >= 0;

        /// <summary>현재 조합 중인 글자(없으면 빈 문자열).</summary>
        public string Composing => Build(_cho, _jung, _jong);

        /// <summary>입력 문자가 한글 자모인지 판별한다.</summary>
        public static bool IsJamo(char c) => ChoIndex.ContainsKey(c) || JungIndex.ContainsKey(c);

        /// <summary>
        /// 자모 한 개를 입력한다. 조합 결과 더 이상 조합 대상이 아니게 되어
        /// 확정(commit)된 문자열을 반환한다. 확정된 것이 없으면 빈 문자열.
        /// </summary>
        public string Input(char jamo)
        {
            if (JungIndex.TryGetValue(jamo, out int jungIdx))
            {
                return InputVowel(jamo, jungIdx);
            }
            if (ChoIndex.TryGetValue(jamo, out int choIdx))
            {
                return InputConsonant(jamo, choIdx);
            }
            // 한글 자모가 아니면 현재 조합을 확정하고 입력 문자를 그대로 덧붙인다.
            return Flush() + jamo;
        }

        private string InputConsonant(char c, int choIdx)
        {
            bool canBeJong = JongSingleIndex.TryGetValue(c, out int jongSingle);

            // 1) 빈 상태 -> 초성 시작
            if (_cho < 0 && _jung < 0)
            {
                _cho = choIdx;
                return "";
            }

            // 2) 홑 초성만 있는 상태(받침 결합 불가) -> 확정 후 새 초성
            if (_cho >= 0 && _jung < 0)
            {
                return CommitAndStartCho(choIdx);
            }

            // 3) 홑 모음만 있는 상태 -> 확정 후 새 초성
            if (_cho < 0 && _jung >= 0)
            {
                return CommitAndStartCho(choIdx);
            }

            // 4) 받침 없는 CV -> 받침으로 시도
            if (_jong == 0)
            {
                if (canBeJong)
                {
                    _jong = jongSingle;
                    return "";
                }
                return CommitAndStartCho(choIdx);
            }

            // 5) 받침 있는 CVC -> 겹받침 결합 시도
            if (JongCombine.TryGetValue((_jong, c), out int combined))
            {
                _jong = combined;
                return "";
            }
            return CommitAndStartCho(choIdx);
        }

        private string InputVowel(char v, int jungIdx)
        {
            // 1) 빈 상태 -> 홑 모음 시작
            if (_cho < 0 && _jung < 0)
            {
                _jung = jungIdx;
                return "";
            }

            // 2) 홑 초성만 있는 상태 -> 정상 결합 (ㄱ + ㅏ = 가)
            if (_cho >= 0 && _jung < 0)
            {
                _jung = jungIdx;
                return "";
            }

            // 3) 홑 모음만 있는 상태 -> 복합 모음 시도, 실패 시 확정 후 새 홑 모음
            if (_cho < 0 && _jung >= 0)
            {
                if (JungCombine.TryGetValue((_jung, v), out int comp))
                {
                    _jung = comp;
                    return "";
                }
                string committed = Build(_cho, _jung, _jong);
                Reset();
                _jung = jungIdx;
                return committed;
            }

            // 4) 받침 없는 CV -> 복합 모음 시도, 실패 시 확정 후 새 홑 모음
            if (_jong == 0)
            {
                if (JungCombine.TryGetValue((_jung, v), out int comp))
                {
                    _jung = comp;
                    return "";
                }
                string committed = Build(_cho, _jung, _jong);
                Reset();
                _jung = jungIdx;
                return committed;
            }

            // 5) 받침 있는 CVC + 모음 -> 연음: 받침(의 마지막 자음)이 다음 글자의 초성으로 이동
            int restJong, newCho;
            if (JongSplit.TryGetValue(_jong, out var split))
            {
                restJong = split.rest;
                newCho = ChoIndex[split.moved];
            }
            else
            {
                restJong = 0;
                newCho = ChoIndex[Jong[_jong]];
            }

            string head = Build(_cho, _jung, restJong); // 받침 일부를 떼어낸 앞 글자
            Reset();
            _cho = newCho;
            _jung = jungIdx;
            return head;
        }

        /// <summary>
        /// 백스페이스. 조합 중인 글자를 한 단계 분해한다.
        /// 분해할 것이 있었으면 true, 조합이 비어 있어 처리하지 못했으면 false(컨트롤러가 확정 문자를 지운다).
        /// </summary>
        public bool Backspace()
        {
            if (_jong > 0)
            {
                _jong = JongSplit.TryGetValue(_jong, out var split) ? split.rest : 0;
                return true;
            }
            if (_jung >= 0)
            {
                _jung = JungSplit.TryGetValue(_jung, out int prev) ? prev : -1;
                return true;
            }
            if (_cho >= 0)
            {
                _cho = -1;
                return true;
            }
            return false;
        }

        /// <summary>조합 중인 글자를 확정 문자열로 반환하고 상태를 비운다.</summary>
        public string Flush()
        {
            string result = Build(_cho, _jung, _jong);
            Reset();
            return result;
        }

        /// <summary>상태를 모두 비운다.</summary>
        public void Reset()
        {
            _cho = -1;
            _jung = -1;
            _jong = 0;
        }

        // ── 내부 헬퍼 ────────────────────────────────────────────────
        private string CommitAndStartCho(int choIdx)
        {
            string committed = Build(_cho, _jung, _jong);
            Reset();
            _cho = choIdx;
            return committed;
        }

        private static string Build(int cho, int jung, int jong)
        {
            if (cho >= 0 && jung >= 0)
            {
                int code = SyllableBase + (cho * 21 + jung) * 28 + jong;
                return ((char)code).ToString();
            }
            if (cho >= 0)
            {
                return Cho[cho].ToString();
            }
            if (jung >= 0)
            {
                return Jung[jung].ToString();
            }
            return "";
        }

        private static Dictionary<char, int> BuildIndex(char[] table)
        {
            var map = new Dictionary<char, int>(table.Length);
            for (int i = 0; i < table.Length; i++)
            {
                if (table[i] != '\0' && !map.ContainsKey(table[i]))
                {
                    map[table[i]] = i;
                }
            }
            return map;
        }
    }
}
