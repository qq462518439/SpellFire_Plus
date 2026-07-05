using SpellFire.Runtime.Models;

namespace SpellFire.Runtime.Contracts
{
    public interface IRuntimeSessionService
    {
        RuntimeSessionSnapshot Attach(int processId);
    }
}
