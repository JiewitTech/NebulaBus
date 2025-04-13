using NebulaBus.Rabbitmq;
using System;
using System.Reflection;
using System.Text.Json;

namespace NebulaBus
{
    public class NebulaOptions
    {
        internal RabbitmqOptions RabbitmqOptions { get; }
        internal string RedisConnectionString { get; set; }
        public string ClusterName { get; set; } = $"{Assembly.GetEntryAssembly().GetName().Name}";
        public byte ExecuteThreadCount { get; set; } = (byte)Environment.ProcessorCount;
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions();

        public NebulaOptions()
        {
            RabbitmqOptions = new RabbitmqOptions();
        }

        public void UseRabbitmq(Action<RabbitmqOptions> optionsAction)
        {
            optionsAction(RabbitmqOptions);
        }

        public void UseRedisStore(string redisConnectionString)
        {
            RedisConnectionString = redisConnectionString;
        }
    }
}