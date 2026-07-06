using System.Collections.Generic;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public interface IMovementService
    {
        bool InMovement { get; }

        void Go(IReadOnlyList<Vector3> points);

        void StopMove();

        void StopMoveTo();
    }
}
