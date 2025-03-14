using System.Runtime.Versioning;
using System.ServiceProcess;

namespace GrouperService
{
    [SupportedOSPlatform("windows")]
    public partial class GrouperService : ServiceBase
    {
        private readonly Worker _worker;


        public GrouperService()
        {
            InitializeComponent();
            _worker = new(EventLog);
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
