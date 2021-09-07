//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent.Models;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Monitoring.VNext.DiagnosticSettings.Model.Builders
{
    public class DiagnosticSettingsResourceModelBuilder : DiagnosticSettingsModelBuilderBase
    {
        private const int LocalCacheTTLInMin = 60 * 24;
        private const string LogCategoriesCachKeyPrefix = "LogCagtegories_CacheKey_";
        private readonly IMemoryCache _localCache;
        private readonly DiagnosticSettingsHelper _dsHelper;
        private readonly ILogger _logger;

        public DiagnosticSettingsResourceModelBuilder(IMemoryCache localCache, DiagnosticSettingsHelper dsHelper, ILogger logger)
        {
            _localCache = localCache ?? throw new ArgumentNullException(nameof(localCache));
            _dsHelper = dsHelper ?? throw new ArgumentNullException(nameof(dsHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<List<DiagnosticSettingsLogsOrMetricsModel>> BuildAllLogsDiagnosticSettingsPropertyAsync(IAzure fluentClient, string monitoredResourceId)
        {
            if (fluentClient == null)
            {
                throw new ArgumentNullException(nameof(fluentClient));
            }

            var res = new List<DiagnosticSettingsLogsOrMetricsModel>();
            var categories = await GetLogsCategoriesForResourceAsync(fluentClient, monitoredResourceId);
            
            return categories.Select(category =>
            {
               var logCategory = new DiagnosticSettingsLogsOrMetricsModel();
               logCategory.Category = category.Name;
               logCategory.Enabled = true;
               return logCategory;
            }).ToList();
        }

        private async Task<List<IDiagnosticSettingsCategory>> GetLogsCategoriesForResourceAsync(IAzure fluentClient, string resourceId)
        {
            string resourceProviderType = _dsHelper.ExtractFullyQualifiedResourceProviderType(resourceId);
            _logger.Information("Started getting log categories for resource {resourceId} with fully qualified resourceProviderType {resourceProviderType}", resourceId, resourceProviderType);

            var logCategories = _localCache.Get<List<IDiagnosticSettingsCategory>>(GetLogCategoryCacheKey(resourceProviderType));

            if (logCategories == null) {
                var categories = await fluentClient.DiagnosticSettings
                        .ListCategoriesByResourceAsync(resourceId) ?? new List<IDiagnosticSettingsCategory>();

                logCategories = categories.Where(c => c.Type == CategoryType.Logs).ToList();

                _logger.Information("Setting log categories cache for resource provider type {resourceProviderType}", resourceProviderType);
                _localCache.Set<List<IDiagnosticSettingsCategory>>(GetLogCategoryCacheKey(resourceProviderType), logCategories, 1, TimeSpan.FromMinutes(LocalCacheTTLInMin));
            }
            else
            {
                _logger.Information("Retrieved logs categories successfully from local cache for resource provider type {resourceProviderType}", resourceProviderType);
            }

            return logCategories;
        }

        private string GetLogCategoryCacheKey(string resourceProviderType)
        {
            return $"{LogCategoriesCachKeyPrefix}-{resourceProviderType}";
        }
    }
}