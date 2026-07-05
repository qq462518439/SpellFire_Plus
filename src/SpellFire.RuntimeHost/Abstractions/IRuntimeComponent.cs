namespace SpellFire.RuntimeHost.Abstractions
{
    public interface IRuntimeComponent
    {
        string Name { get; }

        RuntimeComponentStatus Probe(int processId);

        void Cleanup(int processId);
    }
}
