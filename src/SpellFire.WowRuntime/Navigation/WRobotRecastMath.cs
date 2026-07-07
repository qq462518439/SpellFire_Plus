using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Navigation
{
    internal static class WRobotRecastMath
    {
        public const float TileSize = 533.3333f;

        private const float OriginX = -17066.666f;
        private const float OriginZ = -17066.666f;

        public static float[] ToRecast(Vector3 wow)
        {
            return new[]
            {
                -wow.Y,
                wow.Z,
                -wow.X
            };
        }

        public static Vector3 ToWow(float x, float y, float z)
        {
            return new Vector3(-z, -x, y);
        }

        public static void GetTileByLocation(Vector3 wow, out int x, out int y)
        {
            float[] recast = ToRecast(wow);
            x = (int)((recast[0] - OriginX) / TileSize);
            y = (int)((recast[2] - OriginZ) / TileSize);
        }

        public static string ResolveContinentName(int mapId)
        {
            switch (mapId)
            {
                case 0:
                    return "Azeroth";
                case 1:
                    return "Kalimdor";
                case 530:
                    return "Expansion01";
                case 571:
                    return "Northrend";
                default:
                    return string.Empty;
            }
        }
    }
}
