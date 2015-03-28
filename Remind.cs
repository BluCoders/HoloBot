using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace HoloBot
{
    /// <summary>
    /// To set a reminder for a user and remind at a specific time.
    /// </summary>
    class Remind
    {
        public      static  string  sqlite_db       = Program.nick;
        public      static  string  sqlite_table    = "remind";
        public      static  int     sqlite_version  = 3;
        private     static  bool    warmed_up       = false;


        /// <summary>
        /// Set a reminder for a user
        /// </summary>
        /// <param name="toUser">User whom the reminder is set to</param>
        /// <param name="fromUser">User behind setting the reminder</param>
        /// <param name="time">Which time to remind user</param>
        /// <param name="reminder">What the reminder is. Can be empty</param>
        /// <returns></returns>
        public static bool SetReminder(string toUser, string fromUser, string time, string reminder = null)
        {
            using (SQLiteConnection con = new SQLiteConnection(String.Format("Data Source={0}.sqlite;Version={1};", sqlite_db, sqlite_version)))
            using (SQLiteCommand command = con.CreateCommand())
            {
                DateTime remindTime = DateTime.UtcNow;

                con.Open();

                // Check if application has warmed up. I.e. being the first time going through application command. If true it skips doing query.
                if(!warmed_up)
                {
                    CheckTable(command);
                    warmed_up = true;
                }

                // Try to format time
                Console.WriteLine("Formating time");
                try
                {
                    remindTime = FormatTime(time);
                }
                catch(ArgumentNullException)
                {
                    Program.WriteChannel(Program.channel, "Time is empty!");
                    return false;
                }
                catch(FormatException)
                {
                    Program.WriteChannel(Program.channel, "Time format invalid");
                    return false;
                }
                catch(Exception ex)
                {
                    Program.WriteChannel(Program.channel, ex.Message);
                    return false;
                }

                Console.WriteLine("Set Reminder");
                try
                {
                    // If reminding yourself
                    if(toUser == fromUser)
                    {
                        Console.WriteLine("Reminding yourself");
                        // If what to remind exists
                        if (reminder == null)
                        {
                            command.CommandText = "INSERT INTO " + sqlite_table + " (toUser, time, reminder) VALUES (@toUser, @time, 'You asked me to remind you')";
                            command.Parameters.AddWithValue("@toUser", toUser);
                            command.Parameters.AddWithValue("@time", remindTime);
                        }
                        else
                        {
                            command.CommandText = "INSERT INTO " + sqlite_table + " (toUser, time, reminder) VALUES (@toUser, @time, @reminder)";
                            command.Parameters.AddWithValue("@toUser", toUser);
                            command.Parameters.AddWithValue("@time", remindTime);
                            command.Parameters.AddWithValue("@reminder", "Reminder: " + reminder);
                        }
                    }
                    // If someone else reminds you
                    else
                    {
                        Console.WriteLine("Reminding others");
                        if (reminder == null)
                        {
                            command.CommandText = "INSERT INTO " + sqlite_table + " (toUser, time, reminder) VALUES (@toUser, @time, @reminder)";
                            command.Parameters.AddWithValue("@toUser", toUser);
                            command.Parameters.AddWithValue("@time", remindTime);
                            command.Parameters.AddWithValue("@reminder", fromUser + " asked me to remind you");
                        }
                        else
                        {
                            command.CommandText = "INSERT INTO " + sqlite_table + " (toUser, time, reminder) VALUES (@toUser, @time, @reminder)";
                            command.Parameters.AddWithValue("@toUser", toUser);
                            command.Parameters.AddWithValue("@time", remindTime);
                            command.Parameters.AddWithValue("@reminder", fromUser + " reminded you: " + reminder);
                        }
                    }
                    Console.WriteLine("Executing command");
                    // Execute command
                    command.ExecuteScalar();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    Program.WriteChannel(Program.channel, ex.Message);
                    return false;
                }
                // Close connection
                con.Close();
                Program.WriteChannel(Program.channel, "Reminder set!");
                Console.WriteLine("Closed connection");
            }
            return true;
        }
        /// <summary>
        /// Check if table exists. If not, create it
        /// </summary>
        private static void CheckTable(SQLiteCommand query)
        {
            // Command only for SQLite >V3.3
            query.CommandText = "CREATE TABLE IF NOT EXISTS " + sqlite_table + " (`id` INTEGER PRIMARY KEY AUTOINCREMENT UNIQUE, `toUser` VARCHAR(42) NOT NULL, `time` DateTime NOT NULL, `reminder` VARCHAR(255))";
            query.ExecuteNonQuery();
        }
        /// <summary>
        /// Format time to yyyy-MM-dd HH:mm:ss
        /// </summary>
        /// <param name="timeToParse">String of time</param>
        /// <returns>timeToParse formatted into a DateTime type</returns>
        private static DateTime FormatTime(string timeToParse)
        {
            string format = "yyyy-MM-dd HH:mm:ss";
            //const DateTimeStyles style = DateTimeStyles.AllowWhiteSpaces;
            DateTime dt = DateTime.ParseExact(timeToParse, format, null);
            Console.WriteLine(dt.ToString());
            Console.WriteLine(DateTime.UtcNow);
            //DateTime.TryParseExact(timeToParse, format, CultureInfo.InvariantCulture, style, out dt);
            
            if (dt < DateTime.UtcNow)
                throw new Exception("Trying to remind yourself in the past! Go by UTC");
            else if (string.IsNullOrEmpty(dt.ToString()) == false)
                return dt;
            // FormatException if time is empty
            throw new FormatException();
        
        }
        /// <summary>
        /// Get full reminder
        /// </summary>
        /// <param name="remindId">The row ID get from</param>
        /// <returns>SQLiteDataReader as ExecuteReader()</returns>
        private static SQLiteDataReader GetReminder(SQLiteCommand query, int remindId)
        {
            query.CommandText = "SELECT * FROM " + sqlite_table + " WHERE id='@id'";
            query.Parameters.AddWithValue("@id", remindId);
            return query.ExecuteReader();
        }
        /// <summary>
        /// Get a query of reminders
        /// </summary>
        /// <param name="minutes">Timespan to check for. In minutes</param>
        /// <returns>SQLiteDataReader as ExecuteReader()</returns>
        private static SQLiteDataReader ListReminders(SQLiteCommand query, int minutes)
        {
            DateTime time = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(minutes)).ToUniversalTime();
            query.CommandText = "SELECT * FROM " + sqlite_table + " WHERE time > date(@time) ORDER BY date(time) DESC";
            query.Parameters.AddWithValue("@time", time);
            return query.ExecuteReader();
        }
        /// <summary>
        /// Remove a set reminder (recommended after reminding the person)
        /// </summary>
        /// <param name="remindId">The row ID to be deleted</param>
        /// <returns>Bool: If it has been removed or not</returns>
        private static bool RemoveReminder(SQLiteConnection con, int remindId)
        {
            using (SQLiteCommand query = con.CreateCommand())
            {
                query.CommandText = "DELETE FROM " + sqlite_table + " WHERE id=@id";
                query.Parameters.AddWithValue("@id", remindId);
                var results = query.ExecuteNonQuery();
            
                // If succeeded to delete, i.e. more than one row affected
                if (results > 0)
                    return true;
                return false;
            }
        }
        /// <summary>
        /// Check for reminders ready to be set into countdown
        /// </summary>
        /// <param name="minutes">The window to check for due reminders. Recommended 10min</param>
        public static void RemindChecker(int minutes = Program.remindTimer)
        {
            // Start a connection
            using (SQLiteConnection con = new SQLiteConnection(String.Format("Data Source={0}.sqlite;Version={1};", sqlite_db, sqlite_version)))
            // Loop to check for due reminders
            while(true)
            {
                try
                {
                    // Issue new command
                    using (SQLiteCommand command = con.CreateCommand())
                    {
                        // Open connection and get list of reminders as `SQLiteDataReader` type
                        con.Open();
                        var list = ListReminders(command, minutes);
                        if(!list.HasRows)
                        {
                            throw new Exception("No rows");
                        }
                        using (list)
                        {
                            while(list.Read())
                            {
                                // Get the time from row
                                DateTime rowTime = list.GetDateTime(2);
                            
                                // Loop until reminder needs to be executed
                                while (true)
                                {
                                    if (rowTime < DateTime.UtcNow)
                                    {
                                        Program.WriteUser(list.GetString(1), list.GetString(3));
                                        if(RemoveReminder(con, list.GetInt16(0)) == false)
                                        {
                                            Program.WriteUser(list.GetString(1), "Couldn't delete reminder, contact admin as you'll get repeated messages!");
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        // Sleep 15s
                                        Thread.Sleep(15 * 1000);
                                    }
                                }
                            }
                        }
                    }
                }
                catch(Exception)
                {
                }
                finally
                {
                    con.Close();
                    // Sleep for the set amount of minutes to be able to check every set minutes for new due reminders. (do it last to check when bot first starts up)
                    Thread.Sleep(minutes * 60 * 1000);
                }
            }
        }
        /// <summary>
        /// Get user to remind
        /// </summary>
        /// <param name="inputLine">String that contains a username</param>
        /// <returns>Username</returns>
        public static string GetUsername(object inputLine)
        {
            if(inputLine.ToString().Contains("remind in"))
            {
                return Program.GetUsername(inputLine);
            }
            else
            {
                string username = inputLine.ToString().Substring(inputLine.ToString().IndexOf("remind") + 7, inputLine.ToString().IndexOf(" in ") - (inputLine.ToString().IndexOf("remind") + 7));
                if(username == "me")
                {
                    return Program.GetUsername(inputLine);
                }
                else
                {
                    return username;
                }
            }
        }
        /// <summary>
        /// Get time in which to remind someone or yourself
        /// </summary>
        /// <param name="inputLine">String that contains the time</param>
        /// <returns>The time</returns>
        public static string GetRemindTime(object inputLine)
        {
            string input = inputLine.ToString();
            if(input.Contains(" to "))
            {
                return input.Substring(input.IndexOf(" in ") + 4, (input.IndexOf(" to ") - 4 ) - input.IndexOf(" in "));
            }
            return input.Substring(input.IndexOf(" in ") + 4, (input.Length - (input.IndexOf(" in ") + 4) ));
        }
        /// <summary>
        /// Get reminder message 
        /// </summary>
        /// <param name="inputLine">String that contains a reminder message</param>
        /// <returns>The message</returns>
        public static string GetMessage(object inputLine)
        {
            string input = inputLine.ToString();
            if(input.Contains(" to "))
            {
                return input.Substring(input.IndexOf(" to ") + 4, (input.Length - (input.IndexOf(" to ") + 4)));
            }
            return null;
        }
        /// <summary>
        /// Check if database file exists and create if not
        /// </summary>
        public static void CheckDatabase()
        {
            string databaseFile = String.Format("{0}.sqlite", sqlite_db);
            if(!File.Exists(databaseFile))
                SQLiteConnection.CreateFile(databaseFile);
        }
    }
}
