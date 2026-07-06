using System;
using System.Collections.Generic;
using System.Diagnostics;
using SpellFire.MemoryRobot.Abstractions;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.ObjectManager
{
    public sealed class MemoryObjectManager : IObjectManager
    {
        private readonly int processId;
        private readonly IMemorySessionFactory memorySessions;
        private readonly IWorldAddressProvider addresses;

        public MemoryObjectManager(int processId, IMemorySessionFactory memorySessions, IWorldAddressProvider addresses)
        {
            this.processId = processId;
            this.memorySessions = memorySessions;
            this.addresses = addresses;
        }

        public WowRuntimeResult<WowObjectSnapshot> GetMe()
        {
            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(1);
            if (!snapshot.Success)
            {
                return WowRuntimeResult<WowObjectSnapshot>.Fail(snapshot.Status, snapshot.Detail);
            }

            return snapshot.Value.Me == null
                ? WowRuntimeResult<WowObjectSnapshot>.Fail(WowRuntimeStatus.ObjectNotFound, "Local player object is not available.")
                : WowRuntimeResult<WowObjectSnapshot>.Ok(snapshot.Value.Me);
        }

        public WowRuntimeResult<WowObjectSnapshot> GetTarget()
        {
            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(1);
            if (!snapshot.Success)
            {
                return WowRuntimeResult<WowObjectSnapshot>.Fail(snapshot.Status, snapshot.Detail);
            }

            return snapshot.Value.Target == null
                ? WowRuntimeResult<WowObjectSnapshot>.Fail(WowRuntimeStatus.ObjectNotFound, "Target object is not available.")
                : WowRuntimeResult<WowObjectSnapshot>.Ok(snapshot.Value.Target);
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetObjects(int limit)
        {
            if (limit <= 0)
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Limit must be greater than zero.");
            }

            WowRuntimeStatus status;
            string detail;
            WorldAddressTable table;
            if (!TryPrepareObjectManager(out table, out status, out detail))
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(status, detail);
            }

            return WowRuntimeResult<ObjectManagerSnapshot>.Fail(
                WowRuntimeStatus.FeatureUnavailable,
                "Object manager address model is present but object list decoding is not implemented yet.");
        }

        public WowRuntimeResult<WowObjectSnapshot> GetObjectByGuid(ulong guid)
        {
            if (guid == 0)
            {
                return WowRuntimeResult<WowObjectSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Guid must be non-zero.");
            }

            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(1024);
            if (!snapshot.Success)
            {
                return WowRuntimeResult<WowObjectSnapshot>.Fail(snapshot.Status, snapshot.Detail);
            }

            foreach (WowObjectSnapshot item in snapshot.Value.Objects)
            {
                if (item.Guid == guid)
                {
                    return WowRuntimeResult<WowObjectSnapshot>.Ok(item);
                }
            }

            return WowRuntimeResult<WowObjectSnapshot>.Fail(WowRuntimeStatus.ObjectNotFound, "Object was not found by guid.");
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetObjectsByEntry(int entry, int limit)
        {
            if (entry <= 0)
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Entry must be greater than zero.");
            }

            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(limit);
            if (!snapshot.Success)
            {
                return snapshot;
            }

            List<WowObjectSnapshot> matches = new List<WowObjectSnapshot>();
            foreach (WowObjectSnapshot item in snapshot.Value.Objects)
            {
                if (item.Entry == entry)
                {
                    matches.Add(item);
                }
            }

            return WowRuntimeResult<ObjectManagerSnapshot>.Ok(
                new ObjectManagerSnapshot(snapshot.Value.Me, snapshot.Value.Target, matches, limit));
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjects(Vector3 center, float radius, int limit)
        {
            if (radius < 0)
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Radius must be zero or greater.");
            }

            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(limit);
            if (!snapshot.Success)
            {
                return snapshot;
            }

            float radiusSquared = radius * radius;
            List<WowObjectSnapshot> matches = new List<WowObjectSnapshot>();
            foreach (WowObjectSnapshot item in snapshot.Value.Objects)
            {
                if (DistanceSquared(center, item.Position) <= radiusSquared)
                {
                    matches.Add(item);
                }
            }

            return WowRuntimeResult<ObjectManagerSnapshot>.Ok(
                new ObjectManagerSnapshot(snapshot.Value.Me, snapshot.Value.Target, matches, limit));
        }

        private bool TryPrepareObjectManager(out WorldAddressTable table, out WowRuntimeStatus status, out string detail)
        {
            table = null;
            status = WowRuntimeStatus.Ready;
            detail = string.Empty;

            if (!ProcessExists(processId))
            {
                status = WowRuntimeStatus.ProcessUnavailable;
                detail = "Target process is not available.";
                return false;
            }

            table = addresses.GetAddressTable(processId);
            if (table == null || !table.HasObjectManager)
            {
                status = WowRuntimeStatus.AddressTableMissing;
                detail = "Object manager address table is not available.";
                return false;
            }

            try
            {
                using (IMemoryRobot robot = memorySessions.Open(processId))
                {
                    if (!robot.Session.IsOpen)
                    {
                        status = WowRuntimeStatus.ProcessUnavailable;
                        detail = "Memory session is not open.";
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                status = WowRuntimeStatus.ReadFailed;
                detail = ex.Message;
                return false;
            }

            return true;
        }

        private static bool ProcessExists(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                using (process)
                {
                    return !process.HasExited;
                }
            }
            catch
            {
                return false;
            }
        }

        private static float DistanceSquared(Vector3 left, Vector3 right)
        {
            float dx = left.X - right.X;
            float dy = left.Y - right.Y;
            float dz = left.Z - right.Z;
            return (dx * dx) + (dy * dy) + (dz * dz);
        }
    }
}
