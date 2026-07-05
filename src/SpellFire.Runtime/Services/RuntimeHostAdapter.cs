using System;
using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;

namespace SpellFire.Runtime.Services
{
    public sealed class RuntimeHostAdapter : IRuntimeHostAdapter
    {
        private readonly IRuntimeFacade runtime;

        public RuntimeHostAdapter(IRuntimeFacade runtime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        public RuntimeHostAttachSnapshot Attach(int processId)
        {
            RuntimeEvaluationSnapshot evaluation = runtime.Evaluate(processId);
            if (evaluation == null || !evaluation.ReadyToConnect)
            {
                return new RuntimeHostAttachSnapshot
                {
                    ProcessId = processId,
                    Accepted = false,
                    Decision = evaluation == null ? "EvaluationUnavailable" : evaluation.Decision,
                    Evaluation = evaluation,
                    Connection = evaluation == null ? null : evaluation.Connection
                };
            }

            RuntimeConnectionSnapshot connection = runtime.Connect(processId);
            return new RuntimeHostAttachSnapshot
            {
                ProcessId = processId,
                Accepted = connection != null && connection.Connected,
                Decision = connection != null && connection.Connected ? "Connected" : "ConnectFailed",
                Evaluation = evaluation,
                Connection = connection
            };
        }

        public RuntimeConnectionSnapshot GetConnection(int processId)
        {
            return runtime.GetConnection(processId);
        }

        public RuntimeConnectionSnapshot Detach(int processId)
        {
            return runtime.Disconnect(processId);
        }
    }
}
