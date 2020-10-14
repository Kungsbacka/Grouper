using System;
using System.ServiceProcess;

namespace GrouperService
{
    static class Program
    {
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("Debug mode - press <enter> to stop...");
                Worker worker = new Worker();
                worker.Start();
                Console.ReadLine();
                worker.Stop();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[] { new GrouperService() };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
