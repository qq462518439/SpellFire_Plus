using System.Collections.Generic;
using SpellFire.Runtime.Contracts;
using SpellFire.Runtime.Models;
using SpellFire.WowRuntime.Core;
using SpellFire.WowRuntime.Movement;
using SpellFire.WowRuntime.Scripting;
using SpellFire.WowRuntime.World;

namespace SpellFire.WowRuntime.Infrastructure
{
    public sealed class ScriptMovementService : IMovementService
    {
        private readonly IScriptService scripts;
        private readonly IWorldState world;
        private readonly int processId;
        private readonly IRuntimeFacade runtime;

        public ScriptMovementService(IScriptService scripts, IWorldState world, int processId, IRuntimeFacade runtime)
        {
            this.scripts = scripts;
            this.world = world;
            this.processId = processId;
            this.runtime = runtime;
        }

        public WowRuntimeResult<MovementStateSnapshot> GetMovementState()
        {
            return world.GetMovementState();
        }

        public WowRuntimeResult<ClickToMoveDiagnosticSnapshot> GetClickToMoveDiagnostic()
        {
            WowRuntimeResult<WorldPhaseSnapshot> phase = world.GetPhase();
            if (!phase.Success)
            {
                return WowRuntimeResult<ClickToMoveDiagnosticSnapshot>.Fail(phase.Status, phase.Detail);
            }

            WowRuntimeResult<PlayerSnapshot> player = world.GetPlayer();
            if (!player.Success)
            {
                return WowRuntimeResult<ClickToMoveDiagnosticSnapshot>.Fail(player.Status, player.Detail);
            }

            WowRuntimeResult<MovementStateSnapshot> movement = world.GetMovementState();
            if (!movement.Success)
            {
                return WowRuntimeResult<ClickToMoveDiagnosticSnapshot>.Fail(movement.Status, movement.Detail);
            }

            string detail = string.Format(
                "ReadOnly=False CtmMoveKnown=True CtmStopNativeKnown=False ClickToMoveTypeRaw={0} ClickToMoveState={1} SpeedKnown={2} Speed={3:0.###}",
                movement.Value.ClickToMoveTypeRaw,
                movement.Value.ClickToMoveState,
                movement.Value.SpeedKnown,
                movement.Value.Speed);

            return WowRuntimeResult<ClickToMoveDiagnosticSnapshot>.Ok(new ClickToMoveDiagnosticSnapshot(
                phase.Value,
                player.Value,
                movement.Value,
                detail));
        }

        public WowRuntimeResult<MovementActionSnapshot> Jump()
        {
            return ExecuteAction("jump", "JumpOrAscendStart(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_JUMP_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> StartMoveForward()
        {
            return ExecuteAction("forward-start", "MoveForwardStart(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_FORWARD_START_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> StartMoveBackward()
        {
            return ExecuteAction("backward-start", "MoveBackwardStart(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_BACKWARD_START_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> StartStrafeLeft()
        {
            return ExecuteAction("strafe-left-start", "StrafeLeftStart(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_STRAFE_LEFT_START_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> StartStrafeRight()
        {
            return ExecuteAction("strafe-right-start", "StrafeRightStart(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_STRAFE_RIGHT_START_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> StartTurnLeft()
        {
            return ExecuteAction("turn-left-start", "TurnLeftStart(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_TURN_LEFT_START_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> StartTurnRight()
        {
            return ExecuteAction("turn-right-start", "TurnRightStart(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_TURN_RIGHT_START_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> StopTurn()
        {
            return ExecuteAction("turn-stop", "TurnLeftStop(); TurnRightStop(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_TURN_STOP_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> FaceTo(Vector3 point)
        {
            return WowRuntimeResult<MovementActionSnapshot>.Fail(
                WowRuntimeStatus.FeatureUnavailable,
                "Precise facing is not stable enough for the minimal movement layer. TurnLeft/TurnRight remain available as observable rotation primitives.");
        }

        public WowRuntimeResult<MovementActionSnapshot> FaceObject(ulong guid)
        {
            return WowRuntimeResult<MovementActionSnapshot>.Fail(
                WowRuntimeStatus.FeatureUnavailable,
                "Precise object facing is not stable enough for the minimal movement layer. Object position lookup remains available through ObjectManager.");
        }

        public WowRuntimeResult<MovementActionSnapshot> Go(IReadOnlyList<Vector3> points)
        {
            if (points == null || points.Count == 0)
            {
                return WowRuntimeResult<MovementActionSnapshot>.Fail(WowRuntimeStatus.InvalidArgument, "Movement.Go requires at least one target point.");
            }

            if (runtime == null)
            {
                return WowRuntimeResult<MovementActionSnapshot>.Fail(WowRuntimeStatus.FeatureUnavailable, "Runtime facade is not available for ClickToMove.");
            }

            Vector3 target = points[0];
            RuntimeOperationSnapshot operation = runtime.ClickToMoveMove(processId, target.X, target.Y, target.Z, 0, 4, 0.5f);
            if (operation == null)
            {
                return WowRuntimeResult<MovementActionSnapshot>.Fail(WowRuntimeStatus.FeatureUnavailable, "ClickToMove operation did not return a result.");
            }

            if (!operation.Ready)
            {
                return WowRuntimeResult<MovementActionSnapshot>.Fail(MapRuntimeStatus(operation.Reason), operation.Detail);
            }

            return WowRuntimeResult<MovementActionSnapshot>.Ok(new MovementActionSnapshot(
                "go-ctm",
                string.Empty,
                operation.Reason,
                operation.Detail));
        }

        public WowRuntimeResult<MovementActionSnapshot> StopMove()
        {
            return ExecuteAction("stop", "MoveForwardStop(); MoveBackwardStop(); StrafeLeftStop(); StrafeRightStop(); AscendStop(); TurnLeftStop(); TurnRightStop(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_STOP_OK\");");
        }

        public WowRuntimeResult<MovementActionSnapshot> StopMoveTo()
        {
            return ExecuteAction("stop-to", "MoveForwardStop(); MoveBackwardStop(); StrafeLeftStop(); StrafeRightStop(); AscendStop(); TurnLeftStop(); TurnRightStop(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_MOVE_STOPTO_OK\");");
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

        private static WowRuntimeStatus MapRuntimeStatus(string reason)
        {
            if (string.Equals(reason, "ProcessUnavailable", System.StringComparison.OrdinalIgnoreCase))
            {
                return WowRuntimeStatus.ProcessUnavailable;
            }

            if (string.Equals(reason, "InvalidArgument", System.StringComparison.OrdinalIgnoreCase))
            {
                return WowRuntimeStatus.InvalidArgument;
            }

            return WowRuntimeStatus.FeatureUnavailable;
        }

    }
}
