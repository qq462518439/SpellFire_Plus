using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;
using SpellFire.Runtime.Services;

namespace SpellFire.Runtime
{
    public sealed class RuntimeFacade : IRuntimeFacade
    {
        private readonly IRuntimeSessionService sessionService;
        private readonly IRuntimeMemoryProbeService memoryProbeService;

        public RuntimeFacade()
            : this(new RuntimeSessionService(), new RuntimeMemoryProbeService())
        {
        }

        public RuntimeFacade(IRuntimeSessionService sessionService)
            : this(sessionService, new RuntimeMemoryProbeService())
        {
        }

        public RuntimeFacade(IRuntimeSessionService sessionService, IRuntimeMemoryProbeService memoryProbeService)
        {
            this.sessionService = sessionService;
            this.memoryProbeService = memoryProbeService;
        }

        public RuntimeSessionSnapshot Attach(int processId)
        {
            return sessionService.Attach(processId);
        }

        public RuntimeConnectionSnapshot Connect(int processId)
        {
            return sessionService.Connect(processId);
        }

        public RuntimeConnectionSnapshot Disconnect(int processId)
        {
            return sessionService.Disconnect(processId);
        }

        public RuntimeConnectionSnapshot GetConnection(int processId)
        {
            return sessionService.GetConnection(processId);
        }

        public RuntimeMemoryProbeSnapshot ProbeMemory(int processId)
        {
            return memoryProbeService.Probe(processId);
        }

        public RuntimeEvaluationSnapshot Evaluate(int processId)
        {
            RuntimeMemoryProbeSnapshot memory = memoryProbeService.Probe(processId);
            RuntimeConnectionSnapshot connection = sessionService.GetConnection(processId);
            bool readyToConnect = memory.Ready && (connection == null || !connection.Connected);

            return new RuntimeEvaluationSnapshot
            {
                ProcessId = processId,
                ReadyToConnect = readyToConnect,
                Decision = CreateDecision(memory, connection, readyToConnect),
                Memory = memory,
                Connection = connection
            };
        }

        private static string CreateDecision(RuntimeMemoryProbeSnapshot memory, RuntimeConnectionSnapshot connection, bool readyToConnect)
        {
            if (connection != null && connection.Connected)
            {
                return "AlreadyConnected";
            }

            if (memory == null)
            {
                return "MemoryProbeUnavailable";
            }

            if (!memory.Ready)
            {
                return "MemoryNotReady:" + memory.Reason;
            }

            return readyToConnect ? "ReadyToConnect" : "NotReady";
        }
    }
}
