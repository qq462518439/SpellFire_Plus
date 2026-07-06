using System.Collections.Generic;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Navigation
{
    public sealed class PathResult
    {
        public PathResult(PathStatus status, IReadOnlyList<Vector3> points, string detail)
        {
            Status = status;
            Points = points ?? new Vector3[0];
            Detail = detail ?? string.Empty;
        }

        public bool Success
        {
            get { return Status == PathStatus.Success; }
        }

        public PathStatus Status { get; }

        public IReadOnlyList<Vector3> Points { get; }

        public string Detail { get; }
    }
}
