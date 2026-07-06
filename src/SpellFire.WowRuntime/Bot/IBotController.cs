using System.Collections.Generic;

namespace SpellFire.WowRuntime.Bot
{
    public interface IBotController
    {
        IReadOnlyList<IBotState> States { get; }

        void AddState(IBotState state);

        BotStateResult PulseOnce(IWowRuntimeContext context);
    }
}
