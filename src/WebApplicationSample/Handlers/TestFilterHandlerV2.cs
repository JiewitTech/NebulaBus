using MessageLibrary;
using NebulaBus;

namespace WebApplicationSample.Handlers;

public class TestFilterHandlerV2 : NebulaHandler<TestMessage>
{
    public override string Name => nameof(TestFilterHandlerV2);
    public override string Group => "TestFilterHandlerGroup";

    protected override async Task Handle(TestMessage message, NebulaHeader header)
    {
        Console.WriteLine($"[{DateTime.Now}]-Received Message: {message.Message} RetryCount: {header.GetRetryCount()}");
        throw new Exception("Test exception");
    }

    protected override async Task FallBackHandle(TestMessage? message, NebulaHeader header, Exception exception)
    {
        Console.WriteLine($"[{DateTime.Now}]-FallBackHandler Received Exception: {exception}");
        await Task.CompletedTask;
    }

    protected override async Task AfterHandle(TestMessage? message, NebulaHeader header)
    {
        Console.WriteLine($"[{DateTime.Now}]-AfterHandler Received Message: {message?.Message}");
        await Task.CompletedTask;
    }

    protected override async Task<bool> BeforeHandle(TestMessage? message, NebulaHeader header)
    {
        Console.WriteLine($"[{DateTime.Now}]-BeforeHandler Received Message: {message?.Message}");
        return await Task.FromResult(false);
    }
}