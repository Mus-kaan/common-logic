//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.Liftr.Contracts
{
    public enum RootScopeLevel
    {
        Tenant,             // '/'
        Subscription,       // '/subscriptions/{subscriptionId}/'
        ResourceGroup,      // '/subscriptions/{subscriptionId}/resourceGroups/{groupName}/'
        Extension,          // '{parentScope}/providers/{extensionNamespace}/{extensionType}/{extensionName}/'
        ManagementGroup,    // '/providers/Microsoft.Management/managementGroups/{managementGroupName}/'
    }
}
