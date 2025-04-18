﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal interface IProcessor : IDisposable
    {
        string Name { get; }
        Task Start(CancellationToken cancellationToken);
        Task Publish(string routingKey, object message, NebulaHeader header);
    }
}