using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Jobs;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace RD.PublishTelemetry.Events
{
    public class ItemPublishTracker
    {
       

        public void PublishEnd(object sender, EventArgs args)
        {
            try
            {
                Publisher publisher = Event.ExtractParameter(args, 0) as Publisher;
                if (publisher == null)
                {
                    Log.Info("PUBLISH TELEMETRY LOG: Cancel Process: Publisher Data is Null.", this);
                }

                // if publishing is not on Web Target than do nothing
                if (publisher.Options == null ||
                    publisher.Options.TargetDatabase == null ||
                    string.IsNullOrEmpty(publisher.Options.TargetDatabase.Name))
                {
                    Log.Info("PUBLISH TELEMETRY LOG: Cancel Process: Publisher.Options Data is Null.", this);
                }

                Item currentItem = publisher.Options.RootItem;
                Job job = JobManager.GetJob(publisher.GetJobName());
                var messages = job.Status.Messages;
                // E.g. "AUDIT (sitecore\admin): PublishProcessed item: master:/sitecore/content/Home, language: en, version: 3, id: {110D559F-DEA5-42EA-9C1C-8A5DF7E70EF9}"
                //Log.Audit(string.Format("AUDIT ({0}): {1}: {2}:{3}, language: {4}, version: {5}, id: {6} to {7}", publisher.Options.UserName, "Publish Mode:" + publisher.Options.Mode.ToString() + "|Deep: " + publisher.Options.Deep.ToString() + "|items Processed: [" + GetUnitsProcessedInfo(messages) + "]", currentItem.Database.Name, currentItem.Paths.Path, currentItem.Language.Name, currentItem.Version.Number.ToString(), currentItem.ID.ToString(), publisher.Options.TargetDatabase.Name), this);

                //Record in Azure Telemetry
                TrackPublishMetric(GetUnitsProcessedInfo(messages), publisher.Options.UserName, currentItem.ID.ToString(), publisher.Options.Mode.ToString(), publisher.Options.Deep.ToString(), currentItem.Database.Name, currentItem.Paths.FullPath, publisher.Options.Language.Name, publisher.Options.TargetDatabase.Name);
            }
            catch (Exception e)
            {
                Log.Error("PUBLISH TELEMETRY LOG: Could not correctly retrieve the published end item in Sitecore. " + e.ToString(), this);
            }
        }

        private int GetUnitsProcessedInfo(System.Collections.Specialized.StringCollection Messages)
        {
            int processItems = 0;
            foreach (var msg in Messages)
            {
                var result = Regex.Match(msg, @"\d+$").Value;
                int processedNum = 0;
                if (int.TryParse(result, out processedNum))
                    processItems += processedNum;
            }
            return processItems;
        }

        private void TrackPublishMetric(int value,string userName, string itemId, string publishMode, string subitems, string databaseName, string itemPath, string publishedLanguage, string targetDatabase)
        {
            TelemetryClient client = CustomTelemetryService.Initialize();

            var publishMetric = new MetricTelemetry("sitecore:publishevent", value);
            publishMetric.Timestamp = DateTime.Now;

            var properties = publishMetric.Context.Properties;
            properties.Add("UserName", userName);
            properties.Add("SitecoreItemID", itemId);
            properties.Add("SitecoreItemPath", itemPath);
            properties.Add("SitecoreItemLanguage", publishedLanguage);
            properties.Add("PublishMode", publishMode);
            properties.Add("SubItemsSelected", subitems);
            properties.Add("DataBase", databaseName);
            properties.Add("TargetDatabase", targetDatabase);
            //Sitecore Instance Name
            properties.Add("InstanceName", Sitecore.Configuration.Settings.InstanceName);
           
            //Track Metric
            client.TrackMetric(publishMetric);
            //Track Event
            client.TrackEvent("sitecore:publishevent", properties);

        }
    }
}