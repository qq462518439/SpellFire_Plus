using System.Collections.Generic;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Movement
{
    public sealed class MovementPathPointFilter
    {
        public IReadOnlyList<Vector3> Prepare(IReadOnlyList<Vector3> points, Vector3 start, float arrivalDistance, int maxPoints)
        {
            List<Vector3> prepared = new List<Vector3>();
            Vector3 previous = start;
            int limit = points == null ? 0 : System.Math.Min(points.Count, maxPoints);
            double skipDistance = System.Math.Max(0.8, arrivalDistance * 0.75);

            for (int i = 0; i < limit; i++)
            {
                Vector3 point = points[i];
                if (MovementProgressTracker.Distance(previous, point) <= skipDistance)
                {
                    previous = point;
                    continue;
                }

                prepared.Add(point);
                previous = point;
            }

            return prepared;
        }
    }
}
