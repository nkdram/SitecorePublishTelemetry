using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace RD.PublishTelemetry
{
   
    public static class CustomTelemetryService
    {
        public static TelemetryClient Initialize()
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.Active;
            ///Get the instrumentation key from Sitecore Connectionstring settings
            configuration.InstrumentationKey =  ConfigurationManager.ConnectionStrings["appinsights.instrumentationkey"].ConnectionString;
            return new TelemetryClient(configuration);
        }
    }

}