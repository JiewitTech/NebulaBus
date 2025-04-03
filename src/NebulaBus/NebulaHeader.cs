﻿using System.Collections;
using System.Collections.Generic;

namespace NebulaBus
{
    public class NebulaHeader : IDictionary<string, string?>
    {
        public const string RequestId = "x-request-id";
        public const string MessageId = "x-message-id";
        public const string Sender = "x-sender";
        public const string SendTimeStamp = "x-send-timestamp";

        private readonly Dictionary<string, string?> _dic;

        public NebulaHeader()
        {
            _dic = new Dictionary<string, string?>();
        }

        public string? this[string key]
        {
            get => _dic.ContainsKey(key) ? _dic[key] : null;
            set => this.Add(key, value);
        }

        public ICollection<string> Keys => _dic.Keys;

        public ICollection<string?> Values => _dic.Values;

        public int Count => _dic.Count;

        public bool IsReadOnly => true;

        public void Add(string key, string? value)
        {
            if (_dic.ContainsKey(key))
                _dic[key] = value;
            else
                _dic.Add(key, value);
        }

        public void Add(KeyValuePair<string, string?> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dic.Clear();
        }

        public bool Contains(KeyValuePair<string, string?> item)
        {
            return _dic.ContainsKey(item.Key) && _dic[item.Key] == item.Value;
        }

        public bool ContainsKey(string key)
        {
            return _dic.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string?>[] array, int arrayIndex)
        {
        }

        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator()
        {
            return _dic.GetEnumerator();
        }

        public bool Remove(string key)
        {
            if (_dic.ContainsKey(key))
            {
                _dic.Remove(key);
                return true;
            }
            return false;
        }

        public bool Remove(KeyValuePair<string, string?> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(string key, out string? value)
        {
            return _dic.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}