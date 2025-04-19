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

        [HttpGet("StressTestSend")]
        public async Task StressTestSend()
        {
            _logger.LogInformation($"{DateTime.Now} Start send StressTestSend Message");

            var tasks = new List<Task>();
            for (int i = 0; i < 2000; i++)
            {
                tasks.Add(_bus.PublishAsync("NebulaBus.TestHandler.V4", new TestMessage { Message = $"StressTestSend Message{i}" }));
            }
            await Task.WhenAll(tasks);
        }

        [HttpGet("StressTestPublish")]
        public async Task StressTestPublish()
        {
            _logger.LogInformation($"{DateTime.Now} Start send StressTestPublish Message");

            var tasks = new List<Task>();
            for (int i = 0; i < 2000; i++)
            {
                tasks.Add(_bus.PublishAsync("NebulaBus.TestHandler", new TestMessage { Message = $"StressTestPublish Message{i}" }));
            }
            await Task.WhenAll(tasks);
        }
        
        [HttpGet("SendToFallBack")]
        public async Task SendToFallBack()
        {
            _logger.LogInformation($"{DateTime.Now} Start send Message");
            await _bus.PublishAsync(nameof(TestFallbackHandler), new TestMessage { Message = "Hello World" });
        }
        
        [HttpGet("SendToFilterHandler")]
        public async Task SendToFilterHandler()
        {
            _logger.LogInformation($"{DateTime.Now} Start send Message");
            await _bus.PublishAsync(nameof(TestFilterHandler), new TestMessage { Message = "Hello World" });
        }
                
        [HttpGet("SendToFilterHandlerV2")]
        public async Task SendToFilterHandlerV2()
        {
            _logger.LogInformation($"{DateTime.Now} Start send Message");
            await _bus.PublishAsync(nameof(TestFilterHandlerV2), new TestMessage { Message = "Hello World" });
        }
        
        [HttpGet("SendToGlobalFilterHandler")]
        public async Task SendToGlobalFilterHandler()
        {
            _logger.LogInformation($"{DateTime.Now} Start send Message");
            await _bus.PublishAsync(nameof(TestGlobalFilterHandler), new TestMessage { Message = "Hello World" });
        }
    }
}