//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Liftr.Fluent.Contracts;
using Microsoft.Liftr.Fluent.Contracts.Geneva;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class InfraV1Options
    {
        private const int c_minParName = 5;
        private const int c_minShortParName = 3;

        public InfraV1Options()
        {
        }

        public string PartnerName { get; set; }

        public string ShortPartnerName { get; set; }

        public EnvironmentType Environment { get; set; }

        public string LocationStr { get; set; }

        [JsonIgnore]
        public Region Location => Region.Create(LocationStr);

        public string DataCoreName { get; set; }

        public string ComputeCoreName { get; set; }

        public string CosmosSecreteName { get; set; }

        [JsonIgnore]
        public PricingTier WebAppTier => new PricingTier(WebAppTierStr, WebAppSizeStr);

        public string WebAppTierStr { get; set; }

        public string WebAppSizeStr { get; set; }

        public string AspNetEnv { get; set; }

        public CertificateOptions ClientCert { get; set; }

        public GenevaOptions MDSOptions { get; set; }

        public void CheckValid()
        {
            if (PartnerName?.Length < c_minParName)
            {
                throw new ArgumentOutOfRangeException($"{nameof(PartnerName)} should not be shorter than {c_minParName} characters: {PartnerName}");
            }

            if (ShortPartnerName?.Length < c_minShortParName)
            {
                throw new ArgumentOutOfRangeException($"{nameof(ShortPartnerName)} should not be shorter than {c_minShortParName} characters: {ShortPartnerName}");
            }

            if (string.IsNullOrEmpty(DataCoreName))
            {
                throw new InvalidOperationException(nameof(DataCoreName));
            }

            if (string.IsNullOrEmpty(ComputeCoreName))
            {
                throw new InvalidOperationException(nameof(ComputeCoreName));
            }

            if (string.IsNullOrEmpty(CosmosSecreteName))
            {
                throw new InvalidOperationException(nameof(CosmosSecreteName));
            }

            if (ClientCert == null)
            {
                throw new InvalidOperationException(nameof(ClientCert));
            }

            if (MDSOptions == null)
            {
                throw new InvalidOperationException(nameof(MDSOptions));
            }

            Region.Create(LocationStr);
            var _ = new PricingTier(WebAppTierStr, WebAppSizeStr);
        }

        public static InfraV1Options FromFile(string path)
        {
            var content = File.ReadAllText(path);
            var result = content.FromJson<InfraV1Options>();
            result.CheckValid();
            return result;
        }
    }
}
