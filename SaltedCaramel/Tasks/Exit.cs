using System;

namespace SaltedCaramel.Tasks
{
    public class Exit
    {
        public static void Execute(SCTask task, SCImplant implant)
        {
            try
            {
                implant.Profile.SendComplete(task.id);
            }
            catch (Exception e)
            {
                implant.Profile.SendError(task.id, e.Message);
            }
            Environment.Exit(0);
        }
    }
}
