using SpellFire.WowRuntime.Core;

namespace SpellFire.WowRuntime.Scripting
{
    public interface IScriptService
    {
        WowRuntimeResult<ScriptExecutionSnapshot> LuaSmoke();

        WowRuntimeResult<ScriptExecutionSnapshot> Execute(string script);
    }
}
