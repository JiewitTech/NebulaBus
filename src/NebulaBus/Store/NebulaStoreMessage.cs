namespace NebulaBus.Store
{
    public class NebulaStoreMessage
    {
        public object Message { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public NebulaHeader Header { get; set; }
        public long TriggerTime { get; set; }
        public string MessageId { get; set; }
        public string Transport { get; set; }

        public string GetKey() =>
            string.IsNullOrEmpty(Transport) ? $"{MessageId}.{Name}" : $"{MessageId}.{Transport}.{Name}";
    }
}