using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace SpellFire.WowRuntime.Navigation
{
    internal sealed class LocalMeshTileProvider
    {
        private readonly string rootDirectory;

        public LocalMeshTileProvider()
            : this(ResolveDefaultRoot())
        {
        }

        public LocalMeshTileProvider(string rootDirectory)
        {
            this.rootDirectory = rootDirectory ?? string.Empty;
        }

        public RdManagedTileProbe Probe()
        {
            if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                return new RdManagedTileProbe(false, false, rootDirectory, "TileSourceMissing");
            }

            string sample = FindSampleTile();
            if (string.IsNullOrWhiteSpace(sample))
            {
                return new RdManagedTileProbe(true, false, rootDirectory, "TileSourceEmpty");
            }

            byte[] bytes;
            string reason;
            bool ready = TryReadTileFile(sample, Path.GetFileName(sample), out bytes, out reason);
            return new RdManagedTileProbe(true, ready, rootDirectory, ready ? "TileDecodeReady" : reason);
        }

        public bool TryLoadTile(string continent, int x, int y, out byte[] tileBytes, out string reason)
        {
            tileBytes = null;
            reason = string.Empty;

            if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                reason = "TileSourceMissing";
                return false;
            }

            if (string.IsNullOrWhiteSpace(continent))
            {
                reason = "TileContinentMissing";
                return false;
            }

            string tileName = continent + "_" + x + "_" + y + ".mesh.gz";
            string tilePath = Path.Combine(rootDirectory, continent, tileName);
            if (!File.Exists(tilePath))
            {
                reason = "TileMissing Path=\"" + tilePath + "\"";
                return false;
            }

            return TryReadTileFile(tilePath, tileName, out tileBytes, out reason);
        }

        private string FindSampleTile()
        {
            try
            {
                string[] files = Directory.GetFiles(rootDirectory, "*.mesh.gz", SearchOption.AllDirectories);
                return files.Length == 0 ? string.Empty : files[0];
            }
            catch (Exception ex)
            {
                return "ERR:" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private static bool TryReadTileFile(string tilePath, string tileName, out byte[] tileBytes, out string reason)
        {
            tileBytes = null;
            reason = string.Empty;

            if (string.IsNullOrWhiteSpace(tilePath) || tilePath.StartsWith("ERR:", StringComparison.Ordinal))
            {
                reason = string.IsNullOrWhiteSpace(tilePath) ? "TileSampleMissing" : tilePath;
                return false;
            }

            try
            {
                byte[] protectedBytes = File.ReadAllBytes(tilePath);
                string mode;
                tileBytes = TryReadProtectedGzip(protectedBytes, tileName, out mode) ?? TryReadRawGzip(protectedBytes, out mode);

                if (tileBytes == null || tileBytes.Length == 0)
                {
                    reason = "TileDecodedEmpty Mode=\"" + mode + "\" Path=\"" + tilePath + "\"";
                    return false;
                }

                reason = "TileDecoded Mode=\"" + mode + "\"";
                return true;
            }
            catch (Exception ex)
            {
                reason = "TileProtectedOrUnreadable " + ex.GetType().Name + ": " + Clean(ex.Message) + " Path=\"" + tilePath + "\"";
                return false;
            }
        }

        private static byte[] TryReadProtectedGzip(byte[] protectedBytes, string tileName, out string mode)
        {
            mode = "ProtectedGzip";
            try
            {
                byte[] compressed = ProtectedData.Unprotect(
                    protectedBytes,
                    Encoding.UTF8.GetBytes(tileName),
                    DataProtectionScope.LocalMachine);
                return DecompressGzip(compressed);
            }
            catch
            {
                return null;
            }
        }

        private static byte[] TryReadRawGzip(byte[] rawBytes, out string mode)
        {
            mode = "RawGzip";
            try
            {
                return DecompressGzip(rawBytes);
            }
            catch (Exception ex)
            {
                mode = "RawGzipFailed:" + ex.GetType().Name + ":" + Clean(ex.Message);
                return null;
            }
        }

        private static byte[] DecompressGzip(byte[] bytes)
        {
            using (MemoryStream input = new MemoryStream(bytes))
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        private static string Clean(string value)
        {
            return (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Replace('"', '\'');
        }

        private static string ResolveDefaultRoot()
        {
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "Runtime", "WRobot", "Data", "Meshes"),
                @"C:\Users\ASUS\Documents\RZB\Data\Meshes"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(candidates[i]) && Directory.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            return candidates[0];
        }
    }
}
