using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SpellFire.WowRuntime.Navigation
{
    public sealed class RdManagedNavigationService : INavigationService
    {
        private readonly string assemblyPath;
        private readonly LocalMeshTileProvider tileProvider;
        private readonly object syncRoot = new object();

        private object rdInstance;
        private Type rdType;
        private MethodInfo addTileMethod;
        private MethodInfo findPathMethod;
        private MethodInfo findZMethod;
        private readonly HashSet<string> loadedTiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public RdManagedNavigationService()
            : this(ResolveDefaultAssemblyPath())
        {
        }

        public RdManagedNavigationService(string assemblyPath)
            : this(assemblyPath, new LocalMeshTileProvider())
        {
        }

        internal RdManagedNavigationService(string assemblyPath, LocalMeshTileProvider tileProvider)
        {
            this.assemblyPath = assemblyPath ?? string.Empty;
            this.tileProvider = tileProvider ?? new LocalMeshTileProvider();
        }

        public NavigationCapabilitySnapshot GetCapability()
        {
            string reason;
            bool assemblyPresent = File.Exists(assemblyPath);
            bool sessionReady = EnsureSession(out reason);
            RdManagedTileProbe tileProbe = tileProvider.Probe();
            bool tileProviderReady = tileProbe.DecodeReady;

            return new NavigationCapabilitySnapshot(
                sessionReady && tileProviderReady,
                false,
                sessionReady && tileProviderReady,
                false,
                false,
                false,
                assemblyPresent,
                sessionReady,
                tileProviderReady,
                "Movement.StopMove/StopMoveTo remain the caller cleanup path; native CTM stop is not proven.",
                sessionReady
                    ? "RDManaged session is ready. TileSourcePresent=" + tileProbe.SourcePresent + " TileProviderReady=" + tileProviderReady + " TileRoot=\"" + tileProbe.SourceRoot + "\" TileReason=\"" + tileProbe.Reason + "\""
                    : "RDManaged session is unavailable: " + reason);
        }

        public PathResult FindPath(PathQuery query)
        {
            if (query == null)
            {
                return new PathResult(PathStatus.RdManagedUnavailable, null, "Path query is null.");
            }

            string continentName = WRobotRecastMath.ResolveContinentName(query.MapId);
            if (string.IsNullOrWhiteSpace(continentName))
            {
                return new PathResult(PathStatus.MapUnavailable, null, "Unsupported map id.");
            }

            string reason;
            if (!EnsureSession(out reason))
            {
                return new PathResult(PathStatus.RdManagedUnavailable, null, reason);
            }

            WRobotRecastMath.GetTileByLocation(query.From, out int fromTileX, out int fromTileY);
            WRobotRecastMath.GetTileByLocation(query.To, out int toTileX, out int toTileY);
            string tileSummary;
            if (!EnsureCoreAndNearbyTiles(continentName, fromTileX, fromTileY, toTileX, toTileY, out tileSummary))
            {
                PathStatus status = tileSummary.IndexOf("TileMissing", StringComparison.Ordinal) >= 0
                    ? PathStatus.TileProviderMissing
                    : PathStatus.TileLoadFailed;

                return new PathResult(
                    status,
                    null,
                    string.Format(
                        "RDManaged is ready, but required tile cannot be loaded. Map={0} Continent={1} FromTile=({2},{3}) ToTile=({4},{5}) TileReason=\"{6}\"",
                        query.MapId,
                        continentName,
                        fromTileX,
                        fromTileY,
                        toTileX,
                        toTileY,
                        tileSummary));
            }

            float[] from = WRobotRecastMath.ToRecast(query.From);
            float[] to = WRobotRecastMath.ToRecast(query.To);
            float[] extent = { 3.5f, 5f, 3.5f };
            object[] args =
            {
                from,
                to,
                extent,
                15000,
                null,
                false
            };

            bool success = (bool)findPathMethod.Invoke(rdInstance, args);
            List<float> raw = args[4] as List<float>;
            bool partial = args[5] is bool && (bool)args[5];
            if (!success || raw == null || raw.Count < 3)
            {
                return new PathResult(
                    PathStatus.NoPath,
                    null,
                    string.Format(
                        "RDManaged FindPath returned no path. Map={0} Continent={1} FromTile=({2},{3}) ToTile=({4},{5}) Success={6} Partial={7} RawCount={8}",
                        query.MapId,
                        continentName,
                        fromTileX,
                        fromTileY,
                        toTileX,
                        toTileY,
                        success,
                        partial,
                        raw == null ? 0 : raw.Count) + " " + tileSummary);
            }

            List<World.Vector3> points = new List<World.Vector3>(raw.Count / 3);
            for (int i = 0; i + 2 < raw.Count; i += 3)
            {
                points.Add(WRobotRecastMath.ToWow(raw[i], raw[i + 1], raw[i + 2]));
            }

            return new PathResult(
                PathStatus.Success,
                points,
                string.Format(
                    "RDManaged FindPath succeeded. Map={0} Continent={1} FromTile=({2},{3}) ToTile=({4},{5}) Partial={6} RawCount={7}",
                    query.MapId,
                    continentName,
                    fromTileX,
                    fromTileY,
                    toTileX,
                    toTileY,
                    partial,
                    raw.Count) + " " + tileSummary);
        }

        public bool TryFindZ(int mapId, float x, float y, float hintZ, out float z)
        {
            z = 0;

            if (string.IsNullOrWhiteSpace(WRobotRecastMath.ResolveContinentName(mapId)))
            {
                return false;
            }

            string reason;
            if (!EnsureSession(out reason))
            {
                return false;
            }

            World.Vector3 position = new World.Vector3(x, y, hintZ);
            WRobotRecastMath.GetTileByLocation(position, out int tileX, out int tileY);
            string continent = WRobotRecastMath.ResolveContinentName(mapId);
            if (!EnsureTile(continent, tileX, tileY, out reason))
            {
                return false;
            }

            float[] recast = WRobotRecastMath.ToRecast(position);
            float[] extent = { 15.5f, 2000f, 15.5f };
            object result = findZMethod.Invoke(rdInstance, new object[] { recast, extent, false });
            z = Convert.ToSingle(result);
            return !float.IsNaN(z) && !float.IsInfinity(z);
        }

        private bool EnsureTile(string continentName, int x, int y, out string reason)
        {
            string key = continentName + ":" + x + ":" + y;
            if (loadedTiles.Contains(key))
            {
                reason = "TileAlreadyLoaded";
                return true;
            }

            byte[] tileBytes;
            if (!tileProvider.TryLoadTile(continentName, x, y, out tileBytes, out reason))
            {
                return false;
            }

            bool added = (bool)addTileMethod.Invoke(rdInstance, new object[] { tileBytes });
            if (!added)
            {
                reason = "RDManagedAddTileFailed Tile=" + key + " Bytes=" + tileBytes.Length;
                return false;
            }

            loadedTiles.Add(key);
            reason = "TileLoaded Tile=" + key + " Bytes=" + tileBytes.Length;
            return true;
        }

        private bool EnsureCoreAndNearbyTiles(string continentName, int fromX, int fromY, int toX, int toY, out string summary)
        {
            string reason;
            if (!EnsureTile(continentName, fromX, fromY, out reason))
            {
                summary = "CoreTileFailed Reason=\"" + reason + "\"";
                return false;
            }

            int requested = 0;
            int loaded = 0;
            int missing = 0;
            int failed = 0;
            for (int x = Math.Min(fromX, toX) - 1; x <= Math.Max(fromX, toX) + 1; x++)
            {
                for (int y = Math.Min(fromY, toY) - 1; y <= Math.Max(fromY, toY) + 1; y++)
                {
                    requested++;
                    if (EnsureTile(continentName, x, y, out reason))
                    {
                        loaded++;
                    }
                    else if (reason.StartsWith("TileMissing", StringComparison.Ordinal))
                    {
                        missing++;
                    }
                    else
                    {
                        failed++;
                    }
                }
            }

            summary = string.Format(
                "TilesRequested={0} TilesReady={1} TilesMissing={2} TilesFailed={3}",
                requested,
                loaded,
                missing,
                failed);
            return true;
        }

        private bool EnsureSession(out string reason)
        {
            lock (syncRoot)
            {
                reason = string.Empty;
                if (rdInstance != null)
                {
                    return true;
                }

                if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
                {
                    reason = "RDManagedAssemblyMissing Path=\"" + assemblyPath + "\"";
                    return false;
                }

                try
                {
                    Assembly assembly = Assembly.LoadFrom(assemblyPath);
                    rdType = assembly.GetType("RDManaged.RD", false);
                    if (rdType == null)
                    {
                        reason = "RDManagedTypeMissing";
                        return false;
                    }

                    Type settingsType = rdType.GetNestedType("RDSettings");
                    if (settingsType == null)
                    {
                        reason = "RDManagedSettingsTypeMissing";
                        return false;
                    }

                    object settings = Activator.CreateInstance(settingsType);
                    SetSetting(settingsType, settings, "OriginX", -17066.666f);
                    SetSetting(settingsType, settings, "OriginY", 0f);
                    SetSetting(settingsType, settings, "OriginZ", -17066.666f);
                    SetSetting(settingsType, settings, "TileWidth", WRobotRecastMath.TileSize);
                    SetSetting(settingsType, settings, "TileHeight", WRobotRecastMath.TileSize);
                    SetSetting(settingsType, settings, "MaxTiles", 4096);
                    SetSetting(settingsType, settings, "MaxPolys", 65535);
                    SetSetting(settingsType, settings, "MaxNodes", 1179630);

                    rdInstance = Activator.CreateInstance(rdType, settings, null);
                    findZMethod = rdType.GetMethod("FindZ", new[]
                    {
                        typeof(float[]),
                        typeof(float[]),
                        typeof(bool)
                    });

                    addTileMethod = rdType.GetMethod("AddTile", new[] { typeof(byte[]) });
                    findPathMethod = rdType.GetMethod("FindPath", new[]
                    {
                        typeof(float[]),
                        typeof(float[]),
                        typeof(float[]),
                        typeof(int),
                        typeof(List<float>).MakeByRefType(),
                        typeof(bool).MakeByRefType()
                    });

                    if (addTileMethod == null || findPathMethod == null || findZMethod == null)
                    {
                        rdInstance = null;
                        reason = "RDManagedMethodMissing";
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    rdInstance = null;
                    reason = "RDManagedSessionCreateFailed " + ex.GetType().Name + ": " + ex.Message;
                    return false;
                }
            }
        }

        private static void SetSetting(Type settingsType, object settings, string name, object value)
        {
            PropertyInfo property = settingsType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                property.SetValue(settings, value, null);
                return;
            }

            FieldInfo field = settingsType.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(settings, value);
            }
        }

        private static string ResolveDefaultAssemblyPath()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
            string root = FindRepositoryRoot(baseDirectory);
            string[] candidates =
            {
                Path.Combine(baseDirectory, "Runtime", "WRobot", "RDManaged.dll"),
                Path.Combine(baseDirectory, "RDManaged.dll"),
                string.IsNullOrWhiteSpace(root) ? string.Empty : Path.Combine(root, "Runtime", "WRobot", "RDManaged.dll"),
                string.IsNullOrWhiteSpace(root) ? string.Empty : Path.Combine(root, "Tools", "WR", "RuntimeWorkbench", "mirror", "OfficialLayout", "Bin", "RDManaged.dll"),
                @"C:\Users\ASUS\Documents\RZB\Bin\RDManaged.dll"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(candidates[i]) && File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return candidates[0];
        }

        private static string FindRepositoryRoot(string start)
        {
            DirectoryInfo current = string.IsNullOrWhiteSpace(start) ? null : new DirectoryInfo(start);
            while (current != null)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")) || Directory.Exists(Path.Combine(current.FullName, "src")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            return string.Empty;
        }
    }
}
