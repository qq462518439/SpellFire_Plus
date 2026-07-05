using System;
using System.Threading;

namespace SpellFire.RuntimeHost.Components
{
    internal static class HookReadySignal
    {
        public static string GetEventName(int processId)
        {
            return "Local\\SpellFireHookReady_" + processId;
        }

        public static string GetHeartbeatEventName(int processId)
        {
            return "Local\\SpellFireHookHeartbeat_" + processId;
        }

        public static string GetShutdownEventName(int processId)
        {
            return "Local\\SpellFireHookShutdown_" + processId;
        }

        public static EventWaitHandle CreateForAttach(int processId)
        {
            return CreateManualResetEvent(GetEventName(processId), false);
        }

        public static EventWaitHandle CreateHeartbeat(int processId)
        {
            return CreateManualResetEvent(GetHeartbeatEventName(processId), false);
        }

        public static EventWaitHandle CreateShutdown(int processId)
        {
            return CreateManualResetEvent(GetShutdownEventName(processId), false);
        }

        public static bool IsSet(int processId)
        {
            using (EventWaitHandle readyEvent = CreateForAttach(processId))
            {
                return readyEvent.WaitOne(0);
            }
        }

        public static bool Wait(EventWaitHandle readyEvent, int timeoutMilliseconds)
        {
            if (readyEvent == null)
            {
                return false;
            }

            return readyEvent.WaitOne(Math.Max(0, timeoutMilliseconds));
        }

        private static EventWaitHandle CreateManualResetEvent(string eventName, bool initialState)
        {
            return new EventWaitHandle(initialState, EventResetMode.ManualReset, eventName);
        }
    }
}
