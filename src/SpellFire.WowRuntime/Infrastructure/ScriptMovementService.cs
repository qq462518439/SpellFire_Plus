using System.Collections.Generic;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Infrastructure
{
    public sealed class ScriptMovementService : IMovementService
    {
        private readonly IScriptService scripts;

        public ScriptMovementService(IScriptService scripts)
        {
            this.scripts = scripts;
        }

        public bool InMovement
        {
            get { return false; }
        }

        public WowRuntimeResult<MovementActionSnapshot> Jump()
        {
            return ExecuteAction("jump", "JumpOrAscendStart(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_JUMP_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> Go(IReadOnlyList<Vector3> points)
        {
            return WowRuntimeResult<MovementActionSnapshot>.Fail(WowRuntimeStatus.FeatureUnavailable, "Path movement is not implemented in the minimal movement layer.");
        }

        public WowRuntimeResult<MovementActionSnapshot> StopMove()
        {
            return ExecuteAction("stop", "MoveForwardStop(); MoveBackwardStop(); StrafeLeftStop(); StrafeRightStop(); AscendStop(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_STOP_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> StopMoveTo()
        {
            return ExecuteAction("stop-to", "MoveForwardStop(); MoveBackwardStop(); StrafeLeftStop(); StrafeRightStop(); AscendStop(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_STOPTO_OK\");");
        }

        private WowRuntimeResult<MovementActionSnapshot> ExecuteAction(string action, string script)
        {
            WowRuntimeResult<ScriptExecutionSnapshot> executed = scripts.Execute(script);
            if (!executed.Success)
            {
                return WowRuntimeResult<MovementActionSnapshot>.Fail(executed.Status, executed.Detail);
            }

            return WowRuntimeResult<MovementActionSnapshot>.Ok(new MovementActionSnapshot(
                action,
                script,
                executed.Value.Reason,
                executed.Value.Detail));
        }
    }
}
