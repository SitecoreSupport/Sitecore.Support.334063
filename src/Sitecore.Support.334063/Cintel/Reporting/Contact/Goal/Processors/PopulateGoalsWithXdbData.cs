using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Sitecore.Analytics;
using Sitecore.Cintel.Commons;
using Sitecore.Cintel.Reporting;
using Sitecore.Cintel.Reporting.Contact.Goal;
using Sitecore.Cintel.Reporting.Processors;
using Sitecore.Globalization;
using Sitecore.Marketing.Definitions.PageEvents;
using Sitecore.Support.Cintel.Reporting.Utility;

namespace Sitecore.Support.Cintel.Reporting.Contact.Goal.Processors
{
    public class PopulateGoalsWithXdbData : ReportProcessorBase
    {
        private IEnumerable<IPageEventDefinition> _analyticsPageEvents;

        internal static NotificationMessage MandatoryDataMissing
        {
            get
            {
                var message = new NotificationMessage
                {
                    Id = 13,
                    MessageType = NotificationTypes.Error,
                    Text = Translate.Text("One or more data entries are missing due to invalid data")
                };

                return message;
            }
        }

        internal static NotificationMessage OptionalDataMissing
        {
            get
            {
                var message = new NotificationMessage
                {
                    Id = 0x13a,
                    MessageType = NotificationTypes.Warning,
                    Text = Translate.Text("Some columns may be missing data")
                };

                return message;
            }
        }

        public override void Process(ReportProcessorArgs args)
        {
            var queryResult = args.QueryResult;
            var resultTableForView = args.ResultTableForView;
            ProjectRawTableIntoResultTable(args, queryResult, resultTableForView);
        }

        private void ProjectRawTableIntoResultTable(
            ReportProcessorArgs args,
            DataTable rawTable,
            DataTable resultTable)
        {
            _analyticsPageEvents = Tracker.MarketingDefinitions.PageEvents;
            FilterTable(rawTable, RowShouldBeRemoved);

            var dictionary1 = rawTable.AsEnumerable()
                .GroupBy(r => r.Field<Guid>("Pages_PageEvents_PageEventDefinitionId"))
                .ToDictionary(e => e.Key, e => e.Count());

            var goalsWithTotalValue = GetGoalsWithTotalValue(rawTable.AsEnumerable()
                .GroupBy(r => r.Field<Guid>("Pages_PageEvents_PageEventDefinitionId")));

            var dictionary2 = rawTable.AsEnumerable()
                .Select(r => r.Field<Guid>("Pages_PageEvents_PageEventDefinitionId")).Distinct()
                .ToDictionary(e => e, e => 1);

            var flag1 = false;
            var flag2 = false;

            foreach (var sourceRow in rawTable.AsEnumerable()
                .OrderBy(r => r.Field<DateTime?>("Pages_PageEvents_DateTime")))
            {
                var dataRow = resultTable.NewRow();
                if (!FillMandatoryData(sourceRow, dataRow))
                {
                    flag1 = true;
                }
                else
                {
                    var goalId = (Guid) dataRow[Schema.GoalId.Name];
                    dataRow[Schema.ConversionIndexAcrossVisits.Name] = dictionary2[goalId];
                    dictionary2[goalId]++;
                    FillValueFromLookup(dataRow, Schema.ConversionCountAcrossVisits.Name, dictionary1, goalId);
                    FillValueFromLookup(dataRow, Schema.TotalGoalValueAcrossVisits.Name, goalsWithTotalValue, goalId);
                    if (!FillOptionalData(sourceRow, dataRow))
                    {
                        flag2 = true;
                    }

                    resultTable.Rows.Add(dataRow);
                }
            }

            if (flag1)
            {
                LogNotificationForView(args.ReportParameters.ViewName, MandatoryDataMissing);
            }

            if (!flag2)
            {
                return;
            }

            LogNotificationForView(args.ReportParameters.ViewName, OptionalDataMissing);
        }

        private static void FillValueFromLookup(
            DataRow resultRow,
            string columnName,
            IDictionary<Guid, int> lookup,
            Guid goalId)
        {
            resultRow[columnName] = lookup[goalId];
        }

        private bool FillOptionalData(DataRow sourceRow, DataRow resultRow)
        {
            var flag = TryFillData(resultRow, Schema.ConversionDateTime, sourceRow, "Pages_PageEvents_DateTime") &
                       TryFillData(resultRow, Schema.HistoricalGoalValue, sourceRow, "Pages_PageEvents_Value") &
                       TryFillData(resultRow, Schema.PageItemId, sourceRow, "Pages_Item__id") &
                       TryFillPagePathAndQuery(resultRow, Schema.HistoricalUrl, sourceRow);
            TryFillData(resultRow, Schema.SiteName, sourceRow, "SiteName");
            return flag;
        }

        private bool FillMandatoryData(DataRow sourceRow, DataRow resultRow)
        {
            return TryFillData(resultRow, Schema.ContactId, sourceRow, "ContactId") &&
                   TryFillData(resultRow, Schema.VisitId, sourceRow, "_id") && TryFillData(resultRow, Schema.GoalId,
                       sourceRow, "Pages_PageEvents_PageEventDefinitionId");
        }

        private static Dictionary<Guid, int> GetGoalsWithTotalValue(
            IEnumerable<IGrouping<Guid, DataRow>> groupedEvents)
        {
            var dictionary = new Dictionary<Guid, int>();
            foreach (var groupedEvent in groupedEvents)
            {
                var num = groupedEvent.Where(r => r.Field<int?>("Pages_PageEvents_Value").HasValue)
                    .Sum(r => r.Field<int>("Pages_PageEvents_Value"));
                dictionary.Add(groupedEvent.Key, num);
            }

            return dictionary;
        }

        private bool RowShouldBeRemoved(DataRow dataRow)
        {
            return PageEvent.GetGoalBy(dataRow.Field<Guid?>("Pages_PageEvents_PageEventDefinitionId")
                       .GetValueOrDefault()) == null;
        }
    }
}