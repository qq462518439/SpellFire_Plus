using System;
using System.IO.MemoryMappedFiles;
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

        private HookCommandResult Execute(int command, int timeoutMilliseconds)
        {
            HookProtocolHeader header = ReadHeader();
            accessor.Write(HookProtocol.Offsets.Command, 0);
            accessor.Write(HookProtocol.Offsets.Sequence, Environment.TickCount);
            accessor.Write(HookProtocol.Offsets.Status, 0);
            accessor.Write(HookProtocol.Offsets.Result, 0);
            accessor.Write(HookProtocol.Offsets.PayloadLength, 0);
            ackEvent.Reset();
            accessor.Write(HookProtocol.Offsets.Command, command);
            commandEvent.Set();

            bool ack = ackEvent.WaitOne(Math.Max(0, timeoutMilliseconds));
            int status = accessor.ReadInt32(HookProtocol.Offsets.Status);
            int result = accessor.ReadInt32(HookProtocol.Offsets.Result);
            int payloadLength = accessor.ReadInt32(HookProtocol.Offsets.PayloadLength);
            int pingCount = accessor.ReadInt32(HookProtocol.Offsets.PingCount);
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
                Ready = header.IsCompatible && ack && status == HookProtocol.Status.Ok
            };
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
