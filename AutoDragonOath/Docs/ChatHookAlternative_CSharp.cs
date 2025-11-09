// ChatHookAlternative_CSharp.cs
// C# alternative using EasyHook or similar library
// This can be integrated into the AutoDragonOath WPF application

using System;
using System.Runtime.InteropServices;
using System.Text;
using EasyHook; // Install-Package EasyHook

namespace AutoDragonOath.Services
{
    /// <summary>
    /// Chat hook service to intercept incoming chat messages from game server
    /// Uses EasyHook for function hooking
    /// </summary>
    public class ChatHookService : IDisposable
    {
        // ====================================================================
        // DELEGATES FOR HOOKED FUNCTIONS
        // ====================================================================

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate uint GCChatHandler_Execute_Delegate(
            IntPtr thisPtr,
            IntPtr pPacket,  // GCChat*
            IntPtr pPlayer   // Player*
        );

        // ====================================================================
        // PACKET STRUCTURE (MARSHALING)
        // ====================================================================

        // We don't need the full structure, just the vtable pointer
        // We'll call methods via function pointers
        [StructLayout(LayoutKind.Sequential)]
        private class GCChatPacket
        {
            public IntPtr VTable; // Pointer to virtual method table
        }

        // Virtual method indices (from analyzing source code)
        private const int VTABLE_GETSOURNAME_INDEX = 0;
        private const int VTABLE_GETSOURN AMEsize_INDEX = 1;
        private const int VTABLE_GETCONTEX_INDEX = 2;
        private const int VTABLE_GETCONTEXSIZE_INDEX = 3;
        private const int VTABLE_GETCHATTYPE_INDEX = 4;

        // ====================================================================
        // HOOK MANAGEMENT
        // ====================================================================

        private LocalHook _chatHook;
        private GCChatHandler_Execute_Delegate _originalFunction;
        private IntPtr _functionAddress;

        // Event fired when chat message is received
        public event EventHandler<ChatMessageEventArgs> ChatMessageReceived;

        // ====================================================================
        // INITIALIZATION
        // ====================================================================

        public ChatHookService()
        {
            // Constructor - hook will be installed when Start() is called
        }

        /// <summary>
        /// Start hooking the chat function
        /// </summary>
        public bool Start(int processId)
        {
            try
            {
                // Find the function address (you need to implement pattern scanning)
                _functionAddress = FindChatHandlerFunction(processId);

                if (_functionAddress == IntPtr.Zero)
                {
                    Console.WriteLine("Failed to find chat handler function");
                    return false;
                }

                // Create the hook
                _chatHook = LocalHook.Create(
                    _functionAddress,
                    new GCChatHandler_Execute_Delegate(HookedChatHandler),
                    this
                );

                // Enable hook for all threads
                _chatHook.ThreadACL.SetExclusiveACL(new int[] { 0 });

                Console.WriteLine($"Chat hook installed at 0x{_functionAddress.ToInt32():X8}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to install hook: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop hooking
        /// </summary>
        public void Stop()
        {
            _chatHook?.Dispose();
            _chatHook = null;
        }

        // ====================================================================
        // HOOKED FUNCTION IMPLEMENTATION
        // ====================================================================

        private uint HookedChatHandler(IntPtr thisPtr, IntPtr pPacket, IntPtr pPlayer)
        {
            try
            {
                if (pPacket != IntPtr.Zero)
                {
                    // Extract chat data from packet
                    var chatData = ExtractChatData(pPacket);

                    // Fire event to notify listeners
                    ChatMessageReceived?.Invoke(this, chatData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in chat hook: {ex.Message}");
            }

            // Call original function
            return _originalFunction(thisPtr, pPacket, pPlayer);
        }

        // ====================================================================
        // PACKET DATA EXTRACTION
        // ====================================================================

        private ChatMessageEventArgs ExtractChatData(IntPtr pPacket)
        {
            var result = new ChatMessageEventArgs();

            try
            {
                // Read vtable pointer
                IntPtr vtable = Marshal.ReadIntPtr(pPacket);

                // Get GetSourName() method pointer
                IntPtr getSourNamePtr = Marshal.ReadIntPtr(vtable, VTABLE_GETSOURNAME_INDEX * IntPtr.Size);
                IntPtr getSourNameSizePtr = Marshal.ReadIntPtr(vtable, VTABLE_GETSOURN AMEsize_INDEX * IntPtr.Size);

                // Call GetSourName() and GetSourNameSize()
                // This is tricky - we need to call the method with thisPtr (thiscall convention)
                var getSourName = Marshal.GetDelegateForFunctionPointer<GetStringMethod>(getSourNamePtr);
                var getSourNameSize = Marshal.GetDelegateForFunctionPointer<GetIntMethod>(getSourNameSizePtr);

                IntPtr namePtr = getSourName(pPacket);
                int nameSize = getSourNameSize(pPacket);

                if (namePtr != IntPtr.Zero && nameSize > 0)
                {
                    byte[] nameBytes = new byte[nameSize];
                    Marshal.Copy(namePtr, nameBytes, 0, nameSize);
                    result.SenderName = Encoding.GetEncoding("VISCII").GetString(nameBytes).TrimEnd('\0');
                }

                // Get message content
                IntPtr getContexPtr = Marshal.ReadIntPtr(vtable, VTABLE_GETCONTEX_INDEX * IntPtr.Size);
                IntPtr getContexSizePtr = Marshal.ReadIntPtr(vtable, VTABLE_GETCONTEXSIZE_INDEX * IntPtr.Size);

                var getContex = Marshal.GetDelegateForFunctionPointer<GetStringMethod>(getContexPtr);
                var getContexSize = Marshal.GetDelegateForFunctionPointer<GetIntMethod>(getContexSizePtr);

                IntPtr contextPtr = getContex(pPacket);
                int contextSize = getContexSize(pPacket);

                if (contextPtr != IntPtr.Zero && contextSize > 0)
                {
                    byte[] contextBytes = new byte[contextSize];
                    Marshal.Copy(contextPtr, contextBytes, 0, contextSize);
                    result.MessageText = Encoding.GetEncoding("VISCII").GetString(contextBytes).TrimEnd('\0');
                }

                // Get channel type
                IntPtr getChatTypePtr = Marshal.ReadIntPtr(vtable, VTABLE_GETCHATTYPE_INDEX * IntPtr.Size);
                var getChatType = Marshal.GetDelegateForFunctionPointer<GetByteMethod>(getChatTypePtr);
                result.ChannelType = getChatType(pPacket);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting chat data: {ex.Message}");
            }

            return result;
        }

        // ====================================================================
        // HELPER DELEGATES FOR CALLING VIRTUAL METHODS
        // ====================================================================

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetStringMethod(IntPtr thisPtr);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int GetIntMethod(IntPtr thisPtr);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate byte GetByteMethod(IntPtr thisPtr);

        // ====================================================================
        // PATTERN SCANNING
        // ====================================================================

        private IntPtr FindChatHandlerFunction(int processId)
        {
            // TODO: Implement pattern scanning
            // You can use the MemoryReader class from AutoDragonOath

            // Example pattern (you need to find this using IDA Pro):
            // 55 8B EC 83 EC ?? 53 56 57 8B F9
            byte[] pattern = new byte[] {
                0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x00, 0x53, 0x56, 0x57, 0x8B, 0xF9
            };
            string mask = "xxxxx?xxxxx";

            // Search in Game.exe module
            IntPtr gameModule = GetModuleHandle("Game.exe");
            if (gameModule == IntPtr.Zero)
                return IntPtr.Zero;

            // Get module size
            MODULEINFO modInfo = new MODULEINFO();
            if (!GetModuleInformation(GetCurrentProcess(), gameModule, out modInfo, Marshal.SizeOf(modInfo)))
                return IntPtr.Zero;

            // Scan for pattern
            return ScanPattern(gameModule, (int)modInfo.SizeOfImage, pattern, mask);
        }

        private IntPtr ScanPattern(IntPtr baseAddress, int size, byte[] pattern, string mask)
        {
            byte[] buffer = new byte[size];
            Marshal.Copy(baseAddress, buffer, 0, size);

            for (int i = 0; i < size - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (mask[j] != '?' && buffer[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                    return IntPtr.Add(baseAddress, i);
            }

            return IntPtr.Zero;
        }

        // ====================================================================
        // WIN32 IMPORTS
        // ====================================================================

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("psapi.dll")]
        private static extern bool GetModuleInformation(
            IntPtr hProcess,
            IntPtr hModule,
            out MODULEINFO lpmodinfo,
            int cb
        );

        [StructLayout(LayoutKind.Sequential)]
        private struct MODULEINFO
        {
            public IntPtr lpBaseOfDll;
            public uint SizeOfImage;
            public IntPtr EntryPoint;
        }

        // ====================================================================
        // DISPOSE
        // ====================================================================

        public void Dispose()
        {
            Stop();
        }
    }

    // ========================================================================
    // EVENT ARGS
    // ========================================================================

    public class ChatMessageEventArgs : EventArgs
    {
        public string SenderName { get; set; }
        public string MessageText { get; set; }
        public byte ChannelType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public string ChannelName
        {
            get
            {
                return ChannelType switch
                {
                    0 => "Near",
                    1 => "Scene",
                    2 => "Team",
                    3 => "Guild",
                    4 => "Private",
                    5 => "System",
                    _ => $"Unknown ({ChannelType})"
                };
            }
        }

        public override string ToString()
        {
            return $"[{ChannelName}] {SenderName}: {MessageText}";
        }
    }

    // ========================================================================
    // USAGE EXAMPLE (in MainViewModel or service)
    // ========================================================================

    public class ChatMonitoringExample
    {
        private ChatHookService _chatHook;

        public void StartMonitoring(int processId)
        {
            _chatHook = new ChatHookService();

            // Subscribe to chat events
            _chatHook.ChatMessageReceived += OnChatMessageReceived;

            // Start hooking
            bool success = _chatHook.Start(processId);

            if (success)
            {
                Console.WriteLine("Chat monitoring started");
            }
            else
            {
                Console.WriteLine("Failed to start chat monitoring");
            }
        }

        private void OnChatMessageReceived(object sender, ChatMessageEventArgs e)
        {
            // Handle the chat message
            Console.WriteLine($"[{e.Timestamp:HH:mm:ss}] {e}");

            // You can:
            // - Save to database
            // - Display in UI
            // - Trigger automation
            // - Filter by keywords
            // - etc.

            // Example: Detect help requests
            if (e.MessageText.Contains("帮助") || e.MessageText.Contains("help"))
            {
                Console.WriteLine("  -> Help request detected!");
            }

            // Example: Log team messages separately
            if (e.ChannelName == "Team")
            {
                SaveTeamMessage(e);
            }
        }

        private void SaveTeamMessage(ChatMessageEventArgs chatData)
        {
            // Implement your team message logging here
        }

        public void StopMonitoring()
        {
            _chatHook?.Dispose();
            _chatHook = null;
        }
    }
}
