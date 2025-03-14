using GrouperLib.Backend;
using GrouperLib.Config;
using GrouperLib.Core;
using GrouperLib.Database;
using GrouperLib.Store;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Timers;

namespace GrouperService
{
    [SupportedOSPlatform("windows")]
    internal partial class Worker
    {
        private const int _workInterval = 10000; // run every 10 seconds
        private int _lastFullProcessHour = 0;
        private readonly EventLog _eventLog;
        private Timer _timer;
        private Grouper _grouper;
        private DocumentDb _documentDb;
        private LogDb _logDb;
        private bool _stopRequested;
        private Dictionary<Guid, DateTime> _lastProcessedDictionary;
        private readonly GrouperConfiguration _config;

        public Worker()
        {
            _config = GrouperConfiguration.CreateFromAppSettings(ConfigurationManager.AppSettings);
        }

        public Worker(EventLog eventLog)
        {
            _eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
            _config = GrouperConfiguration.CreateFromAppSettings(ConfigurationManager.AppSettings);
        }

        public void Start()
        {
            _stopRequested = false;
            SetupDatabases();
            SetupGrouper();
            FillLastProcessedDictionary();
            SetupTimer();
        }

        public void Stop()
        {
            _stopRequested = true;
            DisposeTimer();
        }

        private void SetupGrouper()
        {
            _grouper?.Dispose();
            _grouper = Grouper.CreateFromConfig(_config);
        }

        private void SetupDatabases()
        {
            _documentDb = new DocumentDb(_config, "Grouper Service");
            _logDb = new LogDb(_config);
        }

        private void SetupTimer()
        {
            _timer = new Timer(_workInterval);
            _timer.Elapsed += InvokeGrouper;
            _timer.AutoReset = false;
            _timer.Enabled = true;
        }

        private void FillLastProcessedDictionary()
        {
            _lastProcessedDictionary = 
                _documentDb.GetEntriesByProcessingInterval(min: 1).GetAwaiter().GetResult()
                .ToDictionary(x => x.Document.Id, x => DateTime.Now);
        }

        private void DisposeTimer()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void WriteToEventLog(string message, EventLogEntryType entryType)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine(message);
            }
            else
            {
                _eventLog.WriteEntry(message, entryType);
            }
        }

        private void WriteToLogDb(GrouperDocument document, string message, LogLevel logLevel)
        {
            EventLogItem logItem = new(document, message, logLevel);

            if (Environment.UserInteractive)
            {
                Console.WriteLine(logItem.ToString());
            }
            else
            {
                _logDb.StoreEventLogItemAsync(logItem).GetAwaiter().GetResult();
            }
        }

        private bool ShouldProcessAllDocuments()
        {
            int hour = DateTime.Now.Hour;
            return _lastFullProcessHour != hour && (hour == 6 || hour == 12 || hour == 16);
        }

        // All calls to an empty partial method are optimized out
        static partial void DebugPrint(string str, params object[] args);

        [Conditional("DEBUG")]
        static partial void DebugPrint(string str, params object[] args)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " : " + str, args);
            }
        }

        private void InvokeGrouper(object source, ElapsedEventArgs e)
        {
            Timer timer = (Timer)source;
            List<GrouperDocumentEntry> entries = [];
            bool processAllDocuments = ShouldProcessAllDocuments();
            if (processAllDocuments)
            {
                _lastFullProcessHour = DateTime.Now.Hour;
                entries.AddRange(_documentDb.GetAllEntriesAsync().GetAwaiter().GetResult());
            }
            else
            {
                // Get documents that changed since the last timer event.
                // It doesn't matter if we miss a document. It will be processed in the next full run
                DateTime start = DateTime.Now.AddMilliseconds(-_workInterval);
                try
                {
                    entries.AddRange(_documentDb.GetEntriesByAgeAsync(start).GetAwaiter().GetResult());
                }
                catch (Exception ex)
                {
                    WriteToEventLog(ex.ToString(), EventLogEntryType.Error);
                    throw;
                }
                // Get documents that have a processing interval hint and where the interval has passed.
                IEnumerable<GrouperDocumentEntry> entriesWithInterval =
                    _documentDb.GetEntriesByProcessingInterval(min: 1).GetAwaiter().GetResult();
                foreach (GrouperDocumentEntry entry in entriesWithInterval)
                {
                    if (_lastProcessedDictionary.TryGetValue(entry.Document.Id, out DateTime lastProcessed))
                    {
                        if (lastProcessed.AddMinutes(entry.Document.Interval) < DateTime.Now)
                        {
                            entries.Add(entry);
                        }
                        // If it's not in the dictionary, process interval was added to the document
                        // after the service started and the document should be picked up by the
                        // document age check above (this includes both new and changed documents)
                    }
                }
            }
            foreach (GrouperDocumentEntry entry in entries)
            {
                DebugPrint("Processing group {0}", entry.GroupName);
                if (_stopRequested)
                {
                    return;
                }
                GroupMemberDiff diff;
                try
                {
                    diff = _grouper.GetMemberDiffAsync(entry.Document).GetAwaiter().GetResult();
                    _grouper.UpdateGroupAsync(diff).GetAwaiter().GetResult();
                    if (entry.Document.Interval> 0)
                    {
                        _lastProcessedDictionary[entry.Document.Id] = DateTime.Now;
                    }
                    DebugPrint("Processed group {0}. {1} added, {2} removed.", entry.GroupName, diff.Add.Count(), diff.Remove.Count());
                }
                catch(Exception ex)
                {
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message = ex.InnerException.Message;
                    }
                    WriteToLogDb(entry.Document, message, LogLevel.Error);
                    if (!(ex is GroupNotFoundException || ex is MemberNotFoundException || ex is ChangeRatioException))
                    {
                        WriteToEventLog(message, EventLogEntryType.Error);
                    }
                }
                finally
                {
                    diff = null;
                }
            }
            if (processAllDocuments)
            {
                SetupGrouper();
            }
            if (!_stopRequested)
            {
                timer.Enabled = true;
            }
        }
    }
}
