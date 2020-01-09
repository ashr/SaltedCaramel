using Newtonsoft.Json;
using System;
using System.Threading;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    public class Jobs
    { 
        // Split this out
        public static void Execute(Job job, SCImplant implant)
        {
            SCTask task = job.Task;
            if (task.command == "jobs")
            {
                task.status = "complete";
                task.message = JsonConvert.SerializeObject(implant.JobList);
            }
            else if (task.command == "jobkill")
            {
                Thread t;
                foreach (Job j in implant.JobList)
                {
                    if (j.JobID == Convert.ToInt32(task.@params))
                    {
                        if (j.Kill())
                        {
                            task.status = "complete";
                            task.message = $"Killed job {j.JobID}";
                        }
                        else
                        {
                            task.status = "error";
                            task.message = $"Error stopping job {j.JobID}";
                        }
                    }
                }
            }
        }
    }
}
