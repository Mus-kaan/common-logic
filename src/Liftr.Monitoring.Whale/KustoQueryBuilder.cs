//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceGraph.Models;
using Microsoft.Liftr.Monitoring.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Liftr.Monitoring.Whale
{
    public static class KustoQueryBuilder
    {
        private const string ProjectStatement = "project id, location";

        private static string BuildFilterLineForResourceTypes
        {
            get
            {
                var quotedResourceTypes = SupportedResourceTypes.s_resourceTypes.Select(r => "\"" + r + "\"");
                var concatenatedResourceTypes = string.Join(", ", quotedResourceTypes);
                return $"where type in ({concatenatedResourceTypes})";
            }
        }

        /// <summary>
        /// Generates ResourceGraph query that filters resources based on tags.
        /// E.g., if tag a:b is marked for inclusion, and query c:d is marked for
        /// exclusion, query will be
        /// where (tags["a"] == b) | where not ((tags["c"] == d)) | project id, location .
        /// For more examples, see the tests at the KustoQueryBuilderTests.cs file.
        /// </summary>
        /// <param name="subscriptionId">Subscription id to be included on the ResourceGraph query request object.</param>
        /// <param name="filteringTags">List of filtering tags to include on query.</param>
        /// <param name="filterResourceTypes">Boolean flag indicating whether the filter for resource types should be included.</param>
        /// <returns>ResourceGraph query request object.</returns>
        public static QueryRequest GetQueryRequest(
            string subscriptionId, IEnumerable<FilteringTag> filteringTags, bool filterResourceTypes = true)
        {
            var queryStatements = new List<string>();

            if (filterResourceTypes)
            {
                var statementForResourceTypes = BuildFilterLineForResourceTypes;
                queryStatements.Add(statementForResourceTypes);
            }

            if (filteringTags != null)
            {
                var tagsForInclusion = filteringTags.Where(ft => ft.Action == TagAction.Include);
                var statementForInclusion = BuildFilterLineForTags(tagsForInclusion, true);
                if (!string.IsNullOrEmpty(statementForInclusion))
                {
                    queryStatements.Add(statementForInclusion);
                }

                var tagsForExclusion = filteringTags.Where(ft => ft.Action == TagAction.Exclude);
                var statementForExclusion = BuildFilterLineForTags(tagsForExclusion, false);
                if (!string.IsNullOrEmpty(statementForExclusion))
                {
                    queryStatements.Add(statementForExclusion);
                }
            }

            queryStatements.Add(ProjectStatement);

            var query = string.Join(" | ", queryStatements);

            return new QueryRequest()
            {
                Subscriptions = new List<string>() { subscriptionId },
                Query = query,
                Options = new QueryRequestOptions()
                {
                    Top = 1000,
                    ResultFormat = ResultFormat.ObjectArray,
                },
            };
        }

        private static string BuildFilterLineForTags(
            IEnumerable<FilteringTag> filteringTags, bool isInclusion)
        {
            if (!filteringTags.Any())
            {
                return string.Empty;
            }

            var individualFilters = new List<string>();

            foreach (var tag in filteringTags)
            {
                var escapedName = NormaliseString(tag.Name);
                var escapedValue = NormaliseString(tag.Value);

                if (string.IsNullOrEmpty(escapedValue))
                {
                    individualFilters.Add($"(isempty(tags[\"{escapedName}\"]) and isnotnull(tags[\"{escapedName}\"]))");
                }
                else
                {
                    individualFilters.Add($"(tags[\"{escapedName}\"] == \"{escapedValue}\")");
                }
            }

            var fullFilter = string.Join(" or ", individualFilters);
            var query = isInclusion ? $"where {fullFilter}" : $"where not ({fullFilter})";

            return query;
        }

        private static string NormaliseString(string name)
        {
            if (name == null)
            {
                return string.Empty;
            }

            return name.Replace("\"", "\\\"");
        }
    }
}
