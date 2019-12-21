//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Fluent
{
    public static class DeploymentExtensions
    {
        public static async Task<string> GetDeploymentDetailsAsync(string subscriptionId, string resourceGroupName, string deploymentName, AzureCredentials credentials)
        {
            // There is no way to get error details using the Azure .NET library. That is a known issue that should
            // be fixed in the future. See https://github.com/Azure/azure-libraries-for-net/issues/39.
            // In the mean time, a direct call is made to the Azure REST API to get the details.
            using (var handler = new AzureApiAuthHandler(credentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(credentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path =
                    $"subscriptions/{subscriptionId}/resourcegroups/{resourceGroupName}/providers/Microsoft.Resources/deployments/{deploymentName}";
                uriBuilder.Query = "api-version=2018-05-01";

                var response = await httpClient.GetStringAsync(uriBuilder.Uri);
                return response;
            }
        }

        public static async Task<DeploymentError> GetDeploymentErrorDetailsAsync(string subscriptionId, string resourceGroupName, string deploymentName, AzureCredentials credentials)
        {
            var response = await GetDeploymentDetailsAsync(subscriptionId, resourceGroupName, deploymentName, credentials);
            var httpResult = response.FromJson<AzureResponseJson>();
            return httpResult.Properties.Error;
        }
    }

    public class AzureResponseJson
    {
        public DeploymentPropertiesJson Properties { get; set; }
    }

    public class DeploymentPropertiesJson
    {
        public DeploymentError Error { get; set; }
    }

    public class DeploymentError
    {
        public string Code { get; set; }

        public string Message { get; set; }

        public DeploymentErrorDetailsJson[] Details { get; set; }
    }

    public class DeploymentErrorDetailsJson
    {
        public string Code { get; set; }

        public string Message { get; set; }
    }
}
