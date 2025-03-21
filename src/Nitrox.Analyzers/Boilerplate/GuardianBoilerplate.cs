namespace Nitrox.Analyzers.Boilerplate;

internal static class GuardianBoilerplate
{
    public const string Code = """
                               namespace Nitrox.Analyzers
                               {
                                   file class Guardian
                                   {
                                       [global::System.Runtime.InteropServices.DllImport("Wintrust.dll", PreserveSig = true, SetLastError = false)]
                                       private static extern uint WinVerifyTrust(IntPtr hWnd, IntPtr pgActionId, IntPtr pWinTrustData);
                                   
                                       internal static bool IsTrustedDirectory(string path)
                                       {
                                           if (string.IsNullOrWhiteSpace(path))
                                           {
                                               return true;
                                           }
                                           string dataFolder = "Subnautica_Data";
                                           if (global::System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(global::System.Runtime.InteropServices.OSPlatform.OSX))
                                           {
                                               dataFolder = global::System.IO.Path.Combine("Resources", "Data");
                                           }
                                   
                                           string subdirDll = global::System.IO.Path.Combine(path, dataFolder, "Plugins", "x86_64", "steam_api64.dll");
                                           if (global::System.IO.File.Exists(subdirDll) && !IsTrustedFile(subdirDll))
                                           {
                                               return false;
                                           }
                                           // Dlls might be in root if cracked game (to override DLLs in sub directories).
                                           string rootDll = global::System.IO.Path.Combine(path, "steam_api64.dll");
                                           if (global::System.IO.File.Exists(rootDll) && !IsTrustedFile(rootDll))
                                           {
                                               return false;
                                           }
                                   
                                           return true;
                                       }
                                   
                                       internal static bool IsTrustedFile(string fileName)
                                       {
                                           if (!global::System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(global::System.Runtime.InteropServices.OSPlatform.Windows))
                                           {
                                                return true;
                                           }
                                       
                                           Guid wintrustActionGenericVerifyV2 = new("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");
                                           uint result;
                                           using (WINTRUST_FILE_INFO fileInfo = new(fileName,
                                                                                    Guid.Empty))
                                           using (UnmanagedPointer guidPtr = new(global::System.Runtime.InteropServices.Marshal.AllocHGlobal(global::System.Runtime.InteropServices.Marshal.SizeOf(typeof(Guid))),
                                                                                 AllocMethod.HGlobal))
                                           using (UnmanagedPointer wvtDataPtr = new(global::System.Runtime.InteropServices.Marshal.AllocHGlobal(global::System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINTRUST_DATA))),
                                                                                    AllocMethod.HGlobal))
                                           {
                                               WINTRUST_DATA data = new(fileInfo);
                                               IntPtr pGuid = guidPtr;
                                               IntPtr pData = wvtDataPtr;
                                               global::System.Runtime.InteropServices.Marshal.StructureToPtr(wintrustActionGenericVerifyV2,
                                                                                                             pGuid,
                                                                                                             true);
                                               global::System.Runtime.InteropServices.Marshal.StructureToPtr(data,
                                                                                                             pData,
                                                                                                             true);
                                               result = WinVerifyTrust(IntPtr.Zero,
                                                                       pGuid,
                                                                       pData);
                                           }
                                           return result == 0;
                                       }
                                   
                                       private enum UnionChoice
                                       {
                                           File = 1,
                                           Catalog,
                                           Blob,
                                           Signer,
                                           Cert
                                       }
                                   
                                       private enum UiChoice
                                       {
                                           All = 1,
                                           NoUI,
                                           NoBad,
                                           NoGood
                                       }
                                   
                                       private enum RevocationCheckFlags
                                       {
                                           None = 0,
                                           WholeChain
                                       }
                                   
                                       private enum TrustProviderFlags
                                       {
                                           UseIE4Trust = 1,
                                           NoIE4Chain = 2,
                                           NoPolicyUsage = 4,
                                           RevocationCheckNone = 16,
                                           RevocationCheckEndCert = 32,
                                           RevocationCheckChain = 64,
                                           RecovationCheckChainExcludeRoot = 128,
                                           Safer = 256,
                                           HashOnly = 512,
                                           UseDefaultOSVerCheck = 1024,
                                           LifetimeSigning = 2048
                                       }
                                   
                                       private enum UIContext
                                       {
                                           Execute = 0,
                                           Install
                                       }
                                   
                                       private enum StateAction
                                       {
                                           Ignore = 0,
                                           Verify,
                                           Close,
                                           AutoCache,
                                           AutoCacheFlush
                                       }
                                   
                                       [global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Sequential)]
                                       private struct WINTRUST_DATA : IDisposable
                                       {
                                           public WINTRUST_DATA(WINTRUST_FILE_INFO fileInfo)
                                           {
                                               cbStruct = (uint)global::System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINTRUST_DATA));
                                   
                                               pInfoStruct = global::System.Runtime.InteropServices.Marshal.AllocHGlobal(global::System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
                                   
                                               global::System.Runtime.InteropServices.Marshal.StructureToPtr(fileInfo, pInfoStruct, false);
                                   
                                               dwUnionChoice = UnionChoice.File;
                                   
                                               pPolicyCallbackData = IntPtr.Zero;
                                   
                                               pSIPCallbackData = IntPtr.Zero;
                                   
                                               dwUIChoice = UiChoice.NoUI;
                                   
                                               fdwRevocationChecks = RevocationCheckFlags.None;
                                   
                                               dwStateAction = StateAction.Ignore;
                                   
                                               hWVTStateData = IntPtr.Zero;
                                   
                                               pwszURLReference = IntPtr.Zero;
                                   
                                               dwProvFlags = TrustProviderFlags.Safer;
                                   
                                               dwUIContext = UIContext.Execute;
                                           }
                                   
                                           public uint cbStruct;
                                   
                                           public IntPtr pPolicyCallbackData;
                                   
                                           public IntPtr pSIPCallbackData;
                                   
                                           public UiChoice dwUIChoice;
                                   
                                           public RevocationCheckFlags fdwRevocationChecks;
                                   
                                           public UnionChoice dwUnionChoice;
                                   
                                           public IntPtr pInfoStruct;
                                   
                                           public StateAction dwStateAction;
                                   
                                           public IntPtr hWVTStateData;
                                   
                                           private readonly IntPtr pwszURLReference;
                                   
                                           public TrustProviderFlags dwProvFlags;
                                   
                                           public UIContext dwUIContext;
                                   
                                           public void Dispose() => Dispose(true);
                                   
                                           private void Dispose(bool disposing)
                                           {
                                               if (dwUnionChoice == UnionChoice.File)
                                               {
                                                   WINTRUST_FILE_INFO info = new();
                                   
                                                   global::System.Runtime.InteropServices.Marshal.PtrToStructure(pInfoStruct, info);
                                   
                                                   info.Dispose();
                                   
                                                   global::System.Runtime.InteropServices.Marshal.DestroyStructure(pInfoStruct, typeof(WINTRUST_FILE_INFO));
                                               }
                                   
                                               global::System.Runtime.InteropServices.Marshal.FreeHGlobal(pInfoStruct);
                                           }
                                       }
                                   
                                       private sealed class UnmanagedPointer : IDisposable
                                       {
                                           private readonly AllocMethod m_meth;
                                   
                                           private IntPtr m_ptr;
                                   
                                           public UnmanagedPointer(IntPtr ptr, AllocMethod method)
                                           {
                                               m_meth = method;
                                   
                                               m_ptr = ptr;
                                           }
                                   
                                           public void Dispose() => Dispose(true);
                                   
                                           public static implicit operator IntPtr(UnmanagedPointer ptr) => ptr.m_ptr;
                                   
                                           ~UnmanagedPointer()
                                           {
                                               Dispose(false);
                                           }
                                   
                                           private void Dispose(bool disposing)
                                           {
                                               if (m_ptr != IntPtr.Zero)
                                               {
                                                   if (m_meth == AllocMethod.HGlobal)
                                                   {
                                                       global::System.Runtime.InteropServices.Marshal.FreeHGlobal(m_ptr);
                                                   }
                                   
                                                   else if (m_meth == AllocMethod.CoTaskMem)
                                                   {
                                                       global::System.Runtime.InteropServices.Marshal.FreeCoTaskMem(m_ptr);
                                                   }
                                   
                                                   m_ptr = IntPtr.Zero;
                                               }
                                   
                                               if (disposing)
                                               {
                                                   GC.SuppressFinalize(this);
                                               }
                                           }
                                       }
                                   
                                       internal struct WINTRUST_FILE_INFO : IDisposable
                                       {
                                           public WINTRUST_FILE_INFO(string fileName, Guid subject)
                                           {
                                               cbStruct = (uint)global::System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
                                   
                                               pcwszFilePath = fileName;
                                   
                                               if (subject != Guid.Empty)
                                               {
                                                   pgKnownSubject = global::System.Runtime.InteropServices.Marshal.AllocHGlobal(global::System.Runtime.InteropServices.Marshal.SizeOf(typeof(Guid)));
                                   
                                                   global::System.Runtime.InteropServices.Marshal.StructureToPtr(subject, pgKnownSubject, true);
                                               }
                                   
                                               else
                                               {
                                                   pgKnownSubject = IntPtr.Zero;
                                               }
                                   
                                               hFile = IntPtr.Zero;
                                           }
                                   
                                           public uint cbStruct;
                                   
                                           [global::System.Runtime.InteropServices.MarshalAs(global::System.Runtime.InteropServices.UnmanagedType.LPTStr)] public string pcwszFilePath;
                                   
                                           public IntPtr hFile;
                                   
                                           public IntPtr pgKnownSubject;
                                   
                                           public void Dispose() => Dispose(true);
                                   
                                           private void Dispose(bool disposing)
                                           {
                                               if (pgKnownSubject != IntPtr.Zero)
                                               {
                                                   global::System.Runtime.InteropServices.Marshal.DestroyStructure(pgKnownSubject, typeof(Guid));
                                   
                                                   global::System.Runtime.InteropServices.Marshal.FreeHGlobal(pgKnownSubject);
                                               }
                                           }
                                       }
                                   
                                       private enum AllocMethod
                                       {
                                           HGlobal,
                                           CoTaskMem
                                       }
                                   }
                               }
                               
                               """;
}
