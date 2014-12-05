﻿using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Threading;
using System.Diagnostics;
using System.Timers;

namespace HoloBot
{
	/// <summary>
	/// To set a reminder for a user and remind at a specific time.
	/// </summary>
	class Remind
	{
		public	static	string	sqlite_db	= Program.nick;
		public	static	string	sqlite_table	= "remind";
		public	static	int	sqlite_version	= 3;
		private	static	bool	warmed_up	= false;

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
				DateTime remindTime;

				con.Open();

				// Check if application has warmed up. I.e. being the first time going through application command. If true it skips doing query.
				if(!warmed_up)
				{
					CheckTable(command);
					warmed_up = true;
				}

				// Try to format time
				try
				{
					remindTime = FormatTime(time);
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

				try
				{
					// If reminding yourself
					if(toUser == fromUser)
					{
						// If what to remind isn't null
						if (reminder != null)
						{
							command.CommandText = "INSERT INTO @remind_table (toUser, time, remindText) VALUES (@toUser, @time, 'You asked me to remind you')";
							command.Parameters.AddWithValue("@remind_table", sqlite_table);
							command.Parameters.AddWithValue("@toUser", toUser);
							command.Parameters.AddWithValue("@time", remindTime);
						}
						else
						{
							command.CommandText = "INSERT INTO @remind_table (toUser, time, remindText) VALUES (@toUser, @time, @reminder)";
							command.Parameters.AddWithValue("@remind_table", sqlite_table);
							command.Parameters.AddWithValue("@toUser", toUser);
							command.Parameters.AddWithValue("@time", remindTime);
							command.Parameters.AddWithValue("@reminder", reminder);
						}
					}
					// If someone else remind you
					else
					{
						if (reminder != null)
						{
							command.CommandText = "INSERT INTO @remind_table (toUser, time, remindText) VALUES (@toUser, @time, @reminder)";
							command.Parameters.AddWithValue("@remind_table", sqlite_table);
							command.Parameters.AddWithValue("@toUser", toUser);
							command.Parameters.AddWithValue("@time", remindTime);
							command.Parameters.AddWithValue("@reminder", fromUser + " asked me to remind you");
						}
						else
						{
							command.CommandText = "INSERT INTO @remind_table (toUser, time, remindText) VALUES (@toUser, @time, @reminder)";
							command.Parameters.AddWithValue("@remind_table", sqlite_table);
							command.Parameters.AddWithValue("@toUser", toUser);
							command.Parameters.AddWithValue("@time", remindTime);
							command.Parameters.AddWithValue("@reminder", fromUser + " reminded you: " + reminder);
						}
					}
					// Execute command
					command.ExecuteScalar();
				}
				finally
				{
					// Close connection
					con.Close();
				}
			}
			return true;
		}
		/// <summary>
		/// Check if table exists. If not, create it
		/// </summary>
		/// <param name="command">The SQLiteCommand in which method is located in</param>
		private static void CheckTable(SQLiteCommand command)
		{
			command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='@remind_table'";
			command.Parameters.AddWithValue("@remind_table", sqlite_table);
			var name = command.ExecuteScalar();

			// If name isn't null and it equals tablename
			if (name != null && name.ToString() == sqlite_table)
			{
				// Table exists: Return method
				return;
			}
			// Otherwise create table
			command.CommandText = "CREATE TABLE @remind_table (id INT, toUser VARCHAR(42), time DateTime, reminder VARCHAR(255))";
			command.Parameters.AddWithValue("@remind_table", sqlite_table);
			command.ExecuteNonQuery();
		}
		/// <summary>
		/// Format time to yyyy-MM-dd HH:mm:ss
		/// </summary>
		/// <param name="timeToParse">The string of time</param>
		private static DateTime FormatTime(string timeToParse)
		{
			string format = "yyyy-MM-dd HH:mm:ss";
			return DateTime.ParseExact(timeToParse, format, CultureInfo.InvariantCulture);
		}
		/// <summary>
		/// Get full reminder
		/// </summary>
		/// <param name="command">The SQLiteCommand in which method is located in</param>
		/// <param name="remindId">The row ID get from</param>
		/// <returns>SQLiteDataReader as ExecuteReader()</returns>
		private static SQLiteDataReader GetReminder(SQLiteCommand command, int remindId)
		{
			command.CommandText = "SELECT * FROM @table WHERE id='@id'";
			command.Parameters.AddWithValue("@table", sqlite_table);
			command.Parameters.AddWithValue("@id", remindId);
			return command.ExecuteReader();
		}
		/// <summary>
		/// Get a query of reminders
		/// </summary>
		/// <param name="command">The SQLiteCommand in which method is located in</param>
		/// <param name="minutes">Timespan to check for. In minutes</param>
		/// <returns>SQLiteDataReader as ExecuteReader()</returns>
		private static SQLiteDataReader ListReminders(SQLiteCommand command, int minutes)
		{
			DateTime time = DateTime.Now.Subtract(TimeSpan.FromMinutes(minutes));
			command.CommandText = "SELECT id, time FROM @table WHERE time > date(@time) ORDER BY date(time) DESC";
			command.Parameters.AddWithValue("@table", sqlite_table);
			command.Parameters.AddWithValue("@time", time);
			return command.ExecuteReader();
		}
		/// <summary>
		/// Remove a set reminder (recommended after reminding the person)
		/// </summary>
		/// <param name="command">The SQLiteCommand in which method is located in</param>
		/// <param name="remindId">The row ID to be deleted</param>
		/// <returns>Bool: If it has been removed or not</returns>
		private static bool RemoveReminder(SQLiteCommand command, int remindId)
		{
			command.CommandText = "DELETE FROM @table WHERE id=@id";
			command.Parameters.AddWithValue("@table", sqlite_table);
			command.Parameters.AddWithValue("@id", remindId);
			var results = command.ExecuteScalar();
			// If succeeded to delete
			if (results != null)
			{
				return true;
			}
			return false;
		}
		/// <summary>
		/// Check for reminders ready to be set into countdown
		/// </summary>
		/// <param name="minutes">The window to check for due reminders. Recommended 10min</param>
		public static void remindChecker(int minutes = Program.remindTimer)
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
						using (list)
						{
							while(list.Read())
							{
							DateTime rowTime = list.GetDateTime(2);
							while (true)
							{
								if (rowTime > DateTime.Now)
								{
									
								}
								else{
									// Sleep for the set amount of minutes to be able to check every set minutes for new due reminders. (do it last to check when bot first starts up)
								}
							}
							}
						}
					}
				}
				finally
				{
					con.Close();
					Thread.Sleep(minutes * 60 * 60);	
				}
			}
		}
	}
}