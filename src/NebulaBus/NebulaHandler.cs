﻿using Newtonsoft.Json;
using System;
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
            header[NebulaHeader.Consumer] = Environment.MachineName;
            if (string.IsNullOrEmpty(message)) return;
            var data = JsonConvert.DeserializeObject<T>(message);
            if (data == null) return;
            await Handle(data, header);
        }

        public abstract Task Handle(T message, NebulaHeader header);
    }
}