using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    public class ProcessList
    {
        /// <summary>
        /// Fetch a process listing.
        /// </summary>
        /// <param name="job">Job associated with this task.</param>
        /// <param name="agent">Agent associated with this task.</param>
        public static void Execute(Job job, SCImplant agent)
        {
            SCTask task = job.Task;
            List<Dictionary<string, string>> procList = new List<Dictionary<string, string>>();
            foreach (Process proc in Process.GetProcesses())
            {
                Dictionary<string, string> procEntry = new Dictionary<string, string>
                {
                    { "process_id", proc.Id.ToString() },
                    { "name", proc.ProcessName }
                };

                // This will fail if we don't have permissions to access the process.
                try { procEntry.Add("parent_process_id", GetParentProcess(proc.Handle).ToString()); }
                // Ignore it and move on
                catch { procEntry.Add("parent_process_id", ""); }

                try { procEntry.Add("user", GetProcessUser(proc.Handle)); }
                catch { procEntry.Add("user", ""); }

                try
                {
                    Win32.Kernel32.IsWow64Process(proc.Handle, out bool is64);
                    if (is64) procEntry.Add("arch", "x86");
                    else procEntry.Add("arch", "x64");
                }
                catch { procEntry.Add("arch", ""); }

                procList.Add(procEntry);
            }

            task.status = "complete";
            task.message = JsonConvert.SerializeObject(procList);
        }

        // No way of getting parent process from C#, but we can use NtQueryInformationProcess to get this info.
        public static int GetParentProcess(IntPtr procHandle)
        {
            Win32.Ntdll.PROCESS_BASIC_INFORMATION procinfo = new Win32.Ntdll.PROCESS_BASIC_INFORMATION();
            _ = Win32.Ntdll.NtQueryInformationProcess(
                procHandle,                 // ProcessHandle
                0,                          // processInformationClass
                ref procinfo,               // ProcessBasicInfo
                Marshal.SizeOf(procinfo),   // processInformationLength
                out _);                     // returnLength
            return procinfo.InheritedFromUniqueProcessId.ToInt32();
        }

        /// <summary>
        /// Given a process handle, retrieve the user associated with
        /// that process.
        /// </summary>
        /// <param name="procHandle">Open handle to a process for data to be retrieved from</param>
        /// <returns>Username as string.</returns>
        public static string GetProcessUser(IntPtr procHandle)
        {
            try
            {
                IntPtr tokenHandle = IntPtr.Zero;
                _ = Win32.Advapi32.OpenProcessToken(
                    procHandle,                                 // ProcessHandle
                    (uint)TokenAccessLevels.MaximumAllowed,     // desiredAccess
                    out procHandle);                            // TokenHandle
                return new WindowsIdentity(procHandle).Name;
            }
            catch // If we can't open a handle to the process it will throw an exception
            {
                return "";
            }
        }
    }
}
