using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace libc.hwid.Helpers
{
    public static class Asm
    {
        // Taken from https://stackoverflow.com/a/16487521/3673720

        [DllImport("user32", EntryPoint = "CallWindowProcW", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)] private static extern IntPtr CallWindowProcW([In] byte[] bytes, IntPtr hWnd, int msg, [In, Out] byte[] wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)] public static extern bool VirtualProtect([In] byte[] bytes, IntPtr size, int newProtect, out int oldProtect);

        const int PAGE_EXECUTE_READWRITE = 0x40;

        public static string GetProcessorId()
        {
            byte[] sn = new byte[8];

            if (!ExecuteCode(ref sn))
                return "ND";

            return string.Format("{0}{1}", BitConverter.ToUInt32(sn, 4).ToString("X8"), BitConverter.ToUInt32(sn, 0).ToString("X8"));
        }

        private static bool ExecuteCode(ref byte[] result)
        {
            int num;

            /* The opcodes below implement a C function with the signature:
             * __stdcall CpuIdWindowProc(hWnd, Msg, wParam, lParam);
             * with wParam interpreted as a pointer pointing to an 8 byte unsigned character buffer.
             * */

            byte[] code_x86 = new byte[] {
                0x55,                      /* push ebp */
                0x89, 0xe5,                /* mov  ebp, esp */
                0x57,                      /* push edi */
                0x8b, 0x7d, 0x10,          /* mov  edi, [ebp+0x10] */
                0x6a, 0x01,                /* push 0x1 */
                0x58,                      /* pop  eax */
                0x53,                      /* push ebx */
                0x0f, 0xa2,                /* cpuid    */
                0x89, 0x07,                /* mov  [edi], eax */
                0x89, 0x57, 0x04,          /* mov  [edi+0x4], edx */
                0x5b,                      /* pop  ebx */
                0x5f,                      /* pop  edi */
                0x89, 0xec,                /* mov  esp, ebp */
                0x5d,                      /* pop  ebp */
                0xc2, 0x10, 0x00,          /* ret  0x10 */
            };
            byte[] code_x64 = new byte[] {
                0x53,                                     /* push rbx */
                0x48, 0xc7, 0xc0, 0x01, 0x00, 0x00, 0x00, /* mov rax, 0x1 */
                0x0f, 0xa2,                               /* cpuid */
                0x41, 0x89, 0x00,                         /* mov [r8], eax */
                0x41, 0x89, 0x50, 0x04,                   /* mov [r8+0x4], edx */
                0x5b,                                     /* pop rbx */
                0xc3,                                     /* ret */
            };

            byte[] asmCode = IsX64Process() ? ref code_x64 : ref code_x86;

            IntPtr ptr = new IntPtr(asmCode.Length);

            if (!VirtualProtect(asmCode, ptr, PAGE_EXECUTE_READWRITE, out num))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            ptr = new IntPtr(result.Length);

            return (CallWindowProcW(asmCode, IntPtr.Zero, 0, result, ptr) != IntPtr.Zero);
        }

        private static bool IsX64Process()
        {
            return IntPtr.Size == 8;
        }
    }
}

