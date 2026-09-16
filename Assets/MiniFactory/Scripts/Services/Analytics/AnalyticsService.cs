using System.Collections.Generic;

namespace MiniFactory.Services.Analytics
{
    // Fan-out to N providers at once (Console today; Firebase/AppMetrica could
    // register alongside it later without gameplay code changes).
    public class AnalyticsService
    {
        private readonly List<IAnalyticsProvider> _providers = new List<IAnalyticsProvider>();

        public void RegisterProvider(IAnalyticsProvider provider)
        {
            if (provider != null && !_providers.Contains(provider))
                _providers.Add(provider);
        }

        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            for (int i = 0; i < _providers.Count; i++)
                _providers[i].LogEvent(eventName, parameters);
        }
    }
}
