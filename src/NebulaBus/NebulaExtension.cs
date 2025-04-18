using System;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal static class NebulaExtension
    {
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

        public static async Task ExcuteHandlerWithoutException(Task action, Task? realAction)
        {
            try
            {
                await action;
            }
            catch (NotImplementedException)
            {
                if (realAction != null)
                {
                    await ExcuteWithoutException(realAction);
                }
            }
            catch
            {
            }
        }

        public static async Task<bool> ExcuteBeforeHandlerWithoutException(Task<bool> action)
        {
            try
            {
                return await action;
            }
            catch
            {
                return true;
            }
        }
        
        public static async Task<bool> ExcuteBeforeHandlerWithoutException(Task<bool> action, Task<bool>? realAction)
        {
            try
            {
                return await action;
            }
            catch (NotImplementedException)
            {
                if (realAction != null)
                {
                    return await ExcuteBeforeHandlerWithoutException(realAction);
                }

                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}