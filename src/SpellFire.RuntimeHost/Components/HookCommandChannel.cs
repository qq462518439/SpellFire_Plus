using System;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;

namespace SpellFire.RuntimeHost.Components
{
    internal sealed class HookCommandChannel : IDisposable
    {
        private readonly MemoryMappedFile mapping;
        private readonly MemoryMappedViewAccessor accessor;
        private readonly EventWaitHandle commandEvent;
        private readonly EventWaitHandle ackEvent;

        public HookCommandChannel(int processId)
        {
            mapping = MemoryMappedFile.OpenExisting("Local\\SpellFireHookCommandBuffer_" + processId, MemoryMappedFileRights.ReadWrite);
            accessor = mapping.CreateViewAccessor(0, HookProtocol.BufferSize, MemoryMappedFileAccess.ReadWrite);
            commandEvent = EventWaitHandle.OpenExisting("Local\\SpellFireHookCommand_" + processId);
            ackEvent = EventWaitHandle.OpenExisting("Local\\SpellFireHookAck_" + processId);
        }

        public HookCommandResult Ping(int timeoutMilliseconds)
        {
            HookCommandResult result = Execute(HookProtocol.Commands.Ping, timeoutMilliseconds);
            result.Ready = result.Ready && result.Result == HookProtocol.Results.Ping;
            return result;
        }

        public HookCommandResult GetHookInfo(int timeoutMilliseconds)
        {
            HookCommandResult result = Execute(HookProtocol.Commands.GetHookInfo, timeoutMilliseconds);
            result.Ready = result.Ready && result.Result == HookProtocol.Results.Info;
            return result;
        }

        public HookCommandResult ReadSelfModule(int timeoutMilliseconds)
        {
            HookCommandResult result = Execute(HookProtocol.Commands.ReadSelfModule, timeoutMilliseconds);
            result.Ready = result.Ready &&
                           result.Result == HookProtocol.Results.PeRead &&
                           result.DosSignature == 0x5A4D &&
                           result.PeSignature == 0x4550 &&
                           result.Machine == 0x14C &&
                           result.SectionCount > 0;
            return result;
        }

        public HookCommandResult LuaSmoke(int timeoutMilliseconds)
        {
            HookCommandResult result = Execute(HookProtocol.Commands.LuaSmoke, timeoutMilliseconds);
            result.Ready = result.Ready &&
                           result.Result == HookProtocol.Results.LuaSmoke &&
                           result.MainThreadBridgeReady &&
                           result.LuaBridgeReady &&
                           result.LuaSmokeExecuted;
            return result;
        }

        public HookCommandResult ExecuteLua(string script, int timeoutMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                throw new ArgumentException("Lua script must not be empty.", nameof(script));
            }

            byte[] scriptBytes = Encoding.ASCII.GetBytes(script + "\0");
            if (scriptBytes.Length > HookProtocol.ScriptBufferLength)
            {
                throw new ArgumentException("Lua script exceeds protocol buffer length.", nameof(script));
            }

            accessor.WriteArray(HookProtocol.ScriptBufferOffset, scriptBytes, 0, scriptBytes.Length);
            if (scriptBytes.Length < HookProtocol.ScriptBufferLength)
            {
                byte[] clearTail = new byte[HookProtocol.ScriptBufferLength - scriptBytes.Length];
                accessor.WriteArray(HookProtocol.ScriptBufferOffset + scriptBytes.Length, clearTail, 0, clearTail.Length);
            }

            HookCommandResult result = Execute(HookProtocol.Commands.ExecuteLua, timeoutMilliseconds);
            result.Ready = result.Ready &&
                           result.Result == HookProtocol.Results.ExecuteLua &&
                           result.MainThreadBridgeReady &&
                           result.LuaBridgeReady &&
                           result.LuaSmokeExecuted;
            return result;
        }

        private HookCommandResult Execute(int command, int timeoutMilliseconds)
        {
            HookProtocolHeader header = ReadHeader();
            accessor.Write(HookProtocol.Offsets.Command, 0);
            accessor.Write(HookProtocol.Offsets.Sequence, Environment.TickCount);
            accessor.Write(HookProtocol.Offsets.Status, 0);
            accessor.Write(HookProtocol.Offsets.Result, 0);
            accessor.Write(HookProtocol.Offsets.PayloadLength, 0);
            ClearResultText();
            ackEvent.Reset();
            accessor.Write(HookProtocol.Offsets.Command, command);
            commandEvent.Set();

            bool ack = ackEvent.WaitOne(Math.Max(0, timeoutMilliseconds));
            int status = accessor.ReadInt32(HookProtocol.Offsets.Status);
            int result = accessor.ReadInt32(HookProtocol.Offsets.Result);
            int payloadLength = accessor.ReadInt32(HookProtocol.Offsets.PayloadLength);
            int pingCount = accessor.ReadInt32(HookProtocol.Offsets.PingCount);
            string textPayload = ReadNullTerminatedAscii(HookProtocol.ResultTextOffset, HookProtocol.ResultTextLength);
            return new HookCommandResult
            {
                Ack = ack,
                Magic = header.Magic,
                Version = header.Version,
                HeaderSize = header.HeaderSize,
                Status = status,
                Result = result,
                PayloadLength = payloadLength,
                PingCount = pingCount,
                HookProcessId = accessor.ReadInt32(HookProtocol.Offsets.HookProcessId),
                HookProtocolVersion = accessor.ReadInt32(HookProtocol.Offsets.HookProtocolVersion),
                HookStartTick = accessor.ReadInt32(HookProtocol.Offsets.HookStartTick),
                HeartbeatCount = accessor.ReadInt32(HookProtocol.Offsets.HeartbeatCount),
                ModuleBaseLow = accessor.ReadInt32(HookProtocol.Offsets.ModuleBaseLow),
                DosSignature = accessor.ReadInt32(HookProtocol.Offsets.DosSignature),
                PeSignature = accessor.ReadInt32(HookProtocol.Offsets.PeSignature),
                Machine = accessor.ReadInt32(HookProtocol.Offsets.Machine),
                SectionCount = accessor.ReadInt32(HookProtocol.Offsets.SectionCount),
                MainThreadBridgeReady = accessor.ReadInt32(HookProtocol.Offsets.MainThreadBridgeReady) != 0,
                LuaBridgeReady = accessor.ReadInt32(HookProtocol.Offsets.LuaBridgeReady) != 0,
                LuaSmokeExecuted = accessor.ReadInt32(HookProtocol.Offsets.LuaSmokeExecuted) != 0,
                LuaSmokeLastStatus = accessor.ReadInt32(HookProtocol.Offsets.LuaSmokeLastStatus),
                TextPayload = textPayload,
                Ready = header.IsCompatible && ack && status == HookProtocol.Status.Ok
            };
        }

        private string ReadNullTerminatedAscii(int offset, int length)
        {
            byte[] buffer = new byte[length];
            accessor.ReadArray(offset, buffer, 0, buffer.Length);
            int end = Array.IndexOf(buffer, (byte)0);
            if (end < 0)
            {
                end = buffer.Length;
            }

            return Encoding.ASCII.GetString(buffer, 0, end);
        }

        private void ClearResultText()
        {
            byte[] clear = new byte[HookProtocol.ResultTextLength];
            accessor.WriteArray(HookProtocol.ResultTextOffset, clear, 0, clear.Length);
        }

        private HookProtocolHeader ReadHeader()
        {
            return new HookProtocolHeader
            {
                Magic = accessor.ReadInt32(HookProtocol.Offsets.Magic),
                Version = accessor.ReadInt32(HookProtocol.Offsets.Version),
                HeaderSize = accessor.ReadInt32(HookProtocol.Offsets.HeaderSize)
            };
        }

        public void Dispose()
        {
            accessor.Dispose();
            mapping.Dispose();
            commandEvent.Dispose();
            ackEvent.Dispose();
        }
    }

}
