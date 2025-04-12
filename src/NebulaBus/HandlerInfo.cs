using System;

namespace NebulaBus
{
    internal class HandlerInfo
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public Type Type { get; set; }
        public byte ExcuteThreadCount { get; set; }
    }
}
