using LogicSamples.Handlers;
using MessageLibrary;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NebulaBus;

namespace WebApplication.Controllers;

[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private readonly INebulaBus _bus;

    public TestController(ILogger<TestController> logger, INebulaBus nebulaBus)
    {
        _logger = logger;
        _bus = nebulaBus;
    }

    [HttpGet("Publish")]
    public async Task Publish()
    {
        _logger.LogInformation($"{DateTime.Now} Start send Message");
        await _bus.PublishAsync("NebulaBus.TestHandler", new TestMessage { Message = "Hello World" },
            new Dictionary<string, string>()
            {
                { "customHeader", "123456" },
                { NebulaHeader.RequestId, "8889999" },
            });
    }

    [HttpGet("DelayPublish")]
    public async Task DelayPublish()
    {
        Console.WriteLine($"{DateTime.Now} Start send delay message");
        await _bus.PublishAsync(TimeSpan.FromSeconds(5), "NebulaBus.TestHandler",
            new TestMessage { Message = "Hello World" });
    }

    [HttpGet("Send")]
    public async Task Send()
    {
        _logger.LogInformation($"{DateTime.Now} Start send Message");
        await _bus.PublishAsync("NebulaBus.TestHandler.V1", new TestMessage { Message = "Hello World" });
    }

    [HttpGet("DelaySend")]
    public async Task DelaySend()
    {
        _logger.LogInformation($"{DateTime.Now} Start send Message");
        await _bus.PublishAsync(TimeSpan.FromSeconds(5), "NebulaBus.TestHandler.V1",
            new TestMessage { Message = "Hello World" });
    }

    [HttpGet("StressTestSend")]
    public async Task StressTestSend()
    {
        _logger.LogInformation($"{DateTime.Now} Start send StressTestSend Message");

        var tasks = new List<Task>();
        for (int i = 0; i < 2000; i++)
        {
            tasks.Add(_bus.PublishAsync("NebulaBus.TestHandler.V4",
                new TestMessage { Message = $"StressTestSend Message{i}" }));
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
            tasks.Add(_bus.PublishAsync("NebulaBus.TestHandler",
                new TestMessage { Message = $"StressTestPublish Message{i}" }));
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