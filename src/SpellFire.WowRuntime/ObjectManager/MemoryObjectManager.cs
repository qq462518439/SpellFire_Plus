using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(GetDefaultScanLimit());
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
            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(GetDefaultScanLimit());
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

            try
            {
                using (IMemoryRobot robot = memorySessions.Open(processId))
                {
                    ObjectManagerSnapshot snapshot = ReadSnapshot(robot, table, limit);
                    return WowRuntimeResult<ObjectManagerSnapshot>.Ok(snapshot);
                }
            }
            catch (Exception ex)
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.ReadFailed, ex.Message);
            }
        }

        public WowRuntimeResult<WowObjectSnapshot> GetObjectByGuid(ulong guid)
        {
            if (guid == 0)
            {
                return WowRuntimeResult<WowObjectSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Guid must be non-zero.");
            }

            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(GetDefaultScanLimit());
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

        private int GetDefaultScanLimit()
        {
            WorldAddressTable table = addresses.GetAddressTable(processId);
            return table == null || table.ScanLimit <= 0 ? 512 : table.ScanLimit;
        }

        private static ObjectManagerSnapshot ReadSnapshot(IMemoryRobot robot, WorldAddressTable table, int limit)
        {
            uint clientConnection = ReadUInt32(robot, table.ObjectManager);
            if (clientConnection == 0)
            {
                return new ObjectManagerSnapshot(null, null, Array.Empty<WowObjectSnapshot>(), limit);
            }

            uint objectManager = ReadUInt32(robot, Add(clientConnection, 0x2ED0));
            if (objectManager == 0)
            {
                return new ObjectManagerSnapshot(null, null, Array.Empty<WowObjectSnapshot>(), limit);
            }

            ulong localGuid = ReadUInt64(robot, table.LocalGuid);
            ulong targetGuid = ReadUInt64(robot, table.TargetGuid);
            uint current = ReadUInt32(robot, Add(objectManager, table.FirstObject.ToInt32()));
            HashSet<uint> visited = new HashSet<uint>();
            List<WowObjectSnapshot> objects = new List<WowObjectSnapshot>();

            int maxScan = Math.Min(table.ScanLimit, Math.Max(limit, 1));
            for (int i = 0; i < maxScan && current != 0; i++)
            {
                if (!visited.Add(current))
                {
                    break;
                }

                WowObjectSnapshot item;
                if (TryReadObject(robot, table, current, out item))
                {
                    objects.Add(item);
                }

                current = ReadUInt32(robot, Add(current, table.NextObjectOffset.ToInt32()));
            }

            WowObjectSnapshot me = objects.FirstOrDefault(item => item.Guid == localGuid);
            WowObjectSnapshot target = objects.FirstOrDefault(item => item.Guid == targetGuid);
            return new ObjectManagerSnapshot(me, target, objects, limit);
        }

        private static bool TryReadObject(IMemoryRobot robot, WorldAddressTable table, uint baseAddress, out WowObjectSnapshot snapshot)
        {
            snapshot = null;
            try
            {
                ulong guid = ReadUInt64(robot, Add(baseAddress, table.ObjectGuidOffset.ToInt32()));
                int type = ReadInt32(robot, Add(baseAddress, table.ObjectTypeOffset.ToInt32()));
                if (guid == 0 || type < 0 || type > 7)
                {
                    return false;
                }

                ObjectKind kind = MapKind(type);
                int entry = 0;
                if (table.ObjectEntryOffset != IntPtr.Zero)
                {
                    uint descriptor = ReadUInt32(robot, Add(baseAddress, table.ObjectEntryOffset.ToInt32()));
                    if (descriptor != 0)
                    {
                        entry = ReadInt32(robot, Add(descriptor, 0x8));
                    }
                }

                Vector3 position = ReadPosition(robot, table, baseAddress, kind);
                if (kind == ObjectKind.Player)
                {
                    snapshot = new WowPlayerSnapshot(guid, entry, string.Empty, position, true, true, false, 0);
                }
                else if (kind == ObjectKind.Unit)
                {
                    snapshot = new WowUnitSnapshot(guid, entry, string.Empty, kind, position, true, true, false, 0);
                }
                else if (kind == ObjectKind.GameObject)
                {
                    snapshot = new WowGameObjectSnapshot(guid, entry, string.Empty, position, true);
                }
                else
                {
                    snapshot = new WowObjectSnapshot(guid, entry, string.Empty, kind, position, true);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Vector3 ReadPosition(IMemoryRobot robot, WorldAddressTable table, uint baseAddress, ObjectKind kind)
        {
            int offset = kind == ObjectKind.GameObject
                ? table.GameObjectPositionOffset.ToInt32()
                : table.UnitPositionOffset.ToInt32();

            float x = robot.Reader.Read<float>(Add(baseAddress, offset));
            float y = robot.Reader.Read<float>(Add(baseAddress, offset + 4));
            float z = robot.Reader.Read<float>(Add(baseAddress, offset + 8));
            float rotation = 0;
            if (kind != ObjectKind.GameObject)
            {
                rotation = robot.Reader.Read<float>(Add(baseAddress, offset + 16));
            }

            return new Vector3(x, y, z, rotation);
        }

        private static ObjectKind MapKind(int rawType)
        {
            switch (rawType)
            {
                case 1:
                    return ObjectKind.Item;
                case 2:
                    return ObjectKind.Container;
                case 3:
                    return ObjectKind.Unit;
                case 4:
                    return ObjectKind.Player;
                case 5:
                    return ObjectKind.GameObject;
                case 6:
                    return ObjectKind.DynamicObject;
                case 7:
                    return ObjectKind.Corpse;
                default:
                    return ObjectKind.Object;
            }
        }

        private static IntPtr Add(uint address, int offset)
        {
            return new IntPtr(unchecked((int)(address + (uint)offset)));
        }

        private static uint ReadUInt32(IMemoryRobot robot, IntPtr address)
        {
            return unchecked((uint)robot.Reader.Read<int>(address));
        }

        private static ulong ReadUInt64(IMemoryRobot robot, IntPtr address)
        {
            return unchecked((ulong)robot.Reader.Read<long>(address));
        }

        private static int ReadInt32(IMemoryRobot robot, IntPtr address)
        {
            return robot.Reader.Read<int>(address);
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
