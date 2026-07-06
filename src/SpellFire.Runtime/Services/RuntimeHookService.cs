using System;
using System.Linq;
using SpellFire.Runtime.Models;
using SpellFire.RuntimeHost;
using SpellFire.RuntimeHost.Services;

namespace SpellFire.Runtime.Services
{
    public sealed class RuntimeHookService
    {
        public RuntimeOperationSnapshot Preflight(int processId)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.Preflight(processId));
            }
        }

        public RuntimeOperationSnapshot AttachHook(int processId)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.AttachHook(processId));
            }
        }

        public RuntimeOperationSnapshot GetHookStatus(int processId)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.GetHookStatus(processId));
            }
        }

        public RuntimeOperationSnapshot PingHook(int processId)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.PingHook(processId));
            }
        }

        public RuntimeOperationSnapshot GetHookInfo(int processId)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.GetHookInfo(processId));
            }
        }

        public RuntimeOperationSnapshot ReadHookSelfModule(int processId)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.ReadHookSelfModule(processId));
            }
        }

        public RuntimeOperationSnapshot LuaSmoke(int processId)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.LuaSmoke(processId));
            }
        }

        public RuntimeOperationSnapshot ExecuteLua(int processId, string script)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.ExecuteLua(processId, script));
            }
        }

        public RuntimeOperationSnapshot ClickToMoveMove(int processId, float x, float y, float z, ulong guid, int action, float precision)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.ClickToMoveMove(processId, x, y, z, guid, action, precision));
            }
        }

        public RuntimeOperationSnapshot ShutdownHook(int processId)
        {
            using (RuntimeHostOperationService service = new RuntimeHostOperationService())
            {
                return Map(service.ShutdownHook(processId));
            }
        }

        private static RuntimeOperationSnapshot Map(RuntimeHostOperationResult result)
        {
            return new RuntimeOperationSnapshot
            {
                ProcessId = result == null ? 0 : result.ProcessId,
                Operation = result == null ? string.Empty : result.Operation,
                Ready = result != null && result.Ready,
                Reason = result == null ? "OperationUnavailable" : result.Reason,
                Detail = result == null ? string.Empty : result.Detail,
                HostState = result == null ? RuntimeHostState.Unknown.ToString() : result.HostState.ToString(),
                Components = result == null || result.Components == null
                    ? Array.Empty<RuntimeComponentSnapshot>()
                    : result.Components.Select(component => new RuntimeComponentSnapshot
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
