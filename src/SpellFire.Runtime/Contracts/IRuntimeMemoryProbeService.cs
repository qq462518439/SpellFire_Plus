using SpellFire.Runtime.Models;

namespace SpellFire.Runtime.Contracts
{
    public interface IRuntimeMemoryProbeService
    {
        RuntimeMemoryProbeSnapshot Probe(int processId);
    }
}
