using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniFactory.Services.Analytics
{
    public class ConsoleAnalyticsProvider : IAnalyticsProvider
    {
        public void LogEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                Debug.Log($"[Analytics] {eventName}");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("[Analytics] ").Append(eventName).Append(" {");
            bool first = true;
            foreach (var kvp in parameters)
            {
                if (!first) sb.Append(", ");
                sb.Append(kvp.Key).Append('=').Append(kvp.Value);
                first = false;
            }
            sb.Append('}');
            Debug.Log(sb.ToString());
        }
    }
}
