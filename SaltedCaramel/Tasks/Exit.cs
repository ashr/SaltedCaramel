using System;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    public class Exit
    {
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
