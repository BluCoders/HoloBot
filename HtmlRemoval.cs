using System;
using System.Text.RegularExpressions;

namespace HoloBot
{
    public static class HtmlRemoval
    {
        /// <summary>
        /// Remove HTML from string with Regex.
        /// </summary>
        public static string StripTagsRegex(string source)
        {
            return Regex.Replace(source, "&lt;.*?&gt;", string.Empty);
        }

        /// <summary>
        /// Removes HTML comments from string with Regex.
        /// </summary>
        public static string RemoveHTMLCommentsRegex(string source)
        {
            string output = string.Empty;
            string[] temp = System.Text.RegularExpressions.Regex.Split(source, "&lt;!--");
            foreach (string s in temp)
            {
                string str = string.Empty;
                if (!s.Contains("--&gt;"))
                {
                    str = s;
                }
                else
                {
                    str = s.Substring(s.IndexOf("--&gt;") + 3);
                }
                if (str.Trim() != string.Empty)
                {
                    output = output + str.Trim();
                }
                Console.WriteLine(output);
            }
            return output;
        }
        /// <summary>
        /// Compiled regular expression for performance.
        /// </summary>
        static Regex _htmlRegex = new Regex("&lt;.*?&gt;", RegexOptions.Compiled);

        /// <summary>
        /// Remove HTML from string with compiled Regex.
        /// </summary>
        public static string StripTagsRegexCompiled(string source)
        {
            return _htmlRegex.Replace(source, string.Empty);
        }

        /// <summary>
        /// Remove HTML tags from string using char array.
        /// </summary>
        public static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
    }
}
