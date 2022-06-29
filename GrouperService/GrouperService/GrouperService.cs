using System.Diagnostics;
using System.ServiceProcess;

namespace GrouperService
{
    public partial class GrouperService : ServiceBase
    {
        private readonly Worker _worker;


        public GrouperService()
        {
            InitializeComponent();
            _worker = new Worker(EventLog);
        }

        protected override void OnStart(string[] args)
        {
            _worker.Start();
        }

        protected override void OnStop()
        {
            _worker.Stop();
        }
    }
}
