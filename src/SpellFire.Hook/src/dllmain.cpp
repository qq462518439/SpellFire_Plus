#include <windows.h>
#include "HookProtocol.h"

namespace
{
    namespace Protocol = SpellFireHookProtocol;

    constexpr DWORD WowFrameScriptExecute = 0x819210;
    constexpr DWORD WowDirect3DDevice = 0xC5DF88;
    constexpr DWORD WowDirect3DDeviceVTableOffset = 0x397C;
    constexpr DWORD EndSceneVTableIndex = 42;
    constexpr LONG LuaSmokeIdle = 0;
    constexpr LONG LuaSmokePending = 1;
    constexpr LONG LuaSmokeDone = 2;
    constexpr LONG LuaSmokeFailed = 3;
    constexpr LONG LuaStatusDeviceMissing = 0x2001;
    constexpr LONG LuaStatusVTablePointerMissing = 0x2002;
    constexpr LONG LuaStatusVTableMissing = 0x2003;
    constexpr LONG LuaStatusProtectFailed = 0x2004;
    constexpr LONG LuaStatusTimeout = 0x2005;

    using EndSceneFn = HRESULT(WINAPI*)(void* device);
    using FrameScriptExecuteFn = int(__cdecl*)(const char* command, int a1, int a2);

    HANDLE g_readyEvent = nullptr;
    HANDLE g_heartbeatEvent = nullptr;
    HANDLE g_shutdownEvent = nullptr;
    HANDLE g_commandEvent = nullptr;
    HANDLE g_ackEvent = nullptr;
    HANDLE g_mapping = nullptr;
    HANDLE g_workerThread = nullptr;
    volatile LONG g_pingCount = 0;
    volatile LONG g_heartbeatCount = 0;
    DWORD g_startTick = 0;
    HMODULE g_moduleHandle = nullptr;

    Protocol::CommandBuffer* g_commandBuffer = nullptr;
    EndSceneFn g_originalEndScene = nullptr;
    void** g_endSceneSlot = nullptr;
    volatile LONG g_mainThreadBridgeReady = 0;
    volatile LONG g_luaBridgeReady = 0;
    volatile LONG g_luaSmokeState = LuaSmokeIdle;
    volatile LONG g_luaSmokeLastStatus = 0;
    volatile LONG g_luaSmokeExecuted = 0;

    void BuildEventName(wchar_t* buffer, DWORD bufferLength, const wchar_t* prefix)
    {
        wsprintfW(buffer, L"%s%lu", prefix, GetCurrentProcessId());
        UNREFERENCED_PARAMETER(bufferLength);
    }

    void WriteCapabilityFields()
    {
        if (g_commandBuffer == nullptr)
        {
            return;
        }

        InterlockedExchange(&g_commandBuffer->MainThreadBridgeReady, InterlockedCompareExchange(&g_mainThreadBridgeReady, 0, 0));
        InterlockedExchange(&g_commandBuffer->LuaBridgeReady, InterlockedCompareExchange(&g_luaBridgeReady, 0, 0));
        InterlockedExchange(&g_commandBuffer->LuaSmokeExecuted, InterlockedCompareExchange(&g_luaSmokeExecuted, 0, 0));
        InterlockedExchange(&g_commandBuffer->LuaSmokeLastStatus, InterlockedCompareExchange(&g_luaSmokeLastStatus, 0, 0));
    }

    void CompleteCommand(LONG status, LONG result, LONG payloadLength)
    {
        InterlockedExchange(&g_commandBuffer->Status, status);
        InterlockedExchange(&g_commandBuffer->Result, result);
        InterlockedExchange(&g_commandBuffer->PayloadLength, payloadLength);
        WriteCapabilityFields();
        InterlockedExchange(&g_commandBuffer->Command, 0);
        if (g_ackEvent != nullptr)
        {
            SetEvent(g_ackEvent);
        }
    }

    void ExecutePendingLuaSmoke()
    {
        if (InterlockedCompareExchange(&g_luaSmokeState, LuaSmokePending, LuaSmokePending) != LuaSmokePending)
        {
            return;
        }

        FrameScriptExecuteFn execute = reinterpret_cast<FrameScriptExecuteFn>(WowFrameScriptExecute);
        const char* script = "JumpOrAscendStart(); DEFAULT_CHAT_FRAME:AddMessage(\"SPELLFIRE_LUA_OK\");";
        __try
        {
            int status = execute(script, 0, 0);
            InterlockedExchange(&g_luaSmokeLastStatus, status);
            InterlockedExchange(&g_luaSmokeExecuted, 1);
            InterlockedExchange(&g_luaSmokeState, LuaSmokeDone);
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            InterlockedExchange(&g_luaSmokeLastStatus, static_cast<LONG>(GetExceptionCode()));
            InterlockedExchange(&g_luaSmokeExecuted, 0);
            InterlockedExchange(&g_luaSmokeState, LuaSmokeFailed);
        }
    }

    HRESULT WINAPI EndSceneHook(void* device)
    {
        ExecutePendingLuaSmoke();
        return g_originalEndScene(device);
    }

    bool InstallEndSceneSlotHook()
    {
        if (g_originalEndScene != nullptr)
        {
            return true;
        }

        __try
        {
            void** device = *reinterpret_cast<void***>(WowDirect3DDevice);
            if (device == nullptr)
            {
                InterlockedExchange(&g_luaSmokeLastStatus, LuaStatusDeviceMissing);
                return false;
            }

            void** vtablePointer = *reinterpret_cast<void***>(
                reinterpret_cast<BYTE*>(device) + WowDirect3DDeviceVTableOffset);
            if (vtablePointer == nullptr)
            {
                InterlockedExchange(&g_luaSmokeLastStatus, LuaStatusVTablePointerMissing);
                return false;
            }

            void** vtable = *reinterpret_cast<void***>(vtablePointer);
            if (vtable == nullptr)
            {
                InterlockedExchange(&g_luaSmokeLastStatus, LuaStatusVTableMissing);
                return false;
            }

            g_endSceneSlot = &vtable[EndSceneVTableIndex];
            DWORD oldProtect = 0;
            if (!VirtualProtect(g_endSceneSlot, sizeof(void*), PAGE_EXECUTE_READWRITE, &oldProtect))
            {
                InterlockedExchange(&g_luaSmokeLastStatus, LuaStatusProtectFailed);
                return false;
            }

            g_originalEndScene = reinterpret_cast<EndSceneFn>(*g_endSceneSlot);
            *g_endSceneSlot = reinterpret_cast<void*>(&EndSceneHook);
            FlushInstructionCache(GetCurrentProcess(), g_endSceneSlot, sizeof(void*));
            DWORD ignored = 0;
            VirtualProtect(g_endSceneSlot, sizeof(void*), oldProtect, &ignored);

            InterlockedExchange(&g_mainThreadBridgeReady, 1);
            InterlockedExchange(&g_luaBridgeReady, 1);
            return true;
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            InterlockedExchange(&g_luaSmokeLastStatus, static_cast<LONG>(GetExceptionCode()));
            return false;
        }
    }

    void RestoreEndSceneSlotHook()
    {
        if (g_endSceneSlot == nullptr || g_originalEndScene == nullptr)
        {
            return;
        }

        DWORD oldProtect = 0;
        if (VirtualProtect(g_endSceneSlot, sizeof(void*), PAGE_EXECUTE_READWRITE, &oldProtect))
        {
            *g_endSceneSlot = reinterpret_cast<void*>(g_originalEndScene);
            FlushInstructionCache(GetCurrentProcess(), g_endSceneSlot, sizeof(void*));
            DWORD ignored = 0;
            VirtualProtect(g_endSceneSlot, sizeof(void*), oldProtect, &ignored);
        }

        g_endSceneSlot = nullptr;
        g_originalEndScene = nullptr;
        InterlockedExchange(&g_mainThreadBridgeReady, 0);
        InterlockedExchange(&g_luaBridgeReady, 0);
    }

    DWORD WINAPI HookWorker(LPVOID)
    {
        if (g_heartbeatEvent != nullptr)
        {
            InterlockedIncrement(&g_heartbeatCount);
            SetEvent(g_heartbeatEvent);
        }

        while (true)
        {
            if (g_shutdownEvent == nullptr || g_commandEvent == nullptr)
            {
                Sleep(500);
                if (g_heartbeatEvent != nullptr)
                {
                    InterlockedIncrement(&g_heartbeatCount);
                    SetEvent(g_heartbeatEvent);
                }

                continue;
            }

            HANDLE waitHandles[2] = { g_shutdownEvent, g_commandEvent };
            DWORD waitResult = WaitForMultipleObjects(2, waitHandles, FALSE, 500);
            if (waitResult == WAIT_OBJECT_0)
            {
                break;
            }

            if (waitResult == WAIT_OBJECT_0 + 1 && g_commandBuffer != nullptr)
            {
                LONG command = InterlockedCompareExchange(&g_commandBuffer->Command, 0, 0);
                if (command == Protocol::Commands::Ping)
                {
                    LONG pingCount = InterlockedIncrement(&g_pingCount);
                    InterlockedExchange(&g_commandBuffer->PingCount, pingCount);
                    CompleteCommand(Protocol::Status::Ok, Protocol::Results::Ping, 0);
                }
                else if (command == Protocol::Commands::GetHookInfo)
                {
                    InterlockedExchange(&g_commandBuffer->HookProcessId, static_cast<LONG>(GetCurrentProcessId()));
                    InterlockedExchange(&g_commandBuffer->HookProtocolVersion, static_cast<LONG>(Protocol::Version));
                    InterlockedExchange(&g_commandBuffer->HookStartTick, static_cast<LONG>(g_startTick));
                    InterlockedExchange(&g_commandBuffer->HeartbeatCount, InterlockedCompareExchange(&g_heartbeatCount, 0, 0));
                    CompleteCommand(Protocol::Status::Ok, Protocol::Results::Info, 32);
                }
                else if (command == Protocol::Commands::ReadSelfModule)
                {
                    LONG moduleBaseLow = 0;
                    LONG dosSignature = 0;
                    LONG peSignature = 0;
                    LONG machine = 0;
                    LONG sectionCount = 0;

                    if (g_moduleHandle != nullptr)
                    {
                        BYTE* moduleBase = reinterpret_cast<BYTE*>(g_moduleHandle);
                        IMAGE_DOS_HEADER* dosHeader = reinterpret_cast<IMAGE_DOS_HEADER*>(moduleBase);
                        if (dosHeader->e_magic == IMAGE_DOS_SIGNATURE)
                        {
                            IMAGE_NT_HEADERS32* ntHeaders = reinterpret_cast<IMAGE_NT_HEADERS32*>(moduleBase + dosHeader->e_lfanew);
                            moduleBaseLow = static_cast<LONG>(static_cast<DWORD>(reinterpret_cast<DWORD_PTR>(moduleBase)));
                            dosSignature = dosHeader->e_magic;
                            peSignature = ntHeaders->Signature;
                            machine = ntHeaders->FileHeader.Machine;
                            sectionCount = ntHeaders->FileHeader.NumberOfSections;
                        }
                    }

                    InterlockedExchange(&g_commandBuffer->ModuleBaseLow, moduleBaseLow);
                    InterlockedExchange(&g_commandBuffer->DosSignature, dosSignature);
                    InterlockedExchange(&g_commandBuffer->PeSignature, peSignature);
                    InterlockedExchange(&g_commandBuffer->Machine, machine);
                    InterlockedExchange(&g_commandBuffer->SectionCount, sectionCount);
                    CompleteCommand(Protocol::Status::Ok, Protocol::Results::PeRead, 36);
                }
                else if (command == Protocol::Commands::LuaSmoke)
                {
                    if (!InstallEndSceneSlotHook())
                    {
                        CompleteCommand(Protocol::Status::Failed, Protocol::Results::LuaSmoke, 16);
                    }
                    else
                    {
                        InterlockedExchange(&g_luaSmokeExecuted, 0);
                        InterlockedExchange(&g_luaSmokeLastStatus, 0);
                        InterlockedExchange(&g_luaSmokeState, LuaSmokePending);

                        DWORD start = GetTickCount();
                        bool completed = false;
                        while (GetTickCount() - start < 5000)
                        {
                            LONG state = InterlockedCompareExchange(&g_luaSmokeState, LuaSmokeIdle, LuaSmokeDone);
                            if (state == LuaSmokeDone)
                            {
                                CompleteCommand(Protocol::Status::Ok, Protocol::Results::LuaSmoke, 16);
                                completed = true;
                                break;
                            }

                            state = InterlockedCompareExchange(&g_luaSmokeState, LuaSmokeIdle, LuaSmokeFailed);
                            if (state == LuaSmokeFailed)
                            {
                                CompleteCommand(Protocol::Status::Failed, Protocol::Results::LuaSmoke, 16);
                                completed = true;
                                break;
                            }

                            Sleep(10);
                        }

                        if (!completed)
                        {
                            InterlockedExchange(&g_luaSmokeState, LuaSmokeIdle);
                            InterlockedExchange(&g_luaSmokeLastStatus, LuaStatusTimeout);
                            CompleteCommand(Protocol::Status::Failed, Protocol::Results::LuaSmoke, 16);
                        }
                    }
                }
                else if (command != 0)
                {
                    CompleteCommand(Protocol::Status::UnsupportedCommand, command, 0);
                }
            }

            if (g_heartbeatEvent != nullptr)
            {
                InterlockedIncrement(&g_heartbeatCount);
                SetEvent(g_heartbeatEvent);
            }
        }

        if (g_readyEvent != nullptr)
        {
            ResetEvent(g_readyEvent);
        }

        if (g_heartbeatEvent != nullptr)
        {
            ResetEvent(g_heartbeatEvent);
        }

        return 0;
    }

    void SignalReadyEvent(HMODULE moduleHandle)
    {
        g_moduleHandle = moduleHandle;
        g_startTick = GetTickCount();

        wchar_t eventName[96] = {};
        BuildEventName(eventName, 96, Protocol::ReadyEventPrefix);

        g_readyEvent = CreateEventW(nullptr, TRUE, TRUE, eventName);
        if (g_readyEvent != nullptr)
        {
            SetEvent(g_readyEvent);
        }

        BuildEventName(eventName, 96, Protocol::HeartbeatEventPrefix);
        g_heartbeatEvent = CreateEventW(nullptr, TRUE, FALSE, eventName);

        BuildEventName(eventName, 96, Protocol::ShutdownEventPrefix);
        g_shutdownEvent = CreateEventW(nullptr, TRUE, FALSE, eventName);

        BuildEventName(eventName, 96, Protocol::CommandEventPrefix);
        g_commandEvent = CreateEventW(nullptr, FALSE, FALSE, eventName);

        BuildEventName(eventName, 96, Protocol::AckEventPrefix);
        g_ackEvent = CreateEventW(nullptr, FALSE, FALSE, eventName);

        BuildEventName(eventName, 96, Protocol::MappingPrefix);
        g_mapping = CreateFileMappingW(INVALID_HANDLE_VALUE, nullptr, PAGE_READWRITE, 0, Protocol::BufferSize, eventName);
        if (g_mapping != nullptr)
        {
            g_commandBuffer = static_cast<Protocol::CommandBuffer*>(MapViewOfFile(g_mapping, FILE_MAP_ALL_ACCESS, 0, 0, Protocol::BufferSize));
            if (g_commandBuffer != nullptr)
            {
                ZeroMemory(g_commandBuffer, sizeof(Protocol::CommandBuffer));
                InterlockedExchange(&g_commandBuffer->Magic, static_cast<LONG>(Protocol::Magic));
                InterlockedExchange(&g_commandBuffer->Version, static_cast<LONG>(Protocol::Version));
                InterlockedExchange(&g_commandBuffer->HeaderSize, static_cast<LONG>(Protocol::HeaderSize));
            }
        }

        if (g_workerThread == nullptr)
        {
            g_workerThread = CreateThread(nullptr, 0, HookWorker, nullptr, 0, nullptr);
        }
    }

    void CloseHandles()
    {
        if (g_shutdownEvent != nullptr)
        {
            SetEvent(g_shutdownEvent);
        }

        if (g_workerThread != nullptr)
        {
            WaitForSingleObject(g_workerThread, 1000);
            CloseHandle(g_workerThread);
            g_workerThread = nullptr;
        }

        RestoreEndSceneSlotHook();

        if (g_readyEvent != nullptr)
        {
            CloseHandle(g_readyEvent);
            g_readyEvent = nullptr;
        }

        if (g_heartbeatEvent != nullptr)
        {
            CloseHandle(g_heartbeatEvent);
            g_heartbeatEvent = nullptr;
        }

        if (g_shutdownEvent != nullptr)
        {
            CloseHandle(g_shutdownEvent);
            g_shutdownEvent = nullptr;
        }

        if (g_commandEvent != nullptr)
        {
            CloseHandle(g_commandEvent);
            g_commandEvent = nullptr;
        }

        if (g_ackEvent != nullptr)
        {
            CloseHandle(g_ackEvent);
            g_ackEvent = nullptr;
        }

        if (g_commandBuffer != nullptr)
        {
            UnmapViewOfFile(g_commandBuffer);
            g_commandBuffer = nullptr;
        }

        if (g_mapping != nullptr)
        {
            CloseHandle(g_mapping);
            g_mapping = nullptr;
        }
    }
}

extern "C" __declspec(dllexport) DWORD __stdcall SpellFireHookPing()
{
    return 0x5346484B; // SFHK
}

BOOL APIENTRY DllMain(HMODULE moduleHandle, DWORD reason, LPVOID reserved)
{
    UNREFERENCED_PARAMETER(moduleHandle);
    UNREFERENCED_PARAMETER(reserved);

    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(moduleHandle);
        SignalReadyEvent(moduleHandle);
    }
    else if (reason == DLL_PROCESS_DETACH)
    {
        CloseHandles();
    }

    return TRUE;
}
