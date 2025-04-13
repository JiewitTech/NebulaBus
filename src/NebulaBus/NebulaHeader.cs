using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NebulaBus
{
    [JsonConverter(typeof(NebulaHeaderConverter))]
    public class NebulaHeader : IDictionary<string, string>
    {
        public const string RequestId = "nb-request-id";
        public const string MessageId = "nb-message-id";
        public const string Sender = "nb-sender";
        public const string Consumer = "nb-consumer";
        public const string SendTimeStamp = "nb-send-timestamp";
        public const string Exception = "nb-exception";
        public const string MessageType = "nb-message-type";
        public const string Name = "nb-name";
        public const string Group = "nb-group";
        public const string RetryCount = "nb-retry-count";

        private readonly Dictionary<string, string> _dic;

        public string GetRequestId() => this[RequestId];
        public string GetMessageId() => this[MessageId];
        public int GetRetryCount()
        {
            int.TryParse(this[RetryCount], out var result);
            return result;
        }

        public NebulaHeader()
        {
            _dic = new Dictionary<string, string>();
        }

        public NebulaHeader(IDictionary<string, string> dic)
        {
            _dic = dic.ToDictionary(x => x.Key, x => x.Value);
        }

        public string this[string key]
        {
            get => _dic.ContainsKey(key) ? _dic[key] : "";
            set => this.Add(key, value);
        }

        public ICollection<string> Keys => _dic.Keys;

        public ICollection<string> Values => _dic.Values;

        public int Count => _dic.Count;

        public bool IsReadOnly => true;

        public void Add(string key, string value)
        {
            if (_dic.ContainsKey(key))
                _dic[key] = value;
            else
                _dic.Add(key, value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dic.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _dic.ContainsKey(item.Key) && _dic[item.Key] == item.Value;
        }

        public bool ContainsKey(string key)
        {
            return _dic.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
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

        public bool Remove(KeyValuePair<string, string> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _dic.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Dictionary<string, string> ToDictionary() => _dic;
    }

    public class NebulaHeaderConverter : JsonConverter<NebulaHeader>
    {
        public override NebulaHeader Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 反序列化JSON对象为字典 
            var jsonDict = JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);
            if (jsonDict == null) return new NebulaHeader();
            return new NebulaHeader(jsonDict); ;
        }

        public override void Write(Utf8JsonWriter writer, NebulaHeader value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.ToDictionary(), options);
        }
    }
}