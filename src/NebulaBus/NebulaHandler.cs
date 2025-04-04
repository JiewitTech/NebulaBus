using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace NebulaBus
{
    public abstract class NebulaHandler
    {
        public abstract string Name { get; }
        public abstract string Group { get; }
        public virtual bool LoopRetry => false;
        public virtual TimeSpan RetryDelay => TimeSpan.FromSeconds(10);
        public virtual int MaxRetryCount => 10;

        internal async Task Subscribe<T>(string message, NebulaHeader header)
        {
            if (string.IsNullOrEmpty(message)) return;
            try
            {
                await Handle(JsonSerializer.Deserialize<T>(message), header);
            }
            catch (Exception ex)
            {

            }
        }

        public abstract Task Handle<T>(T message, NebulaHeader header);
    }
}