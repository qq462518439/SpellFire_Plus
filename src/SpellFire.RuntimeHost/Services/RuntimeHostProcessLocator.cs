using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SpellFire.RuntimeHost.Services
{
    public static class RuntimeHostProcessLocator
    {
        public static bool TryFindLatestWow(out int processId, out string label)
        {
            processId = 0;
            label = string.Empty;
            Process process = Process.GetProcessesByName("Wow")
                .OrderByDescending(item => item.StartTime)
                .FirstOrDefault();
            if (process == null)
            {
                return false;
            }

            processId = process.Id;
            label = process.ProcessName + " [PID:" + process.Id.ToString(CultureInfo.InvariantCulture) + "]";
            return true;
        }
    }
}
