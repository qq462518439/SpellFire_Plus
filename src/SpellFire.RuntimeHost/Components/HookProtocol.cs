namespace SpellFire.RuntimeHost.Components
{
    internal static class HookProtocol
    {
        public const int BufferSize = 1024;
        public const int Magic = 0x53464850;
        public const int Version = 2;
        public const int HeaderSize = 88;

        public static class Commands
        {
            public const int Ping = 1;
            public const int GetHookInfo = 2;
            public const int ReadSelfModule = 3;
            public const int LuaSmoke = 4;
            public const int ExecuteLua = 5;
        }

        public static class Status
        {
            public const int Ok = 0x53464F4B;
            public const int Failed = 0x53464641;
        }

        public static class Results
        {
            public const int Ping = 0x50494E47;
            public const int Info = 0x494E464F;
            public const int PeRead = 0x50455244;
            public const int LuaSmoke = 0x4C554153;
            public const int ExecuteLua = 0x45584543;
        }

        public const int ScriptBufferOffset = HeaderSize;
        public const int ScriptBufferLength = 512;
        public const int ResultTextOffset = ScriptBufferOffset + ScriptBufferLength;
        public const int ResultTextLength = BufferSize - ResultTextOffset;

        public static class Offsets
        {
            public const int Magic = 0;
            public const int Version = 4;
            public const int HeaderSize = 8;
            public const int Command = 12;
            public const int Sequence = 16;
            public const int Status = 20;
            public const int Result = 24;
            public const int PayloadLength = 28;
            public const int PingCount = 32;
            public const int HookProcessId = 36;
            public const int HookProtocolVersion = 40;
            public const int HookStartTick = 44;
            public const int HeartbeatCount = 48;
            public const int ModuleBaseLow = 52;
            public const int DosSignature = 56;
            public const int PeSignature = 60;
            public const int Machine = 64;
            public const int SectionCount = 68;
            public const int MainThreadBridgeReady = 72;
            public const int LuaBridgeReady = 76;
            public const int LuaSmokeExecuted = 80;
            public const int LuaSmokeLastStatus = 84;
        }
    }

    internal sealed class HookProtocolHeader
    {
        public int Magic { get; set; }

        public int Version { get; set; }

        public int HeaderSize { get; set; }

        public bool IsCompatible
        {
            get
            {
                return Magic == HookProtocol.Magic &&
                       Version == HookProtocol.Version &&
                       HeaderSize >= HookProtocol.HeaderSize;
            }
        }
    }

    public sealed class HookCommandResult
    {
        public bool Ack { get; set; }

        public int Magic { get; set; }

        public int Version { get; set; }

        public int HeaderSize { get; set; }

        public int Status { get; set; }

        public int Result { get; set; }

        public int PayloadLength { get; set; }

        public int PingCount { get; set; }

        public int HookProcessId { get; set; }

        public int HookProtocolVersion { get; set; }

        public int HookStartTick { get; set; }

        public int HeartbeatCount { get; set; }

        public int ModuleBaseLow { get; set; }

        public int DosSignature { get; set; }

        public int PeSignature { get; set; }

        public int Machine { get; set; }

        public int SectionCount { get; set; }

        public bool MainThreadBridgeReady { get; set; }

        public bool LuaBridgeReady { get; set; }

        public bool LuaSmokeExecuted { get; set; }

        public int LuaSmokeLastStatus { get; set; }

        public string TextPayload { get; set; }

        public bool Ready { get; set; }
    }
}
