using System;
using System.Data;
using Sitecore.Cintel.Commons;
using Sitecore.Cintel.Reporting;
using Sitecore.Cintel.Reporting.Contact.Goal;
using Sitecore.Cintel.Reporting.Processors;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Support.Cintel.Reporting.Utility;

namespace Sitecore.Support.Cintel.Reporting.Contact.Goal.Processors
{
    public class ApplyMasterDataToGoals : ReportProcessorBase
    {
        internal static NotificationMessage GoalMasterDataMissing
        {
            get
            {
                var message = new NotificationMessage
                {
                    Id = 0x29,
                    MessageType = NotificationTypes.Warning,
                    Text = Translate.Text("One or more goals data is missing from the master list")
                };

                return message;
            }
        }

        public override void Process(ReportProcessorArgs args)
        {
            var resultTableForView = args.ResultTableForView;
            Assert.IsNotNull(resultTableForView, "Result table for {0} could not be found.",
                (object) args.ReportParameters.ViewName);

            var flag = false;
            foreach (var dataRow in resultTableForView.AsEnumerable())
            {
                var goalBy = PageEvent.GetGoalBy(((Guid?) dataRow[Schema.GoalId.Name]).GetValueOrDefault());
                if (goalBy != null)
                {
                    dataRow[Schema.GoalDisplayName.Name] =
                        string.IsNullOrEmpty(goalBy.Alias) ? goalBy.Name : goalBy.Alias;
                }
                else
                {
                    flag = true;
                }
            }

            if (!flag)
            {
                return;
            }

            LogNotificationForView(args.ReportParameters.ViewName, GoalMasterDataMissing);
        }
    }
}