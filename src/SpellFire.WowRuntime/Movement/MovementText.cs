using System.Globalization;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    internal static class MovementText
    {
        public static string FormatVector(Vector3 value)
        {
            return "(" +
                   value.X.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.Y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                   value.Z.ToString("0.###", CultureInfo.InvariantCulture) + ")";
        }
    }
}
