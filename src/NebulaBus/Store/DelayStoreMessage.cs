using System;

namespace NebulaBus.Store
{
    internal class DelayStoreMessage
    {
        public string Message { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public NebulaHeader Header { get; set; }
        public DateTimeOffset TriggerTime { get; set; }
        public string MessageId { get; set; }
    }
}