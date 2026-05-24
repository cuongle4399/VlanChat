using System;
using System.Collections.Generic;
using LANChatPro.Models;

namespace LANChatPro.Services
{
    public class ChatHistoryService
    {
        private Dictionary<string, List<ChatMessage>> _history = new();
        private readonly object _lock = new();

        public ChatHistoryService()
        {
            // Strictly in-memory, loaded dynamically from Server history sync
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

        public bool AddGroupMessage(ChatMessage msg)
        {
            lock (_lock)
            {
                if (!_history.TryGetValue("group", out var list))
                {
                    list = new List<ChatMessage>();
                    _history["group"] = list;
                }

                // Check duplicate
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Id == msg.Id)
                    {
                        return false;
                    }
                }

                if (list.Count >= 500)
                {
                    list.RemoveAt(0);
                }

                list.Add(msg);
            }
            return true;
        }

        public bool AddPrivateMessage(string peerId, ChatMessage msg)
        {
            lock (_lock)
            {
                string key = $"private_{peerId}";
                if (!_history.TryGetValue(key, out var list))
                {
                    list = new List<ChatMessage>();
                    _history[key] = list;
                }

                // Check duplicate
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Id == msg.Id)
                    {
                        return false;
                    }
                }

                if (list.Count >= 500)
                {
                    list.RemoveAt(0);
                }

                list.Add(msg);
            }
            return true;
        }
    }
}
