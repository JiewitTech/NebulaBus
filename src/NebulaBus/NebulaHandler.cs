namespace NebulaBus
{
    public abstract class NebulaHandler
    {
        public abstract string Name { get; }
        public abstract string Group { get; }

        public abstract void Handle<T>(T message, NebulaHeader header);
    }
}