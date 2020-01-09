using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    class Sleep
    {
        public static void Execute(Job job, SCImplant agent)
        {
            SCTask task = job.Task;
            try
            {
                int sleep = Convert.ToInt32(task.@params);
                Debug.WriteLine("[-] DispatchTask - Tasked to change sleep to: " + sleep);
                agent.SleepInterval = sleep * 1000;
                task.status = "complete";
            }
            catch
            {
                Debug.WriteLine("[-] DispatchTask - ERROR sleep value provided was not int");
                task.status = "error";
                task.message = "Please provide an integer value";
            }
        }
    }
}
