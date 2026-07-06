namespace SpellFire.WowRuntime.Scripting
{
    public interface IScriptService
    {
        bool Execute(string script);

        string GetValue(string expression);
    }
}
