using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LANChatPro.Models;
using LANChatPro.Storage;
using LANChatPro.Utils;

namespace LANChatPro.Services
{
    public class ChatHistoryService
    {
        private static readonly string HistoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LANChatPro",
            "history.json"
        );

        private Dictionary<string, List<ChatMessage>> _history = new();
        private readonly object _lock = new();

        public ChatHistoryService()
        {
            LoadHistory();
        }

        private void LoadHistory()
        {
            lock (_lock)
            {
                var loaded = JsonStorage.Load(HistoryPath, JsonContext.Default.DictionaryStringListChatMessage);
                if (loaded != null)
                {
                    _history = loaded;
                }
                else
                {
                    _history = new Dictionary<string, List<ChatMessage>>();
                }
            }
        }

        public List<ChatMessage> GetGroupHistory()
        {
            lock (_lock)
            {
                if (_history.TryGetValue("group", out var list))
                {
                    return new List<ChatMessage>(list);
                }
                return new List<ChatMessage>();
            }
        }

        public List<ChatMessage> GetPrivateHistory(string peerId)
        {
            lock (_lock)
            {
                string key = $"private_{peerId}";
                if (_history.TryGetValue(key, out var list))
                {
                    return new List<ChatMessage>(list);
                }
                return new List<ChatMessage>();
            }
        }

        public void AddGroupMessage(ChatMessage msg)
        {
            lock (_lock)
            {
                if (!_history.TryGetValue("group", out var list))
                {
                    list = new List<ChatMessage>();
                    _history["group"] = list;
                }

if (list.Count >= 500)
                {
                    list.RemoveAt(0);
                }

                list.Add(msg);
            }
            SaveHistoryAsync();
        }

        public void AddPrivateMessage(string peerId, ChatMessage msg)
        {
            lock (_lock)
            {
                string key = $"private_{peerId}";
                if (!_history.TryGetValue(key, out var list))
                {
                    list = new List<ChatMessage>();
                    _history[key] = list;
                }

                if (list.Count >= 500)
                {
                    list.RemoveAt(0);
                }

                list.Add(msg);
            }
            SaveHistoryAsync();
        }

        private void SaveHistoryAsync()
        {
            Task.Run(() =>
            {
                lock (_lock)
                {
                    JsonStorage.Save(HistoryPath, _history, JsonContext.Default.DictionaryStringListChatMessage);
                }
            });
        }
    }
}
