using System;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.MemoryRobot.Models;
using SpellFire.MemoryRobot.Native;

namespace SpellFire.MemoryRobot.Services
{
    public sealed class RemoteExecutionService
    {
        private readonly IMemorySessionFactory sessionFactory;

        public RemoteExecutionService(IMemorySessionFactory sessionFactory)
        {
            this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        }

        public RemoteExecutionResult ProbeAllocation(int processId, int size, MemoryProtection protection)
        {
            try
            {
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    IntPtr address = robot.Allocator.Allocate(size, AllocationType.Commit | AllocationType.Reserve, protection);
                    bool freed = false;
                    try
                    {
                        freed = robot.Allocator.Free(address);
                        return new RemoteExecutionResult
                        {
                            ProcessId = processId,
                            Ready = address != IntPtr.Zero && freed,
                            Reason = address != IntPtr.Zero && freed ? "RemoteAllocationReady" : "RemoteAllocationFailed",
                            Detail = "Address=0x" + address.ToString("X") + " Freed=" + freed,
                            Address = address
                        };
                    }
                    finally
                    {
                        if (address != IntPtr.Zero && !freed)
                        {
                            robot.Allocator.Free(address);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new RemoteExecutionResult
                {
                    ProcessId = processId,
                    Ready = false,
                    Reason = "RemoteAllocationFailed",
                    Detail = ex.GetType().Name + ":" + ex.Message
                };
            }
        }

        public RemoteExecutionResult LoadLibrary(int processId, string libraryPath, int timeoutMilliseconds)
        {
            try
            {
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    int moduleHandle = robot.Libraries.LoadLibrary(libraryPath, timeoutMilliseconds);
                    return new RemoteExecutionResult
                    {
                        ProcessId = processId,
                        Ready = moduleHandle != 0,
                        Reason = moduleHandle != 0 ? "LoadLibrarySucceeded" : "LoadLibraryReturnedZero",
                        Detail = "Path=\"" + libraryPath + "\" ModuleHandle=0x" + moduleHandle.ToString("X"),
                        ModuleHandle = new IntPtr(moduleHandle)
                    };
                }
            }
            catch (Exception ex)
            {
                return new RemoteExecutionResult
                {
                    ProcessId = processId,
                    Ready = false,
                    Reason = "LoadLibraryFailed",
                    Detail = ex.GetType().Name + ":" + ex.Message
                };
            }
        }

        public RemoteExecutionResult FreeLibrary(int processId, IntPtr moduleHandle, int timeoutMilliseconds)
        {
            try
            {
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    bool freed = robot.Libraries.FreeLibrary(moduleHandle, timeoutMilliseconds);
                    return new RemoteExecutionResult
                    {
                        ProcessId = processId,
                        Ready = freed,
                        Reason = freed ? "FreeLibrarySucceeded" : "FreeLibraryReturnedFalse",
                        Detail = "ModuleHandle=0x" + moduleHandle.ToString("X"),
                        ModuleHandle = moduleHandle
                    };
                }
            }
            catch (Exception ex)
            {
                return new RemoteExecutionResult
                {
                    ProcessId = processId,
                    Ready = false,
                    Reason = "FreeLibraryFailed",
                    Detail = ex.GetType().Name + ":" + ex.Message,
                    ModuleHandle = moduleHandle
                };
            }
        }

        public RemoteExecutionResult RunThread(int processId, IntPtr startAddress, IntPtr parameterAddress, int timeoutMilliseconds)
        {
            try
            {
                using (IMemoryRobot robot = sessionFactory.Open(processId))
                {
                    uint exitCode = robot.Threads.Run(startAddress, parameterAddress, timeoutMilliseconds);
                    return new RemoteExecutionResult
                    {
                        ProcessId = processId,
                        Ready = true,
                        Reason = "RemoteThreadSucceeded",
                        Detail = "Start=0x" + startAddress.ToString("X") + " Parameter=0x" + parameterAddress.ToString("X"),
                        ExitCode = exitCode
                    };
                }
            }
            catch (Exception ex)
            {
                return new RemoteExecutionResult
                {
                    ProcessId = processId,
                    Ready = false,
                    Reason = "RemoteThreadFailed",
                    Detail = ex.GetType().Name + ":" + ex.Message
                };
            }
        }
    }
}
