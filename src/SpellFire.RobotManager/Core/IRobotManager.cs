using SpellFire.WowRuntime.Core;

namespace SpellFire.RobotManager.Core
{
    public interface IRobotManager
    {
        IProduct CurrentProduct { get; }

        ProductState State { get; }

        RobotManagerResult Start(IWowRuntime runtime, IProduct product);

        RobotManagerResult PulseOnce();

        RobotManagerResult Pause();

        RobotManagerResult Resume();

        RobotManagerResult Stop();
    }
}
