using SpellFire.Runtime.Models;

namespace SpellFire.Runtime.Contracts
{
    public interface IRuntimeFacade
    {
        RuntimeSessionSnapshot Attach(int processId);

        RuntimeConnectionSnapshot Connect(int processId);

        RuntimeConnectionSnapshot Disconnect(int processId);

        RuntimeConnectionSnapshot GetConnection(int processId);

        RuntimeMemoryProbeSnapshot ProbeMemory(int processId);

        RuntimeEvaluationSnapshot Evaluate(int processId);
    }
}
