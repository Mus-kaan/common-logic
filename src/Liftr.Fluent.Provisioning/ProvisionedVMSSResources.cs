//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Azure.Management.Msi.Fluent;
using Microsoft.Azure.Management.Network.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace Microsoft.Liftr.Fluent.Provisioning
{
    public class ProvisionedVMSSResources
    {
        public IResourceGroup ResourceGroup { get; set; }

        public INetwork VNet { get; set; }

        public ISubnet Subnet { get; set; }

        public IIdentity ManagedIdentity { get; set; }

        public IVault RegionalKeyVault { get; set; }

        public IVault GlobalKeyVault { get; set; }

        public IVirtualMachineScaleSet VMSS { get; set; }

        public ILoadBalancer LoadBalancer { get; set; }

        public IPublicIPAddress ClusterIP { get; set; }
    }
}
