using System.Collections.Generic;

namespace MiniFactory.Services.Analytics
{
    public interface IAnalyticsProvider
    {
        void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters);
    }
}
