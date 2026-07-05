using System.Linq;
using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;
using SpellFire.RuntimeHost;
using SpellFire.RuntimeHost.Abstractions;

namespace SpellFire.Runtime.Services
{
    public sealed class RuntimeSessionService : IRuntimeSessionService
    {
        private readonly IRuntimeHostFactory hostFactory;
        private readonly IRuntimeHost host;

        public RuntimeSessionService()
            : this(new RuntimeHostFactory())
        {
        }

        public RuntimeSessionService(IRuntimeHostFactory hostFactory)
        {
            this.hostFactory = hostFactory;
            host = this.hostFactory.CreateHost();
        }

        public RuntimeSessionSnapshot Attach(int processId)
        {
            using (IRuntimeHostSession session = host.Attach(processId))
            {
                return new RuntimeSessionSnapshot
                {
                    ProcessId = session.ProcessId,
                    HostState = session.State.ToString(),
                    Components = session.Components.Select(component => new RuntimeComponentSnapshot
                    {
                        Name = component.Name,
                        Ready = component.Ready,
                        Reason = component.Reason,
                        Detail = component.Detail
                    }).ToArray()
                };
            }
        }

        public RuntimeConnectionSnapshot Connect(int processId)
        {
            IRuntimeHostSession session = host.Attach(processId);
            return CreateConnectionSnapshot(session, "Connected");
        }

        public RuntimeConnectionSnapshot Disconnect(int processId)
        {
            if (!host.Detach(processId, out IRuntimeHostSession session) || session == null)
            {
                return new RuntimeConnectionSnapshot
                {
                    ProcessId = processId,
                    Connected = false,
                    Disconnected = false,
                    HostState = RuntimeHostState.Detached.ToString(),
                    SessionExists = false,
                    SessionDisposed = false,
                    Reason = "SessionNotFound",
                    Components = new RuntimeComponentSnapshot[0]
                };
            }

            return CreateConnectionSnapshot(session, "Disconnected");
        }

        public RuntimeConnectionSnapshot GetConnection(int processId)
        {
            IRuntimeHostSession session = host.GetSessions().FirstOrDefault(item => item.ProcessId == processId);
            if (session == null)
            {
                return new RuntimeConnectionSnapshot
                {
                    ProcessId = processId,
                    Connected = false,
                    Disconnected = false,
                    HostState = RuntimeHostState.Detached.ToString(),
                    SessionExists = false,
                    SessionDisposed = false,
                    Reason = "SessionNotFound",
                    Components = new RuntimeComponentSnapshot[0]
                };
            }

            return CreateConnectionSnapshot(session, session.IsDisposed ? "SessionDisposed" : "SessionConnected");
        }

        private static RuntimeConnectionSnapshot CreateConnectionSnapshot(IRuntimeHostSession session, string reason)
        {
            return new RuntimeConnectionSnapshot
            {
                ProcessId = session.ProcessId,
                Connected = !session.IsDisposed,
                Disconnected = session.IsDisposed,
                HostState = session.State.ToString(),
                SessionExists = true,
                SessionDisposed = session.IsDisposed,
                Reason = reason,
                Components = session.Components.Select(component => new RuntimeComponentSnapshot
                {
                    Name = component.Name,
                    Ready = component.Ready,
                    Reason = component.Reason,
                    Detail = component.Detail
                }).ToArray()
            };
        }
    }
}
