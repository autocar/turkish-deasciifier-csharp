using System;
using System.Collections.Generic;

namespace TurkishDeasciifier
{
    public class Deasciifier
    {
        #region Private Values

        private readonly int _contextSize;
        private readonly bool _aggressive;

        #endregion

        #region Constructors

        public Deasciifier(int contextSize = 20, bool aggressive = false)
        {
            _contextSize = contextSize;
            _aggressive = aggressive;
        }

        #endregion

        #region Public Properties

        public int ContextSize
        {
            get { return _contextSize; }
        }

        public bool IsAggressive
        {
            get { return _aggressive; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Deasciifies the input
        /// </summary>
        /// <param name="asciiString">ascii string</param>
        /// <returns>deasciified string</returns>
        public string DeAsciify(string asciiString)
        {
            if (asciiString == null)
            {
                throw new ArgumentNullException("asciiString");
            }
            return DeAsciify(asciiString, 0, asciiString.Length);
        }

        /// <summary>
        /// Deasciifies the specified input region
        /// </summary>
        /// <param name="asciiString">ascii string</param>
        /// <param name="startIndex">region start index</param>
        /// <param name="length">region size</param>
        /// <returns></returns>
        public string DeAsciify(string asciiString, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(asciiString) || asciiString.Trim().Length == 0)
            {
                return asciiString;
            }
            asciiString += " ";
            char[] buffer = asciiString.ToCharArray(startIndex, length);
            for (int i = startIndex; i < length; i++)
            {
                char ch = buffer[i], x;
                if (NeedCorrection(buffer, ch, i) &&
                    DeasciifierPatterns.TurkishToggleAccentsTable.TryGetValue(ch, out x))
                {
                    // Adds or removes turkish accent at the cursor.
                    SetCharAt(buffer, i, x);
                }
            }

            return new string(buffer).TrimEnd();
        }

        #endregion

        #region Private Methods

        private static void SetCharAt(char[] buffer, int index, char ch)
        {
            buffer[index] = ch;
        }

        /// <summary>
        /// Determine if char at cursor needs correction.
        /// </summary>
        /// <param name="buffer">buffer</param>
        /// <param name="ch">char</param>
        /// <param name="point">index</param>
        /// <returns>whether if needs correction</returns>
        private bool NeedCorrection(char[] buffer, char ch, int point)
        {
            char tr;
            if (!DeasciifierPatterns.TurkishAsciifyTable.TryGetValue(ch, out tr))
                tr = ch;
            else if (!_aggressive)
                return false; // aslı & asli problemi

            bool m = false;
            Dictionary<string, short> pattern;
            if (DeasciifierPatterns.TryGetPattern(tr, out pattern))
            {
                m = MatchPattern(buffer, pattern, point);
            }

            if (tr == 'I')
                return ch == tr ? !m : m;
            return ch == tr ? m : !m;
        }

        private bool MatchPattern(char[] buffer, IDictionary<string, short> pattern, int point)
        {
            char[] s = GetContext(buffer, _contextSize, point);
            int rank = pattern.Count*2;
            int start = 0;
            while (start <= _contextSize)
            {
                int end = _contextSize + 1;
                while (end <= s.Length)
                {
                    string str = new string(s, start, end - start);
                    short r;
                    if (pattern.TryGetValue(str, out r) && Math.Abs(r) < Math.Abs(rank))
                    {
                        rank = r;
                    }
                    end++;
                }
                start++;
            }
            return rank > 0;
        }

        private static char[] GetContext(char[] buffer, int size, int point)
        {
            char[] s = new string(' ', 1 + 2*size).ToCharArray();
            SetCharAt(s, size, 'X');
            int i = size + 1;
            int index = point + 1;
            bool space = false;

            while (!space && (i < s.Length) && (index < buffer.Length))
            {
                char cc = buffer[index];
                char x;
                if (DeasciifierPatterns.TurkishDowncaseAsciifyTable.TryGetValue(cc, out x) == false)
                    space = true;
                else
                    SetCharAt(s, i, x);
                i++;
                index++;
            }

            Array.Resize(ref s, i);
            index = point;
            i = size - 1;
            space = false;
            index--;

            while (i >= 0 && index >= 0)
            {
                char cc = buffer[index];
                char x;
                if (!DeasciifierPatterns.TurkishUpcaseAccentsTable.TryGetValue(cc, out x))
                {
                    if (!space)
                    {
                        i--;
                        space = true;
                    }
                }
                else
                {
                    SetCharAt(s, i, x);
                    i--;
                    space = false;
                }
                //i--;
                index--;
            }
            return s;
        }

        #endregion
    }
}