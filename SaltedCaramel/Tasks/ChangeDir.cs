using System;
using System.Diagnostics;
using System.IO;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    /// <summary>
    /// Task responsible for changing directories.
    /// </summary>
    public class ChangeDir
    {
        /// <summary>
        /// Change directory based on the Job.Task.@params
        /// </summary>
        /// <param name="job">Job associated with this task</param>
        /// <param name="agent">Agent this task is run on.</param>
        public static void Execute(Job job, SCImplant agent)
        {
            SCTask task = job.Task;
            string path = task.@params;

            try
            {
                Directory.SetCurrentDirectory(path);
                task.status = "complete";
                task.message = $"Changed to directory {task.@params}";
            }
            catch (Exception e)
            {
                Debug.WriteLine($"[!] ChangeDir - ERROR: {e.Message}");
                task.status = "error";
                task.message = e.Message;
            }
        }
    }
}
