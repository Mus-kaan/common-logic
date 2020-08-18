//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;

namespace Microsoft.Liftr.Contracts.Marketplace
{
    // https://msazure.visualstudio.com/One/_git/AAPT-SPZA?path=%2Fsrc%2Fsource%2FMicrosoft.MarketPlace.Common.Models%2FAadEntity.cs&_a=contents&version=GBmaster
    public class SaasBeneficiary
    {
        [JsonProperty("emailId")]
        public string EmailId { get; set; }

        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("puid")]
        public string Puid { get; set; }
    }
}
