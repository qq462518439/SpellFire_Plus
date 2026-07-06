using System;
using System.IO;

namespace SpellFire.RuntimeHost.Components
{
    internal static class HookPayloadPathResolver
    {
        public static string GetDefaultPayloadPath()
        {
            foreach (string baseDirectory in GetProbeDirectories())
            {
                string sourcePath = Path.Combine(baseDirectory, "SpellFire.Hook.source.dll");
                if (File.Exists(sourcePath))
                {
                    return sourcePath;
                }

                string payloadPath = Path.Combine(baseDirectory, "SpellFire.Hook.dll");
                if (File.Exists(payloadPath))
                {
                    return payloadPath;
                }
            }

            string assemblyDirectory = Path.GetDirectoryName(typeof(HookPayloadPathResolver).Assembly.Location);
            return Path.Combine(assemblyDirectory ?? AppDomain.CurrentDomain.BaseDirectory, "SpellFire.Hook.dll");
        }

        public static string CreateInjectableCopy(int processId)
        {
            string sourcePath = GetDefaultPayloadPath();
            string directory = Path.Combine(
                GetTempRoot(),
                processId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            string targetPath = Path.Combine(directory, "SpellFire.Hook.dll");
            File.Copy(sourcePath, targetPath, true);
            return targetPath;
        }

        public static string GetTempRoot()
        {
            return Path.Combine(Path.GetTempPath(), "SpellFireHookPayloads");
        }

        private static string[] GetProbeDirectories()
        {
            string assemblyDirectory = Path.GetDirectoryName(typeof(HookPayloadPathResolver).Assembly.Location);
            return new[]
            {
                assemblyDirectory,
                AppDomain.CurrentDomain.BaseDirectory,
                Directory.GetCurrentDirectory()
            };
        }
    }
}
