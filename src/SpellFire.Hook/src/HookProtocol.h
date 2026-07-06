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

    constexpr DWORD BufferSize = 1024;
    constexpr DWORD Magic = 0x53464850; // SFHP
    constexpr DWORD Version = 2;
    constexpr DWORD HeaderSize = 88;

    namespace Commands
    {
        constexpr LONG Ping = 1;
        constexpr LONG GetHookInfo = 2;
        constexpr LONG ReadSelfModule = 3;
        constexpr LONG LuaSmoke = 4;
        constexpr LONG ExecuteLua = 5;
        constexpr LONG ClickToMoveMove = 6;
    }

    namespace Status
    {
        constexpr LONG Ok = 0x53464F4B; // SFOK
        constexpr LONG UnsupportedCommand = 0x53464E53; // SFNS
        constexpr LONG Failed = 0x53464641; // SFFA
    }

    namespace Results
    {
        constexpr LONG Ping = 0x50494E47; // PING
        constexpr LONG Info = 0x494E464F; // INFO
        constexpr LONG PeRead = 0x50455244; // PERD
        constexpr LONG LuaSmoke = 0x4C554153; // LUAS
        constexpr LONG ExecuteLua = 0x45584543; // EXEC
        constexpr LONG ClickToMoveMove = 0x43544D56; // CTMV
    }

    constexpr DWORD ScriptBufferOffset = HeaderSize;
    constexpr DWORD ScriptBufferLength = 512;
    constexpr DWORD ClickToMoveBufferOffset = ScriptBufferOffset;
    constexpr DWORD ClickToMoveBufferLength = 32;
    constexpr DWORD ResultTextOffset = ScriptBufferOffset + ScriptBufferLength;
    constexpr DWORD ResultTextLength = BufferSize - ResultTextOffset;

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
        volatile LONG MainThreadBridgeReady;
        volatile LONG LuaBridgeReady;
        volatile LONG LuaSmokeExecuted;
        volatile LONG LuaSmokeLastStatus;
    };
}
