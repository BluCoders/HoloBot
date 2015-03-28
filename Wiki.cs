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
    /// <summary>
    /// Wiki class to get Wikipedia articles. 
    /// </summary>
    public static class Wiki
    {
        public static async Task<string[]> queryWiki(object query)
        {
            // Array in order of: title, lang, wikiText, titleFragment
            string[] output = new string[4];
            string title = "Could not be found", wikiText = null, titleFragment = null, lang = "en";
            output.SetValue(lang, 2);
        
            // Start Thread to write out to IRC every 3 seconds
            Thread waiting = new Thread(Sleeper);
            waiting.Start(3000);
            
            // Loop for each language
            while(true)
            {
                // Wikipedia article from specified language to get
                string url = "https://" + lang + ".wikipedia.org/w/api.php?action=parse&format=xml&prop=text&redirects=true&page=" + query.ToString().Replace(" ", "+").ToString();
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                    
                        using (HttpResponseMessage response = client.GetAsync(url).Result)
                        {
                            // Get the result
                            string responseBody = await response.Content.ReadAsStringAsync();
                            // Ensure request was successful
                            response.EnsureSuccessStatusCode();

                            // Create XmlReader
                            XmlReaderSettings settings = new XmlReaderSettings();
                            settings.ConformanceLevel = ConformanceLevel.Fragment;
                        
                            // Using XmlReader to read result
                            using (XmlReader reader = XmlReader.Create(new StringReader(responseBody), settings))
                            {
                                // For each Xml result
                                while (reader.Read())
                                {
                                    if (reader.NodeType == XmlNodeType.Element)
                                    {
                                        // To get title of article
                                        if (reader.Name == "parse")
                                        {
                                            while (reader.MoveToNextAttribute())
                                            {
                                                title = reader.Value;
                                            }
                                        }
                                        // To get reroute if it exists.
                                        else if (reader.Name == "r")
                                        {
                                            if(reader["tofragment"] != null)
                                            {
                                                titleFragment = reader["tofragment"];
                                            }
                                        }
                                        // Get article itself
                                        else if (reader.Name == "text")
                                        {
                                            wikiText = reader.ReadInnerXml().ToString();
                                        
                                            // If it's not null
                                            if (wikiText != null)
                                            {
                                                string replaceWith = " ";
                                                // If there is rerouting
                                                if(titleFragment != null)
                                                {
                                                    // Get rerouted article
                                                    wikiText = wikiText.Substring(wikiText.IndexOf("id=\"" + titleFragment + "\""), wikiText.Length - wikiText.IndexOf("id=\"" + titleFragment + "\""));
                                                }
                                                // Clean article of html tags and newlines
                                                //wikiText = HtmlRemoval.RemoveHTMLCommentsRegex(wikiText);
                                                wikiText = wikiText.Substring(wikiText.IndexOf("&lt;p&gt;"), wikiText.Length - wikiText.IndexOf("&lt;p&gt;"));
                                                wikiText = Regex.Replace(HtmlRemoval.StripTagsRegex(wikiText), @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
                                                wikiText = wikiText.Replace("\r\n", replaceWith).Replace("\n", replaceWith).Replace("\r", replaceWith);
                                        
                                                // Get first 50 words and try to get a sentence (Sentence may be longer then 50 words)
                                                wikiText = FirstSentence(FirstWords(wikiText, 50));
                                            }
                                        }
                                    }
                                }
                            
                                // If there is no article and language isn't japanese (to stop testing for more articles)
                                if(wikiText == null && lang != "ja")
                                {
                                    // Switch languages until there is an article
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
                                    
                                    //Continue the loop for each language
                                    continue;
                                }
                        
                                // Close the waiting thread so no more messages of `Still waiting...` will be written
                                waiting.Abort();
                        
                                // Set the output to each value
                                output.SetValue(title, 0);
                                output.SetValue(wikiText, 1);
                                output.SetValue(lang, 2);
                        
                                // If there was a reroute
                                if (titleFragment != null)
                                {
                                    // Set 4th place in array to array fragment
                                    output.SetValue("#" + titleFragment.Replace(" ", "_"), 3);
                                }
                                // If there wasn't a reroute
                                else
                                {
                                    // Set 4th place in array to null
                                    output.SetValue(null, 3);
                                }
                                return output;
                            }
                        }
                    }
                    catch(WebException ex)
                    {
                        Program.WriteChannel(Program.channel, "We couldn't establish a connection");
                        Console.WriteLine(ex.Message);
                        return output;
                    }
                    catch(Exception ex)
                    {
                        Program.WriteChannel(Program.channel, "Something went wrong getting wikipedia article");
                        Console.WriteLine(ex.ToString());
                        return output;
                    }
                }
            }
        }
        /// <summary>
        /// Execute during search. Writes 3 times
        /// </summary>
        /// <param name="sleep">Amount of time to wait before writing</param>
        public static void Sleeper(object sleep)
        {
            for (int i = 0; i < 3; i++)
            {
                Thread.Sleep((int)sleep);
                Program.WriteChannel(Program.channel, "Still searching..");
            }
        }
        /// <summary>
        /// Get the first words
        /// </summary>
        /// <param name="input">String to get first words from</param>
        /// <param name="numberWords">Number of words to get</param>
        /// <returns></returns>
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
            catch(Exception ex)
            {
                Console.WriteLine("Couldn't get first words: " + ex.Message);
            }
            return string.Empty;
        }
        /// <summary>
        /// Get the first sentence of a string
        /// </summary>
        /// <param name="paragraph">Sting to get sentence from</param>
        /// <returns></returns>
        public static string FirstSentence(string paragraph)
        {
            for (int i = 0; i < paragraph.Length; i++)
            {
                switch (paragraph[i])
                {
                    // Special dot for japanese
                    case '。':
                    case '.':
                        if (i < (paragraph.Length - 1) &&
                        char.IsWhiteSpace(paragraph[i + 1]))
                        {
                            goto case '!';
                        }
                        break;
                    // Special exclamation mark for japanese
                    case '！':
                    case '!':
                        return paragraph.Substring(0, i + 1);
                }
            }
            return paragraph;
        }
    }
}
