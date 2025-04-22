using System;
using System.Threading.Tasks;

namespace NebulaBus
{
    public interface INebulaFilter
    {
        Task<bool> BeforeHandle(object data, NebulaHeader header);
        Task AfterHandle(object data, NebulaHeader header);
        Task FallBackHandle(object? data, NebulaHeader header, Exception exception);
    }
}