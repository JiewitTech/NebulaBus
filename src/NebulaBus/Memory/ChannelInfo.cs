using System.Threading.Channels;

namespace NebulaBus.Memory
{
    public class ChannelInfo
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public Channel<(NebulaHeader header, byte[] body)> Channel { get; set; }
    }
}