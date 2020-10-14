using GrouperLib.Core;
using System.Threading.Tasks;

namespace GrouperLib.Database
{
    public interface ILogger
    {
        Task StoreEventLogItemAsync(EventLogItem logItem);
        Task StoreOperationalLogItemAsync(OperationalLogItem logItem);
    }
}