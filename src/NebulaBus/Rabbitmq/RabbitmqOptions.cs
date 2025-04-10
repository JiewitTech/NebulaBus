using System;
using RabbitMQ.Client;

namespace NebulaBus.Rabbitmq
{
    public class RabbitmqOptions
    {
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string HostName { get; set; } = "localhost";
        public string VirtualHost { get; set; } = "/";
        public int Port { get; set; } = 5672;
        public string ExchangeName { get; set; } = "nebula-bus-exchange";
        public SslOption SslOption { get; set; } = new SslOption();
        /// <summary>
        /// 全局预取数
        /// </summary>
        public ushort Qos { get; set; } = 0;
        /// <summary>
        /// 动态自定义预取规则
        /// </summary>
        public Func<string, string, ushort> GetQos { get; set; }
    }
}