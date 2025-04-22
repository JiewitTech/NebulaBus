using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace NebulaBus
{
    public class NebulaOptions
    {
        public string ClusterName { get; set; } = $"{Assembly.GetEntryAssembly().GetName().Name}";
        public byte ExecuteThreadCount { get; set; } = (byte)Environment.ProcessorCount;
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions();
        internal List<INebulaServiceProvider> NebulaServiceProviders { get; private set; }

        public NebulaOptions()
        {
            NebulaServiceProviders = new List<INebulaServiceProvider>();
        }

        public void AddNebulaServiceProvider(INebulaServiceProvider nebulaServiceProvider)
        {
            NebulaServiceProviders.Add(nebulaServiceProvider);
        }
    }
}