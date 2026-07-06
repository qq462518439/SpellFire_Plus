namespace SpellFire.WowRuntime.World
{
    public struct Vector3
    {
        public Vector3(float x, float y, float z, float rotation = 0)
        {
            X = x;
            Y = y;
            Z = z;
            Rotation = rotation;
        }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public float Rotation { get; }
    }
}
