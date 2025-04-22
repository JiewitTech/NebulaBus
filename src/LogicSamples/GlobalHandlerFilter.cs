using MessageLibrary;
using NebulaBus;

namespace LogicSamples;

public class GlobalHandlerFilter : INebulaFilter
{
    public async Task<bool> BeforeHandle(object data, NebulaHeader header)
    {
        Console.WriteLine($"{DateTime.Now} GlobalHandlerFilter.BeforeHandle");
        return data switch
        {
            TestMessage msg => await Task.FromResult(true),
            _ => await Task.FromResult(true),
        };
    }

    public async Task AfterHandle(object data, NebulaHeader header)
    {
        Console.WriteLine($"{DateTime.Now} GlobalHandlerFilter.AfterHandle");
    }

    public async Task FallBackHandle(object? data, NebulaHeader header, Exception exception)
    {
        Console.WriteLine($"{DateTime.Now} GlobalHandlerFilter.FallBackHandle");
    }
}