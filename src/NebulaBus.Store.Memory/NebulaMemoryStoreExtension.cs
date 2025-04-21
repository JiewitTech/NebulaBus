namespace NebulaBus.Store.Memory
{
    public static class NebulaMemoryStoreExtension
    {
        public static void UserMemoryStore(this NebulaOptions options)
        {
            options.AddNebulaServiceProvider(new NebulaMemoryStoreServiceProvider());
        }
    }
}