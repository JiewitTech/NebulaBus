using System;

namespace NebulaBus.Store
{
    internal class DelayStoreMessage
    {
        public object Message { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
        public NebulaHeader Header { get; set; }
        public long TriggerTime { get; set; }
        public string MessageId { get; set; }
    }
}