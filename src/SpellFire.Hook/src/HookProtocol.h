#pragma once

#include <windows.h>

namespace SpellFireHookProtocol
{
    constexpr wchar_t ReadyEventPrefix[] = L"Local\\SpellFireHookReady_";
    constexpr wchar_t HeartbeatEventPrefix[] = L"Local\\SpellFireHookHeartbeat_";
    constexpr wchar_t ShutdownEventPrefix[] = L"Local\\SpellFireHookShutdown_";
    constexpr wchar_t CommandEventPrefix[] = L"Local\\SpellFireHookCommand_";
    constexpr wchar_t AckEventPrefix[] = L"Local\\SpellFireHookAck_";
    constexpr wchar_t MappingPrefix[] = L"Local\\SpellFireHookCommandBuffer_";

    constexpr DWORD BufferSize = 256;
    constexpr DWORD Magic = 0x53464850; // SFHP
    constexpr DWORD Version = 1;
    constexpr DWORD HeaderSize = 72;

    namespace Commands
    {
        constexpr LONG Ping = 1;
        constexpr LONG GetHookInfo = 2;
        constexpr LONG ReadSelfModule = 3;
    }

    namespace Status
    {
        constexpr LONG Ok = 0x53464F4B; // SFOK
        constexpr LONG UnsupportedCommand = 0x53464E53; // SFNS
    }

    namespace Results
    {
        constexpr LONG Ping = 0x50494E47; // PING
        constexpr LONG Info = 0x494E464F; // INFO
        constexpr LONG PeRead = 0x50455244; // PERD
    }

    struct CommandBuffer
    {
        volatile LONG Magic;
        volatile LONG Version;
        volatile LONG HeaderSize;
        volatile LONG Command;
        volatile LONG Sequence;
        volatile LONG Status;
        volatile LONG Result;
        volatile LONG PayloadLength;
        volatile LONG PingCount;
        volatile LONG HookProcessId;
        volatile LONG HookProtocolVersion;
        volatile LONG HookStartTick;
        volatile LONG HeartbeatCount;
        volatile LONG ModuleBaseLow;
        volatile LONG DosSignature;
        volatile LONG PeSignature;
        volatile LONG Machine;
        volatile LONG SectionCount;
    };
}
