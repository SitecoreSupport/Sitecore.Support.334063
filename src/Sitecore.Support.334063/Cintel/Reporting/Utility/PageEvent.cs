using System;
using System.Globalization;
using Sitecore.DependencyInjection;
using Sitecore.Marketing.Definitions;
using Sitecore.Marketing.Definitions.Goals;

namespace Sitecore.Support.Cintel.Reporting.Utility
{
    public static class PageEvent
    {
        public static IGoalDefinition GetGoalBy(Guid id)
        {
            return !(id == Guid.Empty)
                ? ((GoalDefinitionManager) ServiceLocator.ServiceProvider.GetDefinitionManagerFactory()
                    .GetDefinitionManager<IGoalDefinition>()).Get(id, CultureInfo.InvariantCulture)
                : null;
        }
    }
}