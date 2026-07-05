using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace SpellFire.RuntimeHost.Components
{
    public static class HookPayloadTempCleaner
    {
        public static HookPayloadCleanupResult CleanupInactivePayloads()
        {
            HookPayloadCleanupResult result = new HookPayloadCleanupResult();
            string root = HookPayloadPathResolver.GetTempRoot();
            if (!Directory.Exists(root))
            {
                return result;
            }

            foreach (string processDirectory in Directory.GetDirectories(root))
            {
                string directoryName = Path.GetFileName(processDirectory);
                if (int.TryParse(directoryName, NumberStyles.Integer, CultureInfo.InvariantCulture, out int processId) &&
                    IsProcessRunning(processId))
                {
                    result.SkippedActiveProcessDirectories++;
                    continue;
                }

                CleanupProcessDirectory(processDirectory, result);
            }

            return result;
        }

        private static void CleanupProcessDirectory(string processDirectory, HookPayloadCleanupResult result)
        {
            foreach (string payloadDirectory in Directory.GetDirectories(processDirectory))
            {
                try
                {
                    Directory.Delete(payloadDirectory, true);
                    result.RemovedPayloadDirectories++;
                }
                catch (IOException)
                {
                    result.SkippedLockedDirectories++;
                }
                catch (UnauthorizedAccessException)
                {
                    result.SkippedLockedDirectories++;
                }
            }

            try
            {
                if (Directory.GetFileSystemEntries(processDirectory).Length == 0)
                {
                    Directory.Delete(processDirectory, false);
                    result.RemovedProcessDirectories++;
                }
            }
            catch (IOException)
            {
                result.SkippedLockedDirectories++;
            }
            catch (UnauthorizedAccessException)
            {
                result.SkippedLockedDirectories++;
            }
        }

        private static bool IsProcessRunning(int processId)
        {
            try
            {
                Process.GetProcessById(processId);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
