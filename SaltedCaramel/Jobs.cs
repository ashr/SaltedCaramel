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
        public class Job
        {
            static int JobCount = 0;
            public int JobID;
            public int ProcessID;
            public SCTask Task;
            public string TaskString;
            internal Thread _JobThread;


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

            public void Start()
            {
                _JobThread.Start();
            }

            public bool Status()
            {
                return _JobThread.IsAlive;
            }

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
