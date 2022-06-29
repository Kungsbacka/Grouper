using System;
using System.IO;
using System.ServiceProcess;

namespace GrouperService
{
    static class Program
    {
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Intreactive mode - press <enter> to stop...");
                Worker worker = new Worker();
                worker.Start();
                Console.ReadLine();
                worker.Stop();
            }
            else
            {
                ServiceBase[] servicesToRun;
                servicesToRun = new ServiceBase[] { new GrouperService() };
                ServiceBase.Run(servicesToRun);
            }
        }
    }
}
