using System.Collections.Generic;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class UnavailableMovementService : IMovementService
    {
        public bool InMovement
        {
            get { return false; }
        }

        public void Go(IReadOnlyList<Vector3> points)
        {
        }

        public void StopMove()
        {
        }

        public void StopMoveTo()
        {
        }
    }
}
