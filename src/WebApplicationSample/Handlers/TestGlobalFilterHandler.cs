using MessageLibrary;
using NebulaBus;

namespace WebApplicationSample.Handlers;

public class TestGlobalFilterHandler : NebulaHandler<TestMessage>
{
    public override string Name => nameof(TestGlobalFilterHandler);
    public override string Group => "TestFilterHandlerGroup";

    protected override async Task Handle(TestMessage message, NebulaHeader header)
    {
        Console.WriteLine($"[{DateTime.Now}]-Received Message: {message.Message} RetryCount: {header.GetRetryCount()}");
        throw new Exception("Test exception");
    }

    protected override async Task<bool> BeforeHandle(TestMessage? message, NebulaHeader header)
    {
        Console.WriteLine($"{DateTime.Now} - BeforeHandler Message: {message?.Message} RetryCount: {header.GetRetryCount()}");
        return await Task.FromResult(true);
    }
}