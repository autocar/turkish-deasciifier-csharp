using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace TurkishDeasciifier
{
    using Compatibility;
    public class Deasciifier
    {
        public const int DEFAULT_CONTEXTSIZE = 20;

        #region Private Values
        private readonly int contextSize;
        private readonly bool aggressive;
        private WordSet<string> exclusions;
        private Dictionary<string, string> corrections;
        private string originalString;
        private char[] buffer;
        #endregion

        #region Constructors
        public Deasciifier() : this(DEFAULT_CONTEXTSIZE, false) { }

        public Deasciifier(int contextSize, bool aggressive)
        {
            this.contextSize = contextSize;
            this.aggressive = aggressive;
            //LoadExclusions();
            //LoadCorrections();
        }
        #endregion

        #region Public Methods
        static readonly Dictionary<char, string> UnicodeAsciiMap = new Dictionary<char, string>
        {
            {'ı', "i"}, {'æ', "æ"}, {'ø', "o"}, {'ð', "o"}, {'ł', "l"}, {'đ', "d"}, {'ß', "ss"}, {'þ', "th"}
        };

        /// <summary>
        /// Asciifies the input
        /// </summary>
        /// <param name="str">input string with non-ascii letters</param>
        /// <returns>asciified string</returns>
        public static string Asciify(string str)
        {
            StringBuilder asciified = new StringBuilder();
            string normalized = str.Normalize(NormalizationForm.FormD);
            string append;

            foreach (char ch in normalized)
            {
                if (ch < 128)
                {
                    asciified.Append(ch);
                    continue;
                }

                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                bool isUpper = Char.IsUpper(ch);
                char lowerChar = Char.ToLowerInvariant(ch);

                if (UnicodeAsciiMap.TryGetValue(lowerChar, out append) == false)
                {
                    if (lowerChar == (char)778)
                        append = asciified[asciified.Length - 1].ToString();
                    else
                        append = String.Empty;
                }

                if (append != null)
                {
                    if (isUpper)
                        append = append.ToUpperInvariant();
                    asciified.Append(append);
                }
            }
            return asciified.ToString();
        }

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

            if (exclusions != null)
            {
                CheckExclusions(asciiString);
            }

            this.originalString = asciiString;
            this.buffer = asciiString.ToCharArray(startIndex, length);
            for (var i = startIndex; i < length; i++)
            {
                char ch = buffer[i], x;
                if (NeedCorrection(ch, i) && DeasciifierPatterns.TurkishToggleAccentsTable.TryGetValue(ch, out x))
                {
                    // Adds or removes turkish accent at the cursor.
                    SetCharAt(this.buffer, i, x);
                }
            }
            
            asciiString = new string(buffer);
            if (exclusions != null)
            {
                asciiString = ApplyExclusions(asciiString);
            }
            if (corrections != null)
            {
                asciiString = ApplyCorrections(asciiString);
            }
            return asciiString;
        }
        #endregion

        #region Private Methods
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private static string[] LoadList(string resourceName)
        {
            List<string> list = new List<string>();
            Assembly assembly = Assembly.GetExecutingAssembly();
            resourceName = string.Join(".", new string[] { assembly.GetName().Name, "Resources", resourceName });
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (TextReader reader = new StreamReader(stream))
            {
                string line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()) && line.Trim().Length > 0)
                {
                    list.Add(line.Trim().ToLower());
                }
            }
            return list.ToArray();
        }

        struct WordPosition
        {
            internal int StartIndex;
            internal int EndIndex;
        }

        private List<WordPosition> exclusionPositions;
        private void CheckExclusions(string input)
        {
            exclusionPositions = new List<WordPosition>();
            foreach (string exc in exclusions)
            {
                int idx;
                if ((idx = input.IndexOf(exc)) > -1 &&
                    ((idx > 0 && input[idx-1] == ' ') &&
                    (idx + exc.Length < input.Length && input[idx + exc.Length] == ' ')))
                {
                    exclusionPositions.Add(new WordPosition(){StartIndex=idx, EndIndex=idx+exc.Length});
                }
            }
        }

        private string ApplyExclusions(string output)
        {
            StringBuilder sb = new StringBuilder(output);
            foreach (var exc in exclusionPositions)
            {
                for (int i = exc.StartIndex; i < exc.EndIndex; i++)
                {
                    sb[i] = originalString[i];
                }
            }
            return sb.ToString();
        }

        private void LoadExclusions()
        {
            string[] list = LoadList("exceptions.txt");
            this.exclusions = new WordSet<string>(list);
        }

        private string ApplyCorrections(string output)
        {
            StringBuilder sb = new StringBuilder(output);
            foreach (KeyValuePair<string,string> kvp in corrections)
            {
                int idx;
                int len = kvp.Key.Length;
                if ((idx = output.IndexOf(kvp.Key)) > -1 &&
                    ((idx > 0 && output[idx - 1] == ' ') &&
                    (idx + len < output.Length && output[idx + len] == ' ')))
                {
                    for (int i = idx, j =0; i < idx + len; i++)
                    {
                        sb[i] = kvp.Value[j++];
                    }
                }
            }
            return sb.ToString();
        }

        private void LoadCorrections()
        {
            string[] list = LoadList("corrections.txt");
            this.corrections = new Dictionary<string, string>(list.Length);
            foreach (string line in list)
            {
                string[] w2w = line.Split(',');
                w2w[0] = w2w[0].Trim().ToLower();
                w2w[1] = w2w[1].Trim().ToLower();
                this.corrections.Add(w2w[0], w2w[1]);
            }
        }

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
            else if (!aggressive)
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
            char[] s = GetContext(contextSize, point);
            int rank = pattern.Count * 2;
            int start = 0;
            while (start <= contextSize)
            {
                int end = contextSize + 1;
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

            while (i < s.Length && !space && index < this.buffer.Length)
            {
                char cc = this.buffer[index];
                char x;
                if (!DeasciifierPatterns.TurkishDowncaseAsciifyTable.TryGetValue(cc, out x))
                {
                    if (!space)
                    {
                        i++;
                        space = true;
                    }
                }
                else
                {
                    SetCharAt(s, i, x);
                    i++;
                    space = false;
                }
                //i++;
                index++;
            }

            Array.Resize(ref s, i);
            index = point;
            i = size - 1;
            space = false;
            index--;

            while (i >= 0 && index >= 0)
            {
                char cc = this.buffer[index];
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
