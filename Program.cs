using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace HoloBot
{
    class Program
    {
        public  static string       nick        = "HoloBot";
        private static string       user        = "USER " + nick + " 8 * :SpyTec's C# irc bot";
        public  const string        server      = "79.160.59.72";
        public  static string       channel     = "#blu";
        private const int           port        = 6667;
        private const string        deliminator = "!"; // To execute commands
        private const int           spamBuffer  = 1000; // in ms
        public  const int           remindTimer = 15; // In minutes

        public static StreamWriter writer;
        public static StreamReader reader;
        static void Main(string[] args)
        {
            string inputLine, nickname;
            NetworkStream stream;
            TcpClient irc;
            try
            {
                string command;
                bool waitToConnect = true;
                irc = new TcpClient(server, port);
                stream = irc.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);
                writer.WriteLine("NICK " + nick);
                writer.Flush();
                writer.WriteLine(user);
                writer.Flush();
                while (waitToConnect)
                {
                    while ((inputLine = reader.ReadLine()) != null)
                    {
                        if (inputLine.Contains("NOTICE Auth :Welcome"))
                        {
                            Console.WriteLine("Connected");
                            waitToConnect = false;
                            break;
                        }
                    }
                }
                writer.WriteLine("JOIN " + channel);
                writer.Flush();
                // Specify zero input parameters with empty parentheses. To explain see: http://msdn.microsoft.com/en-us/library/bb397687.aspx
                Thread remindersCheck = new Thread( () => Remind.remindChecker(remindTimer));
                while (true)
                {
                    while ((inputLine = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(inputLine);
                        if (inputLine.Contains((command = "PRIVMSG " + channel + " :" + deliminator)) )
                        {
                            if (inputLine.EndsWith(command + "commands"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :Current commands: {0}ping, {0}wiki, {0}pocky and {0}remind", deliminator);
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.EndsWith(command + "ping"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :!pong");
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.EndsWith(command + "pong"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :!pang");
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.EndsWith(command + "pang"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :!pung");
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.EndsWith(command + "pung"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :!peng");
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.EndsWith(command + "peng"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :Just give up already! I'm alive alright?!");
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.EndsWith(command + "pocky"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :{0:X}ACTION gives BluABK pocky{0:X}", "\x01");
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.EndsWith(command + "wiki"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :Syntax: !wiki query");
                                writer.WriteLine("PRIVMSG " + channel + " :Searches languages: en, sv, no, de, dk and ja");
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.Contains(command + "wiki"))
                            {
                                string query = inputLine.ToString().Substring(inputLine.IndexOf("wiki") + 5, inputLine.Length - inputLine.ToString().IndexOf("wiki") - 5);
                                //writer.WriteLine("PRIVMSG " + channel + " :Query: " + query);
                                //writer.Flush();
                                string[] result = Wiki.queryWiki(query).Result;
                                if(result[1] != null)
                                {
                                    Console.WriteLine(result[1]);
                                    writer.WriteLine("PRIVMSG " + channel + " :{0} - https://{1}.wikipedia.org/wiki/" + result[0].Replace(" ", "%20") + result[3], result[1], result[2]);
                                    writer.Flush();
                                }
                                else
                                {
                                    Console.WriteLine(result[1]);
                                    writer.WriteLine("PRIVMSG " + channel + " :{0}", result[0]);
                                    writer.Flush();
                                }
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.EndsWith(command + "remind"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :Syntax: !remind <user>/me in <time> to <do>");
                                writer.WriteLine("PRIVMSG " + channel + " :Short-Syntax: !remind in <time> to <do>");
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                            else if (inputLine.Contains(command + "remind"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :Under production! Check back with my owner later");
                                writer.Flush();
                                Thread.Sleep(spamBuffer);
                            }
                        }
                        else if (inputLine.Contains("PRIVMSG " + nick + " :" + deliminator))
                        {
                            nickname = GetUsername(inputLine);
                            writer.WriteLine("PRIVMSG " + nickname + " :Hi, write to me in " + channel + " please");
                            writer.Flush();
                            Thread.Sleep(spamBuffer);
                        }
                        else if (inputLine.StartsWith("PING"))
                        {
                            Thread pong = new Thread(Program.PongSender);
                            try
                            {
                                pong.Start(inputLine);
                            }
                            catch (ThreadStateException ex)
                            {
                                Console.WriteLine("Restarting thread. " + ex.Message);
                                pong.Abort();
                                Thread.Sleep(200);
                                pong.Start(inputLine);
                            }
                        }
                    }

                    remindersCheck.Abort();
                    writer.Close();
                    reader.Close();
                    irc.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Thread.Sleep(5000); //Wait 5s before retrying a connection
                string[] argv = { };
                Main(argv);
            }

            Console.ReadLine();
        }
        public static void PongSender(object inputLine)
        {
            string pongString = "PONG " + inputLine.ToString().Substring(5, inputLine.ToString().Length - 5);
            Console.WriteLine(pongString);
            writer.WriteLine(pongString);
            writer.Flush();
        }
        public static string GetUsername(object inputLine)
        {
            return inputLine.ToString().Substring(1, inputLine.ToString().IndexOf("!") - 1);
        }

    }
}