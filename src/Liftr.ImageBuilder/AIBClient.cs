//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    /// <summary>
    /// Client for helping with Azure VM Image Builder interaction.
    /// </summary>
    public class AIBClient : IAIBClient
    {
        private const string c_AIBAPIVersion = "2020-02-14";
        private const string c_aibVHDRunOutputName = "liftr-vhd-output";
        private readonly ILiftrAzure _liftrAzure;
        private readonly Serilog.ILogger _logger;

        public AIBClient(ILiftrAzure liftrAzureClient, Serilog.ILogger logger)
        {
            _liftrAzure = liftrAzureClient ?? throw new ArgumentNullException(nameof(liftrAzureClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> CreateNewSBIVersionByRunAzureVMImageBuilderAsync(
            Region location,
            string rgName,
            string templateName,
            string templateContent,
            CancellationToken cancellationToken = default)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            _logger.Information("Start uploading AIB template in RG {rgName} ...", rgName);
            try
            {
                var deployment = await _liftrAzure.CreateDeploymentAsync(location, rgName, templateContent);
                _logger.Information("Created AIB template in RG {rgName}. Deployment CorrelationId: {DeploymentCorrelationId}", rgName, deployment.CorrelationId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed at uploading AIB template. ");
                throw;
            }

            // https://github.com/Azure/azure-rest-api-specs-pr/blob/87dbc20106afce8c615113d654c14359a3356486/specification/imagebuilder/resource-manager/Microsoft.VirtualMachineImages/preview/2019-05-01-preview/imagebuilder.json#L328
            // https://github.com/Azure/azure-rest-api-specs-pr/blob/87dbc20106afce8c615113d654c14359a3356486/specification/imagebuilder/resource-manager/Microsoft.VirtualMachineImages/preview/2019-05-01-preview/examples/RunImageTemplate.json
            using (var operation = _logger.StartTimedOperation(nameof(CreateNewSBIVersionByRunAzureVMImageBuilderAsync)))
            using (var handler = new AzureApiAuthHandler(_liftrAzure.AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                operation.SetProperty(nameof(templateName), templateName);
                operation.SetProperty(nameof(rgName), rgName);
                operation.SetProperty(nameof(location), location.Name);

                _logger.Information("Run AIB template. rgName: {rgName}. templateName: {templateName}", rgName, templateName);
                _logger.Information("The Azure VM Image Builder logs can be found in the storage account in {AIBRGName}", RunAzureVMImageBuilderException.PackaerLogLocaionMessage(_liftrAzure.FluentClient.SubscriptionId, rgName, templateName));

                var uriBuilder = new UriBuilder(_liftrAzure.AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path =
                    $"/subscriptions/{_liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.VirtualMachineImages/imageTemplates/{templateName}/run";
                uriBuilder.Query = $"api-version={c_AIBAPIVersion}";
                var startRunResponse = await httpClient.PostAsync(uriBuilder.Uri, null);

                if (startRunResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    var asyncOperationResponse = await WaitAsyncOperationAsync(httpClient, startRunResponse, cancellationToken);
                    if (!asyncOperationResponse.OrdinalContains("Succeeded"))
                    {
                        operation.FailOperation(asyncOperationResponse);
                        _logger.Error("Failed at running AIB template. asyncOperationResponse: {@asyncOperationResponse}", asyncOperationResponse);
                        var ex = new RunAzureVMImageBuilderException("Failed at running the Azure VM Image Builder template. There might be some issues in the 'bake-image.sh' or 'bakeImage.ps1' script. ", _liftrAzure.FluentClient.SubscriptionId, rgName, templateName);
                        _logger.Fatal(ex.Message);
                        throw ex;
                    }

                    return asyncOperationResponse;
                }

                string errorMessage = $"Failed at running AIB template with status code {startRunResponse.StatusCode}.";
                if (startRunResponse.Content != null)
                {
                    errorMessage = $"{errorMessage} Erros response: {await startRunResponse.Content.ReadAsStringAsync()}. ";
                }

                operation.FailOperation(errorMessage);
                _logger.Error(errorMessage);
                throw new RunAzureVMImageBuilderException(errorMessage, _liftrAzure.FluentClient.SubscriptionId, rgName, templateName);
            }
        }

        public async Task<string> GetGeneratedVDHSASAsync(
            string rgName,
            string templateName,
            CancellationToken cancellationToken = default)
        {
            var runOutput = await GetAIBRunOutputAsync(rgName, templateName, c_aibVHDRunOutputName, cancellationToken);
            if (TryExtractVHDSASFromRunOutput(runOutput, out var sas))
            {
                return sas;
            }
            else
            {
                throw new InvalidOperationException("Cannot parse the AIB run output: " + runOutput);
            }
        }

        public async Task<string> GetAIBTemplateAsync(
            string rgName,
            string templateName,
            CancellationToken cancellationToken = default)
        {
            using (var operation = _logger.StartTimedOperation(nameof(GetAIBTemplateAsync)))
            using (var handler = new AzureApiAuthHandler(_liftrAzure.AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                var uriBuilder = new UriBuilder(_liftrAzure.AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path =
                    $"/subscriptions/{_liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.VirtualMachineImages/imageTemplates/{templateName}";
                uriBuilder.Query = $"api-version={c_AIBAPIVersion}";
                var runOutputResponse = await httpClient.GetAsync(uriBuilder.Uri);

                if (runOutputResponse.StatusCode == HttpStatusCode.OK)
                {
                    return await runOutputResponse.Content.ReadAsStringAsync();
                }
                else if (runOutputResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    var errMsg = $"Failed at getting AIB template run output. statusCode: '{runOutputResponse.StatusCode}'";
                    if (runOutputResponse?.Content != null)
                    {
                        errMsg = errMsg + $", response: {await runOutputResponse.Content?.ReadAsStringAsync()}";
                    }

                    var ex = new RunAzureVMImageBuilderException(errMsg, _liftrAzure.FluentClient.SubscriptionId, rgName, templateName);
                    operation.FailOperation(ex.Message);
                    _logger.Error(ex.Message);
                    throw ex;
                }
            }
        }

        public Task<string> GetAIBRunOutputAsync(
            string rgName,
            string templateName,
            string runOutputName,
            CancellationToken cancellationToken = default)
        {
            var resourceId = $"/subscriptions/{_liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.VirtualMachineImages/imageTemplates/{templateName}/runOutputs/{runOutputName}";
            return _liftrAzure.GetResourceAsync(resourceId, c_AIBAPIVersion);
        }

        public async Task DeleteVMImageBuilderTemplateAsync(
             string rgName,
             string templateName,
             CancellationToken cancellationToken = default)
        {
            // https://github.com/Azure/azure-rest-api-specs-pr/blob/87dbc20106afce8c615113d654c14359a3356486/specification/imagebuilder/resource-manager/Microsoft.VirtualMachineImages/preview/2019-05-01-preview/imagebuilder.json#L280
            using (var handler = new AzureApiAuthHandler(_liftrAzure.AzureCredentials))
            using (var httpClient = new HttpClient(handler))
            {
                _logger.Information("Delete AIB template. rgName: {rgName}. templateName: {templateName}", rgName, templateName);
                var uriBuilder = new UriBuilder(_liftrAzure.AzureCredentials.Environment.ResourceManagerEndpoint);
                uriBuilder.Path =
                    $"/subscriptions/{_liftrAzure.FluentClient.SubscriptionId}/resourceGroups/{rgName}/providers/Microsoft.VirtualMachineImages/imageTemplates/{templateName}";
                uriBuilder.Query = $"api-version={c_AIBAPIVersion}";
                var deleteResponse = await httpClient.DeleteAsync(uriBuilder.Uri, cancellationToken);

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    _logger.Error("Delete AIB template {templateName} in {rgName} failed. Status code: {deleteResponseStatusCode}", templateName, rgName, deleteResponse.StatusCode);
                    if (deleteResponse?.Content != null)
                    {
                        var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                        _logger.Error("Response body: {errorContent}", errorContent);
                    }

                    throw new InvalidOperationException("Delete template failed.");
                }
                else if (deleteResponse.StatusCode == HttpStatusCode.Accepted)
                {
                    var asyncOperationResponse = await WaitAsyncOperationAsync(httpClient, deleteResponse, cancellationToken);
                }

                _logger.Information("Delete AIB template succeeded. rgName: {rgName}. templateName: {templateName}", rgName, templateName);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        internal static bool TryExtractVHDSASFromRunOutput(string runOutput, out string vhdSAS)
        {
            vhdSAS = null;
            try
            {
                dynamic obj = JObject.Parse(runOutput);
                vhdSAS = obj.properties.artifactUri;

                return true;
            }
            catch
            {
            }

            return false;
        }

        private async Task<string> WaitAsyncOperationAsync(
            HttpClient client,
            HttpResponseMessage startOperationResponse,
            CancellationToken cancellationToken)
        {
            int maxRetry = 5;
            string statusUrl = string.Empty;

            if (startOperationResponse.Headers.Contains("Location"))
            {
                statusUrl = startOperationResponse.Headers.GetValues("Location").FirstOrDefault();
            }

            while (true)
            {
                var statusResponse = await client.GetAsync(new Uri(statusUrl), cancellationToken);
                if (statusResponse.IsSuccessStatusCode)
                {
                    var body = await statusResponse.Content.ReadAsStringAsync();
                    if (body.OrdinalContains("Running") || body.OrdinalContains("InProgress"))
                    {
                        _logger.Debug("Waiting for ARM Async Operation. statusUrl: {statusUrl}", statusUrl);
                        await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                    }
                    else
                    {
                        return body;
                    }
                }
                else
                {
                    _logger.Warning("Check async operation failed. statusUrl: {statusUrl}, StatusCode: {StatusCode}", statusUrl, statusResponse.StatusCode);
                    var body = await statusResponse.Content.ReadAsStringAsync();
                    _logger.Warning("ErrorBody: {ErrorBody}", body);

                    if (maxRetry <= 0)
                    {
                        return body;
                    }

                    maxRetry--;
                    await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
                }
            }
        }
    }
}