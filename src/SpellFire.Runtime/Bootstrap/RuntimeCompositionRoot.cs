using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Services;

namespace SpellFire.Runtime.Bootstrap
{
    public static class RuntimeCompositionRoot
    {
        public static IRuntimeFacade CreateDefaultFacade()
        {
            return new RuntimeFacade(new RuntimeSessionService(), new RuntimeMemoryProbeService(), new RuntimeHookService());
        }
    }
}
