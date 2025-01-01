using GrouperLib.Core;

namespace GrouperLib.Database;

public interface ILogger
{
    Task StoreEventLogItemAsync(EventLogItem logItem);
    Task StoreOperationalLogItemAsync(OperationalLogItem logItem);
}