//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Liftr.Contracts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SourceImageType
    {
        WindowsServer2016Datacenter,
        WindowsServer2016DatacenterCore,
        WindowsServer2016DatacenterContainers,
        WindowsServer2019Datacenter,
        WindowsServer2019DatacenterCore,
        WindowsServer2019DatacenterContainers,

        UbuntuServer1804,
        RedHat7LVM,
        CentOS,

        /// <summary>
        /// Azure Linux Secure Base Image Ubuntu 1604
        /// </summary>
        U1604LTS,

        /// <summary>
        /// Azure Linux Secure Base Image Ubuntu 1804
        /// </summary>
        U1804LTS,
    }

    public static class SourceImageTypeExtensions
    {
        public static bool IsPlatformImage(this SourceImageType type)
        {
            if (type == SourceImageType.U1604LTS ||
                type == SourceImageType.U1804LTS)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool IsWindows(this SourceImageType type)
        => type <= SourceImageType.WindowsServer2019DatacenterContainers;
    }
}
