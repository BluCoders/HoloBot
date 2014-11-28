using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Threading;

namespace HoloBot
{
    class Remind
    {
        public static string sqlite_db = Program.nick;
        public static string sqlite_table = "remind";
        public static int sqlite_version = 3;
        private static bool warmed_up = false;
        public static bool SetReminder(string toUser, string fromUser, string time, string reminder = null)
        {
            using (SQLiteConnection con = new SQLiteConnection(String.Format("Data Source={0}.sqlite;Version={1};", sqlite_db, sqlite_version)))
            using (SQLiteCommand command = con.CreateCommand())
            {
                con.Open();

                // Check if application has warmed up. I.e. being the first time going through application command. If true it skips doing query.
                if(!warmed_up)
                {
                    CheckTable(command);
                    warmed_up = true;
                }

                try
                {
                    FormatTime(time);
                }
                catch(ArgumentNullException)
                {
                    Program.writer.WriteLine("PRIVMSG " + Program.channel + " :Time is empty!");
                    return false;
                }
                catch(FormatException)
                {
                    Program.writer.WriteLine("PRIVMSG " + Program.channel + " :Time format invalid");
                    return false;
                }

                if(toUser == fromUser)
                {
                    if (reminder != null)
                    {
                        command.CommandText = "INSERT INTO @remind_table (toUser, time, remindText) VALUES (@toUser, @time, 'You asked me to remind you')";
                        command.Parameters.AddWithValue("@remind_table", sqlite_table);
                        command.Parameters.AddWithValue("@toUser", toUser);
                        command.Parameters.AddWithValue("@time", time);
                    }
                    else
                    {
                        command.CommandText = "INSERT INTO @remind_table (toUser, time, remindText) VALUES (@toUser, @time, @reminder)";
                        command.Parameters.AddWithValue("@remind_table", sqlite_table);
                        command.Parameters.AddWithValue("@toUser", toUser);
                        command.Parameters.AddWithValue("@time", time);
                        command.Parameters.AddWithValue("@reminder", reminder);
                    }
                }
                else
                {
                    if (reminder != null)
                    {
                        command.CommandText = "INSERT INTO @remind_table (toUser, time, remindText) VALUES (@toUser, @time, @reminder)";
                        command.Parameters.AddWithValue("@remind_table", sqlite_table);
                        command.Parameters.AddWithValue("@toUser", toUser);
                        command.Parameters.AddWithValue("@time", time);
                        command.Parameters.AddWithValue("@reminder", fromUser + " asked me to remind you");
                    }
                    else
                    {
                        command.CommandText = "INSERT INTO @remind_table (toUser, time, remindText) VALUES (@toUser, @time, @reminder)";
                        command.Parameters.AddWithValue("@remind_table", sqlite_table);
                        command.Parameters.AddWithValue("@toUser", toUser);
                        command.Parameters.AddWithValue("@time", time);
                        command.Parameters.AddWithValue("@reminder", fromUser + " reminded you: " + reminder);
                    }
                }
                command.ExecuteScalar();
                con.Close();
            }
            return true;
        }
        public static void CheckTable(SQLiteCommand command)
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='@remind_table'";
            command.Parameters.AddWithValue("@remind_table", sqlite_table);
            var name = command.ExecuteScalar();
            if (name != null && name.ToString() == sqlite_table)
            {
                // Table exists: Return function
                return;
            }
            command.CommandText = "CREATE TABLE @remind_table (id INT, toUser VARCHAR(42), time DateTime, reminder VARCHAR(255))";
            command.Parameters.AddWithValue("@remind_table", sqlite_table);
            command.ExecuteNonQuery();
        }
        public static void FormatTime(string timeToParse)
        {
            string format = "yyyy-MM-dd HH:mm:ss";
            DateTime FormattedTime = DateTime.ParseExact(timeToParse, format, CultureInfo.InvariantCulture);
            Console.WriteLine(FormattedTime);
        }
        public static object GetReminder(SQLiteCommand command, int remindId)
        {
            command.CommandText = "SELECT * FROM @table WHERE id='@id'";
            command.Parameters.AddWithValue("@table", sqlite_table);
            command.Parameters.AddWithValue("@id", remindId);
            return command.ExecuteScalar();
        }
        public static object ListReminders(SQLiteCommand command)
        {
            command.CommandText = "SELECT id, time FROM @table WHERE time > date(now - 10min)";
            command.Parameters.AddWithValue("@table", sqlite_table);
            return command.ExecuteScalar();
        }
        public static bool RemoveReminder(SQLiteCommand command, int remindId)
        {
            command.CommandText = "DELETE FROM @table WHERE id=@id";
            command.Parameters.AddWithValue("@table", sqlite_table);
            command.Parameters.AddWithValue("@id", remindId);
            var results = command.ExecuteScalar();
            if (results != null)
            {
                return true;
            }
            return false;
        }
        public static void remindChecker()
        {
            using (SQLiteConnection con = new SQLiteConnection(String.Format("Data Source={0}.sqlite;Version={1};", sqlite_db, sqlite_version)))
            while(true)
            {
                using (SQLiteCommand command = con.CreateCommand())
                {
                    Thread.Sleep(Program.remindTimer * 60 * 60);
                    con.Open();
                    var list = ListReminders(command);
                    //foreach(var row in list)
                    //{

                    //}
                }
            }
        }
    }
}
