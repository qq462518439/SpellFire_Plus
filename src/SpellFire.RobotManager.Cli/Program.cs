using System;
using SpellFire.RobotManager.Core;
using SpellFire.RobotManager.Testing;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Infrastructure;

namespace SpellFire.RobotManager.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string command = GetArg(args, "--command") ?? "noop-lifecycle";
            int processId = ParseInt(GetArg(args, "--pid"));

            if (command != "noop-lifecycle" && command != "state-machine-hardening" && command != "timed-pulse-loop" && command != "context-probe-product")
            {
                Console.WriteLine("Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"UnknownCommand\" Detail=\"Unsupported command.\"", Escape(command), processId);
                return 1;
            }

            if (processId <= 0)
            {
                Console.WriteLine("Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"InvalidArgument\" Detail=\"PID must be positive.\"", Escape(command), processId);
                return 1;
            }

            try
            {
                IWowRuntime runtime = new WowRuntimeFactory().Create(processId);
                if (command == "state-machine-hardening")
                {
                    return RunStateMachineHardening(command, processId, runtime);
                }

                if (command == "timed-pulse-loop")
                {
                    return RunTimedPulseLoop(command, processId, runtime);
                }

                if (command == "context-probe-product")
                {
                    return RunContextProbeProduct(command, processId, runtime);
                }

                return RunNoopLifecycle(command, processId, runtime);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Result=Fail Command=\"{0}\" ProcessId={1} Ready=False Reason=\"Exception\" Detail=\"{2}: {3}\"", Escape(command), processId, Escape(ex.GetType().Name), Escape(ex.Message));
                return 1;
            }
        }

        private static int RunNoopLifecycle(string command, int processId, IWowRuntime runtime)
        {
            var manager = new RobotManager.Core.RobotManager();
            var product = new NoopProduct();

            RobotManagerResult start = manager.Start(runtime, product);
            RobotManagerResult pulse1 = manager.PulseOnce();
            RobotManagerResult pause = manager.Pause();
            RobotManagerResult pausedPulse = manager.PulseOnce();
            RobotManagerResult resume = manager.Resume();
            RobotManagerResult pulse2 = manager.PulseOnce();
            RobotManagerResult stop = manager.Stop();
            RobotManagerResult afterStopPulse = manager.PulseOnce();

            bool ready =
                start.Success &&
                pulse1.Success &&
                pause.Success &&
                !pausedPulse.Success &&
                pausedPulse.Status == RobotManagerStatus.Paused &&
                resume.Success &&
                pulse2.Success &&
                stop.Success &&
                !afterStopPulse.Success &&
                afterStopPulse.Status == RobotManagerStatus.NoProduct &&
                product.PulseCount == 2 &&
                product.State == ProductState.Stopped;

            Console.WriteLine(
                "Result={0} Command=\"{1}\" ProcessId={2} Ready={3} Reason=\"{4}\" Product=\"{5}\" ProductState={6} PulseCount={7} Start={8} Pulse1={9} Pause={10} PausedPulse={11} Resume={12} Pulse2={13} Stop={14} AfterStopPulse={15}",
                ready ? "OK" : "Fail",
                Escape(command),
                processId,
                ready,
                ready ? "LifecycleVerified" : "LifecycleFailed",
                Escape(product.Name),
                product.State,
                product.PulseCount,
                Format(start),
                Format(pulse1),
                Format(pause),
                Format(pausedPulse),
                Format(resume),
                Format(pulse2),
                Format(stop),
                Format(afterStopPulse));

            return ready ? 0 : 1;
        }

        private static int RunStateMachineHardening(string command, int processId, IWowRuntime runtime)
        {
            var manager = new RobotManager.Core.RobotManager();
            var first = new NoopProduct("FirstProduct");
            var second = new NoopProduct("SecondProduct");

            RobotManagerResult start = manager.Start(runtime, first);
            RobotManagerResult duplicateStart = manager.Start(runtime, second);
            RobotManagerResult stop = manager.Stop();
            RobotManagerResult duplicateStop = manager.Stop();

            var faultManager = new RobotManager.Core.RobotManager();
            var throwing = new ThrowingProduct("pulse");
            RobotManagerResult faultStart = faultManager.Start(runtime, throwing);
            RobotManagerResult faultPulse = faultManager.PulseOnce();
            RobotManagerResult faultedPulse = faultManager.PulseOnce();
            RobotManagerResult faultedPause = faultManager.Pause();
            RobotManagerResult faultedResume = faultManager.Resume();
            RobotManagerResult faultedStop = faultManager.Stop();
            RobotManagerResult afterFaultStopPulse = faultManager.PulseOnce();

            bool ready =
                start.Success &&
                !duplicateStart.Success &&
                duplicateStart.Status == RobotManagerStatus.ProductAlreadyRunning &&
                stop.Success &&
                !duplicateStop.Success &&
                duplicateStop.Status == RobotManagerStatus.NoProduct &&
                faultStart.Success &&
                !faultPulse.Success &&
                faultPulse.Status == RobotManagerStatus.ProductError &&
                throwing.State == ProductState.Stopped &&
                !faultedPulse.Success &&
                faultedPulse.Status == RobotManagerStatus.Faulted &&
                !faultedPause.Success &&
                faultedPause.Status == RobotManagerStatus.Faulted &&
                !faultedResume.Success &&
                faultedResume.Status == RobotManagerStatus.Faulted &&
                faultedStop.Success &&
                !afterFaultStopPulse.Success &&
                afterFaultStopPulse.Status == RobotManagerStatus.NoProduct;

            Console.WriteLine(
                "Result={0} Command=\"{1}\" ProcessId={2} Ready={3} Reason=\"{4}\" DuplicateStart={5} DuplicateStop={6} FaultPulse={7} FaultedPulse={8} FaultedPause={9} FaultedResume={10} FaultedStop={11} AfterFaultStopPulse={12} FaultProductState={13}",
                ready ? "OK" : "Fail",
                Escape(command),
                processId,
                ready,
                ready ? "StateMachineHardened" : "StateMachineFailed",
                Format(duplicateStart),
                Format(duplicateStop),
                Format(faultPulse),
                Format(faultedPulse),
                Format(faultedPause),
                Format(faultedResume),
                Format(faultedStop),
                Format(afterFaultStopPulse),
                throwing.State);

            return ready ? 0 : 1;
        }

        private static int RunTimedPulseLoop(string command, int processId, IWowRuntime runtime)
        {
            var manager = new RobotManager.Core.RobotManager();
            var product = new NoopProduct("TimedPulseProduct");
            RobotManagerResult startProduct = manager.Start(runtime, product);

            using (var loop = new RobotPulseLoop(manager))
            {
                RobotPulseLoopResult startLoop = loop.Start(50);
                System.Threading.Thread.Sleep(260);
                RobotPulseLoopResult stopLoop = loop.Stop(1000);
                RobotManagerResult stopProduct = manager.Stop();

                bool ready =
                    startProduct.Success &&
                    startLoop.Success &&
                    stopLoop.Success &&
                    stopProduct.Success &&
                    loop.PulseCount >= 3 &&
                    product.PulseCount >= 3 &&
                    !loop.IsRunning &&
                    product.State == ProductState.Stopped;

                Console.WriteLine(
                    "Result={0} Command=\"{1}\" ProcessId={2} Ready={3} Reason=\"{4}\" Product=\"{5}\" ProductState={6} LoopRunning={7} LoopPulseCount={8} ProductPulseCount={9} LastPulseUtcTicks={10} StartProduct={11} StartLoop={12} StopLoop={13} StopProduct={14}",
                    ready ? "OK" : "Fail",
                    Escape(command),
                    processId,
                    ready,
                    ready ? "TimedPulseLoopVerified" : "TimedPulseLoopFailed",
                    Escape(product.Name),
                    product.State,
                    loop.IsRunning,
                    loop.PulseCount,
                    product.PulseCount,
                    product.LastPulseUtcTicks,
                    Format(startProduct),
                    Format(startLoop),
                    Format(stopLoop),
                    Format(stopProduct));

                return ready ? 0 : 1;
            }
        }

        private static int RunContextProbeProduct(string command, int processId, IWowRuntime runtime)
        {
            var manager = new RobotManager.Core.RobotManager();
            var product = new ContextProbeProduct();

            RobotManagerResult start = manager.Start(runtime, product);
            RobotManagerResult pulse = manager.PulseOnce();
            RobotManagerResult stop = manager.Stop();

            bool ready =
                start.Success &&
                pulse.Success &&
                stop.Success &&
                product.PulseCount == 1 &&
                product.ScriptExecuted &&
                product.SnapshotAttempted &&
                product.State == ProductState.Stopped;

            Console.WriteLine(
                "Result={0} Command=\"{1}\" ProcessId={2} Ready={3} Reason=\"{4}\" Product=\"{5}\" ProductState={6} PulseCount={7} ScriptExecuted={8} ScriptReason=\"{9}\" SnapshotAttempted={10} SnapshotSucceeded={11} SnapshotStatus=\"{12}\" InWorld={13} HasPlayer={14} ObjectCount={15} Start={16} Pulse={17} Stop={18}",
                ready ? "OK" : "Fail",
                Escape(command),
                processId,
                ready,
                ready ? "ContextProbeVerified" : "ContextProbeFailed",
                Escape(product.Name),
                product.State,
                product.PulseCount,
                product.ScriptExecuted,
                Escape(product.ScriptReason),
                product.SnapshotAttempted,
                product.SnapshotSucceeded,
                Escape(product.SnapshotStatus),
                product.InWorld,
                product.HasPlayer,
                product.ObjectCount,
                Format(start),
                Format(pulse),
                Format(stop));

            return ready ? 0 : 1;
        }

        private static string Format(RobotPulseLoopResult result)
        {
            return string.Format("{0}:{1}", result.Success, result.Status);
        }

        private static string Format(RobotManagerResult result)
        {
            return string.Format("{0}:{1}", result.Success, result.Status);
        }

        private static int ParseInt(string value)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? parsed : 0;
        }

        private static string GetArg(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
