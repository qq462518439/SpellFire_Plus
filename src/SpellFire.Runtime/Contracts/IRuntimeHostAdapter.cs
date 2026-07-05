using SpellFire.Runtime.Models;

namespace SpellFire.Runtime.Contracts
{
    public interface IRuntimeHostAdapter
    {
        RuntimeHostAttachSnapshot Attach(int processId);

        RuntimeConnectionSnapshot GetConnection(int processId);

        RuntimeConnectionSnapshot Detach(int processId);
    }
}
