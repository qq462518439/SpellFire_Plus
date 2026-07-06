using System.Globalization;
using System.Text;

namespace SpellFire.RuntimeHost.Services
{
    public static class RuntimeHostOutputFormatter
    {
        public static string FormatOperation(RuntimeHostOperationResult operation)
        {
            if (operation == null)
            {
                return "ProcessId=0 Operation=\"unknown\" Ready=False Reason=\"OperationUnavailable\" Detail=\"\" HostState=\"Unknown\" Components=0";
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("ProcessId=").Append(operation.ProcessId.ToString(CultureInfo.InvariantCulture));
            builder.Append(" Operation=\"").Append(operation.Operation ?? string.Empty).Append("\"");
            builder.Append(" Ready=").Append(operation.Ready);
            builder.Append(" Reason=\"").Append(operation.Reason ?? string.Empty).Append("\"");
            builder.Append(" Detail=\"").Append(operation.Detail ?? string.Empty).Append("\"");
            builder.Append(" HostState=\"").Append(operation.HostState).Append("\"");
            builder.Append(" Components=").Append((operation.Components == null ? 0 : operation.Components.Count).ToString(CultureInfo.InvariantCulture));
            return builder.ToString();
        }
    }
}
