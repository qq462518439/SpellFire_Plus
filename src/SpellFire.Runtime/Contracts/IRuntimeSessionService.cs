using SpellFire.Runtime.Models;

namespace SpellFire.Runtime.Contracts
{
    public interface IRuntimeSessionService
    {
        RuntimeSessionSnapshot Attach(int processId);

        RuntimeConnectionSnapshot Connect(int processId);

        RuntimeConnectionSnapshot Disconnect(int processId);

        RuntimeConnectionSnapshot GetConnection(int processId);
    }
}
