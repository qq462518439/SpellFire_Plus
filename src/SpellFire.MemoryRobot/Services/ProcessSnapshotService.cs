using System;
using System.Linq;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Models;

namespace SpellFire.MemoryRobot.Services
{
    public sealed class ProcessSnapshotService
    {
        private readonly IMemorySessionFactory sessionFactory;

        public ProcessSnapshotService(IMemorySessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        }

        public ModuleSnapshotResult GetModules(int processId, string moduleName = null)
        {
            try
            {
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    var modules = robot.Modules.GetModules();
                    var matched = string.IsNullOrWhiteSpace(moduleName)
                        ? modules.FirstOrDefault()
                        : modules.FirstOrDefault(module => string.Equals(module.Name, moduleName, StringComparison.OrdinalIgnoreCase));
                    return new ModuleSnapshotResult
                    {
                        ProcessId = processId,
                        Ready = modules.Count > 0,
                        Reason = modules.Count > 0 ? "ModuleSnapshotReady" : "ModuleSnapshotEmpty",
                        Detail = "Count=" + modules.Count + " Match=\"" + (matched?.Name ?? string.Empty) + "\"",
                        Modules = modules,
                        MatchedModule = matched
                    };
                }
            }
            catch (Exception ex)
            {
                return new ModuleSnapshotResult
                {
                    ProcessId = processId,
                    Ready = false,
                    Reason = "ModuleSnapshotFailed",
                    Detail = ex.GetType().Name + ":" + ex.Message,
                    Modules = Array.Empty<Process.ProcessModuleInfo>()
                };
            }
        }

        public MemoryRegionQueryResult QueryRegion(int processId, IntPtr address)
        {
            try
            {
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    bool ok = robot.Regions.TryQuery(address, out var region);
                    return new MemoryRegionQueryResult
                    {
                        ProcessId = processId,
                        Ready = ok,
                        Reason = ok ? "MemoryRegionReady" : "MemoryRegionUnavailable",
                        Detail = "Address=0x" + address.ToString("X"),
                        Region = region
                    };
                }
            }
            catch (Exception ex)
            {
                return new MemoryRegionQueryResult
                {
                    ProcessId = processId,
                    Ready = false,
                    Reason = "MemoryRegionQueryFailed",
                    Detail = ex.GetType().Name + ":" + ex.Message
                };
            }
        }
    }
}
