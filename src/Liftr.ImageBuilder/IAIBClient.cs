//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Liftr.ImageBuilder
{
    public interface IAIBClient
    {
        Task<string> CreateNewSBIVersionByRunAzureVMImageBuilderAsync(
            Region location,
            string rgName,
            string templateName,
            string templateContent,
            CancellationToken cancellationToken = default);

        Task<string> GetGeneratedVDHSASAsync(
           string rgName,
           string templateName,
           CancellationToken cancellationToken = default);

        Task<string> GetAIBRunOutPutAsync(
                string rgName,
                string templateName,
                string runOutputName,
                CancellationToken cancellationToken = default);

        Task<HttpResponseMessage> DeleteVMImageBuilderTemplateAsync(
                string rgName,
                string templateName,
                CancellationToken cancellationToken = default);
    }
}
