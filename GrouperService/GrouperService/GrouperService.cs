using System.Diagnostics;
using System.ServiceProcess;

namespace GrouperService
{
    public partial class GrouperService : ServiceBase
    {
        private readonly EventLog _eventLog;
        private readonly Worker _worker;

        public GrouperService()
        {
            InitializeComponent();
            _eventLog = new EventLog()
            {
                Log = "Application",
                Source = "Application"
            };
            _worker = new Worker(_eventLog);
        }

        protected override void OnStart(string[] args)
        {
            _worker.Start();
            _eventLog.WriteEntry("Grouper service started", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _worker.Stop();
            _eventLog.WriteEntry("Grouper service stopped", EventLogEntryType.Information);
        }
    }
}
