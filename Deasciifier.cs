﻿using System;
using System.Collections.Generic;

namespace TurkishDeasciifier
{
    public class Deasciifier
    {
        public const int DefaultContextsize = 20;

        #region Private Values
        private readonly int _contextSize;
        private readonly bool _aggressive;
        private char[] _buffer;
        #endregion

        #region Constructors
        public Deasciifier() : this(DefaultContextsize, false) { }

        public Deasciifier(int contextSize, bool aggressive)
        {
            _contextSize = contextSize;
            _aggressive = aggressive;
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
            if (asciiString == null) { throw new ArgumentNullException("asciiString"); }
            asciiString += " ";
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
            _buffer = asciiString.ToCharArray(startIndex, length);
            for (var i = startIndex; i < length; i++)
            {
                char ch = _buffer[i], x;
                if (NeedCorrection(ch, i) && DeasciifierPatterns.TurkishToggleAccentsTable.TryGetValue(ch, out x))
                {
                    // Adds or removes turkish accent at the cursor.
                    SetCharAt(_buffer, i, x);
                }
            }
            
            return new string(_buffer);
        }
        #endregion

        #region Private Methods
        private static void SetCharAt(char[] buf, int index, char ch)
        {
            buf[index] = ch;
        }

        /// <summary>
        /// Determine if char at cursor needs correction.
        /// </summary>
        /// <param name="ch">char</param>
        /// <param name="point">index</param>
        /// <returns>whether if needs correction</returns>
        private bool NeedCorrection(char ch, int point)
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
                m = MatchPattern(pattern, point);
            }

            if (tr == 'I')
                return (ch == tr) ? !m : m;
            return (ch == tr) ? m : !m;
        }

        private bool MatchPattern(Dictionary<string, short> pattern, int point)
        {
            char[] s = GetContext(_contextSize, point);
            int rank = pattern.Count * 2;
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

        private char[] GetContext(int size, int point)
        {
            char[] s = new string(' ', 1 + (2 * size)).ToCharArray();
            SetCharAt(s, size, 'X');
            int i = size + 1;
            int index = point + 1;
            bool space = false;

            while (!space && (i < s.Length) && (index < _buffer.Length))
            {
                char cc = _buffer[index];
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
                char cc = _buffer[index];
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
