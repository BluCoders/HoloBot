using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace HoloBot
{
    public static class Wiki
    {
        public static async Task<string[]> queryWiki(object query)
        {
            string[] output = { "first", "second", "en", ""};
            output.SetValue("test", 0);
            string title = "Could not be found", wikiText = null, titleFragment = null, lang = "en";
            Thread waiting = new Thread(Wiki.sleeper);
            waiting.Start(3000);
            while(true)
            {
                string url = "https://" + lang + ".wikipedia.org/w/api.php?action=parse&format=xml&prop=text&redirects=true&page=" + query.ToString().Replace(" ", "+").ToString();
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = client.GetAsync(url).Result)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        response.EnsureSuccessStatusCode();

                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.ConformanceLevel = ConformanceLevel.Fragment;
                        using (XmlReader reader = XmlReader.Create(new StringReader(responseBody), settings))
                        {
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.Element)
                                {
                                    if (reader.Name == "parse")
                                    {
                                        while (reader.MoveToNextAttribute())
                                        {
                                            title = reader.Value;
                                        }
                                    }
                                    else if (reader.Name == "r")
                                    {
                                        if(reader["tofragment"] != null)
                                        {
                                            titleFragment = reader["tofragment"];
                                        }
                                    }
                                    else if (reader.Name == "text")
                                    {
                                        wikiText = reader.ReadInnerXml().ToString();
                                        if (wikiText != null)
                                        {
                                            string replaceWith = " ";
                                            if(titleFragment != null)
                                            {
                                                wikiText = wikiText.Substring(wikiText.IndexOf("id=\"" + titleFragment + "\""), wikiText.Length - wikiText.IndexOf("id=\"" + titleFragment + "\""));
                                            }
                                            //wikiText = HtmlRemoval.RemoveHTMLCommentsRegex(wikiText);
                                            wikiText = wikiText.Substring(wikiText.IndexOf("&lt;p&gt;"), wikiText.Length - wikiText.IndexOf("&lt;p&gt;"));
                                            wikiText = Regex.Replace(HtmlRemoval.StripTagsRegex(wikiText), @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
                                            wikiText = wikiText.Replace("\r\n", replaceWith).Replace("\n", replaceWith).Replace("\r", replaceWith);
                                            wikiText = FirstSentence(FirstWords(wikiText, 50));
                                        }
                                    }
                                }
                            }
                            if(wikiText == null && lang != "ja")
                            {
                                switch(lang)
                                {
                                    case "en":
                                        lang = "sv";
                                        break;
                                    case "sv":
                                        lang = "no";
                                        break;
                                    case "no":
                                        lang = "dk";
                                        break;
                                    case "dk":
                                        lang = "de";
                                        break;
                                    case "de":
                                        lang = "ja";
                                        break;
                                    default:
                                        lang = "en";
                                        break;
                                }
                                continue;
                            }
                            waiting.Abort();
                            output.SetValue(title, 0);
                            output.SetValue(wikiText, 1);
                            output.SetValue(lang, 2);
                            if (titleFragment != null)
	                        {
                                output.SetValue("#" + titleFragment.Replace(" ", "_"), 3);
	                        }
                            else
                            {
                                output.SetValue(titleFragment, 3);
                            }
                            return output;
                        }
                    }
                }
            }
        }
        public static void sleeper(object sleep)
        {
            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep((int)sleep);
                Console.WriteLine("Testing");
                Program.writer.WriteLine("PRIVMSG " + Program.channel + " :Still searching..");
                Program.writer.Flush();
            }
        }
        public static string FirstWords(string input, int numberWords)
        {
            try
            {
                // Number of words we still want to display.
                int words = numberWords;
                // Loop through entire summary.
                for (int i = 0; i < input.Length; i++)
                {
                    // Increment words on a space.
                    if (input[i] == ' ')
                    {
                        words--;
                    }
                    // If we have no more words to display, return the substring.
                    if (words == 0)
                    {
                        return input.Substring(0, i);
                    }
                }
            }
            catch (Exception)
            {
                // Log the error.
            }
            return string.Empty;
        }
        public static string FirstSentence(string paragraph)
        {
            for (int i = 0; i < paragraph.Length; i++)
            {
                switch (paragraph[i])
                {
                    case '。':
                    case '.':
                        if (i < (paragraph.Length - 1) &&
                        char.IsWhiteSpace(paragraph[i + 1]))
                        {
                            goto case '!';
                        }
                        break;
                    case '！':
                    case '!':
                        return paragraph.Substring(0, i + 1);
                }
            }
            return paragraph;
        }
    }
}
