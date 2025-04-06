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
        public virtual TimeSpan RetryInterval => TimeSpan.FromSeconds(10);
        public virtual TimeSpan RetryDelay => TimeSpan.FromSeconds(10);
        public virtual int MaxRetryCount => 10;

        internal abstract Task Subscribe(string message, NebulaHeader header);
    }

    public abstract class NebulaHandler<T> : NebulaHandler
    {
        internal override async Task Subscribe(string message, NebulaHeader header)
        {
            if (string.IsNullOrEmpty(message)) return;
            var data = JsonSerializer.Deserialize<T>(message);
            if (data == null) return;
            await Handle(data, header);
        }

        public abstract Task Handle(T message, NebulaHeader header);
    }
}