using System;

namespace SpellFire.WowRuntime.World
{
    [Flags]
    public enum MovementFlags
    {
        None = 0,
        Moving = 1,
        Flying = 2,
        Mounted = 4,
        Swimming = 8,
        Falling = 16
    }
}
