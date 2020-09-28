//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceGraph.Models;
using Microsoft.Liftr.Monitoring.Common.Models;
using Microsoft.Liftr.Monitoring.Whale;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Liftr.Monitoring.Whale.Tests
{
    public class KustoQueryBuilderTests
    {
        #region Properties

        public static IEnumerable<object[]> OnlyInclusionTags
        {
            get
            {
                return new[]
                {
                    new object[]
                    {
                        // List of filtering tags
                        new List<FilteringTag>()
                        {
                            new FilteringTag()
                                { Name = "a", Value = "b", Action = TagAction.Include },
                            new FilteringTag()
                                { Name = "c", Value = "d", Action = TagAction.Include },
                        },

                        // Expected query
                        "where (tags[\"a\"] == \"b\") or (tags[\"c\"] == \"d\") | project id, location",
                    },
                    new object[]
                    {
                        // List of filtering tags
                        new List<FilteringTag>()
                        {
                            new FilteringTag()
                            {
                                Name = "Abraham Lincoln",
                                Value = "Don't believe everything you read on the internet",
                                Action = TagAction.Include,
                            },
                        },

                        // Expected query
                        "where (tags[\"Abraham Lincoln\"] == \"Don't believe everything you read on the internet\") | project id, location",
                    },
                };
            }
        }

        public static IEnumerable<object[]> OnlyExclusionTags
        {
            get
            {
                return new[]
                {
                    new object[]
                    {
                        // List of filtering tags
                        new List<FilteringTag>()
                        {
                            new FilteringTag()
                                { Name = "*", Value = "*", Action = TagAction.Exclude },
                        },

                        // Expected query
                        "where not ((tags[\"*\"] == \"*\")) | project id, location",
                    },
                    new object[]
                    {
                        // List of filtering tags
                        new List<FilteringTag>()
                        {
                            new FilteringTag()
                                { Name = "\"", Value = string.Empty, Action = TagAction.Exclude },
                        },

                        // Expected query
                        "where not ((isempty(tags[\"\\\"\"]) and isnotnull(tags[\"\\\"\"]))) | project id, location",
                    },
                };
            }
        }

        public static IEnumerable<object[]> MixedTags
        {
            get
            {
                return new[]
                {
                    new object[]
                    {
                        // List of filtering tags
                        new List<FilteringTag>()
                        {
                            new FilteringTag()
                                { Name = "a", Value = "b", Action = TagAction.Include },
                            new FilteringTag()
                                { Name = "c", Value = "d", Action = TagAction.Exclude },
                        },

                        // Expected query
                        "where (tags[\"a\"] == \"b\") | where not ((tags[\"c\"] == \"d\")) | project id, location",
                    },
                };
            }
        }

        public static IEnumerable<object[]> EmptyOrNullTags
        {
            get
            {
                return new[]
                {
                    new object[]
                    {
                        // List of filtering tags
                        null,

                        // Expected query
                        "project id, location",
                    },
                    new object[]
                    {
                        // List of filtering tags
                        new List<FilteringTag>(),

                        // Expected query
                        "project id, location",
                    },
                };
            }
        }

        #endregion

        #region Tests

        [Theory]
        [MemberData(nameof(OnlyInclusionTags))]
        [MemberData(nameof(OnlyExclusionTags))]
        [MemberData(nameof(MixedTags))]
        [MemberData(nameof(EmptyOrNullTags))]
        public void GetQueryRequest_ReturnsExpectedValue(
            IEnumerable<FilteringTag> filteringTags, string expectedQuery)
        {
            var filterResourceTypes = false;
            var returnedQuery = KustoQueryBuilder.GetQueryRequest("mySub", filteringTags, filterResourceTypes);

            // Ensure generated kusto query is equal to expected
            Assert.Equal(expectedQuery, returnedQuery.Query);

            // Ensure only passed subscription is being used
            Assert.Single(returnedQuery.Subscriptions);
            Assert.Equal("mySub", returnedQuery.Subscriptions.First());

            // Ensure other properties have expected value
            Assert.Equal(1000, returnedQuery.Options.Top);
            Assert.Equal(ResultFormat.ObjectArray, returnedQuery.Options.ResultFormat);
            Assert.Null(returnedQuery.Options.Skip);
            Assert.Null(returnedQuery.Options.SkipToken);
        }

        #endregion
    }
}
