using Newtonsoft.Json;
using SharpSploit.Enumeration;
using SharpSploit.Generic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    
    public class DirectoryList
    {
        /// <summary>
        /// List all files and directories.
        /// </summary>
        /// <param name="job">Job associated with this task. Path is in job.Task.@params</param>
        /// <param name="agent">Agent this task is run on.</param>
        public static void Execute(Job job, SCImplant implant)
        {
            SCTask task = job.Task;
            string path = task.@params;
            SharpSploitResultList<Host.FileSystemEntryResult> list;

            try
            {
                if (path != "")
                    list = Host.GetDirectoryListing(path);
                else
                    list = Host.GetDirectoryListing();

                List<Dictionary<string, string>> fileList = new List<Dictionary<string, string>>();

                foreach (Host.FileSystemEntryResult item in list)
                {
                    FileInfo f = new FileInfo(item.Name);
                    Dictionary<string, string> infoDict = new Dictionary<string, string>();
                    try
                    {
                        infoDict.Add("size", f.Length.ToString());
                        infoDict.Add("type", "file");
                        infoDict.Add("name", f.Name);
                        fileList.Add(infoDict);
                    }
                    catch
                    {
                        infoDict.Add("size", "0");
                        infoDict.Add("type", "dir");
                        infoDict.Add("name", item.Name);
                        fileList.Add(infoDict);
                    }
                }

                SCTaskResp response = new SCTaskResp(task.id, JsonConvert.SerializeObject(fileList));
                implant.TryPostResponse(response);
                implant.TrySendComplete(job);
                task.status = "complete";
                task.message = fileList.ToString();
            }
            catch (DirectoryNotFoundException)
            {
                Debug.WriteLine($"[!] DirectoryList - ERROR: Directory not found: {path}");
                implant.Profile.SendError(task.id, "Error: Directory not found.");
                task.status = "error";
                task.message = "Directory not found.";
            }
            catch (Exception e)
            {
                Debug.WriteLine($"DirectoryList - ERROR: {e.Message}");
                implant.Profile.SendError(task.id, $"Error: {e.Message}");
                task.status = "error";
                task.message = e.Message;
            }
        }
    }
}
