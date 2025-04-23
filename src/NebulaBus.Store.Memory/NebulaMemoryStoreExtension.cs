namespace NebulaBus.Store.Memory
{
    public static class NebulaMemoryStoreExtension
    {
        public static void UseMemoryStore(this NebulaOptions options)
        {
            options.AddNebulaServiceProvider(new NebulaMemoryStoreServiceProvider());
        }
    }
}