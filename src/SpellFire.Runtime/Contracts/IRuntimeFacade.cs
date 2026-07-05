using SpellFire.Runtime.Models;

namespace SpellFire.Runtime.Contracts
{
    public interface IRuntimeFacade
    {
        RuntimeSessionSnapshot Attach(int processId);

        RuntimeMemoryProbeSnapshot ProbeMemory(int processId);
    }
}
