using MessageLibrary;
using Microsoft.AspNetCore.Mvc;
using NebulaBus;
using WebApplicationSample.Handlers;

namespace WebApplicationSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<TestController> _logger;
        private readonly INebulaBus _bus;

        public TestController(ILogger<TestController> logger, INebulaBus nebulaBus)
        {
            _logger = logger;
            _bus = nebulaBus;
        }

        [HttpGet("Publish")]
        public IEnumerable<WeatherForecast> Publish()
        {
            _logger.LogInformation($"{DateTime.Now} Start send Message");
            _bus.PublishAsync("NebulaBus.TestHandler", new TestMessage { Message = "Hello World" },
                new Dictionary<string, string>()
                {
                    { "customHeader", "123456" },
                    { NebulaHeader.RequestId, "8889999" },
                });
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
                .ToArray();
        }

        [HttpGet("DelayPublish")]
        public IEnumerable<WeatherForecast> DelayPublish()
        {
            Console.WriteLine($"{DateTime.Now} Start send delay message");
            _bus.PublishAsync(TimeSpan.FromSeconds(5), "NebulaBus.TestHandler",
                new TestMessage { Message = "Hello World" });
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
                .ToArray();
        }

        [HttpGet("Send")]
        public IEnumerable<WeatherForecast> Send()
        {
            _logger.LogInformation($"{DateTime.Now} Start send Message");
            _bus.PublishAsync("NebulaBus.TestHandler.V1", new TestMessage { Message = "Hello World" });
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
                .ToArray();
        }

        [HttpGet("DelaySend")]
        public IEnumerable<WeatherForecast> DelaySend()
        {
            _logger.LogInformation($"{DateTime.Now} Start send Message");
            _bus.PublishAsync(TimeSpan.FromSeconds(5), "NebulaBus.TestHandler.V1",
                new TestMessage { Message = "Hello World" });
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();
        }

        [HttpGet("StressTest")]
        public async Task StressTest()
        {
            _logger.LogInformation($"{DateTime.Now} Start send StressTest Message");

            var tasks = new List<Task>();
            for (int i = 0; i < 2000; i++)
            {
                tasks.Add(_bus.PublishAsync("NebulaBus.TestHandler.V4", new TestMessage { Message = $"StressTest Message{i}" }));
            }
            await Task.WhenAll(tasks);
        }
    }
}