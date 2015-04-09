using System;
using System.Collections;
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
        public      static string   nick            = "HoloBot";
        private     static string   user            = "USER " + nick + " 8 * :SpyTec's C# irc bot";
        public      const string    server          = "79.160.59.72";
        public      static string   channel         = "#blu";   // TODO: Switch from string to array to allow multiple channels
        private     const int       port            = 6667;
        private     const string    deliminator     = "!";      // To execute commands
        private     const int       spamBuffer      = 1000;     // in ms
        public      const int       remindTimer     = 2;        // In minutes
        public      const int       remidnd         = 20;       // Testing
    
        // Global variables to allow easier handling
        public static StreamWriter writer;
        public static StreamReader reader;
        public static TcpClient irc;

        static void Main(string[] args)
        {
            ircManagement();
        }
        private static void ircManagement()
        {
            try
            {
                // Connect to IRC server
                ConnectServer();

                // Join the server(s)
                JoinChannel(channel);

                // Check if database exists and create it if not
                Remind.CheckDatabase();

                // Specify zero input parameters with empty parentheses. To explain see: http://msdn.microsoft.com/en-us/library/bb397687.aspx
                Thread remindersCheck = new Thread(() => Remind.RemindChecker());
                remindersCheck.Start();
                while (true)
                {
                    // Check for new commands
                    commandWatcher(reader);

                    // commandWatcher failed, connection lost; Close everything
                    remindersCheck.Abort();
                    writer.Close();
                    reader.Close();
                    irc.Close();
                    throw new Exception("Connection lost");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Thread.Sleep(5000); //Wait 5s before retrying a connection

                // Restart IRC bot
                ircManagement();
            }
        }
        private static void JoinChannel(string channel)
        {
            writer.WriteLine("JOIN " + channel);
            writer.Flush();
        }
        private static void ConnectServer()
        {
            NetworkStream stream;
            string inputLine;

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
                    // If connected
                    if (inputLine.Contains("NOTICE Auth :Welcome"))
                    {
                        Console.WriteLine("Connected");
                        waitToConnect = false;
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Check for commands and execute
        /// </summary>
        /// <param name="reader">The reader of IRC</param>
        private static void commandWatcher(StreamReader reader)
        {
            string inputLine, command, nickname;
            while ((inputLine = reader.ReadLine()) != null)
            {
                Console.WriteLine(inputLine);
                if (inputLine.Contains((command = "PRIVMSG " + channel + " :" + deliminator)))
                {
                    if (inputLine.EndsWith(command + "commands"))
                    {
                        WriteChannel(channel, String.Format("Current commands: {0}ping, {0}wiki, {0}pocky and {0}remind", deliminator));
                    }
                    else if (inputLine.EndsWith(command + "ping"))
                    {
                        WriteChannel(channel, String.Format("{0}pong", deliminator));
                    }
                    else if (inputLine.EndsWith(command + "pong"))
                    {
                        WriteChannel(channel, String.Format("{0}pang", deliminator));
                    }
                    else if (inputLine.EndsWith(command + "pang"))
                    {
                        WriteChannel(channel, String.Format("{0}pung", deliminator));
                    }
                    else if (inputLine.EndsWith(command + "pung"))
                    {
                        WriteChannel(channel, String.Format("{0}peng", deliminator));
                    }
                    else if (inputLine.EndsWith(command + "peng"))
                    {
                        WriteChannel(channel, "Just give up already! I'm alive alright?!");
                    }
                    else if (inputLine.EndsWith(command + "pocky"))
                    {
                        WriteChannel(channel, String.Format("{0:X}ACTION gives BluABK pocky{0:X}", "\x01"));
                    }
                    else if (inputLine.EndsWith(command + "wiki"))
                    {
                        writer.WriteLine("PRIVMSG " + channel + " :Syntax: {0}wiki query", deliminator);
                        writer.WriteLine("PRIVMSG " + channel + " :Searches languages: en, sv, no, de, dk and ja - in this order");
                        writer.Flush();
                        Thread.Sleep(spamBuffer);
                    }
                    else if (inputLine.Contains(command + "wiki"))
                    {
                        string query = inputLine.ToString().Substring(inputLine.IndexOf("wiki") + 5, inputLine.Length - inputLine.ToString().IndexOf("wiki") - 5);
                        string[] result = Wiki.queryWiki(query).Result;
                        if (result[1] != null)
                        {
                            WriteLineConsole(result[1]);
                            WriteChannel(channel, String.Format("{0} - https://{1}.wikipedia.org/wiki/" + result[0].Replace(" ", "%20") + result[3], result[1], result[2]));
                        }
                        else
                        {
                            WriteLineConsole(result[1]);
                            WriteChannel(channel, String.Format("{0}", result[0]));
                        }
                    }
                    else if (inputLine.EndsWith(command + "remind"))
                    {
                        writer.WriteLine("PRIVMSG " + channel + " :Syntax: {0}remind <user>/me in <time> to <do>", deliminator);
                        writer.WriteLine("PRIVMSG " + channel + " :Short-Syntax: {0}remind in <time> to <do>", deliminator);
                        writer.Flush();
                        Thread.Sleep(spamBuffer);
                    }
                    else if (inputLine.Contains(command + "remind"))
                    {
                        Remind.SetReminder(
                            Remind.GetUsername(inputLine),
                            GetUsername(inputLine),
                            Remind.GetRemindTime(inputLine),
                            Remind.GetMessage(inputLine)
                        );
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
                    PingHandler(inputLine);
                }
            }
        }
        private static void PingHandler(string inputLine)
        {
            Thread pong = new Thread(Program.PongSender);
            try
            {
                pong.Start(inputLine);
            }
            catch (ThreadStateException ex)
            {
                WriteLineConsole("Restarting thread. " + ex.Message);
                pong.Abort();
                Thread.Sleep(200);
                pong.Start(inputLine);
            }
        }
        /// <summary>
        /// Send back PONG in response to a PING
        /// </summary>
        /// <param name="inputLine"></param>
        public static void PongSender(object inputLine)
        {
            string pongString = "PONG " + inputLine.ToString().Substring(5, inputLine.ToString().Length - 5);
            WriteLineConsole(pongString);
            writer.WriteLine(pongString);
            writer.Flush();
        }
        /// <summary>
        /// Get username from IRC message
        /// </summary>
        /// <param name="inputLine">String that contains a username</param>
        /// <returns></returns>
        public static string GetUsername(object inputLine)
        {
            return inputLine.ToString().Substring(1, inputLine.ToString().IndexOf("!") - 1);
        }
        /// <summary>
        /// Write to a user
        /// </summary>
        /// <param name="userName">User to write to</param>
        /// <param name="message">Message to send</param>
        public static void WriteUser(string userName, string message)
        {
            writer.WriteLine("PRIVMSG " + userName + " :{0}", message);
            writer.Flush();
        }
        // TODO: Accept arrays
        /// <summary>
        /// Write to channel
        /// </summary>
        /// <param name="channel">Channel to write to</param>
        /// <param name="message">Message to send</param>
        public static void WriteChannel(string channel, string message)
        {
            writer.WriteLine("PRIVMSG " + channel + " :{0}", message);
            writer.Flush();
            Thread.Sleep(spamBuffer);
        }
        public static void WriteLineConsole(string input)
        {
            Console.WriteLine(input);
        }
    }
}