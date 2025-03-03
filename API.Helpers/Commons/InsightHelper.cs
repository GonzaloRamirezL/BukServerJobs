using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading;

namespace API.Helpers.Commons
{
    public static class InsightHelper
    {
        
        private static TelemetryClient telemetry = new TelemetryClient()
        {
            InstrumentationKey = "b271565f-a131-4e61-ab43-131ab74d45c7"
        };

        public static void logException(Exception ex, string empresa, Dictionary<string, string> properties = null)
        {
            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }

            if (!properties.ContainsKey("EMPRESA"))
            {
                properties.Add("EMPRESA", empresa);
            }
            else
            {
                properties["EMPRESA"] = empresa;
            }
            telemetry.TrackException(ex, properties);
        }

        public static void logMetric(string metrica, TimeSpan total, Dictionary<string, string> properties = null)
        {
            var sample = new MetricTelemetry();
            sample.Name = metrica;
            sample.Sum = total.TotalMilliseconds;
            if (properties != null)
            {
                foreach (KeyValuePair<string, string> prop in properties)
                {
                    sample.Properties.Add(prop.Key, prop.Value);
                }
            }
            telemetry.TrackMetric(sample);
        }

        public static void logMetric(string metrica, int count, Dictionary<string, string> properties = null)
        {
            var sample = new MetricTelemetry();
            sample.Name = metrica;
            sample.Sum = count;
            if (properties != null)
            {
                foreach (KeyValuePair<string, string> prop in properties)
                {
                    sample.Properties.Add(prop.Key, prop.Value);
                }
            }
            

            telemetry.TrackMetric(sample);
        }

        public static void logWarning(string alerta, string empresa)
        {
            telemetry.TrackTrace(alerta, SeverityLevel.Warning, new Dictionary<string, string> { { "EMPRESA", empresa } });
        }
        public static void logTrace(string alerta, string empresa)
        {
            telemetry.TrackTrace(alerta, SeverityLevel.Information, new Dictionary<string, string> { { "EMPRESA", empresa } });
        }

        public static void flush()
        {
            telemetry.Flush();
            Thread.Sleep(2000);
        }
    }
}
