using Microsoft.Data.Sqlite;
using System;
using System.Data;

namespace HistWeb.Utilties
{
    public sealed class Logger
    {
        private static readonly Logger instance = new Logger();
        private readonly string connectionString;

        private Logger()
        {
            // Assuming ApplicationSettings.DatabasePath is a static property that gives the correct path
            connectionString = $"Data Source={ApplicationSettings.DatabasePath}";
        }

        public static Logger Instance
        {
            get
            {
                return instance;
            }
        }

        public void Log(string logSource, string log, string logType)
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.CommandText = "INSERT INTO logs (Timestamp, LogType, LogSource, Log) VALUES (@Timestamp, @LogType, @LogSource, @Log)";

                    cmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                    cmd.Parameters.AddWithValue("@LogType", logType); 
                    cmd.Parameters.AddWithValue("@LogSource", logSource);
                    cmd.Parameters.AddWithValue("@Log", log);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void UpdateQueueStep(int step)
        {
            using (var conn = new SqliteConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"UPDATE masternodesetupqueue SET queue_step = @step";
                    cmd.Parameters.AddWithValue("@step", step + 1);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }


}
