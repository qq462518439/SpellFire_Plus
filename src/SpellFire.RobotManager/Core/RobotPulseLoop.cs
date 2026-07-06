using System;
using System.Threading;

namespace SpellFire.RobotManager.Core
{
    public sealed class RobotPulseLoop : IDisposable
    {
        private readonly IRobotManager manager;
        private readonly object sync = new object();
        private Thread worker;
        private bool stopRequested;

        public RobotPulseLoop(IRobotManager manager)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            this.manager = manager;
            LastResult = RobotPulseLoopResult.Fail(RobotManagerStatus.Stopped, "Not started.");
        }

        public bool IsRunning { get; private set; }

        public int PulseCount { get; private set; }

        public RobotPulseLoopResult LastResult { get; private set; }

        public RobotPulseLoopResult Start(int intervalMs)
        {
            if (intervalMs <= 0)
            {
                return RobotPulseLoopResult.Fail(RobotManagerStatus.InvalidProduct, "Interval must be positive.");
            }

            lock (sync)
            {
                if (IsRunning)
                {
                    return RobotPulseLoopResult.Fail(RobotManagerStatus.ProductAlreadyRunning, "Pulse loop is already running.");
                }

                stopRequested = false;
                IsRunning = true;
                LastResult = RobotPulseLoopResult.Ok("Started.");
                worker = new Thread(() => Run(intervalMs));
                worker.IsBackground = true;
                worker.Name = "SpellFire Robot PulseLoop";
                worker.Start();
                return LastResult;
            }
        }

        public RobotPulseLoopResult Stop(int timeoutMs)
        {
            Thread thread;
            lock (sync)
            {
                if (!IsRunning && worker == null)
                {
                    LastResult = RobotPulseLoopResult.Fail(RobotManagerStatus.Stopped, "Pulse loop is not running.");
                    return LastResult;
                }

                stopRequested = true;
                thread = worker;
            }

            if (thread != null && thread.IsAlive)
            {
                thread.Join(Math.Max(1, timeoutMs));
            }

            lock (sync)
            {
                IsRunning = false;
                worker = null;
                LastResult = RobotPulseLoopResult.Ok("Stopped.");
                return LastResult;
            }
        }

        public void Dispose()
        {
            Stop(1000);
        }

        private void Run(int intervalMs)
        {
            while (true)
            {
                lock (sync)
                {
                    if (stopRequested)
                    {
                        IsRunning = false;
                        return;
                    }
                }

                RobotManagerResult result = manager.PulseOnce();
                lock (sync)
                {
                    if (!result.Success)
                    {
                        IsRunning = false;
                        stopRequested = true;
                        LastResult = RobotPulseLoopResult.Fail(result.Status, result.Detail);
                        return;
                    }

                    PulseCount++;
                    LastResult = RobotPulseLoopResult.Ok(result.Detail);
                }

                Thread.Sleep(intervalMs);
            }
        }
    }
}
