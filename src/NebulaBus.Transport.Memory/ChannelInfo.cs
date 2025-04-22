using System.Threading.Channels;

namespace NebulaBus.Transport.Memory
{
    internal class ChannelInfo
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public Channel<(byte[] header, byte[] body)> Channel { get; set; }
    }
}