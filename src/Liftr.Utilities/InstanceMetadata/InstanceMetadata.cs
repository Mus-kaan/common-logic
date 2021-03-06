//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Liftr.Utilities
{
    public class InstanceMetadata
    {
        // https://docs.microsoft.com/en-us/azure/virtual-machines/windows/instance-metadata-service#instance-api
        private const string c_imdsUri = "http://169.254.169.254/metadata/instance?api-version=2019-04-30";

        internal InstanceMetadata()
        {
        }

        [JsonProperty]
        public ComputeMetadata Compute { get; private set; }

        [JsonProperty]
        public NetworkMetadata Network { get; private set; }

        [JsonProperty]
        public string MachineNameEnv { get; private set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "<Pending>")]
        internal static async Task<InstanceMetadata> LoadAsync()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    using var imdsRequest = new HttpRequestMessage(HttpMethod.Get, c_imdsUri);
                    imdsRequest.Headers.Add("Metadata", "true");
                    var response = await httpClient.SendAsync(imdsRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        var metaResponse = await response.Content.ReadAsStringAsync();
                        var instanceMetadata = JsonConvert.DeserializeObject<InstanceMetadata>(metaResponse);
                        instanceMetadata.MachineNameEnv = Environment.MachineName;
                        return instanceMetadata;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        var errorResponse = await response.Content.ReadAsStringAsync();
                        Console.WriteLine("IMDS return 400 with error message: " + errorResponse);
                    }

                    return null;
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    public class ComputeMetadata
    {
        [JsonProperty]
        public string AzEnvironment { get; private set; }

        [JsonProperty]
        public string CustomData { get; private set; }

        [JsonProperty]
        public string Location { get; private set; }

        [JsonProperty]
        public string Name { get; private set; }

        [JsonProperty]
        public string Offer { get; private set; }

        [JsonProperty]
        public string OsType { get; private set; }

        [JsonProperty]
        public string PlacementGroupId { get; private set; }

        [JsonProperty]
        public PlanMetadata Plan { get; private set; }

        [JsonProperty]
        public int PlatformFaultDomain { get; private set; }

        [JsonProperty]
        public int PlatformUpdateDomain { get; private set; }

        [JsonProperty]
        public string Provider { get; private set; }

        [JsonProperty]
        public string Publisher { get; private set; }

        [JsonProperty]
        public string ResourceGroupName { get; private set; }

        [JsonProperty]
        public string ResourceId { get; private set; }

        [JsonProperty]
        public string Sku { get; private set; }

        [JsonProperty]
        public string SubscriptionId { get; private set; }

        [JsonProperty]
        public string Tags { get; private set; }

        [JsonProperty]
        public string Version { get; private set; }

        [JsonProperty]
        public string VmId { get; private set; }

        [JsonProperty]
        public string VmScaleSetName { get; private set; }

        [JsonProperty]
        public string VmSize { get; private set; }

        [JsonProperty]
        public string Zone { get; private set; }
    }

    public class PlanMetadata
    {
        [JsonProperty]
        public string Name { get; private set; }

        [JsonProperty]
        public string Product { get; private set; }

        [JsonProperty]
        public string Publisher { get; private set; }
    }

    public class NetworkMetadata
    {
        [JsonProperty]
        public InterfaceMetadata[] Interface { get; private set; }
    }

    public class InterfaceMetadata
    {
        [JsonProperty]
        public Ipv4Metadata IPv4 { get; private set; }

        [JsonProperty]
        public Ipv6Metadata IPv6 { get; private set; }

        [JsonProperty]
        public string MacAddress { get; private set; }
    }

    public class Ipv4Metadata
    {
        [JsonProperty]
        public IpAddressMetadata[] IpAddress { get; private set; }

        [JsonProperty]
        public SubnetMetadata[] Subnet { get; private set; }
    }

    public class Ipv6Metadata
    {
        [JsonProperty]
        public IpAddressMetadata[] IpAddress { get; private set; }

        [JsonProperty]
        public SubnetMetadata[] Subnet { get; private set; }
    }

    public class IpAddressMetadata
    {
        [JsonProperty]
        public string PublicIpAddress { get; private set; }

        [JsonProperty]
        public string PrivateIpAddress { get; private set; }
    }

    public class SubnetMetadata
    {
        [JsonProperty]
        public string Address { get; private set; }

        [JsonProperty]
        public int Prefix { get; private set; }
    }
}
