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
        public string ExchangeName { get; set; } = "nebula-bus";
        public SslOption SslOption { get; set; } = new SslOption();
    }
}