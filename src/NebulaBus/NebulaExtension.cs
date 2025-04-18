using System;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal static class NebulaExtension
    {
        public static void ExcuteWithoutException(Action action)
        {
            try
            {
                action();
            }
            catch
            {
            }
        }

        public static async Task ExcuteWithoutException(Task action)
        {
            try
            {
                await action;
            }
            catch
            {
            }
        }

        public static async Task ExcuteWithoutException(Task action, Task realAction)
        {
            try
            {
                await action;
            }
            catch (NotImplementedException)
            {
                await ExcuteWithoutException(realAction);
            }
            catch
            {
            }
        }
    }
}