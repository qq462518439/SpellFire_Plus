namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementStuckResolver
    {
        private readonly IMovementService movement;

        public MovementStuckResolver(IMovementService movement)
        {
            this.movement = movement;
        }

        public bool TryResolve()
        {
            if (movement == null)
            {
                return false;
            }

            movement.StopMove();
            return movement.Jump().Success;
        }
    }
}
