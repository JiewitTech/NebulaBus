using Microsoft.AspNetCore.Mvc;
using NebulaBus;
using WebApplicationSample.Messages;

namespace WebApplicationSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly INebulaBus _bus;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, INebulaBus nebulaBus)
        {
            _logger = logger;
            _bus = nebulaBus;
        }

        [HttpGet("Publish")]
        public IEnumerable<WeatherForecast> Publish()
        {
            _logger.LogInformation($"{DateTime.Now} Start send Message");
            _bus.PublishAsync("NebulaBus.TestHandler", new TestMessage { Message = "Hello World" });
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
            _bus.PublishAsync(TimeSpan.FromSeconds(5), "NebulaBus.TestHandler.V1", new TestMessage { Message = "Hello World" });
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }).ToArray();
        }
    }
}