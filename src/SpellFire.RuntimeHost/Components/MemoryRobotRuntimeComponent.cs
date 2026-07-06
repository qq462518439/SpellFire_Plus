using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Process;
using SpellFire.MemoryRobot.Services;
using SpellFire.RuntimeHost.Abstractions;

namespace SpellFire.RuntimeHost.Components
{
    public sealed class MemoryRobotRuntimeComponent : IRuntimeComponent
    {
        private readonly IMemorySessionFactory sessionFactory;
        private readonly ProcessAttachService attachService;

        public MemoryRobotRuntimeComponent(IMemorySessionFactory sessionFactory, ProcessAttachService attachService)
        {
            this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            this.attachService = attachService ?? throw new ArgumentNullException(nameof(attachService));
        }

        public string Name => "MemoryRobot";

        public RuntimeComponentStatus Probe(int processId)
        {
            var attach = attachService.Attach(processId);
            if (!attach.Ready)
            {
                return new RuntimeComponentStatus
                {
                    Name = Name,
                    Ready = false,
                    Reason = attach.Reason,
                    Detail = attach.Detail
                };
            }

            return new RuntimeComponentStatus
            {
                Name = Name,
                Ready = true,
                Reason = attach.Reason,
                Detail = attach.Detail
            };
        }

        public void Cleanup(int processId)
        {
            attachService.Close(processId);
        }
    }
}
