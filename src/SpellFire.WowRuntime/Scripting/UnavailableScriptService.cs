namespace SpellFire.WowRuntime.Scripting
{
    public sealed class UnavailableScriptService : IScriptService
    {
        public bool Execute(string script)
        {
            return false;
        }

        public string GetValue(string expression)
        {
            return string.Empty;
        }
    }
}
