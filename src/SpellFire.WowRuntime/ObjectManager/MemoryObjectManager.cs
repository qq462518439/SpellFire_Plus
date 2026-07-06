using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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

        public WowRuntimeResult<ObjectManagerDiagnosticSnapshot> GetDiagnostic(int scanLimit)
        {
            if (scanLimit <= 0)
            {
                return WowRuntimeResult<ObjectManagerDiagnosticSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Scan limit must be greater than zero.");
            }

            bool processExists = ProcessExists(processId);
            if (!processExists)
            {
                return WowRuntimeResult<ObjectManagerDiagnosticSnapshot>.Ok(new ObjectManagerDiagnosticSnapshot(
                    false,
                    false,
                    false,
                    "Process",
                    "Target process is not available.",
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    string.Empty,
                    string.Empty));
            }

            WorldAddressTable table = addresses.GetAddressTable(processId);
            bool addressTableReady = table != null && table.HasObjectManager;
            if (!addressTableReady)
            {
                return WowRuntimeResult<ObjectManagerDiagnosticSnapshot>.Ok(new ObjectManagerDiagnosticSnapshot(
                    true,
                    false,
                    false,
                    "AddressTable",
                    "Object manager address table is not available.",
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    string.Empty,
                    string.Empty));
            }

            try
            {
                using (IMemoryRobot robot = memorySessions.Open(processId))
                {
                    if (!robot.Session.IsOpen)
                    {
                        return WowRuntimeResult<ObjectManagerDiagnosticSnapshot>.Ok(new ObjectManagerDiagnosticSnapshot(
                            true,
                            false,
                            true,
                            "Session",
                            "Memory session is not open.",
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            string.Empty,
                            string.Empty));
                    }

                    return WowRuntimeResult<ObjectManagerDiagnosticSnapshot>.Ok(ReadDiagnostic(robot, table, scanLimit));
                }
            }
            catch (Exception ex)
            {
                return WowRuntimeResult<ObjectManagerDiagnosticSnapshot>.Fail(WowRuntimeStatus.ReadFailed, ex.Message);
            }
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetObjects(int limit)
        {
            return GetObjects(limit, GetDefaultScanLimit());
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetObjects(int limit, int scanLimit)
        {
            if (limit <= 0)
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Limit must be greater than zero.");
            }
            if (scanLimit <= 0)
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Scan limit must be greater than zero.");
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
                    ObjectManagerSnapshot snapshot = ReadSnapshot(robot, table, limit, scanLimit);
                    if (snapshot.LocalGuid == 0 && snapshot.Scanned == 0)
                    {
                        return WowRuntimeResult<ObjectManagerSnapshot>.Fail(
                            WowRuntimeStatus.ObjectManagerUnavailable,
                            "Object manager is not ready or the character is not in world.");
                    }

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
            return GetObjectsByEntry(entry, limit, GetDefaultScanLimit());
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetObjectsByEntry(int entry, int limit, int scanLimit)
        {
            if (entry <= 0)
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Entry must be greater than zero.");
            }

            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(scanLimit, scanLimit);
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
                new ObjectManagerSnapshot(snapshot.Value.Me, snapshot.Value.Target, Take(matches, limit), limit, snapshot.Value.LocalGuid, snapshot.Value.TargetGuid, snapshot.Value.Scanned, snapshot.Value.SnapshotUtc));
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetObjectsByKind(ObjectKind kind, int limit)
        {
            return GetObjectsByKind(kind, limit, GetDefaultScanLimit());
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetObjectsByKind(ObjectKind kind, int limit, int scanLimit)
        {
            if (!Enum.IsDefined(typeof(ObjectKind), kind))
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Object kind is not supported.");
            }

            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(scanLimit, scanLimit);
            if (!snapshot.Success)
            {
                return snapshot;
            }

            return WowRuntimeResult<ObjectManagerSnapshot>.Ok(SortByDistance(FilterByKind(snapshot.Value, kind, scanLimit), limit));
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjects(Vector3 center, float radius, int limit)
        {
            return GetNearbyObjects(center, radius, limit, GetDefaultScanLimit());
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjects(Vector3 center, float radius, int limit, int scanLimit)
        {
            if (radius < 0)
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Radius must be zero or greater.");
            }

            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetObjects(scanLimit, scanLimit);
            if (!snapshot.Success)
            {
                return snapshot;
            }

            Vector3 actualCenter = center;
            if (IsZeroVector(center) && snapshot.Value.Me != null)
            {
                actualCenter = snapshot.Value.Me.Position;
            }

            float radiusSquared = radius * radius;
            List<WowObjectSnapshot> matches = new List<WowObjectSnapshot>();
            foreach (WowObjectSnapshot item in snapshot.Value.Objects)
            {
                if (DistanceSquared(actualCenter, item.Position) <= radiusSquared)
                {
                    matches.Add(item);
                }
            }

            matches.Sort(CompareDistanceThenGuid);
            return WowRuntimeResult<ObjectManagerSnapshot>.Ok(
                new ObjectManagerSnapshot(snapshot.Value.Me, snapshot.Value.Target, Take(matches, limit), limit, snapshot.Value.LocalGuid, snapshot.Value.TargetGuid, snapshot.Value.Scanned, snapshot.Value.SnapshotUtc));
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjectsByKind(ObjectKind kind, Vector3 center, float radius, int limit)
        {
            return GetNearbyObjectsByKind(kind, center, radius, limit, GetDefaultScanLimit());
        }

        public WowRuntimeResult<ObjectManagerSnapshot> GetNearbyObjectsByKind(ObjectKind kind, Vector3 center, float radius, int limit, int scanLimit)
        {
            if (!Enum.IsDefined(typeof(ObjectKind), kind))
            {
                return WowRuntimeResult<ObjectManagerSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Object kind is not supported.");
            }

            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetNearbyObjects(center, radius, limit, scanLimit);
            if (!snapshot.Success)
            {
                return snapshot;
            }

            return WowRuntimeResult<ObjectManagerSnapshot>.Ok(SortByDistance(FilterByKind(snapshot.Value, kind, scanLimit), limit));
        }

        public WowRuntimeResult<WowObjectSnapshot> GetNearestObject(Vector3 center, float radius)
        {
            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetNearbyObjects(center, radius, GetDefaultScanLimit());
            if (!snapshot.Success)
            {
                return WowRuntimeResult<WowObjectSnapshot>.Fail(snapshot.Status, snapshot.Detail);
            }

            return FirstOrNotFound(snapshot.Value, "Nearest object is not available.");
        }

        public WowRuntimeResult<WowObjectSnapshot> GetNearestObjectByKind(ObjectKind kind, Vector3 center, float radius)
        {
            WowRuntimeResult<ObjectManagerSnapshot> snapshot = GetNearbyObjectsByKind(kind, center, radius, GetDefaultScanLimit());
            if (!snapshot.Success)
            {
                return WowRuntimeResult<WowObjectSnapshot>.Fail(snapshot.Status, snapshot.Detail);
            }

            return FirstOrNotFound(snapshot.Value, "Nearest object by kind is not available.");
        }

        private static WowRuntimeResult<WowObjectSnapshot> FirstOrNotFound(ObjectManagerSnapshot snapshot, string detail)
        {
            if (snapshot.Objects.Count == 0)
            {
                return WowRuntimeResult<WowObjectSnapshot>.Fail(WowRuntimeStatus.ObjectNotFound, detail);
            }

            return WowRuntimeResult<WowObjectSnapshot>.Ok(snapshot.Objects[0]);
        }

        private static ObjectManagerSnapshot FilterByKind(ObjectManagerSnapshot snapshot, ObjectKind kind, int limit)
        {
            List<WowObjectSnapshot> matches = new List<WowObjectSnapshot>();
            foreach (WowObjectSnapshot item in snapshot.Objects)
            {
                if (item.Kind == kind)
                {
                    matches.Add(item);
                }
            }

            return new ObjectManagerSnapshot(snapshot.Me, snapshot.Target, Take(matches, limit), limit, snapshot.LocalGuid, snapshot.TargetGuid, snapshot.Scanned, snapshot.SnapshotUtc);
        }

        private static ObjectManagerSnapshot SortByDistance(ObjectManagerSnapshot snapshot, int limit)
        {
            List<WowObjectSnapshot> sorted = new List<WowObjectSnapshot>(snapshot.Objects);
            sorted.Sort(CompareDistanceThenGuid);
            return new ObjectManagerSnapshot(snapshot.Me, snapshot.Target, Take(sorted, limit), limit, snapshot.LocalGuid, snapshot.TargetGuid, snapshot.Scanned, snapshot.SnapshotUtc);
        }

        private static IReadOnlyList<WowObjectSnapshot> Take(List<WowObjectSnapshot> source, int limit)
        {
            if (source.Count <= limit)
            {
                return source;
            }

            return source.Take(limit).ToList();
        }

        private static int CompareDistanceThenGuid(WowObjectSnapshot left, WowObjectSnapshot right)
        {
            int distance = left.DistanceFromMe.CompareTo(right.DistanceFromMe);
            if (distance != 0)
            {
                return distance;
            }

            return left.Guid.CompareTo(right.Guid);
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

        private static ObjectManagerSnapshot ReadSnapshot(IMemoryRobot robot, WorldAddressTable table, int limit, int scanLimit)
        {
            uint clientConnection = ReadUInt32(robot, table.ObjectManager);
            if (clientConnection == 0)
            {
                return new ObjectManagerSnapshot(null, null, Array.Empty<WowObjectSnapshot>(), limit, 0, 0, 0);
            }

            uint objectManager = ReadUInt32(robot, Add(clientConnection, 0x2ED0));
            if (objectManager == 0)
            {
                return new ObjectManagerSnapshot(null, null, Array.Empty<WowObjectSnapshot>(), limit, 0, 0, 0);
            }

            ulong localGuid = ReadUInt64(robot, table.LocalGuid);
            ulong targetGuid = ReadUInt64(robot, table.TargetGuid);
            uint current = ReadUInt32(robot, Add(objectManager, table.FirstObject.ToInt32()));
            HashSet<uint> visited = new HashSet<uint>();
            List<WowObjectSnapshot> objects = new List<WowObjectSnapshot>();
            int scanned = 0;

            int maxScan = Math.Min(table.ScanLimit, Math.Max(scanLimit, 1));
            for (int i = 0; i < maxScan && current != 0; i++)
            {
                if (!visited.Add(current))
                {
                    break;
                }

                scanned++;
                WowObjectSnapshot item;
                if (TryReadObject(robot, table, current, out item))
                {
                    objects.Add(item);
                }

                try
                {
                    current = ReadUInt32(robot, Add(current, table.NextObjectOffset.ToInt32()));
                }
                catch
                {
                    break;
                }
            }

            WowObjectSnapshot me = objects.FirstOrDefault(item => item.Guid == localGuid);
            if (me != null && string.IsNullOrEmpty(me.Name))
            {
                string localName = ReadLocalPlayerName(robot);
                if (!string.IsNullOrEmpty(localName))
                {
                    int meIndex = objects.FindIndex(item => item.Guid == localGuid);
                    me = CopyWithName(me, localName);
                    if (meIndex >= 0)
                    {
                        objects[meIndex] = me;
                    }
                }
            }

            List<WowObjectSnapshot> enrichedObjects = EnrichDistances(objects, me);
            WowObjectSnapshot enrichedMe = enrichedObjects.FirstOrDefault(item => item.Guid == localGuid);
            WowObjectSnapshot target = enrichedObjects.FirstOrDefault(item => item.Guid == targetGuid);
            return new ObjectManagerSnapshot(enrichedMe, target, enrichedObjects, limit, localGuid, targetGuid, scanned);
        }

        private static ObjectManagerDiagnosticSnapshot ReadDiagnostic(IMemoryRobot robot, WorldAddressTable table, int scanLimit)
        {
            uint clientConnection = 0;
            uint objectManager = 0;
            ulong localGuid = 0;
            ulong targetGuid = 0;
            uint firstObject = 0;
            int scanned = 0;
            int readableObjects = 0;
            int failedObjects = 0;
            uint firstFailedObject = 0;
            string firstFailedStage = string.Empty;
            string firstFailedDetail = string.Empty;

            try
            {
                clientConnection = ReadUInt32(robot, table.ObjectManager);
            }
            catch (Exception ex)
            {
                return Diagnostic("ClientConnection", ex.Message, clientConnection, objectManager, localGuid, targetGuid, firstObject, scanned, readableObjects, failedObjects, firstFailedObject, firstFailedStage, firstFailedDetail);
            }

            if (clientConnection == 0)
            {
                return Diagnostic("ClientConnection", "Client connection is zero.", clientConnection, objectManager, localGuid, targetGuid, firstObject, scanned, readableObjects, failedObjects, firstFailedObject, firstFailedStage, firstFailedDetail);
            }

            try
            {
                objectManager = ReadUInt32(robot, Add(clientConnection, 0x2ED0));
            }
            catch (Exception ex)
            {
                return Diagnostic("ObjectManager", ex.Message, clientConnection, objectManager, localGuid, targetGuid, firstObject, scanned, readableObjects, failedObjects, firstFailedObject, firstFailedStage, firstFailedDetail);
            }

            if (objectManager == 0)
            {
                return Diagnostic("ObjectManager", "Object manager is zero.", clientConnection, objectManager, localGuid, targetGuid, firstObject, scanned, readableObjects, failedObjects, firstFailedObject, firstFailedStage, firstFailedDetail);
            }

            try
            {
                localGuid = ReadUInt64(robot, table.LocalGuid);
                targetGuid = ReadUInt64(robot, table.TargetGuid);
                firstObject = ReadUInt32(robot, Add(objectManager, table.FirstObject.ToInt32()));
            }
            catch (Exception ex)
            {
                return Diagnostic("ObjectManagerHeader", ex.Message, clientConnection, objectManager, localGuid, targetGuid, firstObject, scanned, readableObjects, failedObjects, firstFailedObject, firstFailedStage, firstFailedDetail);
            }

            HashSet<uint> visited = new HashSet<uint>();
            uint current = firstObject;
            int maxScan = Math.Min(table.ScanLimit, Math.Max(scanLimit, 1));
            for (int i = 0; i < maxScan && current != 0; i++)
            {
                if (!visited.Add(current))
                {
                    return Diagnostic("CycleDetected", "Object linked list cycle detected.", clientConnection, objectManager, localGuid, targetGuid, firstObject, scanned, readableObjects, failedObjects, firstFailedObject, firstFailedStage, firstFailedDetail);
                }

                scanned++;
                string failedStage;
                string failedDetail;
                if (TryReadObjectDiagnostic(robot, table, current, out failedStage, out failedDetail))
                {
                    readableObjects++;
                }
                else
                {
                    failedObjects++;
                    if (firstFailedObject == 0)
                    {
                        firstFailedObject = current;
                        firstFailedStage = failedStage;
                        firstFailedDetail = failedDetail;
                    }
                }

                try
                {
                    current = ReadUInt32(robot, Add(current, table.NextObjectOffset.ToInt32()));
                }
                catch (Exception ex)
                {
                    return Diagnostic("NextObject", ex.Message, clientConnection, objectManager, localGuid, targetGuid, firstObject, scanned, readableObjects, failedObjects, firstFailedObject, firstFailedStage, firstFailedDetail);
                }
            }

            return Diagnostic("Complete", "Diagnostic completed.", clientConnection, objectManager, localGuid, targetGuid, firstObject, scanned, readableObjects, failedObjects, firstFailedObject, firstFailedStage, firstFailedDetail);
        }

        private static ObjectManagerDiagnosticSnapshot Diagnostic(
            string stage,
            string detail,
            uint clientConnection,
            uint objectManager,
            ulong localGuid,
            ulong targetGuid,
            uint firstObject,
            int scanned,
            int readableObjects,
            int failedObjects,
            uint firstFailedObject,
            string firstFailedStage,
            string firstFailedDetail)
        {
            return new ObjectManagerDiagnosticSnapshot(
                true,
                true,
                true,
                stage,
                detail,
                clientConnection,
                objectManager,
                localGuid,
                targetGuid,
                firstObject,
                scanned,
                readableObjects,
                failedObjects,
                firstFailedObject,
                firstFailedStage,
                firstFailedDetail);
        }

        private static bool TryReadObjectDiagnostic(IMemoryRobot robot, WorldAddressTable table, uint baseAddress, out string failedStage, out string failedDetail)
        {
            failedStage = string.Empty;
            failedDetail = string.Empty;

            try
            {
                ulong guid = ReadUInt64(robot, Add(baseAddress, table.ObjectGuidOffset.ToInt32()));
                if (guid == 0)
                {
                    failedStage = "Guid";
                    failedDetail = "Guid is zero.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                failedStage = "Guid";
                failedDetail = ex.Message;
                return false;
            }

            int type;
            try
            {
                type = ReadInt32(robot, Add(baseAddress, table.ObjectTypeOffset.ToInt32()));
                if (type < 0 || type > 7)
                {
                    failedStage = "Type";
                    failedDetail = "Object type is outside expected range.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                failedStage = "Type";
                failedDetail = ex.Message;
                return false;
            }

            try
            {
                ReadPosition(robot, table, baseAddress, MapKind(type));
            }
            catch (Exception ex)
            {
                failedStage = "Position";
                failedDetail = ex.Message;
                return false;
            }

            return true;
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
                string name = ReadName(robot, kind, baseAddress, guid);
                if (kind == ObjectKind.Player)
                {
                    snapshot = new WowPlayerSnapshot(guid, entry, name, position, true, true, false, 0, baseAddress, 0);
                }
                else if (kind == ObjectKind.Unit)
                {
                    snapshot = new WowUnitSnapshot(guid, entry, name, kind, position, true, true, false, 0, baseAddress, 0);
                }
                else if (kind == ObjectKind.GameObject)
                {
                    snapshot = new WowGameObjectSnapshot(guid, entry, name, position, true, baseAddress, 0);
                }
                else
                {
                    snapshot = new WowObjectSnapshot(guid, entry, name, kind, position, true, baseAddress, 0);
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

        private static string ReadName(IMemoryRobot robot, ObjectKind kind, uint baseAddress, ulong guid)
        {
            try
            {
                if (kind == ObjectKind.GameObject)
                {
                    return ReadGameObjectName(robot, baseAddress);
                }

                if (kind == ObjectKind.Unit)
                {
                    return ReadUnitName(robot, baseAddress);
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        private static string ReadGameObjectName(IMemoryRobot robot, uint baseAddress)
        {
            uint info = ReadUInt32(robot, Add(baseAddress, 420));
            if (info == 0)
            {
                return string.Empty;
            }

            uint nameAddress = ReadUInt32(robot, Add(info, 144));
            return ReadStringUtf8(robot, nameAddress, 80);
        }

        private static string ReadUnitName(IMemoryRobot robot, uint baseAddress)
        {
            uint dbCacheRow = ReadUInt32(robot, Add(baseAddress, 2404));
            if (dbCacheRow == 0)
            {
                return string.Empty;
            }

            uint nameAddress = ReadUInt32(robot, Add(dbCacheRow, 92));
            return ReadStringUtf8(robot, nameAddress, 80);
        }

        private static string ReadLocalPlayerName(IMemoryRobot robot)
        {
            return ReadStringUtf8(robot, 0x00C79D98, 80);
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

        private static List<WowObjectSnapshot> EnrichDistances(List<WowObjectSnapshot> objects, WowObjectSnapshot me)
        {
            if (me == null)
            {
                return objects;
            }

            List<WowObjectSnapshot> enriched = new List<WowObjectSnapshot>(objects.Count);
            foreach (WowObjectSnapshot item in objects)
            {
                enriched.Add(CopyWithDistance(item, Distance(me.Position, item.Position)));
            }

            return enriched;
        }

        private static WowObjectSnapshot CopyWithDistance(WowObjectSnapshot item, float distance)
        {
            WowPlayerSnapshot player = item as WowPlayerSnapshot;
            if (player != null)
            {
                return new WowPlayerSnapshot(player.Guid, player.Entry, player.Name, player.Position, player.IsValid, player.IsAlive, player.InCombat, player.TargetGuid, player.BaseAddress, distance);
            }

            WowUnitSnapshot unit = item as WowUnitSnapshot;
            if (unit != null)
            {
                return new WowUnitSnapshot(unit.Guid, unit.Entry, unit.Name, unit.Kind, unit.Position, unit.IsValid, unit.IsAlive, unit.InCombat, unit.TargetGuid, unit.BaseAddress, distance);
            }

            WowGameObjectSnapshot gameObject = item as WowGameObjectSnapshot;
            if (gameObject != null)
            {
                return new WowGameObjectSnapshot(gameObject.Guid, gameObject.Entry, gameObject.Name, gameObject.Position, gameObject.IsValid, gameObject.BaseAddress, distance);
            }

            return new WowObjectSnapshot(item.Guid, item.Entry, item.Name, item.Kind, item.Position, item.IsValid, item.BaseAddress, distance);
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

        private static string ReadStringUtf8(IMemoryRobot robot, uint address, int maxBytes)
        {
            if (address == 0 || maxBytes <= 0)
            {
                return string.Empty;
            }

            byte[] buffer = robot.Reader.ReadBytes(new IntPtr(unchecked((int)address)), maxBytes);
            int length = 0;
            while (length < buffer.Length && buffer[length] != 0)
            {
                length++;
            }

            if (length == 0)
            {
                return string.Empty;
            }

            return Encoding.UTF8.GetString(buffer, 0, length).Trim();
        }

        private static WowObjectSnapshot CopyWithName(WowObjectSnapshot item, string name)
        {
            WowPlayerSnapshot player = item as WowPlayerSnapshot;
            if (player != null)
            {
                return new WowPlayerSnapshot(player.Guid, player.Entry, name, player.Position, player.IsValid, player.IsAlive, player.InCombat, player.TargetGuid, player.BaseAddress, player.DistanceFromMe);
            }

            WowUnitSnapshot unit = item as WowUnitSnapshot;
            if (unit != null)
            {
                return new WowUnitSnapshot(unit.Guid, unit.Entry, name, unit.Kind, unit.Position, unit.IsValid, unit.IsAlive, unit.InCombat, unit.TargetGuid, unit.BaseAddress, unit.DistanceFromMe);
            }

            WowGameObjectSnapshot gameObject = item as WowGameObjectSnapshot;
            if (gameObject != null)
            {
                return new WowGameObjectSnapshot(gameObject.Guid, gameObject.Entry, name, gameObject.Position, gameObject.IsValid, gameObject.BaseAddress, gameObject.DistanceFromMe);
            }

            return new WowObjectSnapshot(item.Guid, item.Entry, name, item.Kind, item.Position, item.IsValid, item.BaseAddress, item.DistanceFromMe);
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

        private static float Distance(Vector3 left, Vector3 right)
        {
            return (float)Math.Sqrt(DistanceSquared(left, right));
        }

        private static bool IsZeroVector(Vector3 value)
        {
            return Math.Abs(value.X) < 0.001f &&
                   Math.Abs(value.Y) < 0.001f &&
                   Math.Abs(value.Z) < 0.001f;
        }
    }
}
