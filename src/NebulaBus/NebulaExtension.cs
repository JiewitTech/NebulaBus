using System;
using System.Threading.Tasks;

namespace NebulaBus
{
    internal static class NebulaExtension
    {
        public static async Task ExcuteWithoutException(Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch
            {
            }
        }

        public static async Task ExcuteHandlerWithoutException(Func<Task> action, Func<Task> action2)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (NotImplementedException)
            {
                if (action2 != null)
                {
                    await ExcuteWithoutException(action2);
                }
            }
            catch
            {
            }
        }

        public static async Task<bool> ExcuteBeforeHandlerWithoutException(Func<Task<bool>> action)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch
            {
                return true;
            }
        }

        public static async Task<bool> ExcuteBeforeHandlerWithoutException(Func<Task<bool>> action, Func<Task<bool>> action2)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (NotImplementedException)
            {
                if (action2 != null)
                {
                    return await ExcuteBeforeHandlerWithoutException(action2);
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