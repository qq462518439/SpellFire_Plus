using System;
using System.Collections.Generic;
using System.Linq;

namespace SpellFire.WowRuntime.Bot
{
    public sealed class BotController : IBotController
    {
        private readonly List<IBotState> states = new List<IBotState>();

        public IReadOnlyList<IBotState> States
        {
            get { return states; }
        }

        public void AddState(IBotState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            states.Add(state);
            states.Sort((left, right) => right.Priority.CompareTo(left.Priority));
        }

        public BotStateResult PulseOnce(IWowRuntimeContext context)
        {
            IBotState state = states.FirstOrDefault(candidate => candidate.CanRun(context));
            if (state == null)
            {
                return new BotStateResult(false, string.Empty, "No runnable state.");
            }

            state.Run(context);
            return new BotStateResult(true, state.Name, string.Empty);
        }
    }
}
