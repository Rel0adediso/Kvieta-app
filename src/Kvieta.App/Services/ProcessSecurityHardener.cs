using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Kvieta.App.Services;

public static class ProcessSecurityHardener
{
    private const int SDDL_REVISION_1 = 1;
    private const int DACL_SECURITY_INFORMATION = 0x00000004;

    // Deny:
    // PROCESS_TERMINATE (0x0001)
    // PROCESS_SUSPEND_RESUME (0x0800)
    // PROCESS_VM_WRITE (0x0020)
    // PROCESS_VM_OPERATION (0x0008)
    // WRITE_DAC (0x00040000)
    // WRITE_OWNER (0x00080000)
    // Deny mask = 0x000C0829
    //
    // SDDL:
    // D:P
    // (A;;GA;;;SY) -> Allow SYSTEM Generic All
    // (A;;GA;;;BA) -> Allow Builtin Administrators Generic All
    // (D;;0x000C0829;;;WD) -> Deny Terminate/Suspend/VM_Write/WriteDAC to Everyone
    // (A;;0x00121410;;;WD) -> Allow Read/Query/Synchronize to Everyone
    private const string HardenedProcessSddl = "D:P(A;;GA;;;SY)(A;;GA;;;BA)(D;;0x000C0829;;;WD)(A;;0x00121410;;;WD)";

    public static bool ProtectCurrentProcess()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            IntPtr pSd = IntPtr.Zero;
            if (!ConvertStringSecurityDescriptorToSecurityDescriptor(
                HardenedProcessSddl,
                SDDL_REVISION_1,
                out pSd,
                out _))
            {
                return false;
            }

            try
            {
                IntPtr currentProcess = GetCurrentProcess();
                return SetKernelObjectSecurity(currentProcess, DACL_SECURITY_INFORMATION, pSd);
            }
            finally
            {
                if (pSd != IntPtr.Zero)
                {
                    LocalFree(pSd);
                }
            }
        }
        catch
        {
            return false;
        }
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
        string stringSecurityDescriptor,
        int stringSdRevision,
        out IntPtr securityDescriptor,
        out uint securityDescriptorSize);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetKernelObjectSecurity(
        IntPtr handle,
        int securityInformation,
        IntPtr securityDescriptor);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
