using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.Data.Sqlite;
using LANChatServer.Models;

namespace LANChatServer
{
    public static class DbService
    {
        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lanchat.db");
        private static readonly string ConnectionString = $"Data Source={DbPath}";

        public static event Action<string, Color>? OnLog;

        private static void LogInfo(string message)
        {
            OnLog?.Invoke(message, Color.FromArgb(35, 165, 90)); // Green
            Console.WriteLine(message);
        }

        private static void LogError(string message)
        {
            OnLog?.Invoke(message, Color.FromArgb(242, 63, 67)); // Red
            Console.WriteLine(message);
        }

        public static void Initialize()
        {
            try
            {
                // Explicitly set SQLitePCL provider for Native AOT compatibility using Windows winsqlite3
                SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_winsqlite3());

                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    string createTableSql = @"
                        CREATE TABLE IF NOT EXISTS messages (
                            id TEXT PRIMARY KEY,
                            type TEXT NOT NULL,
                            sender_id TEXT NOT NULL,
                            sender_username TEXT NOT NULL,
                            sender_machinename TEXT NOT NULL,
                            sender_ip TEXT NOT NULL,
                            sender_port INTEGER NOT NULL,
                            sender_avatarindex INTEGER NOT NULL,
                            content TEXT NOT NULL,
                            file_id TEXT,
                            file_name TEXT,
                            file_size INTEGER,
                            file_port INTEGER,
                            is_private INTEGER NOT NULL,
                            recipient_id TEXT,
                            timestamp TEXT NOT NULL
                        );";
                    using (var command = new SqliteCommand(createTableSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                LogInfo($"[DB] SQLite database initialized successfully at: {DbPath}");
            }
            catch (Exception ex)
            {
                LogError($"[DB ERROR] Failed to initialize SQLite database: {ex.ToString()}");
            }
        }

        public static void SaveMessage(NetworkMessage msg)
        {
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    string insertSql = @"
                        INSERT INTO messages (
                            id, type, sender_id, sender_username, sender_machinename, 
                            sender_ip, sender_port, sender_avatarindex, content, 
                            file_id, file_name, file_size, file_port, is_private, 
                            recipient_id, timestamp
                        ) VALUES (
                            @id, @type, @sender_id, @sender_username, @sender_machinename, 
                            @sender_ip, @sender_port, @sender_avatarindex, @content, 
                            @file_id, @file_name, @file_size, @file_port, @is_private, 
                            @recipient_id, @timestamp
                        );";

                    using (var command = new SqliteCommand(insertSql, connection))
                    {
                        command.Parameters.AddWithValue("@id", msg.Id);
                        command.Parameters.AddWithValue("@type", msg.Type);
                        command.Parameters.AddWithValue("@sender_id", msg.SenderId);
                        command.Parameters.AddWithValue("@sender_username", msg.SenderUsername ?? string.Empty);
                        command.Parameters.AddWithValue("@sender_machinename", msg.SenderMachineName ?? string.Empty);
                        command.Parameters.AddWithValue("@sender_ip", msg.SenderIp ?? string.Empty);
                        command.Parameters.AddWithValue("@sender_port", msg.SenderPort);
                        command.Parameters.AddWithValue("@sender_avatarindex", msg.SenderAvatarIndex);
                        command.Parameters.AddWithValue("@content", msg.Content ?? string.Empty);
                        command.Parameters.AddWithValue("@file_id", (object?)msg.FileId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@file_name", (object?)msg.FileName ?? DBNull.Value);
                        command.Parameters.AddWithValue("@file_size", msg.FileSize);
                        command.Parameters.AddWithValue("@file_port", msg.FilePort);
                        command.Parameters.AddWithValue("@is_private", msg.IsPrivate ? 1 : 0);
                        command.Parameters.AddWithValue("@recipient_id", (object?)msg.RecipientId ?? DBNull.Value);
                        command.Parameters.AddWithValue("@timestamp", msg.Timestamp.ToString("o"));

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"[DB ERROR] Failed to save message {msg.Id} to database: {ex.Message}");
            }
        }

        public static List<NetworkMessage> GetGroupHistory(int limit = 100)
        {
            var history = new List<NetworkMessage>();
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    // Use subquery to get latest N then return in chronological order
                    string querySql = @"
                        SELECT * FROM (
                            SELECT * FROM messages 
                            WHERE is_private = 0 
                            ORDER BY timestamp DESC 
                            LIMIT @limit
                        ) ORDER BY timestamp ASC;";

                    using (var command = new SqliteCommand(querySql, connection))
                    {
                        command.Parameters.AddWithValue("@limit", limit);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                history.Add(ReadNetworkMessage(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"[DB ERROR] Failed to fetch group history: {ex.Message}");
            }

            return history;
        }

        public static List<NetworkMessage> GetPrivateHistory(string clientId)
        {
            var history = new List<NetworkMessage>();
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    string querySql = @"
                        SELECT * FROM messages 
                        WHERE is_private = 1 AND (sender_id = @clientId OR recipient_id = @clientId) 
                        ORDER BY timestamp ASC;";

                    using (var command = new SqliteCommand(querySql, connection))
                    {
                        command.Parameters.AddWithValue("@clientId", clientId);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                history.Add(ReadNetworkMessage(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"[DB ERROR] Failed to fetch private history for {clientId}: {ex.Message}");
            }

            return history;
        }

        private static NetworkMessage ReadNetworkMessage(SqliteDataReader reader)
        {
            return new NetworkMessage
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                Type = reader.GetString(reader.GetOrdinal("type")),
                SenderId = reader.GetString(reader.GetOrdinal("sender_id")),
                SenderUsername = reader.GetString(reader.GetOrdinal("sender_username")),
                SenderMachineName = reader.GetString(reader.GetOrdinal("sender_machinename")),
                SenderIp = reader.GetString(reader.GetOrdinal("sender_ip")),
                SenderPort = reader.GetInt32(reader.GetOrdinal("sender_port")),
                SenderAvatarIndex = reader.GetInt32(reader.GetOrdinal("sender_avatarindex")),
                Content = reader.GetString(reader.GetOrdinal("content")),
                FileId = reader.IsDBNull(reader.GetOrdinal("file_id")) ? string.Empty : reader.GetString(reader.GetOrdinal("file_id")),
                FileName = reader.IsDBNull(reader.GetOrdinal("file_name")) ? string.Empty : reader.GetString(reader.GetOrdinal("file_name")),
                FileSize = reader.GetInt64(reader.GetOrdinal("file_size")),
                FilePort = reader.GetInt32(reader.GetOrdinal("file_port")),
                IsPrivate = reader.GetInt32(reader.GetOrdinal("is_private")) == 1,
                RecipientId = reader.IsDBNull(reader.GetOrdinal("recipient_id")) ? string.Empty : reader.GetString(reader.GetOrdinal("recipient_id")),
                Timestamp = DateTime.Parse(reader.GetString(reader.GetOrdinal("timestamp")))
            };
        }

        public static void ResetData()
        {
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = new SqliteCommand("DELETE FROM messages;", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new SqliteCommand("VACUUM;", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Không thể xóa dữ liệu: {ex.Message}", ex);
            }
        }
    }
}
