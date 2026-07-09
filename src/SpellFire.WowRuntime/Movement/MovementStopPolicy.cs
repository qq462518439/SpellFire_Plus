namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementStopPolicy
    {
        private readonly IMovementService movement;

        public MovementStopPolicy(IMovementService movement)
        {
            this.movement = movement;
        }

        public bool Stop()
        {
            if (movement == null)
            {
                return false;
            }

            return movement.StopMove().Success;
        }
    }
}
