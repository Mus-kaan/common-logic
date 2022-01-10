//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Liftr.Monitoring.VNext.Whale.Client.Interfaces;
using Microsoft.Liftr.Monitoring.VNext.Whale.Models;
using Newtonsoft.Json;
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
        private readonly IMemoryCache _localCache;
        private readonly DiagnosticSettingsHelper _dsHelper;
        private readonly ILogger _logger;

        public DiagnosticSettingsResourceModelBuilder(IMemoryCache localCache, DiagnosticSettingsHelper dsHelper, ILogger logger)
        {
            _localCache = localCache ?? throw new ArgumentNullException(nameof(localCache));
            _dsHelper = dsHelper ?? throw new ArgumentNullException(nameof(dsHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<List<DiagnosticSettingsLogsOrMetricsModel>> BuildAllLogsDiagnosticSettingsPropertyAsync(IArmClient _armClient, string monitoredResourceId, string DiagnosticSettingsV2ApiVersion, string tenantId)
        {
            if (_armClient == null)
            {
                throw new ArgumentNullException(nameof(_armClient));
            }

            if (tenantId == null)
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            var res = new List<DiagnosticSettingsLogsOrMetricsModel>();
            var categories = await GetLogsCategoriesForResourceAsync(_armClient, monitoredResourceId, DiagnosticSettingsV2ApiVersion, tenantId);
            
            _logger.Information("Log categories for resource {@monitoredResourceId}. Categories: {@categories}", monitoredResourceId, categories);
            
            return categories.Select(category =>
            {
                var logCategory = new DiagnosticSettingsLogsOrMetricsModel
                {
                    Category = category.Name,
                    Enabled = true
                };
                return logCategory;
            }).ToList();
        }

        private async Task<List<DiagnosticSettingsCategoryResource>> GetLogsCategoriesForResourceAsync(IArmClient _armClient, string resourceId, string DiagnosticSettingsV2ApiVersion, string tenantId)
        {
            string resourceProviderType = _dsHelper.ExtractFullyQualifiedResourceProviderType(resourceId);
            _logger.Information("Started getting log categories for resource {@resourceId} with fully qualified resourceProviderType {@resourceProviderType}", resourceId, resourceProviderType);

            List<DiagnosticSettingsCategoryResource> logCategories = null;

            if (logCategories == null) {
                DiagnosticSettingsCategoryResourceList categories = await ListCategoriesByResourceAsync(_armClient, resourceId, DiagnosticSettingsV2ApiVersion, tenantId) ?? new DiagnosticSettingsCategoryResourceList();
                List<DiagnosticSettingsCategoryResource> categoriesValue = categories.Value;
                logCategories = categoriesValue.Where(c => c.Properties.CategoryType.Equals(CategoryType.Logs)).ToList();

                _logger.Information("Setting log categories cache for resource provider type {@resourceProviderType}", resourceProviderType);
                _localCache.Set<List<DiagnosticSettingsCategoryResource>>(GetLogCategoryCacheKey(resourceProviderType), logCategories, 1, TimeSpan.FromMinutes(LocalCacheTTLInMin));
            }
            else
            {
                _logger.Information("Retrieved logs categories successfully from local cache for resource provider type {@resourceProviderType}", resourceProviderType);
            }

            return logCategories;
        }

        private async Task<DiagnosticSettingsCategoryResourceList> ListCategoriesByResourceAsync(IArmClient _armClient, string resourceId, string apiVersion, string tenantId)
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentNullException(nameof(resourceId));
            }

            if (string.IsNullOrWhiteSpace(apiVersion))
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            _logger.Information($"Fetching list of categories for resource: {resourceId} and tenant: {tenantId}");
            string armDiagnosticSettingsCategoryList = Constants.ArmDiagnosticSettingsCategoryList;
            string response = await _armClient.GetResourceAsync(resourceId + armDiagnosticSettingsCategoryList, apiVersion, tenantId);
            _logger.Information($"Finished getting list of categories for resource: {resourceId}. Categories: {response}");
            
            return JsonConvert.DeserializeObject<DiagnosticSettingsCategoryResourceList>(response);
        }

        private string GetLogCategoryCacheKey(string resourceProviderType)
        {
            return $"{Constants.LogCategoriesCachKeyPrefix}-{resourceProviderType}";
        }
    }
}