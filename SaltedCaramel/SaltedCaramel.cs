using Profiles;

namespace SaltedCaramel
{
    class SaltedCaramel
    {
        static void Main(string[] args)
        {
            DefaultProfile profile = new DefaultProfile();
            SCImplant implant = new SCImplant(profile);
            implant.Start();
        }
    }
}
